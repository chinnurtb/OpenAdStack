﻿// -----------------------------------------------------------------------
// <copyright file="GetValuationsActivity.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using DynamicAllocationUtilities;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Activity for calculating targeting attribute valuations
    /// </summary>
    /// <remarks>
    /// Gets valuations for the provided campaign definition
    /// RequiredValues:
    ///   CampaignDefinition - Inputs to the budget allocation calculations
    /// ResultValues:
    ///   Valuations - Valuations for each targeting attribute set 
    /// </remarks>
    [Name(DynamicAllocationActivityTasks.GetValuations)]
    [RequiredValues("EntityId", "Approved"), ResultValues("Valuations")]
    public class GetValuationsActivity : DynamicAllocationActivity
    {
        /// <summary>
        /// Process the request
        /// </summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var useApprovedInputs = request.Values["Approved"].ToUpperInvariant() == "TRUE";
            var companyId = new EntityId(request.Values["ParentEntityId"]);
            var campaignId = new EntityId(request.Values["EntityId"]);

            try
            {
                var dacFac = new DynamicAllocationCampaignFactory();
                dacFac.BindRuntime(this.Repository);
                var dac = dacFac.BuildDynamicAllocationCampaign(companyId, campaignId, useApprovedInputs);

                // Get valuations from campaign entity. Don't update cache if requesting approved valuations
                var valuationsCache = new ValuationsCache(this.Repository);
                var suppressCacheUpdate = useApprovedInputs;
                var valuations = valuationsCache.GetValuations(dac, suppressCacheUpdate);

                // create and return result
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { "Valuations", ValuationInputs.SerializeValuationsToJson(valuations) }
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
    }
}
