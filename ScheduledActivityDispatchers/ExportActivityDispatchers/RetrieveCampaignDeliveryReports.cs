//-----------------------------------------------------------------------
// <copyright file="RetrieveCampaignDeliveryReports.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
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
    [SourceName("Delivery.RetrieveCampaignReports"), Schedule(typeof(ConfigIntervalSchedule), "Delivery.RetrieveCampaignReportsSchedule")]
    public class RetrieveCampaignDeliveryReports : ScheduledActivitySource
    {
        /// <summary>Maximum number of simultaneous report requests allowed per delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, int> MaxSimultaneousRequests =
            new Dictionary<DeliveryNetworkDesignation, int>
            {
                { DeliveryNetworkDesignation.AppNexus, Config.GetIntValue("AppNexus.MaxReportRequests") },
                { DeliveryNetworkDesignation.GoogleDfp, Config.GetIntValue("GoogleDfp.MaxReportRequests") }
            };

        /// <summary>Dictionary of report request tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> RequestReportTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.RequestCampaignReport },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.RequestCampaignReport }
            };

        /// <summary>Dictionary of report retrieve tasks by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> RetrieveReportTasks =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, AppNexusActivityTasks.RetrieveCampaignReport },
                { DeliveryNetworkDesignation.GoogleDfp, GoogleDfpActivityTasks.RetrieveCampaignReport }
            };

        /// <summary>Dictionary of scheduler registries for requested reports to retrieve by delivery network</summary>
        private static readonly IDictionary<DeliveryNetworkDesignation, string> ReportsToRetrieveSchedulerRegistries =
            new Dictionary<DeliveryNetworkDesignation, string>
            {
                { DeliveryNetworkDesignation.AppNexus, DeliveryNetworkSchedulerRegistries.ReportsToRetrieve + DeliveryNetworkDesignation.AppNexus.ToString() },
                { DeliveryNetworkDesignation.GoogleDfp, DeliveryNetworkSchedulerRegistries.ReportsToRetrieve + DeliveryNetworkDesignation.GoogleDfp.ToString() }
            };

        /// <summary>Gets the time between AppNexus delivery report requests</summary>
        internal static TimeSpan ReportRequestFrequency
        {
            get { return Config.GetTimeSpanValue("Delivery.ReportFrequency"); }
        }

        /// <summary>
        /// Gets the time to wait on in-progress requests before moving
        /// them from in-progress back to the present slot.
        /// </summary>
        internal static TimeSpan ReportRequestExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.ReportsRequestExpiry"); }
        }

        /// <summary>
        /// Gets the time to wait on in-progress retrieves before moving
        /// them from in-progress back to the present slot.
        /// </summary>
        internal static TimeSpan ReportRetrieveExpiry
        {
            get { return Config.GetTimeSpanValue("Delivery.ReportsRetrieveExpiry"); }
        }

        /// <summary>Gets the system auth user id</summary>
        private static string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }
        }

        /// <summary>Creates new scheduled activity requests</summary>
        public override void CreateScheduledRequests()
        {
            this.RequestReports();
            this.RetrieveReports();
        }

        /// <summary>Handler for activity results</summary>
        /// <param name="request">The request</param>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityRequest request, ActivityResult result)
        {
            if (result.Task == AppNexusActivityTasks.RequestCampaignReport ||
                result.Task == GoogleDfpActivityTasks.RequestCampaignReport)
            {
                OnRequestReportResult(request, result);
            }
            else if (result.Task == AppNexusActivityTasks.RetrieveCampaignReport ||
                result.Task == GoogleDfpActivityTasks.RetrieveCampaignReport)
            {
                OnRetrieveReportResult(request, result);
            }
            else
            {
                // TODO: Add a custom attribute for ScheduledActivitySource for the expected Task names
                // TODO: and move this warning down into the ScheduledActivitySource base class.
                LogManager.Log(
                    LogLevels.Warning,
                    "{0} - Received result for unknown activity: '{1}' (RequestId = {2})",
                    this.GetType().FullName,
                    result.Task,
                    result.RequestId);
            }
        }

        /// <summary>Handles the results of the request report activity</summary>
        /// <param name="request">The activity request</param>
        /// <param name="result">The activity result</param>
        internal static void OnRequestReportResult(ActivityRequest request, ActivityResult result)
        {
            string campaignEntityId;
            request.Values.TryGetValue(EntityActivityValues.CampaignEntityId, out campaignEntityId);

            try
            {
                if (!result.Succeeded)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Error requesting report for campaign '{0}' (workitem '{1}')\n{2}: {3}",
                        campaignEntityId,
                        request.Id,
                        result.Error.ErrorId,
                        result.Error.Message);
                    return;
                }

                // Verify success result is well formed
                if (!result.Values.ContainsKey(DeliveryNetworkActivityValues.ReportId) ||
                    !result.Values.ContainsKey(EntityActivityValues.CampaignEntityId) ||
                    !result.Values.ContainsKey(EntityActivityValues.CompanyEntityId) ||
                    !result.Values.ContainsKey(DeliveryNetworkActivityValues.RescheduleReportRequest))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Request report result for campaign '{0}' missing required value(s) (workitem '{1}')\n{2}",
                        campaignEntityId,
                        request.Id,
                        result.SerializeToXml());
                    return;
                }

                // Get values from result
                var reportId = result.Values[DeliveryNetworkActivityValues.ReportId];
                var companyEntityId = result.Values[EntityActivityValues.CompanyEntityId];
                var reschedule = bool.Parse(result.Values[DeliveryNetworkActivityValues.RescheduleReportRequest]);
                var deliveryNetwork = RequestReportTasks.Single(kvp => kvp.Value == result.Task).Key;
                var requestSucceeded = !string.IsNullOrWhiteSpace(reportId);

                // Schedule retrieval for the requested report (if request succeeded)
                if (!requestSucceeded)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        true,
                        "Report request failed for {0} report of campaign '{1}'. Rescheduling.",
                        deliveryNetwork,
                        campaignEntityId);
                }
                else
                {
                    if (!Scheduler.AddToSchedule<string, string>(
                        ReportsToRetrieveSchedulerRegistries[deliveryNetwork],
                        DateTime.UtcNow,
                        reportId,
                        campaignEntityId,
                        companyEntityId))
                    {
                        LogManager.Log(
                            LogLevels.Error,
                            true,
                            "Unable to schedule delivery report retrieval for {0} report {1} of campaign '{2}'",
                            deliveryNetwork,
                            reportId,
                            campaignEntityId);
                        return;
                    }
                    else
                    {
                        LogManager.Log(
                            LogLevels.Information,
                            "Scheduled retrieval for {0} report '{1}' of campaign '{2}'",
                            deliveryNetwork,
                            reportId,
                            campaignEntityId);
                    }
                }

                // Schedule the next report request
                if (reschedule)
                {
                    var nextReportRequestTime = requestSucceeded ? DateTime.UtcNow + ReportRequestFrequency : DateTime.UtcNow;
                    if (!Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                        DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                        nextReportRequestTime,
                        campaignEntityId,
                        companyEntityId,
                        deliveryNetwork))
                    {
                        LogManager.Log(
                            LogLevels.Error,
                            true,
                            "Unable to schedule next delivery report retrieval for {0} campaign '{1}' at ",
                            deliveryNetwork,
                            campaignEntityId,
                            nextReportRequestTime);
                    }
                }
            }
            finally
            {
                // Remove the completed entry from the scheduler
                if (!Scheduler.RemoveCompletedEntry<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                    campaignEntityId))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Unable to remove completed entry for campaign '{0}' from schedule {1}",
                        campaignEntityId,
                        DeliveryNetworkSchedulerRegistries.ReportsToRequest);
                }
            }
        }

        /// <summary>Handles the results of the retrieve report activity</summary>
        /// <param name="request">The activity request</param>
        /// <param name="result">The activity result</param>
        internal static void OnRetrieveReportResult(ActivityRequest request, ActivityResult result)
        {
            var reportId = request.Values[DeliveryNetworkActivityValues.ReportId];
            var campaignEntityId = request.Values[EntityActivityValues.CampaignEntityId];
            var deliveryNetwork = RetrieveReportTasks.Single(kvp => kvp.Value == result.Task).Key;

            try
            {
                if (!result.Succeeded)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Error retrieving report '{0}' for campaign '{1}'\n{2}: {3}",
                        reportId,
                        campaignEntityId,
                        result.Error.ErrorId,
                        result.Error.Message);
                }
                else
                {
                    LogManager.Log(
                        LogLevels.Information,
                        "Retrieved report '{0}' for campaign '{1}'.",
                        reportId,
                        campaignEntityId);
                }
            }
            finally
            {
                if (!Scheduler.RemoveCompletedEntry<string, string>(
                    ReportsToRetrieveSchedulerRegistries[deliveryNetwork],
                    reportId))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Unable to remove report retrieval '{0}' (campaign '{1}') from in-progress reports-to-retrieve registry.",
                        reportId,
                        campaignEntityId);
                }
            }
        }

        /// <summary>Request delivery reports</summary>
        internal void RequestReports()
        {
            LogManager.Log(LogLevels.Trace, "Checking for reports to request.");
            Scheduler.ProcessEntries<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                DateTime.UtcNow,
                ReportRequestExpiry,
                (campaignEntityId, companyEntityId, deliveryNetwork) =>
                    this.SubmitRequestReportRequest(
                        campaignEntityId,
                        companyEntityId,
                        deliveryNetwork));
        }

        /// <summary>Retrieve previously requested reports</summary>
        internal void RetrieveReports()
        {
            foreach (var reportsToRetrieveSchedulerRegistry in ReportsToRetrieveSchedulerRegistries)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Checking for reports to retrieve from {0} in '{1}'...",
                    reportsToRetrieveSchedulerRegistry.Key,
                    reportsToRetrieveSchedulerRegistry.Value);

                Scheduler.ProcessEntries<string, string>(
                    reportsToRetrieveSchedulerRegistry.Value,
                    DateTime.UtcNow,
                    ReportRetrieveExpiry,
                    (reportId, campaignEntityId, companyEntityId) =>
                        this.SubmitRetrieveReportRequest(
                            reportId,
                            campaignEntityId,
                            companyEntityId,
                            reportsToRetrieveSchedulerRegistry.Key));
            }
        }

        /// <summary>Submit an activity request to request a report</summary>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitRequestReportRequest(string campaignEntityId, string companyEntityId, DeliveryNetworkDesignation deliveryNetwork)
        {
            // Check how many reports are in progress
            var inProgressReportRequests =
                Scheduler.GetInProgressCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            var inProgressReportRetrieves =
                Scheduler.GetInProgressCount<string, string>(
                ReportsToRetrieveSchedulerRegistries[deliveryNetwork]);
            var pendingReportRetrieves =
                Scheduler.GetScheduledCount<string, string>(
                ReportsToRetrieveSchedulerRegistries[deliveryNetwork],
                DateTime.UtcNow);
            
            var reportsInProgress =
                inProgressReportRequests +
                inProgressReportRetrieves +
                pendingReportRetrieves;

            if (reportsInProgress >= MaxSimultaneousRequests[deliveryNetwork])
            {
                // Cannot submit the request until others complete
                return false;
            }

            // Create a request to get a report for this line item
            var request = new ActivityRequest
            {
                Task = RequestReportTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId }
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.BackgroundFetch, true);
        }

        /// <summary>Submit an activity request to retrieve a report</summary>
        /// <param name="reportId">ID of the report in the delivery network</param>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="deliveryNetwork">Delivery Network</param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitRetrieveReportRequest(
            string reportId,
            string campaignEntityId,
            string companyEntityId,
            DeliveryNetworkDesignation deliveryNetwork)
        {
            // Create a request to retrieve this report
            var request = new ActivityRequest
            {
                Task = RetrieveReportTasks[deliveryNetwork],
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { DeliveryNetworkActivityValues.ReportId, reportId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId }
                }
            };
            return this.SubmitRequest(request, ActivityRuntimeCategory.BackgroundFetch, true);
        }
    }
}
