//-----------------------------------------------------------------------
// <copyright file="UpdateBudgetAllocations.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using Activities;
using ConfigManager;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;
using ScheduledActivities;
using ScheduledActivities.Schedules;
using Utilities.Storage;

namespace DynamicAllocationActivityDispatchers
{
    /// <summary>
    /// Source for activity requests to update budget allocations
    /// </summary>
    [SourceName("DynamicAllocations.UpdateBudgetAllocations"), Schedule(typeof(ConfigIntervalSchedule), "DynamicAllocation.UpdateBudgetAllocationsSchedule")]
    public class UpdateBudgetAllocations : ScheduledActivitySource
    {
        /// <summary>
        /// Gets the time to wait on in-progress requests before moving
        /// them from in-progress back to the present slot.
        /// </summary>
        private static TimeSpan UpdateAllocationsRequestExpiry
        {
            get { return Config.GetTimeSpanValue("DynamicAllocation.UpdateAllocationsRequestExpiry"); }
        }

        /// <summary>Gets the system auth user id</summary>
        private static string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }
        }

        /// <summary>Creates new scheduled activity requests</summary>
        public override void CreateScheduledRequests()
        {
            LogManager.Log(LogLevels.Trace, "Checking for campaigns to reallocate.");

            Scheduler.ProcessEntries<string, DateTime, bool>(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate,
                DateTime.UtcNow,
                UpdateAllocationsRequestExpiry,
                (campaignEntityId, companyEntityId, allocationStartDate, isInitialAllocation) =>
                    this.SubmitUpdateAllocationRequest(
                        campaignEntityId,
                        companyEntityId,
                        allocationStartDate,
                        isInitialAllocation));
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
                    "Updated budget allocations ({0}) failed for campaign '{1}': {2}\n\nRequest:\n{3}\n\nResult:\n{4}",
                    result.Task,
                    campaignEntityId,
                    result.Error.Message,
                    request.SerializeToXml().Replace("<", "&lt;").Replace(">", "&gt;"),
                    result.SerializeToXml().Replace("<", "&lt;").Replace(">", "&gt;"));
            }
            else
            {
                LogManager.Log(
                    LogLevels.Information,
                    true,
                    "Updated budget allocations ({0}) for campaign '{1}'.\n\nRequest:\n{2}\n\nResult:\n{3}",
                    result.Task,
                    campaignEntityId,
                    request.SerializeToXml().Replace("<", "&lt;").Replace(">", "&gt;"),
                    result.SerializeToXml().Replace("<", "&lt;").Replace(">", "&gt;"));
            }

            Scheduler.RemoveCompletedEntry<string, DateTime, bool>(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate,
                campaignEntityId);
        }

        /// <summary>Submits an update allocation request for a campaign</summary>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="allocationStartDate">Allocation start date</param>
        /// <param name="isInitialAllocation">
        /// Whether the allocation is for the initialization phase or regular reallocation
        /// </param>
        /// <returns>True if the request was submitted; otherwise, false.</returns>
        internal bool SubmitUpdateAllocationRequest(
            string campaignEntityId,
            string companyEntityId,
            DateTime allocationStartDate,
            bool isInitialAllocation)
        {
            // Create a request to update allocations for this campaign
            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                    { DynamicAllocationActivityValues.AllocationStartDate, allocationStartDate.ToString("o", CultureInfo.InvariantCulture) },
                    { DynamicAllocationActivityValues.IsInitialAllocation, isInitialAllocation.ToString() },
                }
            };

            // Submit the request and, if successful, move the corresponding
            // registry item to the in-progress slot.
            LogManager.Log(
                LogLevels.Trace,
                "Submitting {0} request for campaign '{1}' (AllocationStartDate: '{2}')",
                DynamicAllocationActivityTasks.GetBudgetAllocations,
                campaignEntityId,
                allocationStartDate);
            if (!this.SubmitRequest(request, ActivityRuntimeCategory.Background, true))
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Unable to submit {0} request for campaign '{1}'!",
                    DynamicAllocationActivityTasks.GetBudgetAllocations,
                    campaignEntityId);
                return false;
            }

            LogManager.Log(
                LogLevels.Trace,
                "Submitted {0} request for campaign '{1}'. Moving to InProgress.",
                DynamicAllocationActivityTasks.GetBudgetAllocations,
                campaignEntityId);
            return true;
        }
    }
}
