//-----------------------------------------------------------------------
// <copyright file="GetCreativesActivity.cs" company="Rare Crowds Inc">
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
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using EntityActivities;
using EntityUtilities;
using Newtonsoft.Json;

namespace AppNexusActivities.AppActivities
{
    /// <summary>
    /// Activity for getting creatives for an AppNexus advertiser
    /// </summary>
    /// <remarks>
    /// Retrieves creatives for an advertiser from AppNexus
    /// RequiredValues:
    ///   AuthUserId - The user's user id (used to call AppNexus APIs)
    ///   CompanyEntityId - The EntityId of the advertiser CompanyEntity
    ///   CampaignEntityId - The EntityId of the Campaign
    /// </remarks>
    [Name(AppNexusActivityTasks.GetCreatives)]
    [RequiredValues(
        EntityActivityValues.AuthUserId,
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId)]
    public class GetCreativesActivity : AppNexusActivity
    {
        /// <summary>Gets the activity's runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.InteractiveFetch; }
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            var context = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);

            // Check that user is an AppNexusApp user
            var userId = request.Values[EntityActivityValues.AuthUserId];
            var user = this.Repository.GetUser(context, userId);
            if (user.GetUserType() != UserType.AppNexusApp)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Activity not supported for non-AppNexusApp users");
            }

            // Get the company's AppNexus advertiser id
            var company = this.Repository.TryGetEntity(context, companyEntityId) as CompanyEntity;
            if (company == null)
            {
                return this.EntityNotFoundError(companyEntityId);
            }

            var advertiserId = ((CompanyEntity)company).GetAppNexusAdvertiserId();
            if (!advertiserId.HasValue)
            {
                return this.ErrorResult(
                    ActivityErrorId.GenericError,
                    "The company '{0}' ({1}) does not have an AppNexus advertiser id ({2})",
                    company.ExternalName,
                    company.ExternalEntityId,
                    AppNexusEntityProperties.AdvertiserId);
            }

            // Get the campaign to check for existing creatives
            var campaign = this.Repository.TryGetEntity(context, campaignEntityId) as CampaignEntity;
            if (campaign == null)
            {
                return this.EntityNotFoundError(campaignEntityId);
            }

            // Get the campaigns's existing creatives
            var associatedCreatives = campaign.Associations
                .Where(a => a.TargetEntityCategory == CreativeEntity.CategoryName)
                .Select(a => this.Repository.TryGetEntity(context, a.TargetEntityId))
                .Where(c => c != null)
                .OfType<CreativeEntity>()
                .Select(c => c.GetAppNexusCreativeId())
                .Where(id => id != null)
                .ToArray();

            // Get the advertiser's creatives (excluding those already imported)
            using (var client = CreateAppNexusClient(user.UserId))
            {
                var creatives = client.GetAdvertiserCreatives(advertiserId.Value);
                var unassociatedCreatives = creatives
                    .Where(creative => !associatedCreatives.Contains((int)creative[AppNexusValues.Id]));

                var creativesJson = JsonConvert.SerializeObject(unassociatedCreatives);
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { AppNexusActivityValues.Creatives, creativesJson }
                });
            }
        }
    }
}
