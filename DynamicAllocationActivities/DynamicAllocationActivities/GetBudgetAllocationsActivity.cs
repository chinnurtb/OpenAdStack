// -----------------------------------------------------------------------
// <copyright file="GetBudgetAllocationsActivity.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using ScheduledActivities;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Activity for getting budget allocations
    /// </summary>
    /// <remarks>
    /// Gets budget allocations for the provided inputs
    /// RequiredValues:
    ///   CompanyEntityId - EntityId of the company
    ///   CampaignEntityId - EntityId of the campaign
    ///   AllocationStartDate - Start date for the allocation period
    ///   IsInitialAllocation - Whether the allocation is initial (vs reallocation)
    /// ResultValues:
    ///   CampaignEntityId - EntityId of the campaign allocated for
    /// </remarks>
    [Name(DynamicAllocationActivityTasks.GetBudgetAllocations)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId,
        DynamicAllocationActivityValues.AllocationStartDate,
        DynamicAllocationActivityValues.IsInitialAllocation)]
    [ResultValues(EntityActivityValues.CampaignEntityId)]
    public class GetBudgetAllocationsActivity : DynamicAllocationActivity
    {
        /// <summary>
        /// Default time after the campaign ends to schedule cleanup
        /// </summary>
        /// <remarks>Override with DynamicAllocation.CleanupDelay</remarks>
        private const string DefaultCleanupDelayTime = "4.00:00:00";

        /// <summary>Gets the ExportCampaignToAppNexusActivity name</summary>
        internal static string ExportCampaignToAppNexusTaskName
        {
            get { return "APNXExportDACampaign"; }
        }

        /// <summary>
        /// Gets the reallocation schedule.
        /// The schedule is a series of time offsets from the campaign
        /// start time at which reallocations are to be scheduled each day.
        /// </summary>
        private static TimeSpan[] ReallocationSchedule
        {
            get
            {
                return Config.GetTimeSpanValues("DynamicAllocation.ReallocationSchedule");
            }
        }

        /// <summary>
        /// Gets the time after a campaign ends to schedule it for clean
        /// </summary>
        private static TimeSpan PostCampaignCleanupDelay
        {
            get
            {
                try
                {
                    return Config.GetTimeSpanValue("DynamicAllocation.CleanupDelay");
                }
                catch (ArgumentException)
                {
                    return TimeSpan.Parse(DefaultCleanupDelayTime, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>Schedules the campaign for cleanup</summary>
        /// <param name="dac">An IDynamicAllocationCampaign instance.</param>
        /// <returns>True if cleanup was successfully scheduled; otherwise, false.</returns>
        internal static bool ScheduleForCleanup(IDynamicAllocationCampaign dac)
        {
            var company = dac.CompanyEntity;
            var campaign = dac.CampaignEntity;
            var deliveryNetwork = dac.DeliveryNetwork;

            LogManager.Log(
                LogLevels.Trace,
                "Scheduling cleanup for campaign '{0}' ({1})...",
                campaign.ExternalName,
                campaign.ExternalEntityId);

            if (dac.DeliveryNetwork == DeliveryNetworkDesignation.Unknown)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to schedule cleanup for campaign '{0}' ({1}). Delivery Network missing/invalid.");
                return false;
            }

            var cleanupDateTime = ((DateTime)campaign.EndDate) + PostCampaignCleanupDelay;
            return Scheduler.AddToSchedule(
                DeliveryNetworkSchedulerRegistries.CampaignsToCleanup,
                cleanupDateTime,
                campaign.ExternalEntityId.ToString(),
                company.ExternalEntityId.ToString(),
                deliveryNetwork);
        }

        /// <summary>Schedules the next reallocation or cleanup</summary>
        /// <remarks>
        /// If the next allocation period would start after the campaign end date
        /// then the campaign is scheduled for cleanup rather than reallocation
        /// </remarks>
        /// <param name="dac">An IDynamicAllocationCampaign instance.</param>
        /// <param name="immediate">Whether to schedule for immediate reallocation</param>
        /// <param name="reallocationType">The type of reallocation being scheduled</param>
        /// <param name="currentTime">the current time</param>
        /// <returns>the time of the next reallocation</returns>
        internal static DateTime ScheduleNextReallocation(
            IDynamicAllocationCampaign dac,
            bool immediate,
            ReallocationScheduleType reallocationType,
            DateTime currentTime)
        {
            var company = dac.CompanyEntity;
            var campaign = dac.CampaignEntity;
            var allocationParameters = dac.AllocationParameters;

            // Clear any previously scheduled reallocations for the campaign
            Scheduler.RemoveFromSchedule<string, DateTime, bool>(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate,
                campaign.ExternalEntityId.ToString());

            // Get the next time for scheduling reallocation
            var nextReallocation =
                immediate ? currentTime : // right "now"
                reallocationType == ReallocationScheduleType.FirstReallocation ?
                    //// 1st reallocation is right at the end of the initialization phase
                    //// which is InitialAllocationTotalPeriodDuration after the latter of start date or "now"
                    (currentTime > campaign.StartDate ? currentTime : (DateTime)campaign.StartDate) +
                    allocationParameters.InitialAllocationTotalPeriodDuration :
                FindNextReallocation(campaign, currentTime); // the next time in the regular reallocation schedule

            // Do not schedule for reallocation if the reallocation would occur after the campaign end date.
            // This is how the reallocation chain is terminated.
            if (nextReallocation >= campaign.EndDate)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Not scheduling campaign '{0}' ({1}) for reallocation. Next reallocation ({2}) would be after the campaign end date ({3}).",
                    campaign.ExternalName,
                    campaign.ExternalEntityId,
                    nextReallocation,
                    campaign.EndDate);

                // Schedule for cleanup.
                if (!ScheduleForCleanup(dac))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "An error occured while scheduling cleanup for campaign '{0}' ({1})",
                        campaign.ExternalName,
                        campaign.ExternalEntityId);
                }

                return campaign.EndDate;
            }

            // The allocation period for the scheduled reallocation is either the same as when reallocation
            // is scheduled or the campaign start date (if is to be scheduled before the start date)
            var allocationPeriodStart =
                nextReallocation > (DateTime)campaign.StartDate ?
                    nextReallocation :
                    (DateTime)campaign.StartDate;

            LogManager.Log(
                LogLevels.Trace,
                "Scheduling campaign '{0}' ({1}) for reallocation at {2} for allocation period starting {3}",
                campaign.ExternalName,
                campaign.ExternalEntityId,
                Scheduler.GetTimeSlotKey(nextReallocation),
                allocationPeriodStart);

            // Add the reallocation entry to the schedule for the campaign at the next
            // reallocation time for the determined allocation period start.
            Scheduler.AddToSchedule(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate,
                nextReallocation,
                campaign.ExternalEntityId.ToString(),
                company.ExternalEntityId.ToString(),
                allocationPeriodStart,
                reallocationType == ReallocationScheduleType.Initial);

            return allocationPeriodStart;
        }

        /// <summary>
        /// Finds the next reallocation time for the campaign per the reallocation schedule.
        /// </summary>
        /// <remarks>
        /// This assumes that the campaign start date's time-of-day is the first reallocation
        /// to take place on each day. The schedule's first entry is expected to be 00:00:00.
        /// While seemingly redundant, this is included to maintain parity between the number
        /// of schedule entries and the expected number of reallocations scheduled each day.
        /// </remarks>
        /// <param name="campaign">The campaign entity</param>
        /// <param name="now">the current time</param>
        /// <returns>The next reallocation time</returns>
        internal static DateTime FindNextReallocation(CampaignEntity campaign, DateTime now)
        {
            // TODO: account for what if this is being scheduled from the allocation of a new initialization
            var campaignStartDate = (DateTime)campaign.StartDate;
            
            // Snap to campaign start date if it is in the future
            // TODO: Not so? If get budget allocations is running before campaign start date then this should possibly
            // be the next reallocation after the initialization period
            if (now < campaignStartDate)
            {
                return campaignStartDate;
            }

            // TODO: This works for regular reallocations...
            // Find the next, future reallocation time based upon the the
            // campaign start date time of day and the reallocation schedule.
            var campaignStartTime = campaignStartDate.TimeOfDay;
            var nextReallocation = now.Date + campaignStartTime;
            var scheduleEntry = 0;
            while (true)
            {
                // Check if the time slot key for nextReallocation is in the future.
                var nowTimesSlotKey = Scheduler.GetTimeSlotKey(now);
                var nextReallocationTimeSlotKey = Scheduler.GetTimeSlotKey(nextReallocation);
                if (string.CompareOrdinal(nextReallocationTimeSlotKey, nowTimesSlotKey) > 0)
                {
                    // nextReallocation is in the future, return it.
                    return nextReallocation;
                }

                // Get the next reallocation per the schedule
                if (++scheduleEntry < ReallocationSchedule.Length)
                {
                    nextReallocation =
                        nextReallocation.Date +
                        campaignStartTime +
                        ReallocationSchedule[scheduleEntry];
                }
                else
                {
                    // No more reallocations this day. Go to the next day.
                    scheduleEntry = 0;
                    nextReallocation =
                        nextReallocation.Date.AddDays(1) +
                        campaignStartTime;
                }
            }
        }

        /// <summary>
        /// Creates a comma delimited string of allocation IDs to export
        /// </summary>
        /// <param name="perNodeResults">the per node results</param>
        /// <returns>the comma delimited string of allocation IDs to export</returns>
        internal static string CreateAllocationIdsString(List<PerNodeBudgetAllocationResult> perNodeResults)
        {
            // create a list of the nodes to export 
            var exportAllocationIds = string.Join(
                ",",
                perNodeResults
                    .Where(pnr => pnr.ExportBudget > 0)
                    .Select(pnr => pnr.AllocationId));
            return exportAllocationIds;
        }

        /// <summary>
        /// Creates a comma delimited string of allocation IDs to export
        /// </summary>
        /// <param name="budgetAllocationOutputs">the budget allocation outputs</param>
        /// <param name="numberOfExports">the number of lists to return</param>
        /// <returns>the comma delimited string of allocation IDs to export</returns>
        internal static List<Dictionary<MeasureSet, decimal>> CreateListsOfMeasureSetsToExport(BudgetAllocation budgetAllocationOutputs, int numberOfExports)
        {
            var measureSetExportBudgets = budgetAllocationOutputs.PerNodeResults
                .Where(pnr => pnr.Value.ExportBudget > 0)
                .ToDictionary(pnr => pnr.Key, pnr => pnr.Value.ExportBudget);

            var segmentCount = measureSetExportBudgets.Count / numberOfExports;
            if (measureSetExportBudgets.Count % numberOfExports > 0)
            {
                segmentCount += 1;
            }

            var exportMeasureSetsAndBudgets = new List<Dictionary<MeasureSet, decimal>>();
            for (var i = 0; i < numberOfExports; i++)
            {
                var exportSets = measureSetExportBudgets.Skip(i * segmentCount).Take(segmentCount).ToDictionary();
                exportMeasureSetsAndBudgets.Add(exportSets);
            }

            return exportMeasureSetsAndBudgets;
        }

        /// <summary>
        /// Calculates the value-volume score per node and adds it to the PerNodeResults
        /// </summary>
        /// <param name="newAllocationInputs">The new budget allocation inputs.</param>
        /// <param name="activeAllocation">The previous budget allocation.</param>
        internal static void AddValueVolumeScoreToAllocations(
            ref BudgetAllocation newAllocationInputs, 
            BudgetAllocation activeAllocation)
        {
            newAllocationInputs.ValueVolumeScore = 0;
            var previousPeriodDuration = (int)activeAllocation.PeriodDuration.TotalHours;
            foreach (var newNodeResult in newAllocationInputs.PerNodeResults)
            {
                var measureSet = newNodeResult.Key;

                var previousEffectiveImpressions = 0L;
                if (newAllocationInputs.NodeDeliveryMetricsCollection.ContainsKey(measureSet))
                {
                    previousEffectiveImpressions = newAllocationInputs.NodeDeliveryMetricsCollection[measureSet].CalcEffectiveImpressions(
                        previousPeriodDuration);
                }

                var previousValuation = 0m;
                if (activeAllocation.PerNodeResults.ContainsKey(measureSet))
                {
                    previousValuation = activeAllocation.PerNodeResults[measureSet].Valuation;
                }

                var valueVolumeScore = previousValuation * previousEffectiveImpressions / 1000;
                newAllocationInputs.ValueVolumeScore += valueVolumeScore;
            }
        }

        /// <summary>
        /// Adds allocation IDs to the perNodeResults
        /// </summary>
        /// <param name="budgetAllocationOutputs">the budget allocation outputs</param>
        /// <param name="allocationNodeMap">the allocation ID to node map</param>
        internal static void AddAllocationIdToAllocations(ref BudgetAllocation budgetAllocationOutputs, ref Dictionary<string, MeasureSet> allocationNodeMap)
        {
            // reverse the dictionary
            // this will obliterate data if a measure set has more than one ID, which it shouldn't
            var nodeAllocationMap = allocationNodeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            foreach (var nodeResult in budgetAllocationOutputs.PerNodeResults)
            {
                // lookup AllocationID for measureSet in the lookup table (assuming it exists) otherwise create id (guid string)
                // changes to the format of allocation ids must be reflected in the validation
                // done in ExportDynamicAllocationCampaign2.GetAllocationIdFromCampaignName
                var allocationId = nodeAllocationMap.ContainsKey(nodeResult.Key) ? nodeAllocationMap[nodeResult.Key] : Guid.NewGuid().ToString("N");
            
                allocationNodeMap[allocationId] = nodeResult.Key;
                nodeResult.Value.AllocationId = allocationId;
            }
        }

        /// <summary>Transfer select delivery metrics to the per node results for serialization out.</summary>
        /// <param name="perNodeResults">The allocation node results collection to update.</param>
        /// <param name="nodeDeliveryMetricsCollection">The node delivery metrics collection.</param>
        internal static void UpdateDeliveryMetricsForSerialization(
            Dictionary<MeasureSet, PerNodeBudgetAllocationResult> perNodeResults,
            Dictionary<MeasureSet, IEffectiveNodeMetrics> nodeDeliveryMetricsCollection)
        {
            foreach (var nodeResultElement in perNodeResults)
            {
                var measureSet = nodeResultElement.Key;
                var nodeResult = nodeResultElement.Value;

                // Set default values
                var lifetimeMediaSpend = 0m;
                var lifetimeImpressions = 0L;
                var effectiveMediaSpendRate = 0m;
                var effectiveImpressionRate = 0m;

                // Get values from delivery metrics if present
                if (nodeDeliveryMetricsCollection.ContainsKey(measureSet))
                {
                    var nodeMetrics = nodeDeliveryMetricsCollection[measureSet];

                    lifetimeMediaSpend = nodeMetrics.CalcEffectiveMediaSpend(IEffectiveNodeMetrics.LifetimeLookBack);
                    lifetimeImpressions = nodeMetrics.CalcEffectiveImpressions(IEffectiveNodeMetrics.LifetimeLookBack);
                    effectiveMediaSpendRate = nodeMetrics.CalcEffectiveMediaSpendRate();
                    effectiveImpressionRate = nodeMetrics.CalcEffectiveImpressionRate();
                }

                // Update the node result
                nodeResult.LifetimeMediaSpend = Math.Round(lifetimeMediaSpend, 6);
                nodeResult.LifetimeImpressions = lifetimeImpressions;
                nodeResult.EffectiveMediaSpendRate = Math.Round(effectiveMediaSpendRate, 6);
                nodeResult.EffectiveImpressionRate = Math.Round(effectiveImpressionRate, 6);
            }
        }
      
        /// <summary>
        /// Filters the export budgets in the budgetAllocation according to the exportBudgets dictionary
        /// and splits the budget since each measureSet is exported twice
        /// </summary>
        /// <param name="budgetAllocation">teh budget allocation</param>
        /// <param name="exportBudgets">the filter export budgets</param>
        /// <param name="isIntialAllocation">indicates if this is part of the intialAllocation</param>
        internal static void CreateExportAllocation(
            ref BudgetAllocation budgetAllocation, 
            Dictionary<MeasureSet, decimal> exportBudgets,
            bool isIntialAllocation)
        {
            // TODO: this budget divisor depends on the way we do intial allocation 
            // and should be encapsulated in the same place that that aspect of intial allocation is
            var budgetDivisor = isIntialAllocation ? 2 : 1;

            foreach (var perNodeResult in budgetAllocation.PerNodeResults)
            {
                decimal exportBudget;
                exportBudgets.TryGetValue(perNodeResult.Key, out exportBudget);
                perNodeResult.Value.ExportBudget = exportBudget / budgetDivisor;
            }
        }

        /// <summary>
        /// Create a list of exports to schedule consisting of 1) Export time, 2) Campaign EntityId, 3) CompanyEntityId+ExportAllocationIdsList, 4) Export measure sets
        /// </summary>
        /// <param name="campaignEntityId">the campaign entity Id</param>
        /// <param name="companyEntityId">the company entity Id</param>
        /// <param name="allocationParameters">the allocation parameters</param>
        /// <param name="budgetAllocationOutputs">the budget allocation outputs</param>
        /// <param name="isIntitialAllocation">is intial allocation flag</param>
        /// <param name="periodStart">the current time</param>
        /// <returns>list of exports to schedule</returns>
        internal static List<Tuple<DateTime, string, Tuple<string, string>, Dictionary<MeasureSet, decimal>>> GetExportSchedule(
            EntityId campaignEntityId,
            EntityId companyEntityId,
            AllocationParameters allocationParameters,
            BudgetAllocation budgetAllocationOutputs,
            bool isIntitialAllocation,
            DateTime periodStart)
        {
            var exports = new List<Tuple<DateTime, string, Tuple<string, string>, Dictionary<MeasureSet, decimal>>>();

            if (!isIntitialAllocation)
            {
                // schedule for export at the period start
                var exportMeasureSets = CreateListsOfMeasureSetsToExport(budgetAllocationOutputs, 1);
                var exportAllocationIdsList = CreateAllocationIdsString(
                    budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value).ToList());
                exports.Add(
                    new Tuple<DateTime, string, Tuple<string, string>, Dictionary<MeasureSet, decimal>>(
                        periodStart,
                        campaignEntityId.ToString(),
                        new Tuple<string, string>(
                            companyEntityId.ToString(),
                            exportAllocationIdsList),
                        exportMeasureSets[0]));

                return exports;
            }

            // If this is the initial allocation, split the allocation in groups and set them to export according to the schcedule
            // IntialAllocationSinglePeriodLength should be divisible by InitialAllocationTotalPeriodLength
            var totalTicks = allocationParameters.InitialAllocationTotalPeriodDuration.Ticks;
            var periodTicks = allocationParameters.InitialAllocationSinglePeriodDuration.Ticks;
            var numberOfExports = (int)(totalTicks / periodTicks);

            var exportMeasureSetsLists = CreateListsOfMeasureSetsToExport(budgetAllocationOutputs, numberOfExports);
            var exportAllocationIdsLists = exportMeasureSetsLists.Select(
                msl => CreateAllocationIdsString(
                    budgetAllocationOutputs
                        .PerNodeResults
                        .Where(pnr => msl.ContainsKey(pnr.Key))
                        .Select(pnr => pnr.Value)
                        .ToList()))
                .ToList();

            // schedule the first one for export at period start, the rest for increments after that.
            // schedule each allocation set to be exported twice, 12 hours apart.
            var periodLength = new TimeSpan(periodTicks / 2);
            var count = exportAllocationIdsLists.Count();
            for (var i = 0; i < count * 2; i++)
            {
                exports.Add(
                    new Tuple<DateTime, string, Tuple<string, string>, Dictionary<MeasureSet, decimal>>(
                        periodStart + TimeSpan.FromTicks(periodLength.Ticks * i),
                        campaignEntityId.ToString(),
                        new Tuple<string, string>(
                            companyEntityId.ToString(),
                            exportAllocationIdsLists[i % count]),
                        exportMeasureSetsLists[i % count]));
            }

            return exports;
        }

        /// <summary>Determine if a budget allocation represents initial allocation.</summary>
        /// <param name="budgetAllocation">The budget allocation to test.</param>
        /// <returns>True if this is an initial allocation.</returns>
        internal static bool IsInitialAllocation(BudgetAllocation budgetAllocation)
        {
            if (budgetAllocation.PerNodeResults == null)
            {
                return true;
            }

            return budgetAllocation.PerNodeResults.All(pnr => pnr.Value.ExportCount == 0);
        }

        /// <summary>Attempts to get the node metrics for the campaign</summary>
        /// <param name="campaignEntity">The campaign entity</param>
        /// <returns>Node delivery metrics collection. Empty if not found. Null if there is an error.</returns>
        ////[SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern. Exceptions are wrapped as error results")]
        internal static Dictionary<MeasureSet, IEffectiveNodeMetrics> GetNodeDeliveryMetrics(
            CampaignEntity campaignEntity)
        {
            var serializedNodeMetrics =
                campaignEntity.TryGetPropertyValueByName(daName.AllocationNodeMetrics);

            var nodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>();
            if (serializedNodeMetrics == null)
            {
                // The property will not initially be present on the entity.
                var msg = "Node delivery metrics not found: Entity ID - {0}."
                    .FormatInvariant((string)(EntityId)campaignEntity.ExternalEntityId);
                LogManager.Log(LogLevels.Information, msg);
                return nodeDeliveryMetricsCollection;
            }

            try
            {
                // Cast the dictionary to a Dictionary<MeasureSet, INodeDeliveryMetrics>
                nodeDeliveryMetricsCollection = 
                    AppsJsonSerializer.DeserializeObject<Dictionary<MeasureSet, NodeDeliveryMetrics>>(serializedNodeMetrics)
                    .ToDictionary(kvp => kvp.Key, kvp => (IEffectiveNodeMetrics)kvp.Value);
            }
            catch (Exception e)
            {
                var msg = "Node delivery metrics could not be deserialized: Entity ID - {0}."
                    .FormatInvariant((string)(EntityId)campaignEntity.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg, e);
            }

            return nodeDeliveryMetricsCollection;
        }

        /// <summary>
        /// Build default initialized budget allocation inputs (for initial allocation),
        /// or fully populated inputs for reallocation.
        /// </summary>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <param name="activeAllocation">The active allocation.</param>
        /// <param name="valuations">The current valuations.</param>
        /// <param name="nodeDeliveryMetricsCollection">The node delivery metrics.</param>
        /// <param name="allocationParameters">Allocation parameters for DA.</param>
        /// <returns>The budget allocation inputs or null if there is an error.</returns>
        internal static BudgetAllocation BuildBudgetAllocationInputs(
            CampaignEntity campaignEntity,
            BudgetAllocation activeAllocation,
            IDictionary<MeasureSet, decimal> valuations,
            Dictionary<MeasureSet, IEffectiveNodeMetrics> nodeDeliveryMetricsCollection,
            AllocationParameters allocationParameters)
        {
            // Set default values for budget allocation.
            // Default remaining budget is the total budget.
            var budgetAllocation = new BudgetAllocation();
            budgetAllocation.CampaignStart = campaignEntity.StartDate;
            budgetAllocation.CampaignEnd = campaignEntity.EndDate;
            budgetAllocation.RemainingBudget = campaignEntity.Budget;
            budgetAllocation.PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>();
            budgetAllocation.NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>();
            budgetAllocation.AllocationParameters = allocationParameters;

            // Transfer values from active allocation
            if (activeAllocation.PerNodeResults != null)
            {
                foreach (var perNodeResult in activeAllocation.PerNodeResults)
                {
                    var newPerNodeResult = new PerNodeBudgetAllocationResult
                        {
                            ExportCount = perNodeResult.Value.ExportCount
                        };
                    budgetAllocation.PerNodeResults[perNodeResult.Key] = newPerNodeResult;
                }
            }

            // add the valuation to the budgetAllocation
            foreach (var valuation in valuations)
            {
                if (!budgetAllocation.PerNodeResults.ContainsKey(valuation.Key))
                {
                    budgetAllocation.PerNodeResults[valuation.Key] = new PerNodeBudgetAllocationResult();
                }

                budgetAllocation.PerNodeResults[valuation.Key].Valuation = valuation.Value;
            }

            // If this is an initial allocation based on the state of the budget allocation
            // so far, we are done.
            if (IsInitialAllocation(budgetAllocation))
            {
                return budgetAllocation;
            }

            // If we don't have remaining budget we don't proceed with reallocation
            // TODO: Evaluate feasibility of contining reallocation dry
            var remainingBudgetOrNull = campaignEntity.GetRemainingBudget();
            if (!remainingBudgetOrNull.HasValue)
            {
                var message = "Remaining budget is not available for campaign '{0}' ({1})"
                    .FormatInvariant(campaignEntity.ExternalName, campaignEntity.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, message);
            }

            budgetAllocation.RemainingBudget = remainingBudgetOrNull.Value;
            budgetAllocation.PeriodDuration = activeAllocation.PeriodDuration;
            budgetAllocation.NodeDeliveryMetricsCollection = nodeDeliveryMetricsCollection;

            // calculate the value-volume score of the previous allocation
            AddValueVolumeScoreToAllocations(ref budgetAllocation, activeAllocation);

            return budgetAllocation;
        }

        /// <summary>Check if we should do an export.</summary>
        /// <param name="budgetAllocation">The budget allocation instance</param>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <param name="exportBudgets">The export budgets.</param>
        /// <returns>True if we there are things to export.</returns>
        internal static bool CheckExport(BudgetAllocation budgetAllocation, CampaignEntity campaignEntity, IDictionary<MeasureSet, decimal> exportBudgets)
        {
            // If there is nothing to export don't. If the campaign is over, don't export.
            return exportBudgets.Count > 0 && budgetAllocation.PeriodStart < campaignEntity.EndDate;
        }

        /// <summary>
        /// Gets the AllocationNodeMap or initializes a new one if it doesn't exist
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="campaignEntity">the campaignEntity</param>
        /// <returns>the allocationNodeMap</returns>
        internal Dictionary<string, MeasureSet> GetOrCreateAllocationNodeMap(RequestContext context, CampaignEntity campaignEntity)
        {
            // see if there's an association to the campaign
            var allocationNodeMapBlobAssociation = campaignEntity.TryGetAssociationByName(daName.AllocationNodeMap);
            if (allocationNodeMapBlobAssociation == null)
            {
                // No existing AllocationNodeMap, return a new one
                return new Dictionary<string, MeasureSet>();
            }

            // If there's an association, get the blob
            var allocationNodeMapBlob = (BlobEntity)this.Repository.GetEntity(context, allocationNodeMapBlobAssociation.TargetEntityId);

            // TODO: check this for errors? 
            return allocationNodeMapBlob.DeserializeBlob<Dictionary<string, MeasureSet>>();
        }

        /// <summary>Retrieve the active allocation if present.</summary>
        /// <param name="context">The repository request context.</param>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <returns>The active allocation if found. Null otherwise.</returns>
        ////[SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern. Exceptions are wrapped as error results")]
        internal BudgetAllocation GetActiveAllocation(
            RequestContext context, CampaignEntity campaignEntity)
        {
            var activeAllocation = new BudgetAllocation();
            var activeAllocationAssociation =
                campaignEntity.TryGetAssociationByName(daName.AllocationSetActive);

            // This would be normal on initial allocation. Don't set the error result.
            if (activeAllocationAssociation == null)
            {
                return activeAllocation;
            }

            var activeAllocationEntityId = activeAllocationAssociation.TargetEntityId;
            var activeAllocationEntity = (BlobEntity)this.Repository.GetEntity(context, activeAllocationEntityId);

            try
            {
                var activeAllocationJson = activeAllocationEntity.DeserializeBlob<string>();
                activeAllocation = AppsJsonSerializer.DeserializeObject<BudgetAllocation>(activeAllocationJson);
            }
            catch (Exception e)
            {
                var msg = "Current active allocation could not be deserialized: Entity ID - {0}."
                    .FormatInvariant((string)activeAllocationEntityId);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg, e);
            }

            return activeAllocation;
        }

        /// <summary>
        /// Add the latest budgetallocationsOutputs objects to the top of the DAAllocationsHistory index blob that is associated with the campaign.
        /// </summary>
        /// <param name="context">The context. </param>
        /// <param name="campaign">The campaign object. </param>
        /// <param name="allocationStartTime"> The allocationStartTime. </param>
        /// <param name="budgetAllocationOutputsId"> The budget allocation outputs entity ID. </param>
        internal void AddBudgetAllocationsOutputToHistory(
            RequestContext context,
            CampaignEntity campaign,
            DateTime allocationStartTime,
            EntityId budgetAllocationOutputsId)
        {
            // Get the BudgetAllocationHistoryIndex blob
            var budgetAllocationHistoryAssociation = campaign.TryGetAssociationByName(daName.AllocationHistoryIndex);

            List<HistoryElement> index;

            if (budgetAllocationHistoryAssociation != null)
            {
                // association exists, put this json val at top of existing list
                var blobEntity = this.Repository.TryGetEntity(context, budgetAllocationHistoryAssociation.TargetEntityId) as BlobEntity;

                // TODO: add null check
                var existingJson = blobEntity.DeserializeBlob<string>();

                index = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(existingJson);
            }
            else
            {
                index = new List<HistoryElement>();
            }

            // create the new history element 
            var historyElement = new HistoryElement
            {
                AllocationStartTime = allocationStartTime.ToString("o", CultureInfo.InvariantCulture),
                AllocationOutputsId = budgetAllocationOutputsId.ToString()
            };

            // insert the new jsonVals at front of list
            index.Insert(0, historyElement);

            // Add the allocations to the AllocationsHistory association for the campaign
            var indexJson = AppsJsonSerializer.SerializeObject(index);
            var newHistorysBlob = BlobEntity.BuildBlobEntity(new EntityId(), indexJson) as IEntity;
            newHistorysBlob.ExternalName = daName.AllocationHistoryIndex;

            campaign.AssociateEntities(
                daName.AllocationHistoryIndex, 
                null,
                new HashSet<IEntity> { newHistorysBlob }, 
                AssociationType.Relationship, 
                true);

            if (!this.Repository.TrySaveEntity(context, newHistorysBlob))
            {
                var msg = "Failed to save history blob entity {0}."
                    .FormatInvariant(newHistorysBlob.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.DataAccess, msg);
            }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);

            // get allocation period start time
            var reallocationStartTime = DateTime.Parse(
                request.Values[DynamicAllocationActivityValues.AllocationStartDate],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

            // get whether this is for initial allocation or reallocation
            var isInitialAllocation = Convert.ToBoolean(
                request.Values[DynamicAllocationActivityValues.IsInitialAllocation],
                CultureInfo.InvariantCulture);

            // Cache "now" for use throughout the rest of the activity.
            // This simplifies timing issues, reduces race conditionas 
            // and also provides an override mechanism for the simulator.
            var currentTime = request.Values.ContainsKey("time") ?
                DateTime.Parse(request.Values["time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) :
                DateTime.UtcNow;

            try
            {
                this.GetBudgetAllocations(
                    context,
                    companyEntityId,
                    campaignEntityId,
                    reallocationStartTime,
                    isInitialAllocation,
                    currentTime);

                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, campaignEntityId.ToString() }
                });
            }
            catch (DataAccessEntityNotFoundException enotfound)
            {
                return this.EntityNotFoundError(enotfound);
            }
            catch (ActivityException e)
            {
                LogManager.Log(LogLevels.Error, e.ToString());
                return this.ErrorResult(e);
            }
        }

        /// <summary>Transfer an association from the old campaign to the updated.</summary>
        /// <param name="campaignEntity">The working campaign entity.</param>
        /// <param name="updatedCampaignEntity">The campaign entity to update.</param>
        /// <param name="associationName">The name of the association.</param>
        private static void TransferAssociation(CampaignEntity campaignEntity, CampaignEntity updatedCampaignEntity, string associationName)
        {
            var sourceAssociation = campaignEntity.TryGetAssociationByName(associationName);
            if (sourceAssociation != null)
            {
                sourceAssociation = new Association(sourceAssociation);
            }

            var oldAssociation = updatedCampaignEntity.TryGetAssociationByName(associationName);
            if (oldAssociation != null)
            {
                updatedCampaignEntity.Associations.Remove(oldAssociation);
            }

            if (sourceAssociation != null)
            {
                updatedCampaignEntity.Associations.Add(sourceAssociation);
            }
        }

        /// <summary>
        /// Get the next set of budget allocations after clearing any scheduled reallocations/exports.
        /// Then schedules the next reallocation and export(s).
        /// </summary>
        /// <param name="context">Entity repository context</param>
        /// <param name="companyEntityId">The company entity id</param>
        /// <param name="campaignEntityId">The campaign entity id</param>
        /// <param name="allocationPeriodStart">Allocation period start</param>
        /// <param name="isInitialAllocation">Whether getting initialization phase allocations or reallocation</param>
        /// <param name="currentTime">The "current" time</param>
        private void GetBudgetAllocations(
            RequestContext context,
            EntityId companyEntityId,
            EntityId campaignEntityId,
            DateTime allocationPeriodStart,
            bool isInitialAllocation,
            DateTime currentTime)
        {
            var dacFac = new DynamicAllocationCampaignFactory();
            dacFac.BindRuntime(this.Repository);
            var dac = dacFac.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId);

            // Get the company and campaign entities
            var campaignEntity = dac.CampaignEntity;

            // Clear any previously scheduled reallocations for the campaign
            Scheduler.RemoveFromSchedule<string, DateTime, bool>(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate,
                campaignEntity.ExternalEntityId.ToString());

            // Clear any previously scheduled exports for the campaign
            Scheduler.RemoveFromSchedule<string, string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CampaignsToExport,
                campaignEntity.ExternalEntityId.ToString());

            // Get approved valuations from campaign entity. Don't update cache requesting approved valuations
            var dacApproved = dacFac.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId, true);
            var valuationsCache = new ValuationsCache(this.Repository);
            var valuations = valuationsCache.GetValuations(dacApproved, true);
            
            // create a dynamic allocation engine instance
            var dynamicAllocationEngine = dac.CreateDynamicAllocationEngine();

            // Get the current active allocation.
            var activeAllocation = this.GetActiveAllocation(context, campaignEntity);

            // Get the NodeDeliveryMetricsCollection.
            var nodeDeliveryMetricsCollection = GetNodeDeliveryMetrics(campaignEntity);

            var allocationParameters = dac.AllocationParameters;

            // Build budget allocation inputs from the active allocation and delivery data if present,
            // or defaults for initial allocation.
            BudgetAllocation budgetAllocation = BuildBudgetAllocationInputs(
                campaignEntity,
                activeAllocation,
                valuations,
                nodeDeliveryMetricsCollection,
                allocationParameters);

            // Schedule first reallocation if currently running initial allocation,
            // otherwise schedule a regular reallocation.
            var reallocationType = isInitialAllocation ?
                    ReallocationScheduleType.FirstReallocation :
                    ReallocationScheduleType.RegularReallocation;
            var nextAllocationPeriodStart = ScheduleNextReallocation(
                dac,
                false, // !immediate
                reallocationType,
                currentTime);

            // correctly set the period length
            budgetAllocation.PeriodStart = allocationPeriodStart;
            budgetAllocation.PeriodDuration = nextAllocationPeriodStart - allocationPeriodStart;

            // get budget allocations from DA (force initial allocation if initialization phase has not completed)
            budgetAllocation = dynamicAllocationEngine.GetBudgetAllocations(budgetAllocation, isInitialAllocation);

            // Transfer select delivery metrics on the node results so they get serialized
            UpdateDeliveryMetricsForSerialization(
                budgetAllocation.PerNodeResults,
                budgetAllocation.NodeDeliveryMetricsCollection);

            // Sever the NodeDeliveryMetricsCollection from the updated budget allocation object
            budgetAllocation.NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>();

            // set the LastModifiedDate on the allocation outputs
            budgetAllocation.LastModifiedDate = new PropertyValue(PropertyType.Date, currentTime); 

            // read in the allocationNodeMap. if it doesnt exist then we'll create an empty one.
            var allocationNodeMap = dac.RetrieveAllocationNodeMap();

            AddAllocationIdToAllocations(ref budgetAllocation, ref allocationNodeMap);
            
            // save the allocationNodeMap to a blob
            var newAllocationNodeMapBlob = BlobEntity.BuildBlobEntity(new EntityId(), allocationNodeMap) as IEntity;
            newAllocationNodeMapBlob.ExternalName = daName.AllocationNodeMap;

            // Associate the newAllocationNodeMapBlob to the campaign (replacing the old one if it exists)
            campaignEntity.AssociateEntities(
                daName.AllocationNodeMap,
                string.Empty,
                new HashSet<IEntity> { newAllocationNodeMapBlob },
                AssociationType.Relationship,
                true);
            
            if (!this.Repository.TrySaveEntity(context, newAllocationNodeMapBlob))
            {
                var msg = "Failed to save blob entity {0}."
                    .FormatInvariant(newAllocationNodeMapBlob.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.DataAccess, msg);
            }

            // Schedule exports to the delivery network set on either the campaign or its company
            var deliveryNetwork = dac.DeliveryNetwork;
            if (deliveryNetwork == DeliveryNetworkDesignation.Unknown)
            {
                var message = "Delivery network missing/unknown '{0}' ({1})"
                    .FormatInvariant(campaignEntity.ExternalName, campaignEntity.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, message);
            }

            var exports = GetExportSchedule(
                campaignEntityId,
                companyEntityId,
                allocationParameters,
                budgetAllocation,
                isInitialAllocation,
                currentTime);
            LogManager.Log(
                LogLevels.Trace,
                "Scheduling {0} exports to {1} for campaign '{2}' ({3}) at {4}",
                exports.Count,
                deliveryNetwork,
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId,
                string.Join(", ", exports.Select(e => e.Item1)));

            var periodDuration = TimeSpan.FromTicks(budgetAllocation.PeriodDuration.Ticks / exports.Count);
            var periodStart = budgetAllocation.PeriodStart;
            budgetAllocation.PeriodDuration = periodDuration;
            IEntity lastAllocationBlob = null;
            for (var i = 0; i < exports.Count; i++)
            {
                var exportBudgets = exports[i].Item4;
                CreateExportAllocation(ref budgetAllocation, exportBudgets, isInitialAllocation);

                // set the correct offset periodStart
                budgetAllocation.PeriodStart = periodStart + TimeSpan.FromTicks(i * periodDuration.Ticks);

                // save allocations to blob as json
                var exportAllocationJson = AppsJsonSerializer.SerializeObject(budgetAllocation);
                lastAllocationBlob = BlobEntity.BuildBlobEntity(new EntityId(), exportAllocationJson);
                this.Repository.TrySaveEntity(context, lastAllocationBlob);

                // Add newest allocation to the history index
                // TODO: check start time is correct
                this.AddBudgetAllocationsOutputToHistory(
                    context,
                    campaignEntity,
                    budgetAllocation.PeriodStart,
                    lastAllocationBlob.ExternalEntityId);

                // If there is nothing to export don't. By running to this point instead of 
                // exiting earlier we make sure the allocation history has been updated
                // with unprocessed delivery results.
                if (!CheckExport(budgetAllocation, campaignEntity, exportBudgets))
                {
                    continue;
                }

                // add to export schedule
                if (!Scheduler.AddToSchedule(
                    DeliveryNetworkSchedulerRegistries.CampaignsToExport,
                    exports[i].Item1,
                    exports[i].Item2,
                    exports[i].Item3.Item1,
                    lastAllocationBlob.ExternalEntityId.ToString(),
                    deliveryNetwork))
                {
                    var message = "Unable to schedule export to {0} for campaign '{1}' ({2}) at {3} for allocations {4}"
                        .FormatInvariant(
                            deliveryNetwork,
                            campaignEntity.ExternalName,
                            campaignEntity.ExternalEntityId,
                            exports[i].Item1,
                            lastAllocationBlob.ExternalEntityId);
                    throw new ActivityException(ActivityErrorId.GenericError, message);
                }
            }

            // create a new copy of the lastAllocationBlob so that the AllocationSetActive is a different blob 
            // than the one's used in the history
            var allocationSetActive = dac.CreateAndAssociateActiveAllocationBlob(budgetAllocation);
            this.Repository.TrySaveEntity(context, allocationSetActive);

            // Set whether the initialization phase has completed
            campaignEntity.SetInitializationPhaseComplete(!isInitialAllocation);

            this.UpdateCampaignEntityWithRetry(context, campaignEntity);
        }

        /// <summary>Update the campaign Entity.</summary>
        /// <param name="context">Entity repository context</param>
        /// <param name="campaignEntity">The campaign entity id</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        private void UpdateCampaignEntityWithRetry(RequestContext context, CampaignEntity campaignEntity)
        {
            // We will try three times to get the latest version and merge to overcome
            // a collision
            var retryCount = 3;
            while (retryCount-- > 0)
            {
                try
                {
                    this.UpdateCampaign(context, campaignEntity);
                    return;
                }
                catch (Exception)
                {
                    var retryMsg = "GetBudgetAllocation: failed to save campaign entity. Retries left {0}, {1}"
                        .FormatInvariant(retryCount, campaignEntity.ExternalEntityId);
                    LogManager.Log(LogLevels.Information, retryMsg);                    
                }
            }

            var msg = "GetBudgetAllocation: Failed to save campaign entity {0}"
                .FormatInvariant(campaignEntity.ExternalEntityId);
            throw new ActivityException(ActivityErrorId.DataAccess, msg);
        }

        /// <summary>Update the campaign Entity.</summary>
        /// <param name="context">Entity repository context</param>
        /// <param name="campaignEntity">The campaign entity.</param>
        private void UpdateCampaign(RequestContext context, CampaignEntity campaignEntity)
        {
            var updatedCampaignEntity = this.Repository.GetEntity<CampaignEntity>(context, campaignEntity.ExternalEntityId);
            TransferAssociation(campaignEntity, updatedCampaignEntity, daName.AllocationNodeMap);
            TransferAssociation(campaignEntity, updatedCampaignEntity, daName.AllocationHistoryIndex);
            TransferAssociation(campaignEntity, updatedCampaignEntity, daName.AllocationSetActive);
            updatedCampaignEntity.SetInitializationPhaseComplete(campaignEntity.GetInitializationPhaseComplete());
            this.Repository.SaveEntity(context, updatedCampaignEntity);
        }
    }
}
