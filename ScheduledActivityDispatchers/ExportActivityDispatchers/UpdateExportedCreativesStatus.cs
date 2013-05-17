//-----------------------------------------------------------------------
// <copyright file="UpdateExportedCreativesStatus.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// Source for activity requests to update creative status
    /// </summary>
    /// <remarks>Scheduled to run every 30 minutes</remarks>
    [SourceName("Delivery.UpdateCreativeStatus"), Schedule(typeof(ConfigIntervalSchedule), "Delivery.UpdateCreativeStatusSchedule")]
    public class UpdateExportedCreativesStatus : ScheduledActivitySource
    {
        /// <summary>Dictionary of the campaign cleanup tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> UpdateCreativeStatusTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.UpdateCreativeAuditStatus },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.UpdateCreativeStatus },
            };

        /// <summary>Gets the time between AppNexus creative status update requests</summary>
        internal static TimeSpan CreativeStatusUpdateFrequency
        {
            get { return Config.GetTimeSpanValue("Delivery.CreativeUpdateFrequency"); }
        }

        /// <summary>
        /// Gets the time after which an in-progress status update is considered expired and should be resubmitted
        /// </summary>
        internal static TimeSpan CreativeStatusUpdateRequestExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.CreativeStatusUpdateRequestExpiry"); }
        }

        /// <summary>Gets the system auth user id</summary>
        private static string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }
        }

        /// <summary>Creates new scheduled activity requests</summary>
        public override void CreateScheduledRequests()
        {
            Scheduler.ProcessEntries<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow,
                CreativeStatusUpdateRequestExpiry,
                (creativeEntityId, companyEntityId, deliveryNetwork) =>
                    this.SubmitUpdateCreativeStatusRequest(
                        creativeEntityId,
                        companyEntityId,
                        deliveryNetwork));
        }

        /// <summary>Handler for activity results</summary>
        /// <param name="request">The request</param>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityRequest request, ActivityResult result)
        {
            var creativeEntityId = request.Values[EntityActivityValues.CreativeEntityId];
            try
            {
                if (!result.Succeeded)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Error updating export status for creative '{0}' (workitem '{1}'): {2}",
                        creativeEntityId,
                        request.Id,
                        result.Error.Message);
                    return;
                }
                
                if (!result.Values.ContainsKey("AuditStatus"))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Update exported status result for creative '{0}' missing required '{1}' value (workitem '{2}'):\n{3}",
                        creativeEntityId,
                        "AuditStatus",
                        request.Id,
                        result.SerializeToXml());
                    return;
                }

                var companyEntityId = request.Values[EntityActivityValues.CompanyEntityId];
                var auditStatus = result.Values["AuditStatus"].ToUpperInvariant();
                if (auditStatus == "PENDING")
                {
                    // Reschedule to check again later
                    LogManager.Log(
                        LogLevels.Trace,
                        "Cretive '{0}' (company '{1}') audit pending. Rescheduling update.",
                        creativeEntityId,
                        companyEntityId);
                    Scheduler.AddToSchedule(
                        DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                        DateTime.UtcNow + CreativeStatusUpdateFrequency,
                        creativeEntityId,
                        companyEntityId,
                        DeliveryNetworkDesignation.AppNexus); // TODO: Fix to handle other delivery networks
                }
                else if (auditStatus == "AUDITED" || auditStatus == "NO_AUDIT")
                {
                    // End of the line: audit completed/unneeded
                    LogManager.Log(
                        LogLevels.Information,
                        "Creative '{0}' (company '{1}') audit completed. Audit Status: '{2}'",
                        creativeEntityId,
                        companyEntityId,
                        auditStatus);
                }
                else
                {
                    // End of the line: audit failed
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Creative '{0}' (company '{1}') audit failed. Audit Status: {2}",
                        creativeEntityId,
                        companyEntityId,
                        auditStatus);
                }
            }
            finally
            {
                // Remove the completed entry from in-progress
                Scheduler.RemoveCompletedEntry<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                    creativeEntityId);
            }
        }

        /// <summary>Submits an update creative status request</summary>
        /// <param name="creativeEntityId">Creative EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitUpdateCreativeStatusRequest(string creativeEntityId, string companyEntityId, DeliveryNetworkDesignation deliveryNetwork)
        {
            // Create a request to get a report for this line item
            var request = new ActivityRequest
            {
                Task = UpdateCreativeStatusTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CreativeEntityId, creativeEntityId }
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.Background, true);
        }
    }
}
