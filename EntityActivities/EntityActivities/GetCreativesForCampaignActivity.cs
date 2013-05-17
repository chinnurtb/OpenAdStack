//-----------------------------------------------------------------------
// <copyright file="GetCreativesForCampaignActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting creatives for a campaign
    /// </summary>
    /// <remarks>
    /// Gets all creatives associated with a campaign
    /// RequiredValues:
    ///   CampaignEntityId - ExternalEntityId of the campaign to get associated creatives for
    /// ResultValues:
    ///   Creatives - List of creatives as a json list
    /// </remarks>
    [Name(EntityActivityTasks.GetCreativesForCampaign)]
    [RequiredValues(EntityActivityValues.EntityId)]
    [ResultValues(EntityActivityValues.Creatives)]
    public class GetCreativesForCampaignActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);

            // Get the campaign
            var campaign = this.Repository.GetEntity(internalContext, campaignEntityId) as CampaignEntity;

            // Get the creatives associated with the campaign by the external type.
            var creativeEntityIds = campaign.Associations
                .Where(a => a.TargetEntityCategory == CreativeEntity.CreativeEntityCategory)
                .Select(a => a.TargetEntityId)
                .ToArray();
            var creatives = this.Repository.GetEntitiesById(externalContext, creativeEntityIds).ToArray();

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Creatives, creatives.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }
    }
}
