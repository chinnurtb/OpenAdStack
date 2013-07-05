//-----------------------------------------------------------------------
// <copyright file="AppNexusCampaignExportMetrics.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;

namespace AppNexusActivities
{
    /// <summary>Container for export metrics</summary>
    internal class AppNexusCampaignExportMetrics
    {
        /// <summary>
        /// Format for the metrics summary log message
        /// </summary>
        private const string SummaryLogMessageFormat =
@"Export summary for campaign '{0}' ({1}):
    Total allocation nodes:     {2}
    Allocations for export:     {3}
    Nodes w/daily media budget: {4}
        Nodes without budget:   {5}
    Nodes w/export budget:      {6}
        Nodes without budget:   {7}
    AppNexus Export:
        Created campaigns:      {8}
        Created profiles:       {9}
        Updated campaigns:      {10}
        Uncreated campaigns:    {11}
        Deleted campaigns:      {12}
    Failed nodes:               {13}";

        /// <summary>
        /// Format for the metrics details log message
        /// </summary>
        private const string DetailedLogMessageFormat =
@"Export details for campaign '{0}' ({1}):
Nodes w/daily media budget:
{3}

Nodes without budget:
{4}

Nodes w/export budget:
{5}

Nodes without budget:
{6}

Created campaigns:
{7}

Created profiles:
{8}

Updated campaigns:
{9}

Uncreated campaigns:
{10}

Deleted campaigns:
{11}

Failed nodes:
{12}";

        /// <summary>The CampaignEntity being exported</summary>
        private readonly CampaignEntity campaignEntity;

        /// <summary>The number of allocations to be exported</summary>
        private readonly int allocationsForExportCount;

        /// <summary>All of the allocations for the CampaignEntity</summary>
        private readonly IEnumerable<PerNodeBudgetAllocationResult> allocations;

        /// <summary>
        /// Initializes a new instance of the AppNexusCampaignExportMetrics class
        /// </summary>
        /// <param name="allocationsForExportCount">
        /// Number of nodes for export (creates and updates)
        /// </param>
        /// <param name="campaignEntity">
        /// The CampaignEntity being exported
        /// </param>
        /// <param name="allocations">
        /// The allocations for the CampaignEntity being exported
        /// </param>
        public AppNexusCampaignExportMetrics(int allocationsForExportCount, CampaignEntity campaignEntity, IEnumerable<PerNodeBudgetAllocationResult> allocations)
        {
            this.allocationsForExportCount = allocationsForExportCount;
            this.campaignEntity = campaignEntity;
            this.allocations = allocations;

            this.FailedAllocationExports = new List<string>();
            this.UpdatedCampaigns = new List<string>();
            this.CreatedCampaigns = new Dictionary<string, int>();
            this.CreatedProfiles = new Dictionary<string, int>();
            this.DeletedCampaigns = new List<string>();
            this.UncreatedCampaigns = new List<string>();
        }

        /// <summary>
        /// Gets the list of allocation ids for which export failed
        /// (includes creates, updates and deactivations)
        /// </summary>
        public IList<string> FailedAllocationExports { get; private set; }

        /// <summary>
        /// Gets the list of allocation ids for which campaigns were updated (active)
        /// </summary>
        public IList<string> UpdatedCampaigns { get; private set; }

        /// <summary>
        /// Gets the list of allocation ids for which campaigns were created
        /// along with their AppNexus ids
        /// </summary>
        public IDictionary<string, int> CreatedCampaigns { get; private set; }

        /// <summary>
        /// Gets the list of allocation ids for which targeting profiles corresponding
        /// to their measure sets were created along with their AppNexus ids
        /// </summary>
        public IDictionary<string, int> CreatedProfiles { get; private set; }

        /// <summary>
        /// Gets the list of allocation ids for which campaigns were deleted
        /// </summary>
        public IList<string> DeletedCampaigns { get; private set; }

        /// <summary>
        /// Gets the list of allocation ids for which campaigns were not created
        /// </summary>
        public IList<string> UncreatedCampaigns { get; private set; }

        /// <summary>Logs the collected metrics</summary>
        /// <remarks>
        /// Includes log entry with a summary of the metrics logged as either an
        /// Information or Error entry depending upon if there were any failures.
        /// Also logs a detailed Trace entry including individual allocation ids.
        /// </remarks>
        public void LogMetrics()
        {
            var nodesWithPeriodMediaBudget = this.allocations
                .Where(node => node.PeriodMediaBudget > 0)
                .Select(node => node.AllocationId);

            var nodesWithoutPeriodMediaBudget = this.allocations
                .Where(node => node.PeriodMediaBudget <= 0)
                .Select(node => node.AllocationId);

            var nodesWithExportBudget = this.allocations
                .Where(node => node.ExportBudget > 0)
                .Select(node => node.AllocationId);

            var nodesWithoutExportBudget = this.allocations
                .Where(node => node.ExportBudget <= 0)
                .Select(node => node.AllocationId);

            LogManager.Log(
                this.FailedAllocationExports.Count > 0 ? LogLevels.Error : LogLevels.Information,
                true, ////this.FailedAllocationExports.Count > 0,
                SummaryLogMessageFormat,
                this.campaignEntity.ExternalName,
                this.campaignEntity.ExternalEntityId,
                this.allocations.Count(),
                this.allocationsForExportCount,
                nodesWithPeriodMediaBudget.Count(),
                nodesWithoutPeriodMediaBudget.Count(),
                nodesWithExportBudget.Count(),
                nodesWithoutExportBudget.Count(),
                this.CreatedCampaigns.Count,
                this.CreatedProfiles.Count,
                this.UpdatedCampaigns.Count,
                this.UncreatedCampaigns.Count,
                this.DeletedCampaigns.Count,
                this.FailedAllocationExports.Count);

            LogManager.Log(
                LogLevels.Trace,
                DetailedLogMessageFormat,
                this.campaignEntity.ExternalName,
                this.campaignEntity.ExternalEntityId,
                this.allocations.Count(),
                string.Join(", ", nodesWithPeriodMediaBudget),
                string.Join(", ", nodesWithoutPeriodMediaBudget),
                string.Join(", ", nodesWithExportBudget),
                string.Join(", ", nodesWithoutExportBudget),
                this.CreatedCampaigns.ToString<string, int>(),
                this.CreatedProfiles.ToString<string, int>(),
                string.Join(", ", this.UpdatedCampaigns),
                string.Join(", ", this.UncreatedCampaigns),
                string.Join(", ", this.DeletedCampaigns),
                string.Join(", ", this.FailedAllocationExports));
        }
    }
}
