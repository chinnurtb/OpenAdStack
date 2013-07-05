// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApproveValuationInputsActivity.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Activity for approving Valuation Inputs allocations
    /// </summary>
    /// <remarks>
    /// Gets budget allocations for the provided inputs
    /// RequiredValues:
    ///   BudgetAllocationInputs - Inputs to the budget allocation calculations
    /// ResultValues:
    ///   BudgetAllocations - Outputs from the budget allocation calculations
    /// </remarks>
    [Name(DynamicAllocationActivityTasks.ApproveValuationInputs)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId), ResultValues("BudgetAllocation")]
    public class ApproveValuationInputsActivity : DynamicAllocationActivity
    {
        /// <summary>Override to handle results of submitted requests.</summary>
        /// <param name="result">The result of the previously submitted work item</param>
        public override void OnActivityResult(ActivityResult result)
        {
            LogManager.Log(
                LogLevels.Trace,
                "ApproveValuationInputsActivity: Submit request result for DAApproveValuationsInputs Result:{0}",
                result.Succeeded);
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);

            try
            {
                var dacFac = new DynamicAllocationCampaignFactory();
                dacFac.BindRuntime(this.Repository);
                this.ApproveValuationInputs(context, dacFac, companyEntityId, campaignEntityId);
                return SuccessResult();
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

        /// <summary>Approves the current draft valuation inputs</summary>
        /// <remarks>
        /// If the initialization phase has not already completed, schedules
        /// reallocation for initialization phase with new inputs.
        /// </remarks>
        /// <param name="context">Entity request context</param>
        /// <param name="dacFac">An IDynamicAllocationCampaignFactory instance.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="campaignEntityId">Campaign entity id.</param>
        private void ApproveValuationInputs(
            RequestContext context, 
            IDynamicAllocationCampaignFactory dacFac,
            EntityId companyEntityId,
            EntityId campaignEntityId)
        {
            var dac = dacFac.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId);
            
            // Make sure we have cached valuations on the entity
            var cache = new ValuationsCache(this.Repository);
            cache.GetValuations(dac);

            // Refresh the campaign
            dac = dacFac.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId);
            var campaign = dac.CampaignEntity;
            
            // Set the 'InputsApprovedVersion' to the version of the refreshed campaign
            campaign.SetPropertyByName(daName.InputsApprovedVersion, (int)campaign.LocalVersion, PropertyFilter.Extended);

            // Save the campaign
            try
            {
                this.Repository.SaveEntity(context, campaign);
            }
            catch (DataAccessException e)
            {
                var msg = "Failed to save approved valuations version on campaign {0}."
                    .FormatInvariant((EntityId)campaign.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.DataAccess, msg, e);
            }

            // If the initialization phase has not yet completed schedule a reallocation to
            // immediately restart the initialization phase using the new valuation inputs.
            if (!campaign.GetInitializationPhaseComplete())
            {
                GetBudgetAllocationsActivity.ScheduleNextReallocation(
                    dac,
                    true, // immediate
                    ReallocationScheduleType.Initial,
                    DateTime.UtcNow);
            }
        }
    }
}
