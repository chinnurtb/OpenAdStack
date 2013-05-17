//-----------------------------------------------------------------------
// <copyright file="GetCompaniesForCompanyActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting companies for a company
    /// </summary>
    /// <remarks>
    /// Gets all companies associated with a company
    /// RequiredValues:
    ///   CompanyEntityId - ExternalEntityId of the company to get associated companies for
    ///   TODO: Add Association.ExternalName as a request value???
    /// ResultValues:
    ///   Companies - List of companies as a json list
    /// </remarks>
    [Name(EntityActivityTasks.GetCompaniesForCompany)]
    [RequiredValues(EntityActivityValues.CompanyEntityId)]
    [ResultValues(EntityActivityValues.Companies)]
    public class GetCompaniesForCompanyActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);

            // Get the company
            var company = this.Repository.TryGetEntity(internalContext, companyEntityId) as CompanyEntity;
            if (company == null)
            {
                return EntityNotFoundError(companyEntityId);
            }

            // Get the companies associated with the company by the external type. for now, grab both the advertisers and agencies
            // ToDo: Verify the associations returned can be both advertisers and agencies
            var companyEntityIds = company.Associations
                .Where(a => a.TargetExternalType == "Advertiser" || a.TargetExternalType == "Agency")
                .Select(a => a.TargetEntityId)
                .ToArray();
            var companies = this.Repository.GetEntitiesById(externalContext, companyEntityIds).ToArray();

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Companies, companies.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }
    }
}
