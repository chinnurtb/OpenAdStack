// -----------------------------------------------------------------------
// <copyright file="GetCampaignDeliveryDataActivity.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Utilities;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Activity for getting campaign delivery data and processing it for use in budget allocation calculation
    /// </summary>
    /// <remarks>
    /// AuthUserId - authenticated user id this action is being performed as
    /// CompanyEntityId - company entity holding the campaign
    /// CampaignEntityId - campaign to update with delivery data
    /// DeliveryReportEntityId - blob entity with the delivery report data
    /// </remarks>
    [Name(DynamicAllocationActivityTasks.GetCampaignDeliveryData)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId)]
    public class GetCampaignDeliveryDataActivity : DynamicAllocationActivity
    {
        /// <summary>
        /// Amount of unstable data to exclude from the end of a retrieved report.
        /// Set the to zero until we decide if it needs to be used on a per network basis.
        /// </summary>
        internal static readonly TimeSpan ReportDeadZone = new TimeSpan(0, 0, 0);

        /// <summary>
        /// Amount of history we retrieve for calculating node metrics.
        /// This should be more than adequate to make sure we are including all new data since the
        /// last time data was retrieved, but small enough to still be performant.
        /// </summary>
        internal static readonly TimeSpan HistoryLookBack = new TimeSpan(72, 0, 0);

        /// <summary>Value to use for lifetime lookback.</summary>
        internal static readonly TimeSpan LifetimeLookBack = new TimeSpan(0);

        /// <summary>Minimum hours of reported data in allocation period to generate allocation inputs.</summary>
        internal static readonly decimal MinimumHoursOfData = 2;

        /// <summary>Map network to delivery data parser.</summary>
        internal static readonly Dictionary<DeliveryNetworkDesignation, IRawDeliveryDataParser> NetworkRawDeliveryDataParserMap =
            new Dictionary<DeliveryNetworkDesignation, IRawDeliveryDataParser>
                {
                    { DeliveryNetworkDesignation.AppNexus, new ApnxReportCsvParser() },
                    { DeliveryNetworkDesignation.GoogleDfp, new DfpReportCsvParser() }
                };

        /// <summary>Gets or sets the IDynamicAllocationCampaign instance.</summary>
        internal IDynamicAllocationCampaign Dac { get; set; }

        /// <summary>Gets default IEntityRepository request context.</summary>
        internal RequestContext DefaultRepositoryContext { get; private set; }

        /// <summary>Gets or sets DeliveryMetrics.</summary>
        internal IDeliveryMetrics DeliveryMetrics { get; set; }

        /// <summary>Get the delivery data for the campaign.</summary>
        /// <param name="lookBackDuration">The amount of delivery data to retrieve.</param>
        /// <returns>A CanonicalDeliveryData object, or null for failure.</returns>
        internal CanonicalDeliveryData GetDeliveryData(TimeSpan lookBackDuration)
        {
            var rawDeliveryDataIndexes = this.Dac.RawDeliveryData.RetrieveRawDeliveryDataIndexItems();

            if (rawDeliveryDataIndexes == null)
            {
                throw new ActivityException(ActivityErrorId.GenericError, "Unable to retrieve raw delivery data.");
            }

            var canonicalDeliveryDataList = this.BuildCanonicalDeliveryData(rawDeliveryDataIndexes, lookBackDuration);

            // For present we only allow one network
            if (canonicalDeliveryDataList.Count > 1)
            {
                throw new ActivityException(ActivityErrorId.GenericError, "Data from multiple networks not currently supported.");
            }

            // If we have a single entry in the list return it
            if (canonicalDeliveryDataList.Count == 1)
            {
                return canonicalDeliveryDataList[0];
            }

            // If the list is empty result is empty
            return new CanonicalDeliveryData();
        }

        /// <summary>Build canonical delivery data from raw delivery data.</summary>
        /// <param name="rawDeliveryDataIndexes">Raw delivery data indexes bound to the correct parser.</param>
        /// <param name="lookBackDuration">The amount of data to build.</param>
        /// <returns>
        /// List of canonical delivery data records or null if it could not be parsed.
        /// List may be empty if there are no collections in input list.
        /// </returns>
        internal List<CanonicalDeliveryData> BuildCanonicalDeliveryData(
            IEnumerable<RawDeliveryDataIndexItem> rawDeliveryDataIndexes,
            TimeSpan lookBackDuration)
        {
            var canonicalDeliveryDataCollections = new List<CanonicalDeliveryData>();

            foreach (var rawDeliveryDataIndexEntry in rawDeliveryDataIndexes)
            {
                // Get the network
                var network = rawDeliveryDataIndexEntry.DeliveryNetwork;
                
                // Get the index. Reverse the order to be most recent first.
                //// TODO: This is a break in ecapsulation that we are dependent on sort order
                //// TODO: as established by the report activity. A timestamp in the index would fix this.
                var rawDeliveryDataIndex = rawDeliveryDataIndexEntry.RawDeliveryDataEntityIds.Reverse();

                var canonicalDeliveryData = this.GetCanonicalDeliveryDataFromIndex(
                    rawDeliveryDataIndex, lookBackDuration, network);
                canonicalDeliveryDataCollections.Add(canonicalDeliveryData);
            }

            return canonicalDeliveryDataCollections;
        }

        /// <summary>Build a canonical delivery data object from a raw delivery data index.</summary>
        /// <param name="rawDeliveryDataIndex">Raw delivery data index.</param>
        /// <param name="lookBackDuration">The amount of data to build.</param>
        /// <param name="network">The network designation of the raw data source.</param>
        /// <returns>
        /// List of canonical delivery data records or null if it could not be parsed.
        /// List may be empty if there are no collections in input list.
        /// </returns>
        internal CanonicalDeliveryData GetCanonicalDeliveryDataFromIndex(
            IEnumerable<EntityId> rawDeliveryDataIndex,
            TimeSpan lookBackDuration,
            DeliveryNetworkDesignation network)
        {
            // Latest delivery data accumulated so far
            var canonicalDeliveryData = new CanonicalDeliveryData(network);
            var parser = NetworkRawDeliveryDataParserMap[network];

            var earlyLookBackCutoff = canonicalDeliveryData.ApplyLookBack(
                lookBackDuration, this.DeliveryMetrics.PreviousLatestCampaignDeliveryHour);

            foreach (var rawDataEntityId in rawDeliveryDataIndex)
            {
                var rawDeliveryDataItem = this.Dac.RawDeliveryData.RetrieveRawDeliveryDataItem(rawDataEntityId);

                // No partial success.
                if (!canonicalDeliveryData.AddRawData(
                    rawDeliveryDataItem.RawDeliveryData, rawDeliveryDataItem.DeliveryDataReportDate, parser))
                {
                    throw new ActivityException(ActivityErrorId.GenericError, "Unable to add raw data");
                }

                // If we're doing a lifetime lookback process all index entries
                if (lookBackDuration == LifetimeLookBack)
                {
                    continue;
                }

                // Determine if we have gone past our earlyLookbackCutoff
                if (canonicalDeliveryData.EarliestDeliveryDataDate <= earlyLookBackCutoff)
                {
                    break;
                }
            }

            return canonicalDeliveryData;
        }

        /// <summary>Get delivery eligibility history for the nodes.</summary>
        /// <param name="lookBackDuration">The amount of history to construct.</param>
        /// <returns>The eligibility history.</returns>
        internal EligibilityHistoryBuilder GetEligibilityHistory(TimeSpan lookBackDuration)
        {
            var context = this.DefaultRepositoryContext;

            if ((int)lookBackDuration.TotalHours == 0)
            {
                throw new ActivityException(ActivityErrorId.GenericError, "Invalid eligibility lookback duration.");
            }

            var index = this.Dac.BudgetAllocationHistory.RetrieveAllocationHistoryIndex().ToList();

            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder();
            index = eligibilityHistoryBuilder.FilterIndex(
                index, lookBackDuration, this.DeliveryMetrics.PreviousLatestCampaignDeliveryHour).ToList();

            // Process the allocation histories
            foreach (var historyElement in index)
            {
                var historyBlobEntityId = historyElement.AllocationOutputsId;
                var allocationHistoryDataBlob = (BlobEntity)this.Repository.GetEntity(context, historyBlobEntityId);

                var allocationHistoryJson = allocationHistoryDataBlob.DeserializeBlob<string>();
                var allocationHistory = AppsJsonSerializer.DeserializeObject<BudgetAllocation>(allocationHistoryJson);

                eligibilityHistoryBuilder.AddEligibilityHistory(allocationHistory);
            }

            return eligibilityHistoryBuilder;
        }

        /// <summary>Attempts to get the node metrics for the campaign</summary>
        internal void InitDeliveryMetrics()
        {
            var campaignEntity = this.Dac.CampaignEntity;

            // Get the allocation parameters
            var margin = this.Dac.AllocationParameters.Margin;
            var perMillFees = this.Dac.AllocationParameters.PerMilleFees;

            // Get the measure map
            var measureMap = this.Dac.RetrieveMeasureMap();

            // Get the serialized node metrics
            var serializedNodeMetrics =
                campaignEntity.TryGetPropertyValueByName(DynamicAllocationEntityProperties.AllocationNodeMetrics);

            if (serializedNodeMetrics == null)
            {
                // Set up delivery metrics with an empty NodeDeliveryMetrics
                this.DeliveryMetrics = new DeliveryMetrics(
                    ReportDeadZone,
                    new DeliveryDataCoster(measureMap, margin, perMillFees),
                    new Dictionary<MeasureSet, NodeDeliveryMetrics>());

                // The property will not initially be present on the entity.
                LogManager.Log(
                    LogLevels.Information,
                    "Node delivery metrics not found: Entity ID - {0}.",
                    campaignEntity.ExternalEntityId);
                return;
            }

            try
            {
                // Get the node metrics collection
                var nodeMetricsCollection =
                    AppsJsonSerializer.DeserializeObject<Dictionary<MeasureSet, NodeDeliveryMetrics>>(serializedNodeMetrics);

                // Set up delivery metrics with an populated NodeDeliveryMetrics
                this.DeliveryMetrics = new DeliveryMetrics(
                    ReportDeadZone,
                    new DeliveryDataCoster(measureMap, margin, perMillFees),
                    nodeMetricsCollection);
            }
            catch (AppsJsonException e)
            {
                var msg = "Node delivery metrics could not be deserialized: Entity ID - {0}."
                    .FormatInvariant(campaignEntity.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg, e);
            }
        }

        /// <summary>Attempts update the node metrics property on the campaign.</summary>
        internal void UpdateCampaign()
        {
            var campaignEntityId = (EntityId)this.Dac.CampaignEntity.ExternalEntityId;

            if (this.DeliveryMetrics.NodeMetricsCollection == null)
            {
                var msg = "Attempt to update campaign with null Node metrics: Entity ID - {0}."
                    .FormatInvariant(campaignEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, msg);
            }

            string serializedNodeMetrics;
            try
            {
                serializedNodeMetrics = AppsJsonSerializer.SerializeObject(this.DeliveryMetrics.NodeMetricsCollection);
            }
            catch (AppsJsonException e)
            {
                var msg = "Node delivery metrics could not be serialized: Entity ID - {0}."
                    .FormatInvariant(campaignEntityId);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg, e);
            }

            // Set serialized node metrics
            var allocationNodeMetrics = new EntityProperty(
                daName.AllocationNodeMetrics, serializedNodeMetrics, PropertyFilter.Extended);

            // Set the remaining budget and media budget cap
            var remainingBudget = new EntityProperty(
                daName.RemainingBudget, this.DeliveryMetrics.RemainingBudget);
            var lifetimeMediaBudgetCap = new EntityProperty(
                daName.LifetimeMediaBudgetCap, this.DeliveryMetrics.LifetimeMediaBudgetCap);

            var updatedProperties = 
                new[] { allocationNodeMetrics, remainingBudget, lifetimeMediaBudgetCap };

            if (!this.Repository.TryUpdateEntity(this.DefaultRepositoryContext, campaignEntityId, updatedProperties))
            {
                var msg = "GetCampaignDeliveryDataActivity: updated campaign could not be saved, Entity Id: {0}."
                    .FormatInvariant(campaignEntityId);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg);
            }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            // Resolve our request inputs
            this.DefaultRepositoryContext = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);

            string errorLogMsg = string.Empty;
            try
            {
                this.Dac = new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId);
                this.GetCampaignDeliveryData();
                return this.SuccessResult();
            }
            catch (DataAccessEntityNotFoundException e)
            {
                errorLogMsg = e.Message;
                return this.EntityNotFoundError(e);
            }
            catch (DataAccessException e)
            {
                errorLogMsg = e.Message;
                return this.ErrorResult(ActivityErrorId.DataAccess, e);
            }
            catch (ActivityException e)
            {
                errorLogMsg = e.Message;
                return this.ErrorResult(e);
            }
            catch (AppsGenericException e)
            {
                errorLogMsg = e.Message;
                return this.ErrorResult(ActivityErrorId.GenericError, e);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(errorLogMsg))
                {
                    LogManager.Log(LogLevels.Error, errorLogMsg);
                }
            }
        }

        /// <summary>Gets delivery data for the campaign</summary>
        private void GetCampaignDeliveryData()
        {
            // Get the company and campaign entities
            var campaignEntity = this.Dac.CampaignEntity;

            // Initialize the delivery metrics
            this.InitDeliveryMetrics();

            // Get the raw delivery data for a lookback period starting from the
            // last reported data
            var canonicalDeliveryData = this.GetDeliveryData(HistoryLookBack);

            // Get the eligibility history for our lookback period
            var eligibilityHistory = this.GetEligibilityHistory(HistoryLookBack);

            // Get the nodeMap
            var nodeMap = this.Dac.RetrieveAllocationNodeMap();

            // Calculate and add lifetime metrics
            this.DeliveryMetrics.CalculateNodeMetrics(
                canonicalDeliveryData, 
                eligibilityHistory, 
                nodeMap, 
                campaignEntity.Budget);
            
            // Update the node metrics on the campaign
            this.UpdateCampaign();
        }

        /// <summary>Nested class to hold the result of accumulated lifetime metrics for an allocation</summary>
        internal class LifetimeDeliveryResults
        {
            /// <summary>
            /// Gets or sets RemainingBudget.
            /// </summary>
            internal decimal RemainingBudget { get; set; }

            /// <summary>
            /// Gets or sets LifetimeMediaBudgetCap.
            /// </summary>
            internal decimal LifetimeMediaBudgetCap { get; set; }

            /// <summary>
            /// Gets or sets LatestCampaignDeliveryHour.
            /// </summary>
            internal DateTime PreviousLatestCampaignDeliveryHour { get; set; }

            /// <summary>
            /// Gets or sets NodeMetrics.
            /// </summary>
            internal Dictionary<MeasureSet, NodeDeliveryMetrics> NodeMetricsCollection { get; set; }
        }
    }
}
