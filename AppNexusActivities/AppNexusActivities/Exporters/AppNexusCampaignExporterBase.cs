//-----------------------------------------------------------------------
// <copyright file="AppNexusCampaignExporterBase.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Activities;
using AppNexusActivities.Measures;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityActivities;
using EntityUtilities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Collection of static methods related to exporting DynamicAllocation campaigns to AppNexus
    /// </summary>
    internal abstract class AppNexusCampaignExporterBase : CampaignExporterBase<IAppNexusApiClient>, IAppNexusCampaignExporter
    {
        /// <summary>Name of the default domain list embedded resource</summary>
        private const string DefaultDomainListResourceName = "AppNexusActivities.Resources.DefaultIncludeDomainList.txt";

        /// <summary>Backing field for ExporterDefaults</summary>
        private IPersistentDictionary<string> exporterDefaults;

        /// <summary>AppNexus campaign export metrics</summary>
        private AppNexusCampaignExportMetrics metrics;

        /// <summary>
        /// Initializes a new instance of the AppNexusCampaignExporterBase class.
        /// </summary>
        /// <param name="version">Version of the exporter</param>
        /// <param name="companyEntity">Advertiser company</param>
        /// <param name="campaignEntity">Campaign being exported</param>
        /// <param name="campaignOwner">Owner of the campaign being exported</param>
        public AppNexusCampaignExporterBase(
            int version,
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
            : base(DeliveryNetworkDesignation.AppNexus, version, companyEntity, campaignEntity, campaignOwner)
        {
        }

        /// <summary>
        /// Gets the persistent dictionary for configurable exporter defaults
        /// </summary>
        private IPersistentDictionary<string> ExporterDefaults
        {
            get
            {
                return
                    this.exporterDefaults =
                    this.exporterDefaults ??
                    PersistentDictionaryFactory.CreateDictionary<string>("apnx-defaults");
            }
        }

        /// <summary>Gets the default include domain list</summary>
        /// <remarks>
        /// List is loaded from the exporter defaults persistent dictonary
        /// and initialized with the embedded default list if not present.
        /// </remarks>
        private string[] DefaultIncludeDomainList
        {
            get
            {
                if (!this.ExporterDefaults.ContainsKey(ExporterDefaultsKeys.DefaultDomainListDictionaryKey))
                {
                    this.ExporterDefaults[ExporterDefaultsKeys.DefaultDomainListDictionaryKey] =
                        ReadEmbeddedResource(DefaultDomainListResourceName);
                }

                var defaultIncludeDomainList = this.ExporterDefaults[ExporterDefaultsKeys.DefaultDomainListDictionaryKey];
                return !string.IsNullOrWhiteSpace(defaultIncludeDomainList) ?
                    defaultIncludeDomainList
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray() :
                    null;
            }
        }

        /// <summary>Gets the AllocationIds of the nodes successfully exported</summary>
        /// <remarks>AllocationIds sourced from metrics tracked during export</remarks>
        private string[] ExportedAllocationIds
        {
            get
            {
                return this.metrics.CreatedCampaigns.Keys
                    .Concat(this.metrics.UpdatedCampaigns)
                    .ToArray();
            }
        }

        /// <summary>
        /// Updates AppNexus campaigns from budget allocations, creating the campaigns
        /// and corresponding targeting profiles as eneded.
        /// </summary>
        /// <param name="advertiserId">The advertiser id</param>
        /// <param name="lineItemId">The line item id</param>
        /// <param name="creativeIds">The creative ids</param>
        /// <param name="activeAllocations">The budget allocations</param>
        /// <param name="campaignAllocationIds">
        /// The AllocationIds of previously exported AppNexus campaigns
        /// and whether they are currently active or not.
        /// </param>
        /// <param name="exportAllocationIds">
        /// The AllocationIds of the nodes to be exported (active)
        /// </param>
        /// <returns>
        /// The AllocationIds of the nodes successfully exported
        /// </returns>
        /// <exception cref="AppNexusClientException">
        /// An error occured while calling AppNexus.
        /// </exception>
        public string[] ExportAppNexusCampaigns(
            int advertiserId,
            int lineItemId,
            int[] creativeIds,
            BudgetAllocation activeAllocations,
            IDictionary<string, bool> campaignAllocationIds,
            string[] exportAllocationIds)
        {
            LogManager.Log(
                LogLevels.Information,
                "Exporting {0} allocations as profile/campaigns to AppNexus...",
                activeAllocations.PerNodeResults.Count);

            // For tracking export metrics
            this.metrics = new AppNexusCampaignExportMetrics(
                exportAllocationIds.Length,
                this.CampaignEntity,
                activeAllocations.PerNodeResults.Values);
            
            // Create/update the AppNexus campaigns and targeting profiles for nodes with Allocation IDs in the export list
            var nodesForExport = activeAllocations.PerNodeResults
                .Where(allocation => exportAllocationIds.Contains(allocation.Value.AllocationId))
                .ToArray();
            
            // Create campaigns that do not exist yet (neither active nor inactive)
            var nodesToCreate = nodesForExport
                .Where(allocation => !campaignAllocationIds.ContainsKey(allocation.Value.AllocationId))
                .ToDictionary();
            this.CreateCampaignsForNodes(nodesToCreate, advertiserId, lineItemId, creativeIds);

            // Update campaigns that already exist (includes activating inactive campaigns)
            var nodesToUpdate = nodesForExport
                .Where(allocation => campaignAllocationIds.ContainsKey(allocation.Value.AllocationId))
                .ToDictionary();
            this.UpdateCampaignsForNodes(nodesToUpdate, advertiserId, lineItemId, creativeIds);

            // Deactivate active campaigns that are not included in the exportAllocationIds from Dynamic Allocation
            var campaignsToDelete = campaignAllocationIds
                .Where(kvp => kvp.Value)
                .Where(kvp => !exportAllocationIds.Contains(kvp.Key))
                .Select(kvp => kvp.Key);
            this.DeleteCampaigns(campaignsToDelete, advertiserId);

            // Log export metrics
            this.metrics.LogMetrics();

            // List of the AllocationIds of successfully exported nodes
            return this.ExportedAllocationIds;
        }

        /// <summary>
        /// Exports an AppNexus line-item for the dynamic allocation campaign
        /// </summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="name">Line item name</param>
        /// <param name="code">Line item code</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="totalBudget">Total budget</param>
        /// <param name="activeAllocations">
        /// Active allocations (for line-item profile targeting)
        /// </param>
        /// <returns>The AppNexus line item id</returns>
        public int ExportAppNexusLineItem(
            int advertiserId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget,
            BudgetAllocation activeAllocations)
        {
            // Get/create the domain list
            var includeDomainListId = this.CampaignEntity.GetAppNexusIncludeDomainListId();
            if (includeDomainListId == null)
            {
                // TODO: Encapsulate in private method
                var includeDomainList =
                    this.CampaignEntity.GetAppNexusIncludeDomainList() ??
                    this.CompanyEntity.GetAppNexusIncludeDomainList() ??
                    this.DefaultIncludeDomainList;
                if (includeDomainList != null)
                {
                    var domainListName =
                        "{0}:{1}".FormatInvariant(
                            this.CompanyEntity.ExternalEntityId,
                            this.CampaignEntity.ExternalEntityId);
                    includeDomainListId = this.Client.CreateDomainList(
                        domainListName,
                        this.CampaignEntity.ExternalName,
                        includeDomainList);
                    this.CampaignEntity.SetAppNexusIncludeDomainListId(
                        (int)includeDomainListId);
                }
            }

            // Get the frequency caps and domain targets
            var frequencyCaps = this.GetFrequencyCaps(
                activeAllocations.PerNodeResults.Keys);
            var domainTargets = activeAllocations.PerNodeResults
                .SelectMany(node =>
                    this.GetDomainTargets(node.Key));

            // Create a line-item targeting profile
            var profileId = this.Client.CreateLineItemProfile(
                (int)advertiserId,
                name,
                frequencyCaps,
                includeDomainListId,
                domainTargets.ToArray());
            
            // Create and return the line item
            return this.Client.CreateLineItem(
                advertiserId,
                profileId,
                name,
                code,
                active,
                startDate,
                endDate,
                totalBudget);
        }

        /// <summary>Builds the IConfig used by the exporter</summary>
        /// <returns>The config</returns>
        protected override IConfig BuildConfig()
        {
            var config = (CustomConfig)base.BuildConfig();
            var isAppNexusApp = this.CampaignOwner.GetUserType() == UserType.AppNexusApp;
            config.Overrides["AppNexus.IsApp"] = isAppNexusApp.ToString();
            if (isAppNexusApp)
            {
                config.Overrides["AppNexus.App.UserId"] = this.CampaignOwner.UserId;
            }

            return config;
        }

        /// <summary>Gets the AppNexus demographic age range</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The age range</returns>
        protected abstract Tuple<int, int> GetAppNexusAgeRange(MeasureSet measureSet);

        /// <summary>Gets the AppNexus allow unknown age value</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>Whether to allow unknown age</returns>
        protected abstract bool GetAppNexusAllowUnknownAge(MeasureSet measureSet);

        /// <summary>Gets the AppNexus demographic maximum age</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The maximum age</returns>
        protected abstract string GetAppNexusGender(MeasureSet measureSet);

        /// <summary>Gets the AppNexus DMA metro codes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus metro codes</returns>
        protected abstract IDictionary<int, string> GetAppNexusMetroCodes(MeasureSet measureSet);

        /// <summary>Gets the AppNexus region codes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus region codes</returns>
        protected abstract IEnumerable<string> GetAppNexusRegions(MeasureSet measureSet);

        /// <summary>Gets the creative page location from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The page location</returns>
        protected abstract PageLocation GetPageLocation(MeasureSet measureSet);

        /// <summary>Gets the inventory attributes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The inventory attribute values</returns>
        protected abstract IEnumerable<int> GetInventoryAttributes(MeasureSet measureSet);

        /// <summary>Gets the content category from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The content category ids and whether to include/exclude them</returns>
        protected abstract IDictionary<int, bool> GetContentCategories(MeasureSet measureSet);

        /// <summary>Gets the AppNexus segments from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus segments</returns>
        protected abstract IDictionary<int, string> GetAppNexusSegments(MeasureSet measureSet);

        /// <summary>Gets the AppNexus domain targets from the measure sets</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain targets</returns>
        protected abstract IEnumerable<string> GetDomainTargets(MeasureSet measureSet);

        /// <summary>Gets the AppNexus domain-list targets from the measure sets</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain-list targets</returns>
        protected abstract IDictionary<int, bool> GetDomainListTargets(MeasureSet measureSet);

        /// <summary>Gets the AppNexus frequency caps from the measure sets</summary>
        /// <param name="measureSets">The MeasureSets</param>
        /// <returns>The frequency caps</returns>
        protected abstract IDictionary<AppNexusFrequencyType, int> GetFrequencyCaps(IEnumerable<MeasureSet> measureSets);

        /// <summary>Reads text from an embedded resource</summary>
        /// <param name="resourceName">Name of the embedded resource</param>
        /// <returns>Text contents of the embedded resource</returns>
        private static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        /// <summary>
        /// Creates campaigns (and corresponding targeting profiles) for the provided allocation nodes
        /// </summary>
        /// <param name="nodesToCreate">Allocation nodes to create campaigns for</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <param name="lineItemId">The AppNexus line-item id</param>
        /// <param name="creativeIds">The AppNexus creative ids</param>
        private void CreateCampaignsForNodes(
            IDictionary<MeasureSet, PerNodeBudgetAllocationResult> nodesToCreate,
            int advertiserId,
            int lineItemId,
            int[] creativeIds)
        {
            var nodeCount = nodesToCreate.Count();
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
                    // Check if a profile for the node already exists
                    int profileId;
                    var profile = this.Client.GetProfileByCode(advertiserId, nodeAllocation.Value.AllocationId);
                    if (profile != null)
                    {
                        profileId = (int)profile[AppNexusValues.Id];
                    }
                    else
                    {
                        profileId = this.CreateNodeAllocationProfile(
                            nodeAllocation,
                            advertiserId);
                        this.metrics.CreatedProfiles.Add(
                            nodeAllocation.Value.AllocationId,
                            profileId);
                    }

                    var campaignId = this.CreateCampaign(
                        nodeAllocation,
                        advertiserId,
                        lineItemId,
                        profileId,
                        creativeIds);
                    this.metrics.CreatedCampaigns.Add(
                        nodeAllocation.Value.AllocationId,
                        campaignId);
                }
                catch (AppNexusClientException ance)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error exporting allocation node '{0}' of campaign '{1}' ({2}): {3}",
                        nodeAllocation.Value.AllocationId,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        ance);
                    this.metrics.FailedAllocationExports.Add(nodeAllocation.Value.AllocationId);
                }
            }
        }

        /// <summary>
        /// Updates campaigns for the provided allocation nodes
        /// </summary>
        /// <param name="nodesToCreate">Allocation nodes to update the campaigns for</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <param name="lineItemId">The AppNexus line-item id</param>
        /// <param name="creativeIds">The AppNexus creative ids</param>
        private void UpdateCampaignsForNodes(
            IDictionary<MeasureSet, PerNodeBudgetAllocationResult> nodesToCreate,
            int advertiserId,
            int lineItemId,
            int[] creativeIds)
        {
            var nodeCount = nodesToCreate.Count();
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
                    nodeAllocation);
                try
                {
                    // Find the existing campaign and its profile id
                    var campaignCode = nodeAllocation.Value.AllocationId;
                    var oldCampaign = this.Client.GetCampaignByCode(advertiserId, campaignCode);
                    var profileId = (int)oldCampaign[AppNexusValues.ProfileId];

                    // Delete the old campaign and create a new one
                    this.DeleteCampaign(advertiserId, campaignCode);
                    this.CreateCampaign(nodeAllocation, advertiserId, lineItemId, profileId, creativeIds);
                    this.metrics.UpdatedCampaigns.Add(nodeAllocation.Value.AllocationId);
                }
                catch (AppNexusClientException ance)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error exporting allocation node '{0}' of campaign '{1}' ({2}): {3}",
                        nodeAllocation.Value.AllocationId,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        ance);
                    this.metrics.FailedAllocationExports.Add(nodeAllocation.Value.AllocationId);
                }
            }
        }

        /// <summary>Deletes the specified campaigns</summary>
        /// <param name="campaignsToDelete">
        /// List of allocation ids to delete the campaigns for.
        /// Allocation ids are used as the AppNexus 'code' values.
        /// </param>
        /// <param name="advertiserId">The AppNexus advertiser's id</param>
        private void DeleteCampaigns(
            IEnumerable<string> campaignsToDelete,
            int advertiserId)
        {
            var count = campaignsToDelete.Count();
            var i = 0;

            foreach (var allocationId in campaignsToDelete)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "{0}/{1} - Deleting campaign for budget-less allocation node '{2}' of campaign '{3}' ({4}).",
                    ++i,
                    count,
                    allocationId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);

                try
                {
                    this.DeleteCampaign(advertiserId, allocationId);
                    this.metrics.DeletedCampaigns.Add(allocationId);
                }
                catch (AppNexusClientException ance)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error deleting AppNexus campaign for allocation node '{0}' of campaign '{1}' ({2}): {3}",
                        allocationId,
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        ance);
                    this.metrics.FailedAllocationExports.Add(allocationId);
                }
            }
        }

        /// <summary>
        /// Delete the campaign for the allocation
        /// </summary>
        /// <remarks>Campaign code is the per node result's AllocationId</remarks>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="campaignCode">AppNexus campaign code</param>
        private void DeleteCampaign(int advertiserId, string campaignCode)
        {
            try
            {
                var campaign = this.Client.GetCampaignByCode(advertiserId, campaignCode);
                if (campaign == null)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "No campaign found with code '{0}' for advertiser '{1}'",
                        campaignCode,
                        advertiserId);
                    return;
                }

                this.Client.DeleteCampaign(advertiserId, (int)campaign[AppNexusValues.Id]);
                LogManager.Log(
                    LogLevels.Trace,
                    "Deleted AppNexus campaign with code '{0}' for advertiser '{1}'",
                    campaignCode,
                    advertiserId);
            }
            catch (AppNexusClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Error deleting campaign with code '{0}' for advertiser '{1}'",
                    campaignCode,
                    advertiserId);
                throw;
            }
        }

        /*
        /// <summary>Updates the AppNexus campaign for the allocation</summary>
        /// <param name="nodeAllocation">The per-node budget allocation result and measure set</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <param name="creativeIds">The AppNexus ids of the creatives</param>
        [Obsolete("Delete existing campaigns and re-create them", true)]
        private void UpdateCampaign(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> nodeAllocation,
            int advertiserId,
            int[] creativeIds)
        {
            var measures = nodeAllocation.Key;
            var allocation = nodeAllocation.Value;

            var lifetimeBudget = allocation.LifetimeMediaSpend + allocation.ExportBudget;
            var lifetimeImpressionCap = allocation.LifetimeImpressions + allocation.PeriodImpressionCap;
            var campaignName = DynamicAllocationActivityUtilities.MakeExportUnitNameForAllocation(
                allocation,
                this.CampaignEntity,
                measures, 
                lifetimeBudget);

            try
            {
                this.Client.UpdateCampaign(
                    allocation.AllocationId,
                    advertiserId,
                    campaignName,
                    creativeIds,
                    true,
                    this.CampaignEntity.StartDate,
                    this.CampaignEntity.EndDate,
                    lifetimeBudget,
                    lifetimeImpressionCap,
                    allocation.MaxBid);

                LogManager.Log(
                    LogLevels.Trace,
                    "Updated AppNexus Campaign for Allocation '{0}' of Campaign '{1}' ({2})",
                    allocation.AllocationId,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);
            }
            catch (AppNexusClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to update AppNexus Campaign for Allocation '{0}'.",
                    allocation.AllocationId);
                throw;
            }
        }
        */

        /// <summary>Creates an AppNexus campaign for the allocation</summary>
        /// <param name="nodeAllocation">The per-node budget allocation result and measure set</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <param name="lineItemId">The AppNexus line-item id</param>
        /// <param name="profileId">The AppNexus targeting profile id</param>
        /// <param name="creativeIds">The AppNexus ids of the creatives</param>
        /// <returns>The created campaign's AppNexus id</returns>
        private int CreateCampaign(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> nodeAllocation,
            int advertiserId,
            int lineItemId,
            int profileId,
            int[] creativeIds)
        {
            var measures = nodeAllocation.Key;
            var allocation = nodeAllocation.Value;

            var lifetimeBudget = allocation.ExportBudget;
            var lifetimeImpressionCap = allocation.PeriodImpressionCap;
            var campaignName = DynamicAllocationActivityUtilities.MakeExportUnitNameForAllocation(
                allocation,
                this.CampaignEntity,
                measures,
                lifetimeBudget);

            try
            {
                var campaignId = this.Client.CreateCampaign(
                    advertiserId,
                    campaignName,
                    allocation.AllocationId,
                    lineItemId,
                    profileId,
                    creativeIds,
                    true,
                    this.CampaignEntity.StartDate,
                    this.CampaignEntity.EndDate,
                    lifetimeBudget,
                    lifetimeImpressionCap,
                    allocation.MaxBid);

                LogManager.Log(
                    LogLevels.Trace,
                    "Created AppNexus Campaign '{0}' for Allocation '{1}' of Campaign '{2}' ({3})",
                    campaignId,
                    allocation,
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId);
                return campaignId;
            }
            catch (AppNexusClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to create AppNexus Campaign for Allocation '{0}'.",
                    allocation.AllocationId);
                throw;
            }
        }

        /// <summary>Creates an AppNexus targeting profile for the measures of the allocation</summary>
        /// <param name="nodeAllocation">The node allocation</param>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <returns>The AppNexus id of the created profile</returns>
        private int CreateNodeAllocationProfile(
            KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> nodeAllocation,
            int advertiserId)
        {
            var measures = nodeAllocation.Key;
            var allocation = nodeAllocation.Value;

            try
            {
                var profileId = this.Client.CreateCampaignProfile(
                    advertiserId,
                    allocation.AllocationId,
                    this.GetAppNexusAllowUnknownAge(measures),
                    this.GetAppNexusAgeRange(measures),
                    this.GetAppNexusGender(measures),
                    this.GetAppNexusSegments(measures),
                    this.GetAppNexusMetroCodes(measures),
                    this.GetAppNexusRegions(measures),
                    this.GetPageLocation(measures),
                    this.GetInventoryAttributes(measures),
                    this.GetContentCategories(measures),
                    this.GetDomainListTargets(measures));
                LogManager.Log(
                    LogLevels.Trace,
                    "Created AppNexus Targeting Profile '{0}' for Measures '{1}' (AllocationId {2})",
                    profileId,
                    measures,
                    allocation.AllocationId);
                return profileId;
            }
            catch (AppNexusClientException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to create AppNexus Targeting Profile for Allocation '{0}'.",
                    allocation.AllocationId);
                throw;
            }
        }

        /// <summary>Keys for the ExporterDefaults dictionary</summary>
        private static class ExporterDefaultsKeys
        {
            /// <summary>Default Include Domain List</summary>
            public const string DefaultDomainListDictionaryKey = "DefaultIncludeDomainList";
        }
    }
}
