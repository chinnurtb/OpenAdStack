//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaigns.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Activities;
using AppNexusUtilities;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using GoogleDfpUtilities;
using ScheduledActivities;
using ScheduledActivities.Schedules;
using Utilities.Storage;

namespace DeliveryNetworkActivityDispatchers
{
    /// <summary>
    /// Source for activity requests to update creative status
    /// </summary>
    /// <remarks>Scheduled to run every 30 minutes</remarks>
    [SourceName("Delivery.ExportDACampaigns"), Schedule(typeof(ConfigIntervalSchedule), "Delivery.ExportDACampaignsSchedule")]
    public class ExportDynamicAllocationCampaigns : ScheduledActivitySource
    {
        /// <summary>Dictionary of the export campaign tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> ExportCampaignTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.ExportDACampaign },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.ExportDACampaign }
            };

        /// <summary>
        /// Gets the time after which an in-progress campaign export is considered expired and should be resubmitted
        /// </summary>
        internal static TimeSpan ExportCampaignRequestExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.ExportDACampaignRequestExpiry"); }
        }

        /// <summary>Gets the system auth user id</summary>
        private static string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }
        }

        /// <summary>Creates new scheduled activity requests</summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is logged. Processing needs to continue with other requests.")]
        public override void CreateScheduledRequests()
        {
            LogManager.Log(LogLevels.Trace, "Checking for campaigns to export.");
            Scheduler.ProcessEntries<string, string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CampaignsToExport,
                DateTime.UtcNow,
                ExportCampaignRequestExpiry,
                (campaignEntityId, companyEntityId, exportAllocationsEntityId, deliveryNetwork) =>
                    this.SubmitDynamicAllocationCampaignExportRequest(
                        campaignEntityId,
                        companyEntityId,
                        exportAllocationsEntityId,
                        deliveryNetwork));
        }

        /// <summary>Handler for activity results</summary>
        /// <param name="request">The request</param>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityRequest request, ActivityResult result)
        {
            if (result.Task == DynamicAllocationActivityTasks.IncrementExportCounts)
            {
                HandleIncrementExportCountsResult(request, result);
            }
            else if (ExportCampaignTasks.Values.Contains(result.Task))
            {
                this.HandleExportCampaignResult(request, result);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "{0} - Received result for unknown activity: '{1}' (RequestId = {2})",
                    this.GetType().FullName,
                    result.Task,
                    result.RequestId);
            }
        }

        /// <summary>
        /// Handle results from the increment export counts activity
        /// </summary>
        /// <param name="request">The activity request</param>
        /// <param name="result">The activity result</param>
        private static void HandleIncrementExportCountsResult(ActivityRequest request, ActivityResult result)
        {
            if (!result.Succeeded)
            {
                LogManager.Log(
                    LogLevels.Error,
                    true,
                    "Failed to increment export counts for campaign {0} (workitem '{1}')\n{2}: {3}",
                    request.Values[EntityActivityValues.CampaignEntityId],
                    request.Id,
                    result.Error.ErrorId,
                    result.Error.Message);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Information,
                    "Incremented export counts for campaign {0}",
                    request.Values[EntityActivityValues.CampaignEntityId]);
            }
        }

        /// <summary>
        /// Handle results from the delivery network export activities
        /// </summary>
        /// <param name="request">The activity request</param>
        /// <param name="result">The activity result</param>
        private void HandleExportCampaignResult(ActivityRequest request, ActivityResult result)
        {
            var campaignEntityId = request.Values[EntityActivityValues.CampaignEntityId];

            try
            {
                if (!result.Succeeded)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Error exporting campaign '{0}' (workitem '{1}')\n{2}: {3}",
                        campaignEntityId,
                        request.Id,
                        result.Error.ErrorId,
                        result.Error.Message);
                    return;
                }

                // Check for values required in successful results
                if (!result.Values.ContainsKey(DeliveryNetworkActivityValues.ExportedAllocationIds))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Export campaign result for campaign '{0}' missing required '{1}' value (workitem '{2}')\n{3}",
                        campaignEntityId,
                        DeliveryNetworkActivityValues.ExportedAllocationIds,
                        request.Id,
                        result.SerializeToXml());
                    return;
                }

                // Log the exported allocations
                var companyEntityId = request.Values[EntityActivityValues.CompanyEntityId];
                var exportedAllocationIds = result.Values[DeliveryNetworkActivityValues.ExportedAllocationIds];
                var exportedAllocationsCount = exportedAllocationIds.Split(',').Length;
                LogManager.Log(
                    LogLevels.Information,
                    "Exported {0} allocations for campaign {1} (company {2})\n{3}",
                    exportedAllocationsCount,
                    campaignEntityId,
                    companyEntityId,
                    exportedAllocationIds);

                // Submit a request to increment the DA export counts
                var incrementExportCountsRequest = new ActivityRequest
                {
                    Task = DynamicAllocationActivityTasks.IncrementExportCounts,
                    Values =
                    {
                        { EntityActivityValues.AuthUserId, SystemAuthUserId },
                        { EntityActivityValues.CompanyEntityId, companyEntityId },
                        { EntityActivityValues.CampaignEntityId, campaignEntityId },
                        { DeliveryNetworkActivityValues.ExportedAllocationIds, exportedAllocationIds },
                    }
                };
                this.SubmitRequest(incrementExportCountsRequest, ActivityRuntimeCategory.Background, true);
            }
            finally
            {
                // Remove the completed campaign export entry from the export schedule
                Scheduler.RemoveCompletedEntry<string, string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.CampaignsToExport,
                    campaignEntityId);
            }
        }

        /// <summary>Submits an activity request to export a dynamic allocation campaign</summary>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="exportAllocationsEntityId">Export Allocations EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        private bool SubmitDynamicAllocationCampaignExportRequest(
            string campaignEntityId,
            string companyEntityId,
            string exportAllocationsEntityId,
            DeliveryNetworkDesignation deliveryNetwork)
        {
            // Create a request to get a report for this line item
            var request = new ActivityRequest
            {
                Task = ExportCampaignTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                    { DynamicAllocationActivityValues.ExportAllocationsEntityId, exportAllocationsEntityId }
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.Background, true);
        }
    }
}
