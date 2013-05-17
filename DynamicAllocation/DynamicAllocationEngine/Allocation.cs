// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Allocation.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamicAllocation
{
    /// <summary>
    /// class for constants and methods needed in both IntialAllocation and Reallocation 
    /// </summary>
    public class Allocation
    {
        /// <summary>The measure info</summary>
        private readonly MeasureInfo measureInfo;

        /// <summary>
        /// Initializes a new instance of the Allocation class
        /// </summary>
        /// <param name="measureInfo">The measure info</param>
        public Allocation(MeasureInfo measureInfo)
        {
            this.measureInfo = measureInfo;
        }

        /// <summary>Gets the measure info</summary>
        protected MeasureInfo MeasureInfo
        {
            get { return this.measureInfo; }
        }

        // parameters that effect the budget allocation/reallocation logic

        /// <summary>
        /// Calculates the budget to spend during the coming day.
        /// </summary>
        /// <param name="remainingBudget">the amount of budget left after previous period's spending</param>
        /// <param name="remainingTime">the amoutn of time left in the campaign</param>
        /// <param name="periodDuration">the period length</param>
        /// <returns>a decimal budget for the day</returns>
        internal static decimal CalculatePeriodBudget(decimal remainingBudget, TimeSpan remainingTime, TimeSpan periodDuration)
        {
            return CalculatePeriodBudget(remainingBudget, remainingTime, periodDuration.Ticks);
        }

        /// <summary>
        /// Calculates the budget to spend during the coming period.
        /// </summary>
        /// <param name="remainingBudget">the amount of budget left after previous period's spending</param>
        /// <param name="remainingTime">the amoutn of time left in the campaign</param>
        /// <param name="periodDurationTicks">length of the coming period in ticks</param>
        /// <returns>a decimal budget for the day</returns>
        internal static decimal CalculatePeriodBudget(decimal remainingBudget, TimeSpan remainingTime, long periodDurationTicks)
        {
            if (remainingTime.Ticks > 0 && periodDurationTicks > 0)
            {
                // Should never be less than one since that would allocate more than remainingBudget
                var numberOfPeriods = Math.Max((decimal)remainingTime.Ticks / periodDurationTicks, 1);
                return remainingBudget / numberOfPeriods;
            }

            // No time no budget
            return 0m;
        }

        /// <summary>Determine if campaign history exists and contains a given measureSet</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="perNodeResults">The per node results.</param>
        /// <returns>True if the history exists and contains the measureSet</returns>
        internal static bool HistoryContainsMeasureSet(MeasureSet measureSet, Dictionary<MeasureSet, PerNodeBudgetAllocationResult> perNodeResults)
        {
            // TODO: is this the correct criteria for assessing if there is history?
            if (perNodeResults.ContainsKey(measureSet) &&
                !string.IsNullOrWhiteSpace(perNodeResults[measureSet].AllocationId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the measureSet from tier to cover that most reduces the size of measureSetsToCover
        /// </summary>
        /// <param name="cover">the measureSets in the cover so far</param>
        /// <param name="tier">the measureSets in this tier</param>
        /// <param name="measureSetsToCover">the measureSets we have left to cover</param>
        /// <param name="index">the index into the measureSetsToCover</param>
        internal static void AddBestGreedyMeasureSet(ref List<MeasureSet> cover, ref List<MeasureSet> tier, ref Dictionary<int, List<MeasureSet>> measureSetsToCover, int index)
        {
            // nothing to do
            if (tier.Count == 0)
            {
                return;
            }

            // calculate the coverage score for each measureSet in the tier in parallel
            var coverageScore = new ConcurrentDictionary<MeasureSet, int>();
            var measureSetsToCoverBag = measureSetsToCover.ContainsKey(index) ? 
                new ConcurrentBag<MeasureSet>(measureSetsToCover[index]) : 
                new ConcurrentBag<MeasureSet>();
            Parallel.ForEach(
                tier,
                measureSet =>
                {
                    var measureSetsLeftToCover = measureSetsToCoverBag.Where(ms => !ms.IsSubsetOf(measureSet));
                    coverageScore[measureSet] = measureSetsLeftToCover.Count();
                });

            // take the best one and update the measureSetsToCover
            var bestCoverageScore = coverageScore.Min(kvp => kvp.Value);
            var bestMeasureSet = coverageScore.First(kvp => kvp.Value == bestCoverageScore).Key;
      
            cover.Add(bestMeasureSet);
            tier.Remove(bestMeasureSet);
            if (measureSetsToCover.ContainsKey(index))
            {
                measureSetsToCover[index] = measureSetsToCover[index].Where(ms => !ms.IsSubsetOf(bestMeasureSet)).ToList();
            }
        }

        /// <summary>
        /// takes in the overall budget and computes the budget and impression caps
        /// </summary>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <param name="budgets">the budgets</param>
        internal void PerNodeResultsFromBudgets(
            ref BudgetAllocation budgetAllocation,
            Dictionary<MeasureSet, decimal> budgets)
        {
            foreach (var budget in budgets)
            {
                // this modifies perNodeResult
                this.CalculateCaps(
                    new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(budget.Key, budgetAllocation.PerNodeResults[budget.Key]), 
                    budgetAllocation, 
                    budget.Value);
            }
        }

        /// <summary>
        /// Calculates the budgets and impression cap for the node given and eCpm guess, and an overall budget 
        /// </summary>
        /// <param name="perNodeResult">the perNodeResult whose caps are being calculated</param>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <param name="newBudget">the new total budget of the perNodeResult</param>
        internal void CalculateCaps(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> perNodeResult,
            BudgetAllocation budgetAllocation,
            decimal newBudget)
        {  
            var measureSet = perNodeResult.Key;
            var valuation = perNodeResult.Value.Valuation;
            var margin = budgetAllocation.AllocationParameters.Margin;
            var perMilleFees = budgetAllocation.AllocationParameters.PerMilleFees;

            HistoryMetrics historyMetrics = this.GetHistoryMetrics(measureSet, budgetAllocation, newBudget);
            var previousPeriodEffectiveMediaSpend = historyMetrics.MediaSpend;
            var previousPeriodEffectiveImpressions = historyMetrics.Impressions;
            var estimatedNonMediaCostPerMille = historyMetrics.EstimatedNonMediaCostPerMille;

            // set irrelevant historical data to zero
            perNodeResult.Value.ExportBudget = 0;

            perNodeResult.Value.MaxBid = Math.Round(valuation - estimatedNonMediaCostPerMille, 2);

            if (newBudget <= 0)
            {
                return;
            }

            // Estimate media budget and impression cap based on the budget we want to allocate base on previous period
            // TODO: This is incorrect - we should calculate an hourly media budget and multiply by duration
            var mediaBudget = this.measureInfo.CalculateMediaSpend(
                measureSet,
                newBudget,
                previousPeriodEffectiveMediaSpend,
                previousPeriodEffectiveImpressions,
                margin,
                perMilleFees);

            var impressionCap = previousPeriodEffectiveMediaSpend == 0 ? 0 
                : (long)Math.Round(previousPeriodEffectiveImpressions * mediaBudget / previousPeriodEffectiveMediaSpend);

            perNodeResult.Value.PeriodMediaBudget = Math.Round(mediaBudget, 2);
            perNodeResult.Value.PeriodTotalBudget = Math.Round(newBudget, 2);
            perNodeResult.Value.PeriodImpressionCap = impressionCap;
        }

        /// <summary>Determine estimated history to seed initial allocation.</summary>
        /// <param name="measureSet">measure set.</param>
        /// <param name="budgetAllocation">The budget allocation Outputs.</param>
        /// <param name="overallBudget">overall budget for measureset.</param>
        /// <returns>Seed history</returns>
        internal HistoryMetrics GetHistoryMetrics(MeasureSet measureSet, BudgetAllocation budgetAllocation, decimal overallBudget)
        {
            var estimatedCostPerMille = budgetAllocation.AllocationParameters.DefaultEstimatedCostPerMille;
  
            var valuation = budgetAllocation.PerNodeResults[measureSet].Valuation;
            var margin = budgetAllocation.AllocationParameters.Margin;
            var perMilleFees = budgetAllocation.AllocationParameters.PerMilleFees;
            var previousMediaSpend = 0m;
            var previousImpressions = 0L;
            var previousTotalSpend = 0m;
            decimal estimatedNonMediaCostPerMille;
            
            // If we have history, pull spend and impressions from that
            if (budgetAllocation.NodeDeliveryMetricsCollection.ContainsKey(measureSet))
            {
                var nodeDeliveryMetrics = budgetAllocation.NodeDeliveryMetricsCollection[measureSet];
                var periodDuration = (int)budgetAllocation.PeriodDuration.TotalHours;

                previousImpressions = nodeDeliveryMetrics.CalcEffectiveImpressions(periodDuration);
                previousMediaSpend = nodeDeliveryMetrics.CalcEffectiveMediaSpend(periodDuration);
                previousTotalSpend = nodeDeliveryMetrics.CalcEffectiveTotalSpend(
                    this.measureInfo, measureSet, periodDuration, margin, perMilleFees);
            }

            if (previousMediaSpend != 0 && previousImpressions != 0 && previousTotalSpend != 0)
            {
                estimatedNonMediaCostPerMille = 1000 * (previousTotalSpend - previousMediaSpend) / previousImpressions;
            }
            else
            {
                // Calculate a ratio of some default media cost based on ecpm to the cooresponding total cost and use that
                // to adjust valuation and cap our ecpm.
                // TODO: For a very low ecpm flat rate could kick in
                var milles = 100;
                var dummyMediaCost = estimatedCostPerMille * milles;
                var dummyImpressions = milles * 1000;
                var dummyTotalCost = this.measureInfo.CalculateTotalSpend(measureSet, dummyImpressions, dummyMediaCost, margin, perMilleFees);
                var defaultCostRatio = dummyMediaCost / dummyTotalCost;
                estimatedNonMediaCostPerMille = (dummyTotalCost - dummyMediaCost) / milles;
                estimatedCostPerMille = Math.Min(valuation - estimatedNonMediaCostPerMille, estimatedCostPerMille);

                // Set up default first day spend and impressions based on a default ecpm
                if (estimatedCostPerMille > 0m)
                {
                    previousMediaSpend = overallBudget * defaultCostRatio;
                    previousImpressions = (long)Math.Round(previousMediaSpend * 1000 / estimatedCostPerMille);
                }
            }

            return new HistoryMetrics
            {
                MediaSpend = previousMediaSpend,
                Impressions = previousImpressions,
                EstimatedNonMediaCostPerMille = estimatedNonMediaCostPerMille
            };
        }

        /// <summary>
        /// Calculates whether the valuation is high enough to justify the estimated data costs
        /// </summary>
        /// <param name="perNodeResult">the perNodeResult</param>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <returns>true if the valuation justifies the cost</returns>
        internal bool ValuationJustifiesDataCost(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> perNodeResult, 
            BudgetAllocation budgetAllocation)
        {
            return perNodeResult.Value.Valuation > 0.25m + this.GetHistoryMetrics(
                                perNodeResult.Key,
                                budgetAllocation,
                                0).EstimatedNonMediaCostPerMille;
        }

        /// <summary>Estimated history to seed initial allocation</summary>
        internal struct HistoryMetrics
        {
            /// <summary>Gets or sets MediaSpend.</summary>
            internal decimal MediaSpend { get; set; }

            /// <summary>Gets or sets Impressions.</summary>
            internal long Impressions { get; set; }

            /// <summary>Gets or sets EstimatedNonMediaCostPerMille.</summary>
            internal decimal EstimatedNonMediaCostPerMille { get; set; }
        }
    }
}
