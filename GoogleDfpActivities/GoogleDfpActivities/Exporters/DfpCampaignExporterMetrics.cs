//-----------------------------------------------------------------------
// <copyright file="DfpCampaignExporterMetrics.cs" company="Rare Crowds Inc">
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
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpActivities.Exporters
{
    /// <summary>Container for DFP export metrics</summary>
    internal class DfpCampaignExporterMetrics
    {
        /// <summary>
        /// Format for the metrics summary log message
        /// </summary>
        private const string SummaryLogMessageFormat =
@"Export summary for campaign '{0}' ({1}):
    Allocations for export:   {2}
    Line-Items:
        Previously exported:  {3}
        Created:              {4}/{5}
        Updated/unpaused:     {6}/{7}
        Paused:               {8}/{9}
    Failures :                {10}";

        /// <summary>
        /// Format for the metrics details log message
        /// </summary>
        private const string DetailedLogMessageFormat =
@"Export details for campaign '{0}' ({1}):
Allocations for export:
{3}

Previously exported:
{4}

Create Succeeded:
{5}

Create Failed:
{6}

Update Succeeded:
{7}

Update Failed:
{8}

To Pause ({9}/{10} succeeded):
{11}
";

        /// <summary>The CampaignEntity being exported</summary>
        private readonly CampaignEntity campaignEntity;

        /// <summary>All of the allocations for the CampaignEntity</summary>
        private readonly IEnumerable<PerNodeBudgetAllocationResult> allocations;

        /// <summary>Gets the allocation/line-item ids previously exported</summary>
        private readonly IDictionary<string, long> previouslyExported;

        /// <summary>Initializes a new instance of the DfpCampaignExporterMetrics class</summary>
        /// <param name="campaignEntity">The CampaignEntity being exported</param>
        /// <param name="allocations">The allocations being exported</param>
        /// <param name="lineItems">The previously exported line-items</param>
        public DfpCampaignExporterMetrics(CampaignEntity campaignEntity, IEnumerable<PerNodeBudgetAllocationResult> allocations, IEnumerable<Dfp.LineItem> lineItems)
        {
            this.campaignEntity = campaignEntity;
            this.allocations = allocations;
            this.previouslyExported = lineItems.ToDictionary(li => li.externalId, li => li.id);

            this.ToCreate = new List<string>();
            this.Created = new Dictionary<string, long>();
            this.ToUpdate = new Dictionary<string, long>();
            this.Updated = new Dictionary<string, long>();
            this.ToPause = new Dictionary<string, long>();
            this.Paused = 0;
        }

        /// <summary>Gets the allocation ids to be created</summary>
        public IList<string> ToCreate { get; private set; }
        
        /// <summary>Gets the allocation/line-item ids that were created</summary>
        public IDictionary<string, long> Created { get; private set; }
        
        /// <summary>Gets the allocation/line-item ids to update</summary>
        public IDictionary<string, long> ToUpdate { get; private set; }

        /// <summary>Gets the allocation/line-item ids that were updated</summary>
        public IDictionary<string, long> Updated { get; private set; }

        /// <summary>Gets the allocation/line-item ids to be paused</summary>
        public IDictionary<string, long> ToPause { get; private set; }

        /// <summary>Gets or sets the count of line-items that were paused</summary>
        public int Paused { get; set; }

        /// <summary>Logs the collected metrics</summary>
        /// <remarks>
        /// Includes log entry with a summary of the metrics logged as either an
        /// Information or Error entry depending upon if there were any failures.
        /// Also logs a detailed Trace entry including individual allocation ids.
        /// </remarks>
        public void LogMetrics()
        {
            var failures =
                (this.ToCreate.Count - this.Created.Count) +
                (this.ToUpdate.Count - this.Updated.Count) +
                (this.ToPause.Count - this.Paused);

            LogManager.Log(
                failures > 0 ? LogLevels.Error : LogLevels.Information,
                failures > 0,
                SummaryLogMessageFormat,
                this.campaignEntity.ExternalName,
                this.campaignEntity.ExternalEntityId,
                this.allocations.Count(),
                this.previouslyExported.Count,
                this.Created.Count,
                this.ToCreate.Count,
                this.Updated.Count,
                this.ToUpdate.Count,
                this.Paused,
                this.ToPause.Count,
                failures);

            LogManager.Log(
                failures > 0 ? LogLevels.Error : LogLevels.Trace,
                DetailedLogMessageFormat,
                this.campaignEntity.ExternalName,
                this.campaignEntity.ExternalEntityId,
                string.Join(", ", this.allocations.Select(node => node.AllocationId)),
                string.Join(", ", this.previouslyExported.Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))),
                string.Join(", ", this.Created.Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))),
                string.Join(", ", this.ToCreate.Where(id => !this.Created.ContainsKey(id))),
                string.Join(", ", this.Updated.Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))),
                string.Join(", ", this.ToUpdate.Except(this.Updated).Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))),
                string.Join(", ", this.previouslyExported.Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))),
                this.Paused,
                this.ToPause.Count,
                string.Join(", ", this.ToPause.Select(kvp => "{0} ({1})".FormatInvariant(kvp.Key, kvp.Value))));
        }
    }
}
