// -----------------------------------------------------------------------
// <copyright file="IncrementExportCountsActivity.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Activities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;
using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Activity for incrementing the export counts of exported nodes
    /// </summary>
    /// <remarks>
    /// RequiredValues:
    ///   CompanyEntityId - EntityId of the Company of the campaign exported
    ///   CampaignEntityId - EntityId of the campaign exported
    ///   ExportAllocationIds - AllocationIds of the nodes successfully exported (to increment)
    /// </remarks>
    [Name(DynamicAllocationActivityTasks.IncrementExportCounts)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId,
        DeliveryNetworkActivityValues.ExportedAllocationIds)]
    public class IncrementExportCountsActivity : DynamicAllocationActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
            var exportedAllocationIds = request.Values[DeliveryNetworkActivityValues.ExportedAllocationIds]
                .Split(',').Select(id => id.Trim()).ToArray();
            try
            {
                var context = CreateContext(request, EntityActivityValues.CompanyEntityId);
                this.IncrementExportCounts(context, companyEntityId, campaignEntityId, exportedAllocationIds);

                // Return success
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, campaignEntityId.ToString() }
                });
            }
            catch (DataAccessEntityNotFoundException enotfound)
            {
                return this.EntityNotFoundError(enotfound);
            }
            catch (ActivityException e)
            {
                return this.ErrorResult(e);
            }
        }

        /// <summary>
        /// Increment the export counts in the campaign's active budget allocation
        /// </summary>
        /// <param name="context">Repository request context</param>
        /// <param name="companyEntityId">The company EntityId</param>
        /// <param name="campaignEntityId">The campaign EntityId</param>
        /// <param name="exportedAllocationIds">The AllocationIds of the exported nodes</param>
        private void IncrementExportCounts(
            RequestContext context,
            EntityId companyEntityId,
            EntityId campaignEntityId,
            string[] exportedAllocationIds)
        {
            // Get the active budget allocations
            var dac = new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId);
            var budgetAllocation = dac.RetrieveActiveAllocation();

            LogManager.Log(
                LogLevels.Trace,
                "Incrementing export counts for {0} of {1} nodes.",
                exportedAllocationIds.Length,
                budgetAllocation.PerNodeResults.Count);

            var exportedMeasureSets = budgetAllocation.PerNodeResults
                .Where(pnr => exportedAllocationIds.Contains(pnr.Value.AllocationId))
                .Select(pnr => pnr.Key);
            var dynamicAllocationEngine = dac.CreateDynamicAllocationEngine();
            budgetAllocation = dynamicAllocationEngine.IncrementExportCounts(budgetAllocation, exportedMeasureSets);

            // Updated the campaign's active allocation
            var updatedAllocationBlob = dac.CreateAndAssociateActiveAllocationBlob(budgetAllocation);

            // Save the active allocation blob and the updated campaign
            this.Repository.SaveEntity(context, updatedAllocationBlob);

            var forceUpdateContext = context.BuildContextWithNameFilters(
                new List<string>(), new List<string> { daName.AllocationSetActive });
            this.Repository.ForceUpdateEntity(forceUpdateContext, dac.CampaignEntity);
        }
    }
}
