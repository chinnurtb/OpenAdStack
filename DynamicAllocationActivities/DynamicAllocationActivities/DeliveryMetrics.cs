// -----------------------------------------------------------------------
// <copyright file="DeliveryMetrics.cs" company="Emerging Media Group">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Class to calculate delivery metrics from delivery data.</summary>
    internal class DeliveryMetrics : IDeliveryMetrics
    {
        /// <summary>Set the to 24 hours until we decide if it needs to be used on a per network basis.</summary>
        private static readonly TimeSpan DefaultReportGracePeriod = new TimeSpan(24, 0, 0);
        
        /// <summary>Initializes a new instance of the <see cref="DeliveryMetrics"/> class.</summary>
        /// <param name="reportDeadZone">The report dead zone.</param>
        /// <param name="dataCoster">Data coster helper object.</param>
        /// <param name="nodeMetricsCollection">The node metrics collection.</param>
        internal DeliveryMetrics(
            TimeSpan reportDeadZone,
            IDeliveryDataCost dataCoster,
            Dictionary<MeasureSet, NodeDeliveryMetrics> nodeMetricsCollection)
            : this(reportDeadZone, DefaultReportGracePeriod, dataCoster, nodeMetricsCollection)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DeliveryMetrics"/> class.</summary>
        /// <param name="reportDeadZone">The report dead zone.</param>
        /// <param name="reportGracePeriod">The report grace period.</param>
        /// <param name="dataCoster">Data coster helper object.</param>
        /// <param name="nodeMetricsCollection">The node metrics collection.</param>
        internal DeliveryMetrics(
            TimeSpan reportDeadZone, 
            TimeSpan reportGracePeriod, 
            IDeliveryDataCost dataCoster,
            Dictionary<MeasureSet, NodeDeliveryMetrics> nodeMetricsCollection)
        {
            this.ReportDeadZone = reportDeadZone;
            this.ReportGracePeriod = reportGracePeriod;
            this.DeliveryDataCost = dataCoster;
            this.LifetimeMediaBudgetCap = 0m;
            this.RemainingBudget = 0m;
            this.NodeMetricsCollection = nodeMetricsCollection;
        }

        /// <summary>
        /// Gets helper object to encapsulate data costing
        /// </summary>
        public IDeliveryDataCost DeliveryDataCost { get; private set; }

        /// <summary>
        /// Gets RemainingBudget.
        /// </summary>
        public decimal RemainingBudget { get; private set; }

        /// <summary>
        /// Gets LifetimeMediaBudgetCap.
        /// </summary>
        public decimal LifetimeMediaBudgetCap { get; private set; }

        /// <summary>
        /// Gets NodeMetricsCollection.
        /// </summary>
        public Dictionary<MeasureSet, NodeDeliveryMetrics> NodeMetricsCollection { get; private set; }

        /// <summary>
        /// Gets PreviousLatestCampaignDeliveryHour.
        /// </summary>
        public DateTime PreviousLatestCampaignDeliveryHour
        {
            get
            {
                var hour = DateTime.MinValue;
                if (this.NodeMetricsCollection.Values.Any())
                {
                    hour = this.NodeMetricsCollection.Values.Max(m => m.LastProcessedDeliveryHour);
                }

                return hour;
            }
        }

        /// <summary>
        /// Gets ReportDeadZone, the amount of unstable data to exclude
        /// from the end of a retrived report.
        /// </summary>
        internal TimeSpan ReportDeadZone { get; private set; }

        /// <summary>
        /// Gets ReportGracePeriod, the amount of time we allow trailing
        /// empties in the delivery data to be treated as not significant.
        /// </summary>
        internal TimeSpan ReportGracePeriod { get; private set; }

        /// <summary>Calculate lifetime metrics that require iteration of the campaign delivery history.</summary>
        /// <param name="canonicalDeliveryData">The delivery data lookback set for this DA Campaign as a dictionary.</param>
        /// <param name="eligibilityHistoryBuilder">The eligibility history lookback set.</param>
        /// <param name="nodeMap">The of allocationIds to measureSets.</param>
        /// <param name="totalBudget">budget for campaign.</param>
        public void CalculateNodeMetrics(
            ICanonicalDeliveryData canonicalDeliveryData,
            IEligibilityHistoryBuilder eligibilityHistoryBuilder,
            Dictionary<string, MeasureSet> nodeMap,
            decimal totalBudget)
        {
            if (eligibilityHistoryBuilder == null)
            {
                return;
            }

            // Accumulator for the delivered periods per measureSet - this will effectively
            // perform a pivot on the delivered data vs measureSet
            var deliveryDataPivot = new Dictionary<MeasureSet, List<Dictionary<string, PropertyValue>>>();

            foreach (var record in canonicalDeliveryData.DeliveryDataForNetwork)
            {
                // Accumulate delivered periods
                var allocationId = record[RawDeliveryDataParserBase.AllocationIdFieldName];
                var measureSet = nodeMap[allocationId];

                // If this is the first data we are adding for this node initialize
                // the dictionary entry for the node with an empty collection.
                if (!deliveryDataPivot.ContainsKey(measureSet))
                {
                    deliveryDataPivot[measureSet] = new List<Dictionary<string, PropertyValue>>();
                }

                deliveryDataPivot[measureSet].Add(record);
            }

            // Last reported hour is the one before the report dead zone starts
            var validHourOffset = new TimeSpan(1, 0, 0) + this.ReportDeadZone;
            var lastValidReportHour = canonicalDeliveryData.LatestDeliveryReportDate;
            if (DateTime.MinValue + validHourOffset < lastValidReportHour)
            {
                lastValidReportHour -= validHourOffset;
            }

            // Iterate over all the measures in the node map updating the metrics
            // for nodes with eligibility
            foreach (var node in nodeMap)
            {
                var measureSet = node.Value;

                // Make sure delivery data at least has an empty collection in case there is
                // eligibility but no delivery.
                var perNodeDeliveryData = new List<Dictionary<string, PropertyValue>>();
                if (deliveryDataPivot.ContainsKey(measureSet))
                {
                    perNodeDeliveryData = deliveryDataPivot[measureSet];
                }

                // Don't update if there is no eligibility or delivered data
                var hasHistory = eligibilityHistoryBuilder.EligibilityHistory.ContainsKey(measureSet)
                                && eligibilityHistoryBuilder.EligibilityHistory[measureSet].Any();

                if (!hasHistory && !perNodeDeliveryData.Any())
                {
                    continue;
                }

                // Build a new node delivery metrics if one did not previously exist.
                if (!this.NodeMetricsCollection.ContainsKey(measureSet))
                {
                    this.NodeMetricsCollection[measureSet] = new NodeDeliveryMetrics();
                }

                var nodeMetrics = this.NodeMetricsCollection[measureSet];
                this.UpdateNodeMetrics(
                    nodeMetrics,
                    measureSet,
                    perNodeDeliveryData,
                    lastValidReportHour,
                    canonicalDeliveryData.LatestDeliveryDataDate,
                    GetCampaignDeliveryDataActivity.HistoryLookBack,
                    eligibilityHistoryBuilder);
            }

            // Aggregate the totals based on the entire NodeMetricsCollection
            var totalMediaSpend = this.NodeMetricsCollection.Sum(m => m.Value.TotalMediaSpend);
            var totalSpend = this.NodeMetricsCollection.Sum(m => m.Value.TotalSpend);

            // Calculate lifetime media budget. Project as a percentage of total budget
            // based on the ratio to-date of media spend to total spend. If totalSpend
            // is zero set the budget cap to the totalBudget (default until there is data
            // to make a projection).
            var lifetimeMediaBudgetCap = totalBudget;
            if (totalSpend != 0)
            {
                lifetimeMediaBudgetCap = Math.Round(totalBudget * totalMediaSpend / totalSpend, 2);
            }

            this.LifetimeMediaBudgetCap = lifetimeMediaBudgetCap;

            // Round to two places on return and return 0 if negative
            var remainingBudget = Math.Round(totalBudget - totalSpend, 2);
            this.RemainingBudget = Math.Max(0, remainingBudget);
        }

        /// <summary>Apply a lookback making sure it is not an invalid DateTime</summary>
        /// <param name="lookBackDuration">The look back duration.</param>
        /// <param name="lookBackStart">The lookback start.</param>
        /// <returns>The new date with lookback subtracted, or DateTime.MinValue.</returns>
        internal static DateTime ApplyLookBack(TimeSpan lookBackDuration, DateTime lookBackStart)
        {
            var newStart = lookBackStart;
            if (DateTime.MinValue + lookBackDuration < newStart)
            {
                newStart -= lookBackDuration;
            }

            return newStart;
        }

        /// <summary>Round a datetime to an hour bucket.</summary>
        /// <param name="fullDate">The full date.</param>
        /// <returns>The hour bucketized date.</returns>
        internal static DateTime GetUtcHourBucket(DateTime fullDate)
        {
            return new DateTime(
                fullDate.Year,
                fullDate.Month,
                fullDate.Day,
                fullDate.Hour,
                0,
                0,
                DateTimeKind.Utc);
        }

        /// <summary>Get an index into a 168 hour array representing seven days of delivery starting 00:00 Sunday.</summary>
        /// <param name="deliveryHour">The utc delivery hour.</param>
        /// <returns>An index into the array.</returns>
        internal static int GetProfileHourIndex(DateTime deliveryHour)
        {
            var profileHour = ((int)deliveryHour.DayOfWeek * 24) + deliveryHour.Hour;
            return profileHour;
        }

        /// <summary>
        /// Filter eligibility history to include only eligibility history entries with
        /// unprocessed hours.
        /// </summary>
        /// <param name="eligibilityHistory">The eligibility history.</param>
        /// <param name="lastProcessedEligibilityHour">The last processed hour of eligibility.</param>
        /// <param name="lastEligibleHour">The last eligible hour in the current data being processed.</param>
        /// <returns>Filtered eligibility history.</returns>
        internal static IEnumerable<EligibilityPeriod> GetUnprocessedElibilityHistory(
            List<EligibilityPeriod> eligibilityHistory,
            DateTime lastProcessedEligibilityHour,
            DateTime lastEligibleHour)
        {
            return eligibilityHistory.Where(ep =>
                ep.EligibilityEnd > lastProcessedEligibilityHour &&
                ep.EligibilityStart <= lastEligibleHour);
        }

        /// <summary>Create a list of unprocessed eligible hours.</summary>
        /// <param name="lastValidEligibilityHour">The last valid hour of unprocessed eligibility.</param>
        /// <param name="lastProcessedEligibilityHour">The last processed hour of eligibility.</param>
        /// <param name="unprocessedElibilityHistory">The unprocessed elibility history.</param>
        /// <returns>A list of hour buckets that are eligible but unprocessed.</returns>
        internal static List<DateTime> GetUnprocessedEligibleHours(
            DateTime lastValidEligibilityHour, 
            DateTime lastProcessedEligibilityHour, 
            IEnumerable<EligibilityPeriod> unprocessedElibilityHistory)
        {
            var unprocessedEligibleHours = new List<DateTime>();
            foreach (var ep in unprocessedElibilityHistory)
            {
                // later of the hour after lastProcessedEligibilityHour or EligibilityStart
                var firstEligibleHourOfPeriod = ep.EligibilityStart <= lastProcessedEligibilityHour
                                        ? lastProcessedEligibilityHour.AddHours(1)
                                        : ep.EligibilityStart;

                // earlier of EligibilityEnd or lastEligibleHour
                var lastEligibleHourOfPeriod = ep.EligibilityEnd <= lastValidEligibilityHour
                                        ? ep.EligibilityEnd 
                                        : lastValidEligibilityHour;

                for (var hour = firstEligibleHourOfPeriod; hour <= lastEligibleHourOfPeriod; hour = hour.AddHours(1))
                {
                    unprocessedEligibleHours.Add(hour);
                }
            }

            // Return only de-duped result
            return unprocessedEligibleHours.Distinct().ToList();
        }

        /// <summary>Update the metrics carried forward for a given node.</summary>
        /// <param name="nodeMetrics">The delivery metrics for a single node.</param>
        /// <param name="measureSet">The measure set of the node.</param>
        /// <param name="perNodeDeliveryData">A subset of delivery data for the measure set.</param>
        /// <param name="lastValidReportHour">Last hour of the period covered by the latest report.</param>
        /// <param name="lastCampaignDeliveryHour">Last reported hour of actual delivery for campaign.</param>
        /// <param name="lookBackDuration">look back duration.</param>
        /// <param name="eligibilityHistoryBuilder">The node delivery eligibility history.</param>
        internal void UpdateNodeMetrics(
            NodeDeliveryMetrics nodeMetrics, 
            MeasureSet measureSet, 
            List<Dictionary<string, PropertyValue>> perNodeDeliveryData, 
            DateTime lastValidReportHour, 
            DateTime lastCampaignDeliveryHour,
            TimeSpan lookBackDuration,
            IEligibilityHistoryBuilder eligibilityHistoryBuilder)
        {
            // Capture the last eligible hour and last delivery hour that have
            // already been aggregated into the node metrics
            var lastProcessedEligibilityHour = nodeMetrics.LastProcessedEligibilityHour;
            var lastProcessedDeliveryHour = nodeMetrics.LastProcessedDeliveryHour;

            // Use the later of the last delivery hour for the campaign and the
            // last valid hour report hour less the lookback to mark the end of when we consider
            // non-delivery significant.
            var lastValidEligibilityHour = lastCampaignDeliveryHour;
            var lookBackHour = ApplyLookBack(lookBackDuration, lastValidReportHour);
            if (lookBackHour > lastValidEligibilityHour)
            {
                lastValidEligibilityHour = lookBackHour;
            }

            // If there is no history there should be nothing to do but allow the
            // possibility of delivery data when history is incorrect.
            var eligibilityHistory = new List<EligibilityPeriod>();
            if (eligibilityHistoryBuilder.EligibilityHistory.ContainsKey(measureSet))
            {
                eligibilityHistory = eligibilityHistoryBuilder.EligibilityHistory[measureSet];
            }

            // Filter eligibility history to include only new eligibility info.
            var unprocessedElibilityHistory = GetUnprocessedElibilityHistory(
                eligibilityHistory, lastProcessedEligibilityHour, lastValidEligibilityHour).ToList();

            // Create a list of unprocessed eligible hours
            var unprocessedEligibleHours = GetUnprocessedEligibleHours(
                lastValidEligibilityHour, lastProcessedEligibilityHour, unprocessedElibilityHistory);

            // Filter the delivery data to exclude data already processed as well as any data that should be
            // excluded from the end of the most recent report. Process earliest to latest.
            var unprocessedDeliveryData = perNodeDeliveryData.Where(r => 
                    (DateTime)r[RawDeliveryDataParserBase.HourFieldName] > lastProcessedDeliveryHour
                    && (DateTime)r[RawDeliveryDataParserBase.HourFieldName] <= lastValidEligibilityHour)
                    .OrderBy(r => (DateTime)r[RawDeliveryDataParserBase.HourFieldName]);

            // Calculate per-hour metrics for delivered hours
            foreach (var deliveryRecord in unprocessedDeliveryData)
            {
                var deliveryHour = (DateTime)deliveryRecord[RawDeliveryDataParserBase.HourFieldName];
                var impressions = (long)deliveryRecord[RawDeliveryDataParserBase.ImpressionsFieldName];
                var mediaSpend = (decimal)deliveryRecord[RawDeliveryDataParserBase.MediaSpendFieldName];
                this.UpdateNodeMetricsForHour(ref nodeMetrics, impressions, mediaSpend, deliveryHour, measureSet);
            }

            // Calculate per-hour metrics for zero-delivery hours. Process earliest to latest.
            var zeroDeliveryEligibleHours = unprocessedEligibleHours.Except(unprocessedDeliveryData.Select(
                    deliveryRecord => (DateTime)deliveryRecord[RawDeliveryDataParserBase.HourFieldName]))
                    .OrderBy(r => r);
            foreach (var zeroDeliveryEligibleHour in zeroDeliveryEligibleHours)
            {
                this.UpdateNodeMetricsForHour(ref nodeMetrics, 0L, 0m, zeroDeliveryEligibleHour, measureSet);
            }

            // Update the total number of eligible hours for the node. This is the zero delivery eligible
            // hours plus the hours for which we have a new delivery record
            nodeMetrics.TotalEligibleHours = nodeMetrics.DeliveryProfile.Sum(h => h.Value.EligibilityCount);

            // Update the last processed delivery hour
            if (unprocessedDeliveryData.Any())
            {
                nodeMetrics.LastProcessedDeliveryHour = unprocessedDeliveryData.Max(
                    deliveryRecord => (DateTime)deliveryRecord[RawDeliveryDataParserBase.HourFieldName]);
            }

            // Update the last processed eligibility hour
            if (unprocessedElibilityHistory.Any())
            {
                var lastEligibilityHour = unprocessedElibilityHistory.Max(e => e.EligibilityEnd);
                nodeMetrics.LastProcessedEligibilityHour = lastEligibilityHour > lastValidEligibilityHour
                                                               ? lastValidEligibilityHour
                                                               : lastEligibilityHour;
            }
        }

        /// <summary>Update node metrics for a given hour.</summary>
        /// <param name="nodeMetrics">The node metrics collection.</param>
        /// <param name="impressions">The impressions for the hour.</param>
        /// <param name="mediaSpend">The media spend for the hour.</param>
        /// <param name="deliveryHour">The delivery hour.</param>
        /// <param name="measureSet">The measure set of the node.</param>
        internal void UpdateNodeMetricsForHour(
            ref NodeDeliveryMetrics nodeMetrics, 
            long impressions, 
            decimal mediaSpend, 
            DateTime deliveryHour, 
            MeasureSet measureSet)
        {
            // If we have never seen this hour initialize the hour metrics object
            if (!nodeMetrics.DeliveryProfile.ContainsKey(GetProfileHourIndex(deliveryHour)))
            {
                nodeMetrics.DeliveryProfile[GetProfileHourIndex(deliveryHour)] = new NodeHourMetrics();
            }

            var hourMetrics = nodeMetrics.DeliveryProfile[GetProfileHourIndex(deliveryHour)];
            
            UpdateNodeHour(ref hourMetrics, impressions, mediaSpend, deliveryHour);

            // Update the accumulators on the NodeDeliveryMetrics object
            nodeMetrics.TotalImpressions += impressions;
            nodeMetrics.TotalMediaSpend += mediaSpend;
            nodeMetrics.TotalSpend += this.DeliveryDataCost.CalculateHourCost(impressions, mediaSpend, measureSet);
        }

        /// <summary>Update the hour averages</summary>
        /// <param name="hourMetrics">The hour metrics.</param>
        /// <param name="impressions">The impressions for the hour.</param>
        /// <param name="mediaSpend">The media spend for the hour.</param>
        /// <param name="hour">The hour to update</param>
        private static void UpdateNodeHour(
            ref NodeHourMetrics hourMetrics,
            long impressions,
            decimal mediaSpend,
            DateTime hour)
        {
            // If the last update was for the same delivery hour assume it was delayed report data
            // that should replace the previous entry.
            var overWrite = hourMetrics.LastAddedHour == hour
                && hourMetrics.LastNImpressions.Count > 0;

            // EligibilityCount doesn't increment if this is an overwrite
            var newEligibilityCount = overWrite ? hourMetrics.EligibilityCount : hourMetrics.EligibilityCount + 1;

            // Back into impressions and spend for the hour and update the average
            hourMetrics.AverageImpressions = ((hourMetrics.AverageImpressions * hourMetrics.EligibilityCount) + impressions)
                                         / newEligibilityCount;
            hourMetrics.AverageMediaSpend = ((hourMetrics.AverageMediaSpend * hourMetrics.EligibilityCount) + mediaSpend)
                                        / newEligibilityCount;
            hourMetrics.EligibilityCount = newEligibilityCount;

            hourMetrics.LastAddedHour = hour;

            if (overWrite)
            {
                hourMetrics.LastNImpressions[0] = impressions;
                hourMetrics.LastNMediaSpend[0] = mediaSpend;
                return;
            }

            // Remember up to LastNMax impressions and spend values
            hourMetrics.AddToLastNImpressions(impressions);
            hourMetrics.AddToLastNMediaSpend(mediaSpend);
        }
    }
}
