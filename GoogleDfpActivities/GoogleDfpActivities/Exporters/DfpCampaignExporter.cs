//-----------------------------------------------------------------------
// <copyright file="DfpCampaignExporter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
using EntityActivities;
using GoogleDfpActivities.Measures;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Newtonsoft.Json;
using Utilities.Storage;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpActivities.Exporters
{
    /// <summary>
    /// Exporter for exporting DynamicAllocation nodes to Google DFP line-items
    /// </summary>
    internal class DfpCampaignExporter : CampaignExporterBase<IGoogleDfpClient>
    {
        /// <summary>Version of the exporter</summary>
        private const int ExporterVersion = 1;

        /// <summary>Exporter for Google DFP advertisers</summary>
        private readonly DfpAdvertiserExporter AdvertiserExporter;

        /// <summary>Backing field for ActiveAllocations</summary>
        private readonly BudgetAllocation activeAllocations;

        /// <summary>Used for tracking export metrics </summary>
        private DfpCampaignExporterMetrics metrics;

        /// <summary>Initializes a new instance of the DfpCampaignExporter class.</summary>
        /// <param name="companyEntity">Company Entity</param>
        /// <param name="campaignEntity">Campaign Entity</param>
        /// <param name="campaignOwner">Campaign owner User Entity</param>
        /// <param name="creativeEntitites">Creative Entities</param>
        /// <param name="exportAllocationsEntity">Export Allocations Entity</param>
        public DfpCampaignExporter(
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner,
            CreativeEntity[] creativeEntitites,
            BlobEntity exportAllocationsEntity)
            : base(DeliveryNetworkDesignation.GoogleDfp, ExporterVersion, companyEntity, campaignEntity, campaignOwner)
        {
            this.metrics = null;
            this.CreativeEntities = creativeEntitites;
            this.AdvertiserExporter = new DfpAdvertiserExporter(this.CompanyEntity);
            var activeAllocationsJson = exportAllocationsEntity.DeserializeBlob<string>();
            this.activeAllocations = JsonConvert.DeserializeObject<BudgetAllocation>(activeAllocationsJson);
        }

        /// <summary>
        /// Gets a value indicating whether an order exists in DFP for the exporter's CampaignEntity
        /// </summary>
        /// <returns>True if an order exists; otherwise, false.</returns>
        public bool OrderExists
        {
            get
            {
                var orderId = this.CampaignEntity.GetDfpOrderId();
                if (!orderId.HasValue)
                {
                    return false;
                }

                try
                {
                    this.Client.GetOrder(orderId.Value);
                    return true;
                }
                catch (GoogleDfpClientException)
                {
                    return false;
                }
            }
        }

        /// <summary>Gets the DFP order id for the exporter's CampaignEntity</summary>
        protected long OrderId
        {
            get { return this.CampaignEntity.GetDfpOrderId().Value; }
        }

        /// <summary>Gets the Google DFP creative ids for the exporter's CampaignEntities</summary>
        /// <returns>Google DFP creative ids</returns>
        protected long[] ImageCreativeIds
        {
            get
            {
                return this.CreativeEntities
                    .Select(creative => creative.GetDfpCreativeId())
                    .Where(creativeId => creativeId != null)
                    .Cast<long>()
                    .ToArray();
            }
        }

        /// <summary>Gets the exporter's CreativeEntities</summary>
        protected CreativeEntity[] CreativeEntities { get; private set; }

        /// <summary>Creates a Google DFP order for the exporter's CampaignEntity.</summary>
        /// <returns>Google DFP order id</returns>
        public long CreateOrder()
        {
            var orderId = this.Client.CreateOrder(
                    this.AdvertiserExporter.AdvertiserId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.StartDate,
                    this.CampaignEntity.EndDate);
            LogManager.Log(
                LogLevels.Information,
                "Created Google DFP Order '{0}' for CampaignEntity '{1}' ({2})",
                orderId,
                this.CampaignEntity.ExternalName,
                this.CampaignEntity.ExternalEntityId);
            if (!this.Client.ApproveOrder(orderId))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to approve DFP Order '{0}' for CampaignEntity '{1}' ({2})",
                    orderId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Activated Google DFP Order '{0}' for CampaignEntity '{1}' ({2})",
                    orderId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);
            }

            return orderId;
        }

        /// <summary>Updates the Google DFP order for a CampaignEntity</summary>
        /// <remarks>
        /// Sets the order start/end dates to the campaign's current values.
        /// </remarks>
        public void UpdateOrder()
        {
            LogManager.Log(
                LogLevels.Trace,
                "Updating Google DFP order {0} for campaign '{1}' ({2}) under Google DFP advertiser {3}",
                this.OrderId,
                this.CampaignEntity.ExternalName,
                this.CampaignEntity.ExternalEntityId,
                this.AdvertiserExporter.AdvertiserId);
            this.Client.UpdateOrder(
                this.OrderId,
                this.CampaignEntity.StartDate,
                this.CampaignEntity.EndDate);
        }

        /// <summary>
        /// Updates Google DFP line-items from budget allocations,
        /// creating the line-items as needed.
        /// </summary>
        /// <exception cref="GoogleDfpClientException">
        /// An error occured while calling Google DFP.
        /// </exception>
        public void ExportLineItems()
        {
            // Get the already exported line-items for the order
            var lineItemsByAllocationId = this.Client.GetLineItemsForOrder(this.OrderId)
                .ToDictionary(li => li.externalId);

            // Allocation nodes to be exported
            var nodesForExport =
                this.activeAllocations.PerNodeResults
                .Where(node => node.Value.ExportBudget > 0m);

            // Setup the metrics (used to track export)
            this.metrics = new DfpCampaignExporterMetrics(
                this.CampaignEntity,
                nodesForExport.Select(kvp => kvp.Value),
                lineItemsByAllocationId.Values);

            try
            {
                // Pause the existing line-items
                var existingLineItemIds = lineItemsByAllocationId.Values
                    .Select(li => li.id)
                    .ToArray();
                if (existingLineItemIds.Length > 0)
                {
                    var lineItemsPaused = this.Client.PauseLineItems(existingLineItemIds);
                    LogManager.Log(
                        LogLevels.Trace,
                        "Paused {0}/{1} line-items before exporting campaign '{2}' ({3})",
                        lineItemsPaused,
                        existingLineItemIds.Length,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId);
                }

                // Allocations that have not been exported previously
                var nodesToCreate = nodesForExport
                    .Where(node => !lineItemsByAllocationId.ContainsKey(node.Value.AllocationId))
                    .ToDictionary();

                // Previously exported line-items that are to be re-exported with their allocation nodes
                var lineItemNodesToUpdate = nodesForExport
                    .Where(node => lineItemsByAllocationId.ContainsKey(node.Value.AllocationId))
                    .Join(
                        lineItemsByAllocationId,
                        node => node.Value.AllocationId,
                        kvp => kvp.Key,
                        (node, kvp) => new Tuple<Dfp.LineItem, MeasureSet, PerNodeBudgetAllocationResult>(kvp.Value, node.Key, node.Value));

                /* TODO: Remove after verifying pausing all line-items pre-export is okay
                // Previously exported line-items that need to be paused
                var lineItemsToPause = lineItemsByAllocationId
                    .Where(kvp => !exportAllocationIds.Contains(kvp.Key))
                    .Select(kvp => kvp.Value)
                    .ToArray();
                 */

                // Create line-items for nodes that have never been exported
                this.CreateLineItemsForNodes(nodesToCreate);

                // Update line-items that have been previously exported
                this.UpdateLineItemsForNodes(lineItemNodesToUpdate);

                /* TODO: Remove after verifying pausing all line-items pre-export is okay
                // Deactivate active campaigns that are not included in the exportAllocationIds from Dynamic Allocation
                this.PauseLineItems(lineItemsToPause);
                 */
            }
            finally
            {
                // Log export metrics
                this.metrics.LogMetrics();
            }
        }
        
        /// <summary>
        /// Filters the list of creatives for only those compatible
        /// with the specified AdUnits and placements.
        /// </summary>
        /// <param name="creatives">Creatives to filter</param>
        /// <param name="includeAdUnitIds">The AdUnitIds</param>
        /// <param name="placementIds">The placement ids</param>
        /// <returns>The compatible creatives</returns>
        internal Dfp.Creative[] GetCompatibleCreatives(
            Dfp.Creative[] creatives,
            string[] includeAdUnitIds,
            long[] placementIds)
        {
            var placements = this.Client.GetPlacements(placementIds);
            var adUnitIds = includeAdUnitIds
                .Concat(placements
                    .SelectMany(p => p.targetedAdUnitIds))
                .ToArray();
            var adUnits = this.Client.GetAdUnits(adUnitIds);
            return creatives
                //// TODO: Do we need to filter here, or let DFP deal with it?
                .Where(c => IsCompatibleImageCreative(c, adUnits) || true)
                .ToArray();
        }

        /// <summary>Checks if a creative is compatible with any of the AdUnits</summary>
        /// <param name="creative">The creative</param>
        /// <param name="adUnits">The AdUnits</param>
        /// <returns>True if the creative is compatible; otherwise, false</returns>
        private static bool IsCompatibleImageCreative(
            Dfp.Creative creative,
            Dfp.AdUnit[] adUnits)
        {
            return adUnits.Any(adUnit =>
                adUnit.adUnitSizes.Any(size =>
                    size.environmentType == Dfp.EnvironmentType.BROWSER &&
                    size.size == creative.size));
        }

        /// <summary>Creates line-items for the provided allocation nodes</summary>
        /// <param name="nodesToCreate">Allocation nodes to create campaigns for</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is logged. Must continue with other nodes.")]
        private void CreateLineItemsForNodes(
            IDictionary<MeasureSet, PerNodeBudgetAllocationResult> nodesToCreate)
        {
            var nodeCount = nodesToCreate.Count();
            var orderId = this.OrderId;

            var creativeIds = this.ImageCreativeIds;
            if (creativeIds.Length == 0)
            {
                var message =
                    "No valid, exported, Google DFP image creatives found for campaign '{0}' ({1})"
                    .FormatInvariant(this.CampaignEntity.ExternalName, this.CampaignEntity.ExternalEntityId);
                LogManager.Log(LogLevels.Error, message);
                throw new DeliveryNetworkExporterException(message);
            }

            var creatives = this.Client.GetCreatives(creativeIds);

            var lineItemIds = new List<long>();
            var i = 0;
            foreach (var nodeAllocation in nodesToCreate)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "{0}/{1} - Exporting allocation node '{2}' of campaign '{3}' ({4})\nAllocation: {5}",
                    ++i,
                    nodeCount,
                    nodeAllocation.Value.AllocationId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId,
                    nodeAllocation.Value);
                try
                {
                    var lineItemId = this.CreateLineItem(
                        nodeAllocation,
                        orderId,
                        creatives);
                    lineItemIds.Add(lineItemId);
                    this.metrics.Created.Add(
                        nodeAllocation.Value.AllocationId,
                        lineItemId);
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error exporting allocation node '{0}' of campaign '{1}' ({2}): {3}",
                        nodeAllocation.Value.AllocationId,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        e);
                }
            }
        }

        /// <summary>Updates campaigns for the provided allocation nodes</summary>
        /// <param name="lineItemNodeAllocations">Line-item/node pairs for updating</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is logged. Must continue with other nodes.")]
        private void UpdateLineItemsForNodes(
            IEnumerable<Tuple<Dfp.LineItem, MeasureSet, PerNodeBudgetAllocationResult>> lineItemNodeAllocations)
        {
            var count = lineItemNodeAllocations.Count();
            var i = 0;

            foreach (var lineItemNodeAllocation in lineItemNodeAllocations)
            {
                var lineItem = lineItemNodeAllocation.Item1;
                var measureSet = lineItemNodeAllocation.Item2;
                var allocation = lineItemNodeAllocation.Item3;
                LogManager.Log(
                    LogLevels.Trace,
                    "{0}/{1} - Updating line-item {2} for allocation node '{3}' of campaign '{4}' ({5})\nAllocation: {6}",
                    ++i,
                    count,
                    lineItem.id,
                    allocation.AllocationId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId,
                    allocation);
                try
                {
                    this.UpdateLineItem(
                        lineItem,
                        measureSet,
                        allocation);
                    this.metrics.Updated.Add(
                        allocation.AllocationId,
                        lineItem.id);
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error exporting allocation node '{0}' of campaign '{1}' ({2}): {3}",
                        allocation.AllocationId,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        e);
                }
            }
        }

        /* TODO: Remove after verifying pausing all line-items pre-export is okay
        /// <summary>Pauses the specified line-items</summary>
        /// <param name="lineItemsToPause">Line-items to pause</param>
        private void PauseLineItems(
            Dfp.LineItem[] lineItemsToPause)
        {
            var count = lineItemsToPause.Count();
            var lineItemStrings = lineItemsToPause
                .Select(li => "\t{0}\t{1}\t{2}".FormatInvariant(li.id, li.name, li.externalId));
            LogManager.Log(
                LogLevels.Trace,
                "Pausing {0} line-item(s) of campaign '{1}' ({2}):\n{3}",
                lineItemStrings.Count(),
                this.CampaignEntity.ExternalName,
                this.CampaignEntity.ExternalEntityId,
                string.Join("\n", lineItemStrings));
            try
            {
                var lineItemIds = lineItemsToPause
                    .Select(li => li.id)
                    .ToArray();
                var paused = this.Client.PauseLineItems(lineItemIds);
                if (paused < count)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Failed to pause {0} line-item(s) of campaign '{1}' ({2})",
                        count - paused,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId);
                }

                this.metrics.Paused = paused;
            }
            catch (GoogleDfpClientException dfpe)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Error pausing Google DFP line-items for campaign '{0}' ({1}): {2}",
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId,
                    dfpe);
            }
        }
         */

        /// <summary>Updates the Google DFP line-item for the allocation</summary>
        /// <param name="lineItem">The line-item to update</param>
        /// <param name="measureSet">The node measures</param>
        /// <param name="allocation">The node budget allocation</param>
        private void UpdateLineItem(
            Dfp.LineItem lineItem,
            MeasureSet measureSet,
            PerNodeBudgetAllocationResult allocation)
        {
            var lifetimeBudget = allocation.LifetimeMediaSpend + allocation.ExportBudget;
            var lifetimeImpressionCap = allocation.LifetimeImpressions + allocation.PeriodImpressionCap;
            var lineItemName = DynamicAllocationActivityUtilities.MakeExportUnitNameForAllocation(
                allocation,
                this.CampaignEntity,
                measureSet,
                lifetimeBudget);

            try
            {
                this.Client.UpdateLineItem(
                    lineItem.id,
                    lineItemName,
                    lifetimeBudget / lifetimeImpressionCap,
                    lifetimeImpressionCap,
                    this.CampaignEntity.StartDate,
                    this.CampaignEntity.EndDate);

                LogManager.Log(
                    LogLevels.Trace,
                    "Updated Google DFP line-item {0} ({1}) for Allocation '{2}' of Campaign '{3}' ({4})",
                    lineItem.name,
                    lineItem.id,
                    allocation.AllocationId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);
            }
            catch (GoogleDfpClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to update Google DFP line-item {0} ({1}) for Allocation '{2}'.",
                    lineItem.name,
                    lineItem.id,
                    allocation.AllocationId);
                throw;
            }
        }

        /// <summary>Creates an AppNexus campaign for the allocation</summary>
        /// <param name="nodeAllocation">The per-node budget allocation result and measure set</param>
        /// <param name="orderId">The Google DFP order id</param>
        /// <param name="creatives">The Google DFP creatives</param>
        /// <returns>The created Google DFP line-item's id</returns>
        private long CreateLineItem(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> nodeAllocation,
            long orderId,
            Dfp.Creative[] creatives)
        {
            var measures = nodeAllocation.Key;
            var allocation = nodeAllocation.Value;

            var lifetimeBudget = allocation.LifetimeMediaSpend + allocation.ExportBudget;
            var lifetimeImpressionCap = allocation.LifetimeImpressions + allocation.PeriodImpressionCap;
            var lineItemName = DynamicAllocationActivityUtilities.MakeExportUnitNameForAllocation(
                allocation,
                this.CampaignEntity,
                measures,
                lifetimeBudget);

            try
            {
                var adUnitIds = this.MeasureMap.GetDfpAdUnitIds(measures);
                var placementIds = this.MeasureMap.GetDfpPlacementIds(measures);
                var locationIds = this.MeasureMap.GetDfpLocationsIds(measures);
                var technologyTargeting = this.MeasureMap.GetDfpTechnologyTargeting(measures);
                var compatibleCreatives = this.GetCompatibleCreatives(creatives, adUnitIds, placementIds);

                // Create the line item
                var lineItemId = this.Client.CreateLineItem(
                    orderId,
                    lineItemName,
                    allocation.AllocationId,
                    lifetimeBudget / lifetimeImpressionCap,
                    lifetimeImpressionCap,
                    this.CampaignEntity.StartDate,
                    this.CampaignEntity.EndDate,
                    adUnitIds,
                    true,
                    placementIds,
                    locationIds,
                    technologyTargeting,
                    compatibleCreatives);

                // Add compatible creatives to the line-item
                foreach (var creative in compatibleCreatives)
                {
                    this.Client.AddCreativeToLineItem(
                        lineItemId,
                        creative.id);
                }
                
                LogManager.Log(
                    LogLevels.Trace,
                    "Created Google DFP line-item '{0}' for Allocation '{1}' of Campaign '{2}' ({3}) with creatives {4}",
                    lineItemId,
                    allocation,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId,
                    string.Join(", ", compatibleCreatives.Select(c => "{0} ({1})".FormatInvariant(c.name, c.id))));

                return lineItemId;
            }
            catch (GoogleDfpClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to create Google DFP line-item for Allocation '{0}'.",
                    allocation.AllocationId);
                throw;
            }
        }

        /*
        /// <summary>Creates an AppNexus targeting profile for the measures of the allocation</summary>
        /// <param name="measureMap">The measure map</param>
        /// <param name="nodeAllocation">The node allocation</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <returns>The AppNexus id of the created profile</returns>
        private static int CreateProfile(MeasureMap measureMap, KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> nodeAllocation, long advertiserId)
        {
            var measures = nodeAllocation.Key;
            var allocation = nodeAllocation.Value;

            try
            {
                var profileId = DfpClient.CreateProfile(
                    advertiserId,
                    allocation.AllocationId,
                    measureMap.GetAppNexusAgeRange(measures),
                    measureMap.GetAppNexusGender(measures),
                    measureMap.GetAppNexusSegments(measures),
                    measureMap.GetAppNexusMetroCodes(measures),
                    measureMap.GetAppNexusRegions(measures),
                    measureMap.GetPageLocation(measures),
                    measureMap.GetInventoryAttributes(measures),
                    measureMap.GetContentCategories(measures));
                LogManager.Log(
                    LogLevels.Trace,
                    "Created AppNexus Targeting Profile '{0}' for Measures '{1}' (AllocationId {2})",
                    profileId,
                    measures,
                    allocation.AllocationId);
                return profileId;
            }
            catch (GoogleDfpClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to create AppNexus Targeting Profile for Allocation '{0}'.",
                    allocation.AllocationId);
                throw;
            }
        }
        */
    }
}
