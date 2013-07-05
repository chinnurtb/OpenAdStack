//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaignActivity.cs" company="Rare Crowds Inc">
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
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Newtonsoft.Json;
using ScheduledActivities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for exporting AppNexus campaigns from a CampaignEntity's active DA budget allocations
    /// </summary>
    /// <remarks>
    /// Optimistically updates existing line-item and campaigns. If not found then creates them.
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    /// ResultValues:
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    ///   LineItemId - The AppNexus id of the line-item under which campaigns were exported
    /// </remarks>
    [Name(AppNexusActivityTasks.ExportDACampaign)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId,
        DynamicAllocationActivityValues.ExportAllocationsEntityId)]
    [ResultValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId,
        AppNexusActivityValues.LineItemId,
        DeliveryNetworkActivityValues.ExportedAllocationIds)]
    public class ExportDynamicAllocationCampaignActivity : AppNexusActivity
    {
        /// <summary>
        /// Gets the time between AppNexus delivery report requests
        /// </summary>
        internal static TimeSpan ReportRequestFrequency
        {
            get { return Config.GetTimeSpanValue("Delivery.ReportFrequency"); }
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
            var exportAllocationsEntityId = new EntityId(request.Values[DynamicAllocationActivityValues.ExportAllocationsEntityId]);

            // Get the entities
            var companyEntity = this.Repository.GetEntity<CompanyEntity>(context, companyEntityId);
            var campaignEntity = this.Repository.GetEntity<CampaignEntity>(context, campaignEntityId);
            var campaignOwner = this.Repository.GetUser(context, campaignEntity.GetOwnerId());
            var activeAllocationsBlobEntity = this.Repository.GetEntity<BlobEntity>(context, exportAllocationsEntityId);

            // Create the AppNexus campaigns
            var activeAllocationsJson = activeAllocationsBlobEntity.DeserializeBlob<string>();
            var activeAllocations = JsonConvert.DeserializeObject<BudgetAllocation>(activeAllocationsJson);
            string[] exportedAllocationIds = new string[0];

            using (var client = this.CreateAppNexusClient(
                context, companyEntity, campaignEntity, campaignOwner))
            {
                using (var exporter = GetCampaignExporter(
                    companyEntity, campaignEntity, campaignOwner))
                {
                    // Get the AppNexus advertiser id
                    var advertiserId = companyEntity.GetAppNexusAdvertiserId() ??
                        this.CreateAppNexusAdvertiser(client, context, ref companyEntity);

                    // Create a line item if not previously created
                    var lineItemId = campaignEntity.GetAppNexusLineItemId();
                    if (lineItemId == null)
                    {
                        this.CreateLineItem(
                            exporter,
                            context,
                            (int)advertiserId,
                            activeAllocations,
                            ref campaignEntity);

                        lineItemId = campaignEntity.GetAppNexusLineItemId();
                    }
                    else
                    {
                        // Line-item already exists. Update it.
                        TryUpdateLineItem(client, (int)advertiserId, campaignEntity);
                    }

                    // Get the line-item from AppNexus
                    var lineItem = client.GetLineItemById((int)advertiserId, (int)lineItemId);

                    // Get the previously created campaigns from the line-item
                    var campaignAllocationIds = (lineItem[AppNexusValues.Campaigns] as object[] ?? new object[0])
                        .Cast<IDictionary<string, object>>()
                        .ToDictionary(
                            campaign => DynamicAllocationActivityUtilities.ParseAllocationIdFromExportUnitName((string)campaign[AppNexusValues.Name]),
                            campaign => (string)campaign[AppNexusValues.State] == AppNexusValues.StateActive);
                    LogManager.Log(
                        LogLevels.Trace,
                        "Found {0} previously exported AppNexus campaigns for '{1}' ({2}):\n{3}",
                        campaignAllocationIds.Count,
                        campaignEntity.ExternalName,
                        campaignEntity.ExternalEntityId,
                        string.Join(", ", campaignAllocationIds.Keys));

                    // Get the audited AppNexus creative ids
                    var auditedCreativeIds = this.GetAuditedAppNexusCreativeIds(client, context, campaignEntity);
                    if (auditedCreativeIds.Length == 0)
                    {
                        // Fail if none of the creatives have passed audit
                        var message =
                            "Unable to export campaign '{0}' ({1}): None of the associated creatives have passed AppNexus audit"
                            .FormatInvariant(campaignEntity.ExternalName, campaignEntity.ExternalEntityId);
                        LogManager.Log(LogLevels.Error, true, message);
                        throw new ActivityException(ActivityErrorId.GenericError, message);
                    }

                    // Get the ids of allocations to be exported
                    var exportAllocationIds = activeAllocations.PerNodeResults.Values
                        .Where(node => node.ExportBudget > 0m)
                        .Select(node => node.AllocationId)
                        .ToArray();

                    // Upsert the AppNexus campaigns
                    exportedAllocationIds = exporter.ExportAppNexusCampaigns(
                        (int)advertiserId,
                        (int)lineItemId,
                        auditedCreativeIds,
                        activeAllocations,
                        campaignAllocationIds,
                        exportAllocationIds);
                }

                // Return the campaign and line-item id to the activity request source
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CompanyEntityId, companyEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, campaignEntity.ExternalEntityId.ToString() },
                    { AppNexusActivityValues.LineItemId, campaignEntity.GetAppNexusLineItemId().ToString() },
                    { DeliveryNetworkActivityValues.ExportedAllocationIds, string.Join(",", exportedAllocationIds) },
                });
            }
        }

        /// <summary>Gets the exporter for the campaign</summary>
        /// <param name="companyEntity">Advertiser company</param>
        /// <param name="campaignEntity">Campaign being exported</param>
        /// <param name="campaignOwner">Owner of the campaign being exported</param>
        /// <returns>The campaign exporter</returns>
        private static IAppNexusCampaignExporter GetCampaignExporter(
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
        {
            var version = campaignEntity.GetExporterVersion();
            switch (version)
            {
                case 0:
                    return new AppNexusCampaignLegacyExporter(
                        companyEntity,
                        campaignEntity,
                        campaignOwner);
                case 1:
                    return new AppNexusCampaignExporter(
                        companyEntity,
                        campaignEntity,
                        campaignOwner);
                default:
                    throw new ArgumentException(
                        "Unknown measures version '{0}' for campaign '{1}' ({2})"
                        .FormatInvariant(
                            version,
                            campaignEntity.ExternalName,
                            campaignEntity.ExternalEntityId),
                        "campaignEntity");
            }
        }

        /// <summary>
        /// Updates the line-item for a campaign entity
        /// </summary>
        /// <param name="client">AppNexus API client</param>
        /// <param name="advertiserId">The advertiser id</param>
        /// <param name="campaignEntity">The campaign entity</param>
        private static void TryUpdateLineItem(
            IAppNexusApiClient client,
            int advertiserId,
            CampaignEntity campaignEntity)
        {
            var lineItemId = campaignEntity.GetAppNexusLineItemId();

            LogManager.Log(
                LogLevels.Trace,
                "Updating AppNexus line-item {0} for campaign '{1}' ({2}) under AppNexus advertiser {3}",
                lineItemId,
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId,
                advertiserId);

            var lifetimeMediaBudgetCap = (decimal)(campaignEntity.GetLifetimeMediaBudgetCap() ?? (decimal?)campaignEntity.Budget);
            client.UpdateLineItem(
                (int)lineItemId,
                advertiserId,
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId.ToString(),
                true,
                campaignEntity.StartDate,
                campaignEntity.EndDate,
                lifetimeMediaBudgetCap);
        }

        /// <summary>Creates an AppNexus line-item for a campaign entity</summary>
        /// <param name="exporter">AppNexus Exporter</param>
        /// <param name="context">Entity repository request context</param>
        /// <param name="advertiserId">AppNexus advertiser id</param>
        /// <param name="activeAllocations">Active budget allocations</param>
        /// <param name="campaignEntity">Campaign entity</param>
        private void CreateLineItem(
            IAppNexusCampaignExporter exporter,
            RequestContext context,
            int advertiserId,
            BudgetAllocation activeAllocations,
            ref CampaignEntity campaignEntity)
        {
            LogManager.Log(
                LogLevels.Trace,
                "Creating AppNexus line-item for campaign '{0}' ({1}) under AppNexus advertiser {2}",
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId,
                advertiserId);

            var lineItemId = exporter.ExportAppNexusLineItem(
                advertiserId,
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId.ToString(),
                true,
                campaignEntity.StartDate,
                campaignEntity.EndDate,
                campaignEntity.Budget,
                activeAllocations);

            campaignEntity.SetAppNexusLineItemId(lineItemId);
            this.Repository.SaveEntity(context, campaignEntity);

            LogManager.Log(
                LogLevels.Trace,
                "Created AppNexus line item '{0}' for campaign '{1}' ({2})",
                lineItemId,
                campaignEntity.ExternalName,
                campaignEntity.ExternalEntityId);

            // Register the line-item for reports
            var initialReportRequest = DateTime.UtcNow + ReportRequestFrequency;
            if (!Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                initialReportRequest,
                campaignEntity.ExternalEntityId.ToString(),
                context.ExternalCompanyId.ToString(),
                DeliveryNetworkDesignation.AppNexus))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Failed to schedule initial report request for AppNexus line-item '{0}' for campaign entity '{1}' ({2}) at {3}.",
                    lineItemId,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    initialReportRequest);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Information,
                    "Initial report request for AppNexus line-item '{0}' for campaign entity '{1}' ({2}) scheduled at {3}.",
                    lineItemId,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    initialReportRequest);
            }
        }
    }
}
