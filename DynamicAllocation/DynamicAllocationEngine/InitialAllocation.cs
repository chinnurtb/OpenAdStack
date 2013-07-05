// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitialAllocation.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicAllocation
{
    /// <summary>
    /// Class for initial allocation calculations
    /// </summary>
    public class InitialAllocation : Allocation
    {
        /// <summary>
        /// Initializes a new instance of the InitialAllocation class
        /// </summary>
        /// <param name="measureInfo">The measure info</param>
        public InitialAllocation(MeasureInfo measureInfo)
            : base(measureInfo)
        {
        }

        /// <summary>
        /// Performs a greedy maximum set coverage from sets in the tier to elements in measureSetCoverage
        /// </summary>
        /// <param name="tier">the measureSets we have to use to cover the measureSetCoverage elements</param>
        /// <param name="measureSetsToCover">the measureSet elements we are trying to cover with the tier measureSets</param>
        /// <param name="numberOfNodesToAllocateOnThisTier">the number of sets we can choose from tier to cover measureSetCoverage</param>
        /// <returns>A subset of tier of size numberOfNodesToAllocateOnThisTier that tries to cover the measureSets in measureSetCoverage</returns>
        internal static IEnumerable<MeasureSet> GreedyMaxCover(
            ref List<MeasureSet> tier,
            ref Dictionary<int, List<MeasureSet>> measureSetsToCover,
            int numberOfNodesToAllocateOnThisTier)
        {
            var cover = new List<MeasureSet>();

            if (numberOfNodesToAllocateOnThisTier > tier.Count) 
            {
                numberOfNodesToAllocateOnThisTier = tier.Count;
            }

            var index = 0;
            var keys = measureSetsToCover.Keys.OrderBy(key => key).ToList();
            for (var i = 0; i < numberOfNodesToAllocateOnThisTier; i++)
            {
                if (keys.Count > index + 1 && measureSetsToCover[keys[index]].Count == 0)
                {
                    index++;

                    // remove already covered sets from the new tier under consideration
                    measureSetsToCover[keys[index]] = measureSetsToCover[keys[index]]
                        .Where(ms => !cover.Any(coverMs => ms.IsSubsetOf(coverMs)))
                        .ToList();
                }

                AddBestGreedyMeasureSet(ref cover, ref tier, ref measureSetsToCover, keys.Count > index ? keys[index] : 0);
            }

            return cover;
        }

        /// <summary>
        /// Calculates the number of nodes to allocate on tier number 'tierNumber'
        /// we want the top tier to get x nodes, the next lower tier to get 2x, etc., and the total to be AllocationNumberOfNodes
        /// </summary>
        /// <param name="tierNumber">the number of measures in the tier</param>
        /// <param name="allocationTopTier">the top tier we are going to allocate to</param>
        /// <param name="allocationNumberofTiersToAllocateTo">the total number of tiers we are going to allocate to</param>
        /// <param name="allocationNumberOfNodes">the number of nodes to allocate</param>
        /// <returns>the number of nodes to allocate to on this tier</returns> 
        internal static int NumberOfNodesToAllocateOnThisTier(
            int tierNumber,
            int allocationTopTier,
            int allocationNumberofTiersToAllocateTo,
            int allocationNumberOfNodes)
        {
            // TODO: verify outputs in correct range (|allocationTopTier - tierNumber| < allocationNumberofTiersToAllocateTo)
            var numberOfPortionsForAllTiers = Math.Pow(2, allocationNumberofTiersToAllocateTo) - 1;
            var numberOfPortionsOnThisTier = Math.Pow(2, allocationTopTier - tierNumber);

            // TODO: make sure this sums up to AllocationNumberOfNodes when considering all tiers 
            return (int)Math.Round((allocationNumberOfNodes * numberOfPortionsOnThisTier) / (double)numberOfPortionsForAllTiers);
        }

        /// <summary>
        /// Allocates budget among the graph nodes when there is no history (ie the first day of a campaign flight)
        /// </summary>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <returns>BudgetAllocation. budgets etc. </returns>
        internal BudgetAllocation AllocateBudget(BudgetAllocation budgetAllocation)
        {
            // the budget for this period 
            budgetAllocation.PeriodBudget = Allocation.CalculatePeriodBudget(
                budgetAllocation.RemainingBudget,
                budgetAllocation.CampaignEnd.Subtract(budgetAllocation.PeriodStart),
                budgetAllocation.PeriodDuration);

            // On this first day, we want to add a buffer across the board, even if this is the only day of the campaign 
            // (we currently rely on line item budgets for stopping things properly on the last day.)
            // TODO: should this continue to get the * 2?
            budgetAllocation.PeriodBudget *= budgetAllocation.AllocationParameters.BudgetBuffer * 2;

            var budgets = this.BudgetMeasureSets(budgetAllocation, budgetAllocation.PeriodBudget);

            this.PerNodeResultsFromBudgets(ref budgetAllocation, budgets);

            // on intial allocation, the export budget should be the same as the media budget
            foreach (var perNodeResult in budgetAllocation.PerNodeResults.Where(pnr => pnr.Value.PeriodTotalBudget > 0))
            {
                perNodeResult.Value.ExportBudget = perNodeResult.Value.PeriodMediaBudget;
            }

            budgetAllocation.AnticipatedSpendForDay =
                budgetAllocation.PerNodeResults.Sum(pnr => pnr.Value.PeriodTotalBudget);

            return budgetAllocation;
        }

        /// <summary>
        /// allocates a total budget to each measure set when there is no history data
        /// </summary>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <param name="periodCampaignBudget">the period campaign budget</param>
        /// <returns>the total budgets for the measure sets</returns>
        internal Dictionary<MeasureSet, decimal> BudgetMeasureSets(BudgetAllocation budgetAllocation, decimal periodCampaignBudget)
        {
            // filter perNodeResults for data costs that are too high
            var perNodeResults = budgetAllocation
                .PerNodeResults
                .Where(pnr => this.ValuationJustifiesDataCost(pnr, budgetAllocation))
                .ToDictionary();

            // get a list of all the persona nodes and include these in the budgeted list
            var personaLevel = perNodeResults.Max(pnr => pnr.Key.Count);
            var personaNodes = perNodeResults.Select(pnr => pnr.Key).Where(ms => ms.Count == personaLevel).ToList();
            var nodesToAllocate = new List<MeasureSet>(personaNodes);

            // reduce the number of nodes we want in the max cover so that the total is correct including the persona nodes
            var numberOfNodesInCover = budgetAllocation.AllocationParameters.AllocationNumberOfNodes - personaNodes.Count;

            // calculate the AllocationTopTier and AllocationNumberOfTiersToAllocateTo such that we handle edge cases on small graphs
            // TODO: make sure this covers all cases
            var graphTopTier = perNodeResults.Keys.Max(ms => ms.Count);
            var graphBottomTier = perNodeResults.Keys.Min(ms => ms.Count);
            var allocationTopTier = Math.Min(graphTopTier, budgetAllocation.AllocationParameters.AllocationTopTier);
            var allocationNumberofTiersToAllocateTo =
                Math.Min(
                    allocationTopTier - graphBottomTier + 1,
                    budgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo);

            // calculate the number of nodes to put in each tier we are allocating to
            // TODO: if a tier has less nodes than we want to allocate, currently we just don't export
            // as many nodes as we might like - may want to bump up the number of nodes on other tiers.
            // (in that case we are exporting a whole tier which will also cause higher tiers not to be spread out.
            // this may be worth having special case handling.)
            var numberOfNodesOnTier = new Dictionary<int, int>();
            for (var tierNumber = allocationTopTier - allocationNumberofTiersToAllocateTo + 1; tierNumber <= allocationTopTier; tierNumber++)
            {
                numberOfNodesOnTier[tierNumber] = NumberOfNodesToAllocateOnThisTier(
                    tierNumber,
                    allocationTopTier,
                    allocationNumberofTiersToAllocateTo,
                    numberOfNodesInCover);
            }

            // create a coverage dictionary to keep track of who is covered so far in the tiers we are allocating to and below
            // TODO: this ensures that we keep adding nodes until we reach our node limit - may want to stop earlier when the lower graph is covered
            var measureSetsToCover = perNodeResults
                .Keys
                .Where(ms => ms.Count <= allocationTopTier && !nodesToAllocate.Contains(ms))
                .GroupBy(ms => ms.Count)
                .ToDictionary(grp => grp.Key, grp => grp.ToList());

            // foreach tier from allocationTopTier - allocationNumberofTiersToAllocateTo + 1 to the allocationTopTier
            var measureSetsByNumberOfMeasures = perNodeResults.Keys.GroupBy(ms => ms.Count).ToDictionary(grp => grp.Key, grp => grp.ToList());
            for (var tierNumber = allocationTopTier - allocationNumberofTiersToAllocateTo + 1; tierNumber <= allocationTopTier; tierNumber++)
            {
                var tier = measureSetsByNumberOfMeasures[tierNumber];

                // do a greedy max cover on the coverage graph with the tier with number of nodes given by the helper function
                var nodesToAllocateOnThisTier = GreedyMaxCover(ref tier, ref measureSetsToCover, numberOfNodesOnTier[tierNumber]);
                nodesToAllocate.AddRange(nodesToAllocateOnThisTier);

                // TODO: this quick fix makes it so we don't add the persona nodes twice
                // switch to a more performant fix 
                nodesToAllocate = nodesToAllocate.Distinct().ToList();
            }

            var budgetPerNode = periodCampaignBudget / nodesToAllocate.Count;
            var budgets = nodesToAllocate.ToDictionary(ms => ms, ms => budgetPerNode);
            return budgets;
        }
    }
}
