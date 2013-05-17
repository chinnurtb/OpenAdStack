// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Valuation.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAllocation
{
    /// <summary>
    /// Class for valuation calculations
    /// </summary>
    public static class Valuation
    {
        /// <summary>
        /// Calculates the set of MeasureSets and their valuations from a CampaignDefinition
        /// </summary>
        /// <param name="campaign">campaign definition</param>
        /// <returns>Dictionary mapping MeasureSets to their valuations </returns>
        public static IDictionary<MeasureSet, decimal> GetValuations(CampaignDefinition campaign)
        {
            // a couple of input checks. we may want more of this kind of thing here or we may want to rely on someone upstream doing it.
            // TODO: decide about an input validation strategy
            if (campaign.MaxPersonaValuation == 0m)
            {
                throw new DynamicAllocationException("Each campaign's max persona valuation must be > 0; This campaign's had " + campaign.MaxPersonaValuation + ".");
            }

            if (campaign.ExplicitValuations == null || campaign.ExplicitValuations.Count(ev => ev.Key.Count == 1) < 2)
            {
                throw new DynamicAllocationException("Each campaign must have at least two explicit valuations.");
            }

            // all explicit valuations with a single measure are measureValuations. this is needed for creating the graph.
            var measureValuations = ExtractMeasureValuations(campaign.ExplicitValuations);

            // all other explicit valuations are overrides. (there shoud not be a valaution on the null measure set, however, it would be harmlessly ignored if there were one)
            var overrides = ExtractOverrides(campaign.ExplicitValuations);

            // create a grouping dictionary based on the campaign.MeasureGroupings. allows you to key on a grouping string and get a collection of all measures in that group
            // includes all measures not in campaign.MeasureGroupings in their own group (because they are ANDed with everyone)
            var groupingDict = CollectMeasuresByGrouping(measureValuations.Keys, campaign.MeasureGroupings);

            // this will be the MeasureSet of the persona measures (ie 'the persona' or 'mr. perfect')
            // it will consist of the measure from each measure group with the highest valuation 
            // (choosing the first one listed in the case of ties)
            var personaMeasureSet = CalculatePersonaMeasureSet(groupingDict, measureValuations);

            // calculates the 'joiningBonus' such that campaign.MaxPersonaValuation = maxMeasureValuation + joiningBonus * (sumOfMaxMeasureValuations - maxMeasureValuation)
            // this is the 'joiningBonus' if there were no overrides.
            var personaValuationsSum = personaMeasureSet.Sum(m => measureValuations[m]);
            var maxMeasureValuation = measureValuations.Select(item => item.Value).Max();
            var joiningBonusWithoutOverrides = (personaValuationsSum == maxMeasureValuation) ? 1 : (campaign.MaxPersonaValuation - maxMeasureValuation) / (personaValuationsSum - maxMeasureValuation);

            // this is a collection of all measure sets that appear in the bid graph
            var measureSets = MeasureSet.SetProduct(groupingDict.Values, true).ToList();
            measureSets.Remove(new MeasureSet());
            measureSets = FilterForPinnedMeasures(measureSets, campaign.PinnedMeasures, campaign.MeasureGroupings);

            // group the measureSets into tiers by the numbers of measures they have.
            // and loop over all tiers, starting from the 1 measure tier, up to the persona tier. 
            var valuations = new ConcurrentDictionary<MeasureSet, decimal>();
            var measureSetsByNumberOfMeasures = measureSets.GroupBy(ms => ms.Count).OrderBy(grouping => grouping.Key);
            foreach (var measureSetLevel in measureSetsByNumberOfMeasures)
            {
                // valuations are effectively the return values of this method 
                ValuateTier(
                    valuations,
                    campaign,
                    measureValuations,
                    overrides,
                    measureSetLevel,
                    personaMeasureSet,
                    joiningBonusWithoutOverrides);
            }

            // filter out disallowed measureSets due to pinning before returning.
            return valuations;
        }

        /// <summary>
        /// extracts the single measure valuations from the the explicit valuations input
        /// </summary>
        /// <param name="explicitValuations">the explicit valuations input</param>
        /// <returns>the single measure valuations</returns>
        internal static IDictionary<long, decimal> ExtractMeasureValuations(IDictionary<MeasureSet, decimal> explicitValuations)
        {
            return explicitValuations
                .Where(ev => ev.Key.Count == 1)
                .ToDictionary(kvp => kvp.Key.Single(), kvp => kvp.Value);
        }
        
        /// <summary>
        /// extracts the single measure valuations from the the explicit valuations input
        /// </summary>
        /// <param name="explicitValuations">the explicit valuations input</param>
        /// <returns>the single measure valuations</returns>
        internal static IDictionary<MeasureSet, decimal> ExtractOverrides(IDictionary<MeasureSet, decimal> explicitValuations)
        {
            return explicitValuations
                .Where(ev => ev.Key.Count > 1)
                .Select(ev => new KeyValuePair<MeasureSet, decimal>(ev.Key, ev.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// creates a kind of inversion of the measureGroupings dictionary, 
        /// so that you can key on the grouping string and get a MeasureSet of all the measures that have that grouping string.
        /// measures not included in the measureGroupings get their own grouping (ie they are 'AND'ed with the others in the bid graph)
        /// </summary>
        /// <param name="measures">IEnumerable of all the measures for the campaign</param>
        /// <param name="measureGroupings">the input dictionary that lists each tagetingAttribute with its grouping string</param>
        /// <returns>returns a dictionary mapping grouping strings to MeasureSets</returns>
        internal static IDictionary<string, MeasureSet> CollectMeasuresByGrouping(
            IEnumerable<long> measures,
            IDictionary<long, string> measureGroupings)
        {
            return measures
                .GroupBy(ms => (measureGroupings != null && measureGroupings.ContainsKey(ms)) ? measureGroupings[ms] : ms.ToString(CultureInfo.InvariantCulture))
                .ToDictionary(grp => grp.Key, grp => new MeasureSet(grp));
        }

        /// <summary>
        /// calcualtes the persona measure set
        /// </summary>
        /// <param name="groupingDictionary">the grouping dictionary (contains the 'OR' gorupings)</param>
        /// <param name="measureValuations">the measure valuations</param>
        /// <returns>the persona measure set</returns>
        internal static MeasureSet CalculatePersonaMeasureSet(
            IDictionary<string, MeasureSet> groupingDictionary,
            IDictionary<long, decimal> measureValuations)
        {
            return new MeasureSet(groupingDictionary.Select(g =>
            {
                // Get the maximum valuation in this measureSet
                decimal maxValuation = g.Value.Select(m2 => measureValuations[m2]).Max();

                // Select the corresponding measure
                return g.Value.First(m => measureValuations[m] == maxValuation);
            }));
        }

        /// <summary>
        /// allocates budget to the nodes in a tier
        /// </summary>
        /// <param name="valuations">the valuations</param>
        /// <param name="campaign">the campaign</param>
        /// <param name="measureValuations">the measureValuations</param>
        /// <param name="overrides">the overrides</param>
        /// <param name="measureSetLevel">the set of measures on this level</param>
        /// <param name="personaMeasureSet">the personaMeasureSet</param>
        /// <param name="joiningBonusWithoutOverrides">the default joining bonus</param>
        internal static void ValuateTier(
            IDictionary<MeasureSet, decimal> valuations,
            CampaignDefinition campaign,
            IDictionary<long, decimal> measureValuations,
            IDictionary<MeasureSet, decimal> overrides,
            IGrouping<int, MeasureSet> measureSetLevel,
            MeasureSet personaMeasureSet,
            decimal joiningBonusWithoutOverrides)
        {
            Parallel.ForEach(
                measureSetLevel,
                measureSet =>
                {
                    // find closest override
                    var closestOverrideOrMaxMeasure = FindClosestOverrideOrMaxMeasure(measureSet, overrides, measureValuations);
                    decimal joiningBonus = CalculateJoiningBonus(
                            measureSet,
                            closestOverrideOrMaxMeasure,
                            personaMeasureSet,
                            measureValuations,
                            campaign,
                            joiningBonusWithoutOverrides);

                    // Calculate Valuation
                    valuations[measureSet] = CalculateValuation(
                        measureSet,
                        closestOverrideOrMaxMeasure,
                        measureValuations,
                        joiningBonus);
                });
        }

        /// <summary>
        /// filters out all non-pinned nodes before returning the valuations
        /// </summary>
        /// <param name="valuations">the raw valuations that include both pinned and non-pinned nodes</param>
        /// <param name="pinnedMeasures">the collection of pinned measures</param>
        /// <param name="measureGroupings">the input measure groupings that contains the 'OR' set grouping information for measures</param>
        /// <returns>the filtered list of valuations containing only pinned nodes</returns>
        internal static List<MeasureSet> FilterForPinnedMeasures(
            List<MeasureSet> valuations,
            ICollection<long> pinnedMeasures,
            IDictionary<long, string> measureGroupings)
        {
            if (pinnedMeasures == null || pinnedMeasures.Count == 0)
            {
                return valuations;
            }

            // create a groupingDict that contains only the pinned measures
            var groupingDict = CollectMeasuresByGrouping(pinnedMeasures, measureGroupings);

            // filter valuations to allow only those measureSets such that every pinned group contains one of the measureSet's measures
            return valuations.Where(v => groupingDict.All(group => group.Value.Any(ms => v.Contains(ms)))).ToList();
        }

        /// <summary>
        /// finds the most relevant override to measureSet
        /// relevant here means the override with the most number of measures that is a subset of measureSet
        /// this method is used by the public method LargestMeasureOrOverride
        /// </summary>
        /// <param name="measureSet">the target measureSet</param>
        /// <param name="overrides">the set of overrides for the campaign</param>
        /// <param name="measureValuations">the set of measureValuations for the campaign</param>
        /// <returns>the single measure valuations that we default to if there are no relevant overrides</returns>
        internal static KeyValuePair<MeasureSet, decimal> FindClosestOverrideOrMaxMeasure(
            MeasureSet measureSet,
            IDictionary<MeasureSet, decimal> overrides,
            IDictionary<long, decimal> measureValuations)
        {
            // return the relevant override with the largest number of measures (breaking ties by valuation)
            var relevantOverrides = overrides.Where(or => or.Key.IsSubsetOf(measureSet)).ToList();
            IEnumerable<KeyValuePair<MeasureSet, decimal>> closestOverrides = relevantOverrides.Where(or => or.Key.Count == relevantOverrides.Max(or2 => or2.Key.Count)).ToList();
            if (closestOverrides.Any())
            {
                return closestOverrides.OrderBy(or => or.Value).First();
            }

            var maxMeasure = measureSet.First(ms => measureValuations[ms] == measureSet.Max(ms2 => measureValuations[ms2]));
            return new KeyValuePair<MeasureSet, decimal>(new MeasureSet { maxMeasure }, measureValuations[maxMeasure]);
        }

        /// <summary>
        /// calculates the joining bonus for the measureSet
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="closestOverrideOrMaxMeasure">the maxMeasureOrOverride</param>
        /// <param name="personaMeasureSet">the personaMeasureSet</param>
        /// <param name="measureValuations">the measureValuations</param>
        /// <param name="campaign">the campaign</param>
        /// <param name="joiningBonusDefault">the joiningBonusDefault</param>
        /// <returns>the joining bonus</returns>
        internal static decimal CalculateJoiningBonus(
            MeasureSet measureSet,
            KeyValuePair<MeasureSet, decimal> closestOverrideOrMaxMeasure,
            MeasureSet personaMeasureSet,
            IDictionary<long, decimal> measureValuations,
            CampaignDefinition campaign,
            decimal joiningBonusDefault)
        {
            // if this measureSet does not have an override
            if (closestOverrideOrMaxMeasure.Key.Count == 1)
            {
                return joiningBonusDefault;
            }

            // there are two cases for overrides: 
            // 1) this measureSet is a subset of the persona measureSet (this will always be true if there are no 'OR's)
            // 2) this measureSet is not a subset of the persona measureSet
            //
            // for case (1) we set the joining bonus value so that the persona node gets the MaxPersonaValuation from the CampaignDefinition.
            // for case (2) we set the joining bonus so that the persona level node that is a superset of this measureSet gets a
            // valuation at max equal to the MaxPersonaValuation.
            if (measureSet.IsSubsetOf(personaMeasureSet))
            {
                // TODO: this method no longer considers the multiple relevant overrides case - will want to reinclude that
                var sum = SumWithoutLargestMeasureOrOverride(personaMeasureSet, closestOverrideOrMaxMeasure, measureValuations);

                // sum == 0 is a degenerate case where measures have zero value and for which the joining bonus value no longer matters. sum > 0 is the usual case.
                return sum > 0 ?
                    (campaign.MaxPersonaValuation - closestOverrideOrMaxMeasure.Value) / sum :
                    joiningBonusDefault;
            }

            // else:
            // calculate what this override will do to the relevant personalevel node and change joining bonus to max at personabid
            var personaLevelMeasureSet = new MeasureSet();
            personaLevelMeasureSet.UnionWith(measureSet);

            // find all measures in personaMeasureSet whose attributes are not represented in measureSet
            foreach (var measure in personaMeasureSet)
            {
                // TODO: engineer a test that makes this blow
                if (!campaign.MeasureGroupings.ContainsKey(measure))
                {
                    personaLevelMeasureSet.Add(measure);
                }
                else
                {
                    // Assume we should add this measure unless we find a measure from the same group already exists.
                    var shouldAdd = true;
                    foreach (var levelMeasure in personaLevelMeasureSet)
                    {
                        if (!campaign.MeasureGroupings.ContainsKey(levelMeasure))
                        {
                            // This measure is not in a group, so irrelevant
                            continue;
                        }

                        if (campaign.MeasureGroupings[levelMeasure].Equals(campaign.MeasureGroupings[measure]))
                        {
                            // This is a measure from the same group!
                            shouldAdd = false;
                            break;
                        }

                        // Else: The measure is part of some other group, irrelevant
                    }

                    if (shouldAdd)
                    {
                        personaLevelMeasureSet.Add(measure);
                    }
                }
            }

            // calculate the persona level measure bid using the global 'joiningBonus'
            var testBid = closestOverrideOrMaxMeasure.Value
                          + (joiningBonusDefault * SumWithoutLargestMeasureOrOverride(personaLevelMeasureSet, closestOverrideOrMaxMeasure, measureValuations));

            // if this exceeds the persona.BidAmount, change joining bonus to limit the bid to the persona bid.
            if (testBid > campaign.MaxPersonaValuation)
            {
                var sum = SumWithoutLargestMeasureOrOverride(personaLevelMeasureSet, closestOverrideOrMaxMeasure, measureValuations);

                // sum == 0 is a degenerate case where measures have zero value and for which the tvalue no longer matters, sum > 0 is the usual case.
                return sum > 0 ?
                                   (campaign.MaxPersonaValuation - closestOverrideOrMaxMeasure.Value) / sum :
                                                                                                                joiningBonusDefault;
            }

            return joiningBonusDefault;
        }

        /// <summary>
        /// calcualtes the valuation (and joining bonus) of the input measureSet
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="closestOverrideOrMaxMeasure">the maxMeasureOrOverride</param>
        /// <param name="measureValuations">the measureValuations</param>
        /// <param name="joiningBonus">the joiningBonus</param>
        /// <returns>the valuation</returns>
        internal static decimal CalculateValuation(
            MeasureSet measureSet,
            KeyValuePair<MeasureSet, decimal> closestOverrideOrMaxMeasure,
            IDictionary<long, decimal> measureValuations,
            decimal joiningBonus)
        {
            return Math.Round(closestOverrideOrMaxMeasure.Value + (joiningBonus * SumWithoutLargestMeasureOrOverride(measureSet, closestOverrideOrMaxMeasure, measureValuations)), 2);
        }

        /// <summary>
        /// finds the sum of the values of all measures of measureSet that are not part of the LargestMeasureOrOverride.
        /// This is what gets scaled by the joining bonus
        /// </summary>
        /// <param name="measureSet">the target measureSet</param>
        /// <param name="maxMeasureOrOverride">the overrides for this measureSet</param>
        /// <param name="measureValuations">the single measure valuations that we default to if there are no relevant overrides</param>
        /// <returns>decimal value of the sum</returns>
        internal static decimal SumWithoutLargestMeasureOrOverride(
            MeasureSet measureSet,
            KeyValuePair<MeasureSet, decimal> maxMeasureOrOverride,
            IDictionary<long, decimal> measureValuations)
        {
            return measureSet.Except(maxMeasureOrOverride.Key).Sum(measure => measureValuations[measure]);
        }
    }
}
