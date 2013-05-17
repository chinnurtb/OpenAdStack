//-----------------------------------------------------------------------
// <copyright file="ExportCreatives.cs" company="Rare Crowds Inc">
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
    [SourceName("Delivery.ExportCreatives"), Schedule(typeof(ConfigIntervalSchedule), "Delivery.ExportCreativesSchedule")]
    public class ExportCreatives : ScheduledActivitySource
    {
        /// <summary>Dictionary of the export campaign tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> ExportCreativeTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.ExportCreative },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.ExportCreative }
            };

        /// <summary>
        /// Gets the time after which an in-progress creative export is considered expired and should be resubmitted
        /// </summary>
        private static TimeSpan CreativeExportRequestExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.ExportCreativeRequestExpiry"); }
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
            LogManager.Log(LogLevels.Trace, "Checking for creatives to export.");
            Scheduler.ProcessEntries<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToExport,
                DateTime.UtcNow,
                CreativeExportRequestExpiry,
                (creativeEntityId, companyEntityId, deliveryNetwork) =>
                    this.SubmitCreativeExportRequest(
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
            if (!result.Succeeded)
            {
                // Log an error alert for further investigation
                LogManager.Log(
                    LogLevels.Error,
                    true,
                    "Error exporting creative '{0}' ({1} workitem '{2}'): {3}",
                    creativeEntityId,
                    request.Task,
                    request.Id,
                    result.Error.Message);
            }
            else
            {
                LogManager.Log(
                    LogLevels.Information,
                    "Exported creative {0}:\n{1}",
                    creativeEntityId,
                    result.Values.ToString<string, string>());
            }

            Scheduler.RemoveCompletedEntry<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToExport,
                creativeEntityId);
        }

        /// <summary>Submit an activity request to export a creative</summary>
        /// <param name="creativeEntityId">Creative EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitCreativeExportRequest(
            string creativeEntityId,
            string companyEntityId,
            DeliveryNetworkDesignation deliveryNetwork)
        {
            // Create a request to get a report for this line item
            var request = new ActivityRequest
            {
                Task = ExportCreativeTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CreativeEntityId, creativeEntityId },
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.Background, true);
        }
    }
}
