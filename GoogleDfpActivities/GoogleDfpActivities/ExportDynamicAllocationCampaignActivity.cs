//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaignActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Google.Api.Ads.Dfp.v201206;
using GoogleDfpActivities.Exporters;
using GoogleDfpActivities.Measures;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Newtonsoft.Json;
using ScheduledActivities;

namespace GoogleDfpActivities
{
    /// <summary>
    /// Activity for exporting Google DFP line-items from a CampaignEntity's active DA budget allocations
    /// </summary>
    /// <remarks>
    /// Optimistically updates existing order and line-items. If not found then creates them.
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    /// ResultValues:
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    ///   OrderId - The AppNexus id of the line-item under which campaigns were exported
    /// </remarks>
    [Name(GoogleDfpActivityTasks.ExportDACampaign)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId,
        DynamicAllocationActivityValues.ExportAllocationsEntityId)]
    [ResultValues(
        EntityActivityValues.CampaignEntityId,
        GoogleDfpActivityValues.OrderId)]
    public class ExportDynamicAllocationCampaignActivity : DfpActivity
    {
        /// <summary>
        /// Gets the time between AppNexus delivery report requests
        /// </summary>
        internal static TimeSpan ReportRequestFrequency
        {
            get { return Config.GetTimeSpanValue("GoogleDfp.ReportFrequency"); }
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessDfpRequest(ActivityRequest request)
        {
            // Get the entities
            var context = CreateContext(request);
            var companyEntity = this.Repository.GetEntity<CompanyEntity>(
                context, request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntity = this.Repository.GetEntity<CampaignEntity>(
                context, request.Values[EntityActivityValues.CampaignEntityId]);
            var campaignOwner = this.Repository.GetEntity<UserEntity>(
                context, campaignEntity.GetOwnerId());
            var exportAllocationsEntity = this.Repository.GetEntity<BlobEntity>(
                context, request.Values[DynamicAllocationActivityValues.ExportAllocationsEntityId]);
            var creativeEntities = this.GetCampaignCreatives(context, campaignEntity);

            try
            {
                // Create the DFP advertiser (if needed).
                using (var exporter = new DfpAdvertiserExporter(companyEntity))
                {
                    if (!exporter.AdvertiserExists)
                    {
                        var advertiserId = exporter.CreateAdvertiser();
                        companyEntity.SetDfpAdvertiserId(advertiserId);
                        this.Repository.SaveEntity(context, companyEntity);
                    }
                }

                // Create the exporter instance
                using (var exporter = new DfpCampaignExporter(
                    companyEntity, campaignEntity, campaignOwner, creativeEntities, exportAllocationsEntity))
                {
                    // Create the DFP order (if needed).
                    if (!exporter.OrderExists)
                    {
                        var orderId = exporter.CreateOrder();
                        campaignEntity.SetDfpOrderId(orderId);
                        this.Repository.SaveEntity(context, campaignEntity);

                        // Register the new order for report requests
                        var initialReportRequest = System.DateTime.UtcNow + ReportRequestFrequency;
                        if (!Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                            DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                            initialReportRequest,
                            campaignEntity.ExternalEntityId.ToString(),
                            context.ExternalCompanyId.ToString(),
                            DeliveryNetworkDesignation.GoogleDfp))
                        {
                            LogManager.Log(
                                LogLevels.Error,
                                "Failed to schedule initial report request for Google DFP order '{0}' for campaign entity '{1}' ({2}) at {3}.",
                                orderId,
                                campaignEntity.ExternalName,
                                campaignEntity.ExternalEntityId,
                                initialReportRequest);
                        }
                        else
                        {
                            LogManager.Log(
                                LogLevels.Information,
                                "Initial report request for Google DFP order '{0}' for campaign entity '{1}' ({2}) scheduled at {3}.",
                                orderId,
                                campaignEntity.ExternalName,
                                campaignEntity.ExternalEntityId,
                                initialReportRequest);
                        }
                    }

                    // Update the order with the latest values from the campaign
                    exporter.UpdateOrder();

                    // Upsert the Google DFP line-items
                    exporter.ExportLineItems();
                }

                // Return the campaign and line-item id to the activity request source
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, campaignEntity.ExternalEntityId.ToString() },
                    { GoogleDfpActivityValues.OrderId, campaignEntity.GetDfpOrderId().ToString() }
                });
            }
            catch (GoogleDfpClientException dfpe)
            {
                LogManager.Log(
                    LogLevels.Error,
                    true,
                    "Google DFP Export Failed for campaign '{0}' ({1}):\n{2}",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    dfpe);
                return this.DfpClientError(dfpe);
            }
        }

        /// <summary>Gets the CreativeEntities for the campaign</summary>
        /// <param name="context">Entity Repository Request Context</param>
        /// <param name="campaignEntity">Campaign Entity</param>
        /// <returns>The CreativeEntities</returns>
        private CreativeEntity[] GetCampaignCreatives(RequestContext context, CampaignEntity campaignEntity)
        {
            var creativeEntityIds =
                campaignEntity.Associations
                .Where(a => a.TargetEntityCategory == CreativeEntity.CreativeEntityCategory)
                .Select(a => a.TargetEntityId)
                .ToArray();
            return this.Repository.GetEntitiesById(context, creativeEntityIds)
                .Cast<CreativeEntity>()
                .ToArray();
        }
    }
}
