//-----------------------------------------------------------------------
// <copyright file="CleanupCampaigns.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Activities;
using AppNexusUtilities;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;
using GoogleDfpUtilities;
using ScheduledActivities;
using ScheduledActivities.Schedules;
using Utilities.Storage;

namespace DeliveryNetworkActivityDispatchers
{
    /// <summary>
    /// Source for activity requests to update budget allocations
    /// </summary>
    [SourceName("Delivery.CleanupCampaigns"), Schedule(typeof(ConfigIntervalSchedule), "Delivery.CleanupCampaignsSchedule")]
    public class CleanupCampaigns : ScheduledActivitySource
    {
        /// <summary>Dictionary of the campaign cleanup tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> CleanupCampaignTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.DeleteLineItem },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.DeleteOrder },
            };

        /// <summary>
        /// Gets the time to wait on in-progress requests before moving
        /// them from in-progress back to the present slot.
        /// </summary>
        private static TimeSpan CleanupLineItemRequestExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.CleanupCampaignsRequestExpiry"); }
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
            LogManager.Log(LogLevels.Trace, "Checking for campaigns to cleanup.");
            Scheduler.ProcessEntries<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CampaignsToCleanup,
                DateTime.UtcNow,
                CleanupLineItemRequestExpiry,
                (campaignEntityId, companyEntityId, deliveryNetwork) =>
                    this.SubmitCampaignCleanupRequest(
                        campaignEntityId,
                        companyEntityId,
                        deliveryNetwork));
        }

        /// <summary>Handler for activity results</summary>
        /// <param name="request">The request</param>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityRequest request, ActivityResult result)
        {
            var campaignEntityId = request.Values[EntityActivityValues.CampaignEntityId];
            if (!result.Succeeded)
            {
                LogManager.Log(
                    LogLevels.Error,
                    true,
                    "Cleanup for campaign '{0}' failed ({1}; workitem '{2}'): {3}",
                    campaignEntityId,
                    request.Task,
                    request.Id,
                    result.Error.Message);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Information,
                    "Cleaned up campaign '{0}' ({1}).",
                    campaignEntityId,
                    result.Task);
            }

            Scheduler.RemoveCompletedEntry<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CampaignsToCleanup,
                campaignEntityId);
        }

        /// <summary>Submit an activity request to cleanup a campaign</summary>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitCampaignCleanupRequest(
            string campaignEntityId,
            string companyEntityId,
            DeliveryNetworkDesignation deliveryNetwork)
        {
            // Create a request to update allocations for this campaign
            var request = new ActivityRequest
            {
                Task = CleanupCampaignTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId }
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.Background, true);
        }
    }
}
