//-----------------------------------------------------------------------
// <copyright file="IAppNexusCampaignExporter.cs" company="Rare Crowds Inc">
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
using DynamicAllocation;

namespace AppNexusActivities
{
    /// <summary>
    /// Interface for exporters of dynamic allocation campaigns to AppNexus.
    /// </summary>
    internal interface IAppNexusCampaignExporter : IDisposable
    {
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
        /// <exception cref="AppNexusClient.AppNexusClientException">
        /// An error occured while calling AppNexus.
        /// </exception>
        string[] ExportAppNexusCampaigns(
            int advertiserId,
            int lineItemId,
            int[] creativeIds,
            BudgetAllocation activeAllocations,
            IDictionary<string, bool> campaignAllocationIds,
            string[] exportAllocationIds);

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
        int ExportAppNexusLineItem(
            int advertiserId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget,
            BudgetAllocation activeAllocations);
    }
}
