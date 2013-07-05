// -----------------------------------------------------------------------
// <copyright file="GetValuationsActivity.cs" company="Rare Crowds Inc">
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
