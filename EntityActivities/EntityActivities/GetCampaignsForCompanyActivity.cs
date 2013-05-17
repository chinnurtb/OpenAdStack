// -----------------------------------------------------------------------
// <copyright file="GetCampaignsForCompanyActivity.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{    
    /// <summary>
    /// Activity for getting Campaigns for Company.
    /// </summary>
    /// <remarks>
    /// Gets all campaigns associated with the company
    /// RequiredValues:
    ///   CompanyEntityId - The ExternalEntityId of the company to get campaigns for
    /// ResultValues:
    ///   Campaigns - List of campaigns as a json list
    /// </remarks>
    [Name(EntityActivityTasks.GetCampaignsForCompany)]
    [RequiredValues(EntityActivityValues.ParentEntityId)]
    [ResultValues(EntityActivityValues.Campaigns)]
    public class GetCampaignsForCompanyActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(
                RepositoryContextType.ExternalEntityGet, request);
            var internalContext = CreateRepositoryContext(
                RepositoryContextType.InternalEntityGet, request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.ParentEntityId]);

            var company = this.Repository.TryGetEntity(internalContext, companyEntityId);
            if (company == null)
            {
                return EntityNotFoundError(companyEntityId);
            }

            // Get the campaigns associated with the Company by the "campaign" association.
            // Note: "campaign" will be replaced later with a constant, probably something like UserEntity.CampaignAssociationName
            var campaignEntityIds = company.Associations
                .Where(a => a.ExternalName == "campaign")
                .Select(a => a.TargetEntityId)
                .ToArray();

            var campaigns = this.Repository.GetEntitiesById(externalContext, campaignEntityIds).ToArray();

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Campaigns, campaigns.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }
    }
}
