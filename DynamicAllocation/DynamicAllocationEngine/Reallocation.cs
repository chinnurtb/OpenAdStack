// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reallocation.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics;

namespace DynamicAllocation
{
    /// <summary>
    /// Class for reallocation calculations (allocations based on historical data)
    /// </summary>
    public class Reallocation : Allocation
    {
        /// <summary>
        /// Initializes a new instance of the Reallocation class
        /// </summary>
        /// <param name="measureInfo">The measure info</param>
        public Reallocation(MeasureInfo measureInfo)
            : base(measureInfo)
        {
        }

        /// <summary>Calculate the amount of budget to allocate over the graph</summary>
        /// <param name="remainingBudget">The remaining budget of the campaign.</param>
        /// <param name="periodCampaignBudget">The period campaign budget.</param>
        /// <returns>The graph budget</returns>
        internal static decimal CalculateGraphBudget(decimal remainingBudget, decimal periodCampaignBudget)
        {
            // A slope that goes from 2 at the beginning to something approaching 1.1 on the last reallocation.
            // TODO: reevaluate the need for this method
            var graphMultiplier = 1.1m;
            return Math.Min(graphMultiplier * periodCampaignBudget, (.10m * remainingBudget) + periodCampaignBudget);
        }

        /// <summary>Apply a boost factor to the exported budget and impression cap</summary>
        /// <param name="perNodeResult">The per node result.</param>
        /// <param name="exportBudgetBoost">multiplier to boost export budget over allocated budget</param>
        internal static void BoostExportBudget(KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> perNodeResult, decimal exportBudgetBoost)
        {
            perNodeResult.Value.ExportBudget *= exportBudgetBoost;
            perNodeResult.Value.PeriodImpressionCap = (long)(perNodeResult.Value.PeriodImpressionCap * exportBudgetBoost);
        }

        /// <summary>Sort per node result by ascending value/impression volume</summary>
        /// <param name="budgetAllocation">The budget allocation Outputs.</param>
        /// <param name="perNodeResults">The per node result collection to sort.</param>
        /// <param name="nodeDeliveryMetricsCollection">The node delivery metrics collection.</param>
        /// <returns>a list of value/volume sorted results</returns>
        internal static List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> SortByValueVolumeScore(
            BudgetAllocation budgetAllocation,
            IEnumerable<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> perNodeResults,
            Dictionary<MeasureSet, IEffectiveNodeMetrics> nodeDeliveryMetricsCollection)
        {
            var perNodeResultsDictionary = perNodeResults.ToDictionary();

            // If there are no node delivery metrics cooresponding to the node result, add a default
            // (zero delivery). This should never happen.
            foreach (var perNodeResult in perNodeResultsDictionary)
            {
                if (nodeDeliveryMetricsCollection.ContainsKey(perNodeResult.Key))
                {
                    continue;
                }

                var msg = "Missing node delivery metrics for exported node: {0}".FormatInvariant(perNodeResult.Key);
                LogManager.Log(LogLevels.Warning, msg);
                nodeDeliveryMetricsCollection.Add(perNodeResult.Key, new NodeDeliveryMetrics());
            }

            return perNodeResultsDictionary
                .OrderBy(pnr => ValueVolumeScore(pnr.Value, nodeDeliveryMetricsCollection[pnr.Key], budgetAllocation.PerNodeResults[pnr.Key].Valuation))
                .ThenBy(pnr => pnr.Value.PeriodTotalBudget)
                .ToList();
        }

        /// <summary>Sort per node result by descending return on ad spend</summary>
        /// <param name="perNodeResults">The per node result collection to sort.</param>
        /// <returns>a list of value/volume sorted results</returns>
        internal static List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> SortByReturnOnAdSpend(
            IEnumerable<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> perNodeResults)
        {
            return perNodeResults
                .OrderByDescending(pnr => pnr.Value.ReturnOnAdSpend)
                .ThenBy(pnr => pnr.Value.PeriodTotalBudget)
                .ToList();
        }

        /// <summary>Sort per node results by node score/valuation/tier descending then budget descending</summary>
        /// <param name="budgetAllocation">The budget allocation Outputs.</param>
        /// <param name="perNodeResults">the per node result collection to sort.</param>
        /// <returns>a list of rank sorted results</returns>
        internal static List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> SortByRank(
            BudgetAllocation budgetAllocation,
            IEnumerable<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> perNodeResults)
        {
            return perNodeResults
                .OrderByDescending(pnr => NodeRank(budgetAllocation.PerNodeResults[pnr.Key].Valuation, pnr))
                .ThenByDescending(pnr => pnr.Value.PeriodTotalBudget).ToList();
        }

        /// <summary>Sort per node results by ascending tier, then descending node rank, then budget descending</summary>
        /// <param name="budgetAllocation">The budget allocation Outputs.</param>
        /// <param name="perNodeResults">the per node result collection to sort.</param>
        /// <returns>a list of rank sorted results</returns>
        internal static List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> SortByTierThenRank(
            BudgetAllocation budgetAllocation,
            IEnumerable<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> perNodeResults)
        {
            return perNodeResults
                .OrderBy(pnr => pnr.Key.Count)
                .ThenByDescending(pnr => NodeRank(budgetAllocation.PerNodeResults[pnr.Key].Valuation, pnr))
                .ThenByDescending(pnr => pnr.Value.PeriodTotalBudget).ToList();
        }

        /// <summary>
        /// Calulates the node rank used in the node sort
        /// </summary>
        /// <param name="valuation">the node's valuation</param>
        /// <param name="perNodeResult">the node's per node result</param>
        /// <returns>the rank</returns>
        internal static double NodeRank(
            decimal valuation,
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> perNodeResult)
        {
            return perNodeResult.Value.NodeScore *
                    perNodeResult.Value.LineagePenalty *
                    (double)valuation *
                    Math.Pow(1, perNodeResult.Key.Count);
        }

        /// <summary>Sort per node results by descending budget</summary>
        /// <param name="perNodeResults">the per node result collection to sort.</param>
        /// <returns>a list of rank sorted results</returns>
        internal static List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> SortByBudget(
            IEnumerable<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> perNodeResults)
        {
            return perNodeResults.OrderByDescending(pnr => pnr.Value.PeriodTotalBudget).ToList();
        }

        /// <summary>
        /// calculates the value-volume 
        /// </summary>
        /// <param name="nodeResult">a per node result</param>
        /// <param name="nodeDeliveryMetrics">Node delivery metrics</param>
        /// <param name="valuation">the valuation</param>
        /// <returns>the value-volume</returns>
        internal static decimal ValueVolumeScore(PerNodeBudgetAllocationResult nodeResult, IEffectiveNodeMetrics nodeDeliveryMetrics, decimal valuation)
        {
            return valuation * nodeDeliveryMetrics.CalcEffectiveImpressionRate() * (decimal)nodeResult.LineagePenalty;
        }

        /// <summary>
        /// Calculate a multiplier applied to a node score based on the performance of ancestors
        /// and descendants of the node. 1 is a neutral score. 0 is the most severe penalty.
        /// </summary>
        /// <param name="budgetAllocation">allocation Outputs.</param>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="ineligibleNodes">The ineligible nodes.</param>
        /// <returns>penalty value</returns>
        internal static double CalculateLineagePenalty(BudgetAllocation budgetAllocation, MeasureSet measureSet, IEnumerable<MeasureSet> ineligibleNodes)
        {
            var exportCount = 0;
            if (budgetAllocation.PerNodeResults.ContainsKey(measureSet))
            {
                var perNodeResult = budgetAllocation.PerNodeResults[measureSet];
                exportCount = perNodeResult.ExportCount;
            }

            if (exportCount > 0)
            {
                return budgetAllocation.AllocationParameters.LineagePenaltyNeutral;
            }

            bool ancestorIneligible = false;
            bool descendantIneligible = false;
            bool applyPenalty = false;
            foreach (var ineligibleNode in ineligibleNodes.TakeWhile(ineligibleNode => !applyPenalty))
            {
                // Detect certain bid overrides - penalty not applied if bid is >= ancestor
                bool bidOverride = measureSet.IsSubsetOf(ineligibleNode) &&
                    budgetAllocation.PerNodeResults[ineligibleNode].Valuation <
                    budgetAllocation.PerNodeResults[measureSet].Valuation;
                if (bidOverride)
                {
                    continue;
                }

                ancestorIneligible = ancestorIneligible || measureSet.IsSubsetOf(ineligibleNode);
                descendantIneligible = descendantIneligible || measureSet.IsSupersetOf(ineligibleNode);
                applyPenalty = ancestorIneligible && descendantIneligible;
            }

            return applyPenalty ? budgetAllocation.AllocationParameters.LineagePenalty : budgetAllocation.AllocationParameters.LineagePenaltyNeutral;
        }

        /// <summary>
        /// Apply a maximum budget value based on the period budget of the campaign.
        /// This prevents a single node from eating too much of the budget.
        /// </summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="baseBudget">The base budget.</param>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <returns>The capped budget.</returns>
        internal static decimal ApplyBudgetCap(MeasureSet measureSet, decimal baseBudget, BudgetAllocation budgetAllocation)
        {
            var cappingPercentage =
                (budgetAllocation.AllocationParameters.LargestBudgetPercentAllowed * measureSet.Count) /
                budgetAllocation.AllocationParameters.NeutralBudgetCappingTier;
            return Math.Min(baseBudget, budgetAllocation.PeriodBudget * cappingPercentage);
        }

        /// <summary>
        /// Computes whether the campaign is under budget
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="exportMeasureSets">the export measureSets</param>
        /// <returns>true if the campaign is under budget</returns>
        internal static bool IsUnderBudget(BudgetAllocation budgetAllocation, List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> exportMeasureSets)
        {
            return exportMeasureSets.Sum(pnr => pnr.Value.PeriodTotalBudget) < budgetAllocation.AnticipatedSpendForDay;
        }

        /// <summary>
        /// Computes if we don't have insight into this node
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation</param>
        /// <param name="pnr">the per node result</param>
        /// <returns>True if we don't have insight into this node</returns>
        internal static bool NoInsight(BudgetAllocation budgetAllocation, KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> pnr)
        {
            var baseTier = budgetAllocation.AllocationParameters.AllocationTopTier -
                budgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo +
                1;
            var topTier = budgetAllocation.AllocationParameters.AllocationTopTier;
            var lineagePenaltyNeutral = budgetAllocation.AllocationParameters.LineagePenaltyNeutral;

            return pnr.Key.Count >= baseTier &&
                pnr.Key.Count <= topTier &&
                Math.Abs(pnr.Value.LineagePenalty - lineagePenaltyNeutral) < .000001 &&
                pnr.Value.ExportCount == 0;
        }

        /// <summary>
        /// Replaces the lowest value-volume nodes with the highest budget nodes until we make our budget goal
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="exportNodes">the current export nodes (expected to be in ascending value-volume order)</param>
        /// <param name="sortedSwapPool">the swap pool (expected to be in descending budget order)</param>
        internal static void AddHighBudgetNodesToMakeSpend(
            BudgetAllocation budgetAllocation,
            ref List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> exportNodes,
            List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> sortedSwapPool)
        {
            // TODO: this loop is guaranteed to terminate / won't overrun the lists given correct inputs
            // though we may want to handle that case somehow anyway
            var exportIndex = 0;
            var swapPoolIndex = 0;
            while (IsUnderBudget(budgetAllocation, exportNodes))
            {
                // replace lowest value volume node with highest budget node etc. until we make budget  
                // skipping nodes that are already in the export list
                if (!exportNodes.Contains(sortedSwapPool[swapPoolIndex]))
                {
                    exportNodes[exportIndex] = sortedSwapPool[swapPoolIndex];
                    exportIndex++;
                }

                swapPoolIndex++;
            }
        }

        /// <summary>
        /// Accumulator for TakeWhile to enforce count and budget limits
        /// </summary>
        /// <param name="maxNodeCount">the node count limit</param>
        /// <param name="maxBudget">the budget limit</param>
        /// <param name="nodeCount">the current node count</param>
        /// <param name="accumulatedBudget">the current budget</param>
        /// <param name="pnr">the per node result being considered for addition</param>
        /// <returns>true if the pnr should be added</returns>
        internal static bool IsUnderCountAndBudgetLimits(
            int maxNodeCount,
            decimal maxBudget,
            ref int nodeCount,
            ref decimal accumulatedBudget,
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> pnr)
        {
            nodeCount++;
            accumulatedBudget += pnr.Value.PeriodTotalBudget;

            return nodeCount <= maxNodeCount && accumulatedBudget < maxBudget + pnr.Value.PeriodTotalBudget;
        }

        /// <summary>
        /// Computes if the we have insight in the campaign. Requires the InsightScore to have been set.
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>True if we have insight</returns>
        internal static bool HaveInsight(BudgetAllocation budgetAllocation)
        {
            return budgetAllocation.InsightScore >= budgetAllocation.AllocationParameters.InsightThreshold;
        }

        /// <summary>
        /// Compares the percent of time that has passed for the this campign with a config threshold
        /// to determine if we should continue looking for insight
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation</param>
        /// <returns>True if we should look for insight</returns>
        internal static bool EarlyEnoughForPhaseOne(BudgetAllocation budgetAllocation)
        {
            var campaignLength = budgetAllocation.CampaignEnd - budgetAllocation.CampaignStart;
            var percentOfCampaignPassed = (budgetAllocation.PeriodStart - budgetAllocation.CampaignStart).Ticks / (double)campaignLength.Ticks;

            return percentOfCampaignPassed < budgetAllocation.AllocationParameters.PhaseOneExitPercentage;
        }

        /// <summary>
        /// Computes if we are in the RISE pahse of the campaign
        /// </summary>
        /// <param name="exportMeasureSets">the list of export measure sets</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>True if we are in the RISE phase of the campaign</returns>
        internal static bool IsRisePhase(List<MeasureSet> exportMeasureSets, BudgetAllocation budgetAllocation)
        {
            // this sets the insight score for the campaign. this should be the first time HaveInsight is called.
            budgetAllocation.InsightScore = InsightScore(budgetAllocation);

            return (EarlyEnoughForPhaseOne(budgetAllocation) && !HaveInsight(budgetAllocation)) ||
                ExportBudgetSum(
                    exportMeasureSets.OrderByDescending(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget)
                        .Take(budgetAllocation.AllocationParameters.MaxNodesToExport).ToList(),
                    budgetAllocation) < budgetAllocation.PeriodBudget;
        }

        /// <summary>
        /// Calcualutes the sum of the total budgets for the measure sets in the exportMeasureSets list
        /// </summary>
        /// <param name="exportMeasureSets">the measure set list</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>the budget sum</returns>
        internal static decimal ExportBudgetSum(List<MeasureSet> exportMeasureSets, BudgetAllocation budgetAllocation)
        {
            var sortedExportMeasureSets = exportMeasureSets.OrderByDescending(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget).Take(budgetAllocation.AllocationParameters.MaxNodesToExport);
            return sortedExportMeasureSets.Sum(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget);
        }

        /// <summary>
        /// The insight score for the campaign
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation inputs</param>
        /// <returns>the insight score (1 is perfect insight, 0 no insight)</returns>
        internal static double InsightScore(BudgetAllocation budgetAllocation)
        {
            // count the number of nodes in the intial allocation tiers for which we have no insight
            var baseTier = budgetAllocation.AllocationParameters.AllocationTopTier -
                budgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo +
                1;

            var topTier = budgetAllocation.AllocationParameters.AllocationTopTier;

            var noInsightCount = budgetAllocation.PerNodeResults
                .Count(pnr => NoInsight(budgetAllocation, pnr));

            var totalCount = budgetAllocation.PerNodeResults
                .Count(pnr => pnr.Key.Count >= baseTier && pnr.Key.Count <= topTier);

            return totalCount != 0 ? (totalCount - noInsightCount) / (double)totalCount : 1;
        }

        /// <summary>
        /// Finds the Nodes that have budget >= the moving average needed
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="exportNodes">the export nodes to be selected from. expected to be sorted in descending budget order</param>
        /// <param name="estimatedExperimentalSpend">the expected average spend of the experimental nodes</param>
        /// <param name="numberOfExperimentalNodesAvailable">the number of experimental nodes available</param>
        /// <returns>the list of nodes that make the moving average cutoff</returns>
        internal static List<MeasureSet> NodesThatMakeBudget(
            BudgetAllocation budgetAllocation,
            List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> exportNodes,
            decimal estimatedExperimentalSpend,
            int numberOfExperimentalNodesAvailable)
        {
            var newExportMeasureSets = new List<MeasureSet>();

            // optimistically assume we will experiment with all our nodes
            var experimentalExportSlots = Math.Min(
                budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes,
                numberOfExperimentalNodesAvailable);

            var currentKeepBudgetSum = 0m;
            var maxPreviousNodesToExport = Math.Min(
                budgetAllocation.AllocationParameters.MaxNodesToExport,
                exportNodes.Count);

            // add the highest speding previously exported nodes as needed until
            // out spending power is at least our budget 
            // (ie. keep as much experimentation as possible while still spending enough)
            var index = 0;
            while (newExportMeasureSets.Count < maxPreviousNodesToExport &&
                currentKeepBudgetSum + (experimentalExportSlots * estimatedExperimentalSpend) < budgetAllocation.PeriodBudget)
            {
                newExportMeasureSets.Add(exportNodes[index].Key);
                currentKeepBudgetSum += exportNodes[index].Value.PeriodTotalBudget;
                experimentalExportSlots--;
                index++;
            }

            return newExportMeasureSets;
        }

        /// <summary>
        /// MeasureSets that should be exported based on budget and average experimental spend
        /// </summary>
        /// <param name="exportMeasureSets">the current export measure sets</param>
        /// <param name="averageExperimentalSpend">the average experimental spend for the previous period</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="numberOfExperimentalNodesAvailable">the number of experimental nodes available</param>
        /// <returns>the updated list of export measure sets</returns>
        internal static List<MeasureSet> PreviouslyExportedNodesToKeep(
            List<MeasureSet> exportMeasureSets,
            decimal averageExperimentalSpend,
            BudgetAllocation budgetAllocation,
            int numberOfExperimentalNodesAvailable)
        {
            // sort the exportMeasureSets by budget
            var unsortedExportNodes = exportMeasureSets.Select(ms => new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(ms, budgetAllocation.PerNodeResults[ms]));
            var exportNodes = SortByBudget(unsortedExportNodes);

            // find the nodes that make the moving average cutoff for budget, given the average expected spend of the experimental nodes
            return NodesThatMakeBudget(budgetAllocation, exportNodes, averageExperimentalSpend, numberOfExperimentalNodesAvailable);
        }

        /// <summary>
        /// computes the tupleScore of the input measureSet 
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="tupleScores">holds the already computed tuple scores so we don't repeat overselves</param>
        /// <param name="nodeDeliveryMetricsCollection">Node delivery metrics.</param>
        /// <returns>the tupleScore</returns>
        internal static double CalculateTupleScore(
            MeasureSet measureSet,
            ref Dictionary<MeasureSet, double> tupleScores,
            Dictionary<MeasureSet, IEffectiveNodeMetrics> nodeDeliveryMetricsCollection)
        {
            // if this measureSets tuple score has already been computed, return that
            double tupleScore;
            if (tupleScores.TryGetValue(measureSet, out tupleScore))
            {
                return tupleScore;
            }

            tupleScore = 0.0;
            var count = 0;

            foreach (var nodeDeliveryMetrics in nodeDeliveryMetricsCollection)
            {
                if (!nodeDeliveryMetrics.Key.IsSupersetOf(measureSet))
                {
                    continue;
                }

                var effectiveImpressionRate = nodeDeliveryMetrics.Value.CalcEffectiveImpressionRate();
                tupleScore += (double)effectiveImpressionRate;
                count++;
            }

            tupleScore = count > 0 ? tupleScore / count : 0;
            tupleScores[measureSet] = tupleScore;
            return tupleScore;
        }

        /// <summary>
        /// computes the CalculateNodeScore of a measureSet
        /// </summary>
        /// <param name="measureSet">the measure set</param>
        /// <param name="tupleScores">dictionary of tuple scores computed so far</param>
        /// <returns>the node score</returns>
        internal static double CalculateNodeScore(
                MeasureSet measureSet,
                ref Dictionary<MeasureSet, double> tupleScores)
        {
            var nodeScore = 0.0;
            var tupleList = MeasureSet.PowerSet(measureSet.ToList());
            foreach (var tuple in tupleList)
            {
                double score;
                if (tupleScores.TryGetValue(tuple, out score))
                {
                    nodeScore += score;
                }
            }

            return nodeScore;
        }

        /// <summary>
        /// Calculates scores related to lineage (LineagePenalty and NodeScore)
        /// </summary>
        /// <param name="budgetAllocation">the allocation Outputs</param>
        internal static void CalculateLineageScores(BudgetAllocation budgetAllocation)
        {
            var tupleScores = new Dictionary<MeasureSet, double>();

            // determine all the new ineligible nodes first
            Parallel.ForEach(
                budgetAllocation.NodeDeliveryMetricsCollection,
                nodeDeliveryMetrics =>
                {
                    var lifeTimeImpressions = nodeDeliveryMetrics.Value.CalcEffectiveImpressions(
                        IEffectiveNodeMetrics.LifetimeLookBack);
                    var totalExportedHours = nodeDeliveryMetrics.Value.TotalEligibleHours;
                    var nodeResult = budgetAllocation.PerNodeResults[nodeDeliveryMetrics.Key];
                    nodeResult.NodeIsIneligible = totalExportedHours > 0 && lifeTimeImpressions == 0;
                });

            foreach (var nodeDeliveryMetrics in budgetAllocation.NodeDeliveryMetricsCollection)
            {
                if (tupleScores.ContainsKey(nodeDeliveryMetrics.Key))
                {
                    continue;
                }

                var powerSet = MeasureSet.PowerSet(nodeDeliveryMetrics.Key.ToList());
                foreach (var tuple in powerSet)
                {
                    CalculateTupleScore(
                        tuple,
                        ref tupleScores,
                        budgetAllocation.NodeDeliveryMetricsCollection);
                }
            }

            var ineligibleNodes = budgetAllocation.PerNodeResults.Where(pnr => pnr.Value.NodeIsIneligible).Select(pnr => pnr.Key).ToList();

            // then calc lineagepenalty and node score
            Parallel.ForEach(
                budgetAllocation.PerNodeResults,
                nodeResult =>
                {
                    nodeResult.Value.LineagePenalty = CalculateLineagePenalty(budgetAllocation, nodeResult.Key, ineligibleNodes);
                    nodeResult.Value.NodeScore = CalculateNodeScore(nodeResult.Key, ref tupleScores);
                });
        }

        /// <summary>
        /// Computes a Miximize and Rarify Phase Allocation
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>the list of measureSets to export</returns>
        internal List<MeasureSet> MaximizeAndRarifyPhaseAllocation(ref BudgetAllocation budgetAllocation)
        {
            //// First we Maximize Value-Volume
            var exportNodes = this.PhaseThree(budgetAllocation);

            // if we are using more than the steady state node count, replace low budget nodes with no insight nodes with
            // the highest node scores up to the percent of budget in the budget buffer
            if (exportNodes.Count >= budgetAllocation.AllocationParameters.MaxNodesToExport)
            {
                return this.PhaseThreePointFive(exportNodes, budgetAllocation);
            }

            // otherwise, rarify
            this.PhaseFour(budgetAllocation, exportNodes);

            return exportNodes.Select(pnr => pnr.Key).ToList();
        }

        /// <summary>
        /// Phase 3 of the Allocation (currently always occurs as a preperation for either phase 3.5 or 4)
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation</param>
        /// <returns>the new export node set</returns>
        internal List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> PhaseThree(BudgetAllocation budgetAllocation)
        {
            // sort the exported nodes by value-volume and take the enough to make budget, or at max the max export count
            var exportNodes = this.GetLargestReturnOnAdSpendNodes(budgetAllocation, budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes);

            exportNodes = SortByValueVolumeScore(
                   budgetAllocation,
                   exportNodes,
                   budgetAllocation.NodeDeliveryMetricsCollection);

            // if we are under budget, replace the lowest value-volume nodes by high budget nodes until we reach budget
            var sortedSwapPool = SortByBudget(
                budgetAllocation.PerNodeResults.Where(pnr =>
                    this.ExportedNodeWorthConsidering(pnr, budgetAllocation) && pnr.Value.ExportCount != 0));
            AddHighBudgetNodesToMakeSpend(budgetAllocation, ref exportNodes, sortedSwapPool);
            return exportNodes;
        }

        /// <summary>
        /// Swap some of the export nodes for rarer nodes that sum to the same budget
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="exportNodes">the current export nodes</param>
        internal void PhaseFour(
            BudgetAllocation budgetAllocation,
            List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> exportNodes)
        {
            budgetAllocation.Phase = 4;

            // we have extra export slots available so we will swap out low budget nodes for groups of rarer crowd nodes of the same collective budget
            var sortedExportNode = SortByValueVolumeScore(budgetAllocation, exportNodes, budgetAllocation.NodeDeliveryMetricsCollection);
            var exportIndex = 0;
            var exportBudget = 0m;
            var maxNodesToSwap = budgetAllocation.AllocationParameters.MaxNodesToExport - exportNodes.Count + 1; // always try to swap one node, thus + 1
            var descendingNodeScoreList = budgetAllocation.PerNodeResults
                    .Where(pnr =>
                        this.ExportedNodeWorthConsidering(pnr, budgetAllocation) &&
                        pnr.Value.ExportCount != 0 &&
                        !exportNodes.Contains(pnr))
                    .ToList();
            var nodeScoreListForSwapping = new List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>>();

            while (nodeScoreListForSwapping.Count + exportNodes.Count - exportIndex < budgetAllocation.AllocationParameters.MaxNodesToExport && exportIndex < exportNodes.Count)
            {
                exportBudget += sortedExportNode[exportIndex].Value.PeriodTotalBudget;
                var exportNodeCounter = 0;
                var exportBudgetTracker = 0.0m;
                var nodeScoreThreshold = sortedExportNode.Take(exportIndex + 1).Max(pnr => pnr.Value.NodeScore);
                var higherNodeScoreNodesAvailable = descendingNodeScoreList
                    .Where(pnr => pnr.Value.NodeScore > nodeScoreThreshold)
                    .ToList();
                if (higherNodeScoreNodesAvailable.Count == 0)
                {
                    break;
                }

                var newNodeScoreListForSwapping = higherNodeScoreNodesAvailable
                    .OrderByDescending(pnr => NodeRank(pnr.Value.Valuation, pnr))
                    .TakeWhile(pnr => IsUnderCountAndBudgetLimits(maxNodesToSwap, exportBudget, ref exportNodeCounter, ref exportBudgetTracker, pnr))
                    .ToList();
                if (newNodeScoreListForSwapping.Sum(pnr => pnr.Value.PeriodTotalBudget) < exportBudget)
                {
                    break;
                }

                nodeScoreListForSwapping = newNodeScoreListForSwapping;
                exportIndex++;
                maxNodesToSwap++;
            }

            // only swap if we found nodes to swap
            if (nodeScoreListForSwapping.Count > 0)
            {
                for (var i = 0; i < exportIndex; i++)
                {
                    exportNodes.Remove(sortedExportNode[i]);
                }

                exportNodes.Add(nodeScoreListForSwapping);
            }
        }

        /// <summary>
        /// Adds high node score no insight nodes for experimentation
        /// </summary>
        /// <param name="currentExportNodes">the current set of export nodes</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>the new export nodes</returns>
        internal List<MeasureSet> PhaseThreePointFive(
            List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> currentExportNodes,
            BudgetAllocation budgetAllocation)
        {
            budgetAllocation.Phase = 3.5;

            // take the largest budget export nodes until the budget just goes over the keep budget
            // and add high node rank no insight nodes with the average budget needed to make the full budget
            var keepBudget = budgetAllocation.AnticipatedSpendForDay;
            var nodeCount = 0;
            var accumulatedBudget = 0m;
            var sortedExportNodes = SortByBudget(currentExportNodes);
            var keepMeasureSets = sortedExportNodes
                .TakeWhile(pnr => IsUnderCountAndBudgetLimits(
                    budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes,
                    keepBudget,
                    ref nodeCount,
                    ref accumulatedBudget,
                    pnr))
                .Select(pnr => pnr.Key)
                .ToList();
            var numNewNodesNeeded = budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes - keepMeasureSets.Count;

            if (numNewNodesNeeded <= 0)
            {
                return keepMeasureSets;
            }

            var budgetBuffer = budgetAllocation.PeriodBudget - keepMeasureSets.Sum(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget);

            // keep the numNewNodes highest nodescore nodes from the no insight set 
            var noInsightNodes = budgetAllocation.PerNodeResults
                .Where(pnr =>
                    NoInsight(budgetAllocation, pnr) &&
                    this.ValuationJustifiesDataCost(pnr, budgetAllocation) &&
                    !keepMeasureSets.Contains(pnr.Key));
            var sortedNoInsightNodes = SortByRank(budgetAllocation, noInsightNodes);

            var numNewNodesAvailable = Math.Min(numNewNodesNeeded, sortedNoInsightNodes.Count);
            var newMeasureSets = sortedNoInsightNodes.Take(numNewNodesAvailable).Select(pnr => pnr.Key).ToList();

            if (numNewNodesAvailable < numNewNodesNeeded)
            {
                var numNewNewNodesNeeded = numNewNodesNeeded - numNewNodesAvailable;

                var swapNodes = budgetAllocation.PerNodeResults
                    .Where(pnr =>
                        pnr.Value.ExportCount == 0 &&
                        this.ValuationJustifiesDataCost(pnr, budgetAllocation) &&
                        !keepMeasureSets.Contains(pnr.Key));
                var sortedSwapNodes = SortByRank(budgetAllocation, swapNodes);

                var numNewNewNodesAvailable = Math.Min(numNewNewNodesNeeded, sortedSwapNodes.Count);
                newMeasureSets.AddRange(sortedSwapNodes.Take(numNewNewNodesAvailable).Select(pnr => pnr.Key));
            }

            keepMeasureSets.AddRange(newMeasureSets);

            // add the correct budget to the newMeasureSets
            var experimentalNodeBudget = newMeasureSets.Count > 0 ?
                Math.Max(budgetBuffer / newMeasureSets.Count, budgetAllocation.AllocationParameters.MinBudget) :
                0;
            foreach (var measureSet in newMeasureSets)
            {
                budgetAllocation.PerNodeResults[measureSet].PeriodTotalBudget = experimentalNodeBudget;
            }

            return keepMeasureSets;
        }

        /// <summary>
        /// Gets the largest budgeted nodes
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="maxNodeCount">the maximum length of the list</param>
        /// <param name="maxBudget">the maximum allowed budget sum</param>
        /// <returns>the largest budgeted nodes</returns>
        internal List<MeasureSet> GetLargestBudgetedNodes(BudgetAllocation budgetAllocation, int maxNodeCount, decimal maxBudget)
        {
            // TODO: this will return a list that is = or somwwhat over budget (by at most on penny less than the last added nodes budget)
            // we may need to compensate for this overrage in some way
            var nodeCount = 0;
            var accumulatedBudget = 0m;
            return budgetAllocation
                .PerNodeResults
                .Where(pnr => this.ExportedNodeWorthConsidering(pnr, budgetAllocation))
                .OrderByDescending(pnr => pnr.Value.PeriodTotalBudget)
                .TakeWhile(pnr => IsUnderCountAndBudgetLimits(maxNodeCount, maxBudget, ref nodeCount, ref accumulatedBudget, pnr))
                .Select(pnr => pnr.Key)
                .ToList();
        }

        /// <summary>
        /// Gets the largest budgeted nodes
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="maxNodeCount">the maximum length of the list</param>
        /// <returns>the largest budgeted nodes</returns>
        internal List<MeasureSet> GetLargestBudgetedNodes(BudgetAllocation budgetAllocation, int maxNodeCount)
        {
            return this.GetLargestBudgetedNodes(budgetAllocation, maxNodeCount, budgetAllocation.PeriodBudget);
        }

        /// <summary>
        /// Gets the largest value-volume nodes
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="maxNodeCount">the maximum length of the list</param>
        /// <returns>the list of largest value-volume nodes</returns>
        internal List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> GetLargestReturnOnAdSpendNodes(BudgetAllocation budgetAllocation, int maxNodeCount)
        {
            // TODO: this will return a list that is = or somewhat over budget (by at most one penny less than the last added node's budget)
            // we may want to compensate for this overage in some way
            var nodeCount = 0;
            var accumulatedBudget = 0m;
            var sortedNodeResults = SortByReturnOnAdSpend(
                budgetAllocation.PerNodeResults.Where(pnr => this.ExportedNodeWorthConsidering(pnr, budgetAllocation)));
            return sortedNodeResults
                .TakeWhile(pnr => 
                    IsUnderCountAndBudgetLimits(
                    maxNodeCount, 
                    budgetAllocation.PeriodBudget, 
                    ref nodeCount, 
                    ref accumulatedBudget, 
                    pnr))
                .ToList();
        }

        /// <summary>
        /// Checks if a previously exported node is worth considering for reexport
        /// </summary>
        /// <param name="pnr">the node</param>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <returns>true if the node is worthy of consideration</returns>
        internal bool ExportedNodeWorthConsidering(KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> pnr, BudgetAllocation budgetAllocation)
        {   
            return !pnr.Value.NodeIsIneligible && 
                pnr.Value.PeriodMediaBudget > 0 && 
                this.ValuationJustifiesDataCost(pnr, budgetAllocation);
        }

        /// <summary>
        /// Calculates the average spend of experimental nodes in the previous period
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>the average spend</returns>
        internal decimal EstimatedExperimentalSpend(BudgetAllocation budgetAllocation)
        {
            // calculate the average spend rate for all previously exported nodes
            var previouslyExportedNodes = budgetAllocation
                .PerNodeResults
                .Where(pnr => pnr.Value.ExportCount > 0)
                .ToList();

            // in the absence of any data, assume nodes will spend the min budget
            if (previouslyExportedNodes.Count == 0 || budgetAllocation.NodeDeliveryMetricsCollection.Count == 0)
            {
                return budgetAllocation.AllocationParameters.MinBudget;
            }

            // cap the spend of each node to the MinBudget.
            // since we won't allocate more than MinBudget to experimental nodes the capped average correctly 
            // relfects the average spending power.
            var averageEstimatedSpend = budgetAllocation
                .NodeDeliveryMetricsCollection
                .Average(
                    kvp =>
                    Math.Min(
                        kvp.Value.CalcEffectiveTotalSpend(
                        this.MeasureInfo,
                            kvp.Key,
                            (int)budgetAllocation.PeriodDuration.TotalHours,
                            budgetAllocation.AllocationParameters.Margin,
                            budgetAllocation.AllocationParameters.PerMilleFees),
                        budgetAllocation.AllocationParameters.MinBudget));

            return averageEstimatedSpend;
        }

        /// <summary>
        /// Reallocates budget among the graph nodes when there is history (ie usually all but the first day of a campaign flight)
        /// </summary>
        /// <param name="budgetAllocation">the budgetAllocation</param>
        /// <returns>budgetAllocation. budgets etc. </returns>
        internal BudgetAllocation AllocateBudget(BudgetAllocation budgetAllocation)
        {
            LogManager.Log(
                LogLevels.Trace,
                "CalculatePeriodBudget: ReallocationStart: {0}, CampaignEnd: {1}",
                budgetAllocation.PeriodStart.ToString("o", CultureInfo.InvariantCulture),
                budgetAllocation.CampaignEnd.ToString("o", CultureInfo.InvariantCulture));

            // the budget for this period.
            var periodBudget = CalculatePeriodBudget(
                budgetAllocation.RemainingBudget,
                budgetAllocation.CampaignEnd.Subtract(budgetAllocation.PeriodStart),
                budgetAllocation.PeriodDuration);

            // unboosted period budget is our anticipated spend
            budgetAllocation.AnticipatedSpendForDay = periodBudget;

            // the budget used to determine how far down the list of nodes by rank we put the export cutoff
            budgetAllocation.PeriodBudget = CalculateGraphBudget(
                budgetAllocation.RemainingBudget,
                periodBudget);

            LogManager.Log(LogLevels.Trace, "PeriodBudget: {0}, GraphBudget: {1}", budgetAllocation.PeriodBudget, budgetAllocation.PeriodBudget);

            // give budget to each measureSet based on its history
            var budgets = this.BudgetMeasureSets(budgetAllocation);

            // update the budgets 
            this.PerNodeResultsFromBudgets(ref budgetAllocation, budgets);

            // Calculate tuple scores and lineage penalties
            // TODO move this later in the stack when we have reduced the consideration node list
            CalculateLineageScores(budgetAllocation);

            // get list of nodes with the largest budgets
            // TODO: make an initial vs. steady state node count that is compatible with intial allocation 
            var exportMeasureSets = GetLargestBudgetedNodes(budgetAllocation, budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes);

            // If the campaign is in RISE phase, do a RISE style allocation, otherwise Maximize and Rarify
            // currently RISE phase means we lack insight or havent made budget yet with the small export set
            if (IsRisePhase(exportMeasureSets, budgetAllocation))
            {
                exportMeasureSets = this.RisePhaseAllocation(exportMeasureSets, budgetAllocation);
            }
            else
            {
                exportMeasureSets = this.MaximizeAndRarifyPhaseAllocation(ref budgetAllocation);
            }

            // set the export budget in the exported nodes and add ExportBudgetBoost
            foreach (var measureSet in exportMeasureSets)
            {
                var perNodeResult = budgetAllocation.PerNodeResults[measureSet];

                this.CalculateCaps(
                    new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(measureSet, perNodeResult),
                    budgetAllocation,
                    perNodeResult.PeriodTotalBudget);

                perNodeResult.ExportBudget = perNodeResult.PeriodMediaBudget;

                // add ExportBudgetBoost
                BoostExportBudget(
                    new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(measureSet, perNodeResult),
                    budgetAllocation.AllocationParameters.ExportBudgetBoost);
            }

            return budgetAllocation;
        }

        /// <summary>
        /// Determines which measureSets to export during the RISE phase
        /// </summary>
        /// <param name="exportMeasureSets">the top budgeted list of measure sets</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <returns>the new list of export measure sets</returns>
        internal List<MeasureSet> RisePhaseAllocation(List<MeasureSet> exportMeasureSets, BudgetAllocation budgetAllocation)
        {
            if (EarlyEnoughForPhaseOne(budgetAllocation) && !HaveInsight(budgetAllocation))
            {
                return this.PhaseOne(exportMeasureSets, budgetAllocation);
            }

            return this.PhaseTwo(exportMeasureSets, budgetAllocation);
        }

        /// <summary>
        /// Phase one allocation 
        /// </summary>
        /// <param name="exportMeasureSets">the current export sets (intended to be the high budget nodes)</param>
        /// <param name="budgetAllocation">the budget allocation</param>
        /// <returns>the new list of export measure sets</returns>
        internal List<MeasureSet> PhaseOne(List<MeasureSet> exportMeasureSets, BudgetAllocation budgetAllocation)
        {
            // otherwise we will focus on nodes for which we wish to gain more insight
            var swapSet = budgetAllocation
                .PerNodeResults
                .Where(pnr => 
                    !pnr.Value.NodeIsIneligible && 
                    NoInsight(budgetAllocation, pnr) &&
                    ValuationJustifiesDataCost(pnr, budgetAllocation))
                .Select(pnr => pnr.Key)
                .ToList();

            // set the phase
            budgetAllocation.Phase = 1;

            // if we have nothing to swap there's nothing to do
            if (swapSet.Count == 0)
            {
                return exportMeasureSets;
            }

            // calculate the average spend of the previous period's experimental nodes, scaled to the current period length
            var estimatedExperimentalSpend = this.EstimatedExperimentalSpend(budgetAllocation);

            // keep some of the previosuly exported nodes 
            var newExportMeasureSets = PreviouslyExportedNodesToKeep(exportMeasureSets, estimatedExperimentalSpend, budgetAllocation, swapSet.Count);

            // add experimental nodes for the rest of the export count available 
            // TODO: control which tiers the swapSet contains to better control where experimentation happens
            var numberOfNodesToAdd = budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes - newExportMeasureSets.Count;
            this.AddExperimentalSpread(numberOfNodesToAdd, budgetAllocation, ref swapSet, ref newExportMeasureSets);

            return newExportMeasureSets;
        }

        /// <summary>
        /// Phase 2 allocation
        /// </summary>
        /// <param name="exportMeasureSets">the current export measure sets (intended to be the high budget nodes) </param>
        /// <param name="budgetAllocation">the budget allocation</param>
        /// <returns>the new list of export measure sets</returns>
        internal List<MeasureSet> PhaseTwo(List<MeasureSet> exportMeasureSets, BudgetAllocation budgetAllocation)
        {
            // set the phase
            budgetAllocation.Phase = 2;

            // if we have insight but havent made budget yet, we will look to lower tiers and lineage penaltied nodes etc. to try to make budget
            var baseTier = budgetAllocation.AllocationParameters.AllocationTopTier -
                budgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo +
                1;

            var swapSet1 = budgetAllocation
                .PerNodeResults
                .Where(pnr => 
                    !pnr.Value.NodeIsIneligible && 
                    pnr.Value.ExportCount == 0 &&
                    pnr.Key.Count < baseTier &&
                    ValuationJustifiesDataCost(pnr, budgetAllocation))
                .Select(pnr => pnr.Key)
                .ToList();

            // if there aren't enough of those, we will use some of these
            var swapSet2 = budgetAllocation
                .PerNodeResults
                .Where(pnr =>
                    !pnr.Value.NodeIsIneligible &&
                    pnr.Value.ExportCount == 0 &&
                    pnr.Key.Count <= budgetAllocation.AllocationParameters.AllocationTopTier &&
                    ValuationJustifiesDataCost(pnr, budgetAllocation))
                .Select(pnr => pnr.Key)
                .ToList();

            // calculate the average spend of the previous period's experimental nodes, scaled to the current period length
            var estimatedExperimentalSpend = this.EstimatedExperimentalSpend(budgetAllocation);

            // keep some of the previosuly exported nodes 
            var newExportMeasureSets = PreviouslyExportedNodesToKeep(
                exportMeasureSets,
                estimatedExperimentalSpend,
                budgetAllocation,
                swapSet1.Count + swapSet2.Count);

            // add nodes from lower tiers if there are any
            if (swapSet1.Count != 0)
            {
                // add experimental nodes for the rest of the export count available 
                // TODO: control which tiers the swapSet contains to better control where experimentation happens
                var numberOfNodesToAdd = budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes - newExportMeasureSets.Count;
                this.AddTopNodeRankNodes(numberOfNodesToAdd, budgetAllocation, swapSet1, ref newExportMeasureSets);
            }

            // if we still haven't added enough nodes add some more by tier, then by node rank
            if (newExportMeasureSets.Count < budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes)
            {
                // add experimental nodes for the rest of the export count available 
                // TODO: control which tiers the swapSet contains to better control where experimentation happens
                var numberOfNodesToAdd = budgetAllocation.AllocationParameters.InitialMaxNumberOfNodes - newExportMeasureSets.Count;
                this.AddTopNodeRankNodesByTier(numberOfNodesToAdd, budgetAllocation, swapSet2, ref newExportMeasureSets);
            }

            return newExportMeasureSets;
        }

        /// <summary>
        /// Adds measureSets to the newExportMeasureSets for expermental spread
        /// </summary>
        /// <param name="nodesToAdd">the number of nodes to add</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="swapSet">the set to draw new nodes from</param>
        /// <param name="newExportMeasureSets">the list to add measureSets to</param>
        internal void AddExperimentalSpread(int nodesToAdd, BudgetAllocation budgetAllocation, ref List<MeasureSet> swapSet, ref List<MeasureSet> newExportMeasureSets)
        {
            if (nodesToAdd <= 0)
            {
                return;
            }

            var budgetLeft = budgetAllocation.PeriodBudget - newExportMeasureSets.Sum(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget);

            nodesToAdd = Math.Min(nodesToAdd, swapSet.Count());

            var averageSpendNeeded = budgetLeft / nodesToAdd;

            averageSpendNeeded = averageSpendNeeded < budgetAllocation.AllocationParameters.MinBudget ?
                budgetAllocation.AllocationParameters.MinBudget :
                averageSpendNeeded;

            var sortedSwapSet = swapSet.OrderBy(ms => budgetAllocation.PerNodeResults[ms].NodeScore);
            var nodeListToAdd = sortedSwapSet.Take(nodesToAdd);

            foreach (var node in nodeListToAdd)
            {
                this.CalculateCaps(
                    new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(node, budgetAllocation.PerNodeResults[node]),
                    budgetAllocation,
                    averageSpendNeeded);
            }

            newExportMeasureSets.AddRange(nodeListToAdd);
        }

        /// <summary>
        /// Adds measureSets to the newExportMeasureSets for expermental spread
        /// </summary>
        /// <param name="nodesToAdd">the number of nodes to add</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="swapSet">the set to draw new nodes from</param>
        /// <param name="newExportMeasureSets">the list to add measureSets to</param>
        internal void AddTopNodeRankNodes(int nodesToAdd, BudgetAllocation budgetAllocation, List<MeasureSet> swapSet, ref List<MeasureSet> newExportMeasureSets)
        {
            if (nodesToAdd <= 0)
            {
                return;
            }

            var budgetLeft = budgetAllocation.PeriodBudget - newExportMeasureSets.Sum(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget);

            nodesToAdd = Math.Min(nodesToAdd, swapSet.Count);
            var averageSpendNeeded = budgetLeft / nodesToAdd;

            // make sure experimental nodes get at least a min budget.
            averageSpendNeeded = averageSpendNeeded < budgetAllocation.AllocationParameters.MinBudget ?
                budgetAllocation.AllocationParameters.MinBudget :
                averageSpendNeeded;

            // sort the swap set by NodeRank
            var swapNodes = swapSet.Select(ms => new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(ms, budgetAllocation.PerNodeResults[ms]));
            var sortedSwapNodes = SortByRank(budgetAllocation, swapNodes);

            var topRankedNodesToAdd = sortedSwapNodes.Take(nodesToAdd);

            foreach (var node in topRankedNodesToAdd)
            {
                newExportMeasureSets.Add(node.Key);

                this.CalculateCaps(
                  node,
                  budgetAllocation,
                  averageSpendNeeded);
            }
        }

        /// <summary>
        /// Adds measureSets to the newExportMeasureSets for expermental spread
        /// </summary>
        /// <param name="nodesToAdd">the number of nodes to add</param>
        /// <param name="budgetAllocation">the budget allocation outputs</param>
        /// <param name="swapSet">the set to draw new nodes from</param>
        /// <param name="newExportMeasureSets">the list to add measureSets to</param>
        internal void AddTopNodeRankNodesByTier(
            int nodesToAdd,
            BudgetAllocation budgetAllocation,
            List<MeasureSet> swapSet,
            ref List<MeasureSet> newExportMeasureSets)
        {
            nodesToAdd = Math.Min(nodesToAdd, swapSet.Count());

            if (nodesToAdd <= 0)
            {
                return;
            }

            var budgetLeft = budgetAllocation.PeriodBudget - newExportMeasureSets.Sum(ms => budgetAllocation.PerNodeResults[ms].PeriodTotalBudget);
            var averageSpendNeeded = budgetLeft / nodesToAdd;

            // make sure experimental nodes get at least a min budget.
            averageSpendNeeded = averageSpendNeeded < budgetAllocation.AllocationParameters.MinBudget ?
                budgetAllocation.AllocationParameters.MinBudget :
                averageSpendNeeded;

            // sort the swap set by Tier and then NodeRank
            var swapNodes = swapSet.Select(ms => new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(ms, budgetAllocation.PerNodeResults[ms]));
            var sortedSwapNodes = SortByTierThenRank(budgetAllocation, swapNodes);

            var topRankedNodesToAdd = sortedSwapNodes.Take(nodesToAdd);

            foreach (var node in topRankedNodesToAdd)
            {
                newExportMeasureSets.Add(node.Key);

                this.CalculateCaps(
                  node,
                  budgetAllocation,
                  averageSpendNeeded);
            }
        }

        /// <summary>
        /// allocates a total budget to each measure set when there is history data
        /// </summary>
        /// <param name="budgetAllocation">the budget allocation Outputs</param>
        /// <returns>the total budgets for the measure sets</returns>
        internal Dictionary<MeasureSet, decimal> BudgetMeasureSets(BudgetAllocation budgetAllocation)
        {
            // Initialize budget per measure set in per node results to zero
            var budgets = budgetAllocation.PerNodeResults.Keys.ToDictionary(ms => ms, ms => 0m);

            // For each node with history, update the budget based on history
            foreach (var nodeDeliveryMetricsEntry in budgetAllocation.NodeDeliveryMetricsCollection)
            {
                var measureSet = nodeDeliveryMetricsEntry.Key;
                var nodeDeliveryMetrics = nodeDeliveryMetricsEntry.Value;

                if (nodeDeliveryMetrics.TotalEligibleHours == 0)
                {
                    continue;
                }

                var budget = this.CalculateTotalSpendBudget(measureSet, nodeDeliveryMetrics, budgetAllocation);
                budget = ApplyBudgetCap(measureSet, budget, budgetAllocation);
                budgets[measureSet] = budget;
            }

            return budgets;
        }

        /// <summary>Calculate node budget in terms of total spend and sets the perNodeResults.ReturnOnAdSpend.</summary>
        /// <param name="measureSet">The measure Set.</param>
        /// <param name="nodeDeliveryMetrics">The node delivery metrics.</param>
        /// <param name="budgetAllocation">The budget Allocation Outputs.</param>
        /// <returns>An budget in terms of total spend.</returns>
        internal decimal CalculateTotalSpendBudget(MeasureSet measureSet, IEffectiveNodeMetrics nodeDeliveryMetrics, BudgetAllocation budgetAllocation)
        {
            var perNodeResult = budgetAllocation.PerNodeResults[measureSet];
            var effectiveImpressionRate = nodeDeliveryMetrics.CalcEffectiveImpressionRate();
            if (perNodeResult.NodeIsIneligible || effectiveImpressionRate == 0)
            {
                perNodeResult.ReturnOnAdSpend = 0;
                return 0;
            }

            // Get the config based settings
            var margin = budgetAllocation.AllocationParameters.Margin;
            var perMilleFees = budgetAllocation.AllocationParameters.PerMilleFees;

            // Calculate ReturnOnAdSpend
            var effectiveTotalSpendRate = nodeDeliveryMetrics.CalcEffectiveTotalSpend(this.MeasureInfo, measureSet, 1, margin, perMilleFees);
            var ecpm = effectiveTotalSpendRate * 1000 / effectiveImpressionRate;
            perNodeResult.ReturnOnAdSpend = perNodeResult.Valuation / ecpm;

            // Calculate total spend for the period based on the impression rate, period length and the ecpi
            var budget = nodeDeliveryMetrics.CalcEffectiveTotalSpend(
                this.MeasureInfo, measureSet, (int)budgetAllocation.PeriodDuration.TotalHours, margin, perMilleFees);

            return budget;
        }
    }
}
