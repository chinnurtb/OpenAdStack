//-----------------------------------------------------------------------
// <copyright file="CreateCompanyActivity.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for creating Company
    /// </summary>
    /// <remarks>
    /// Creates a company and adds an association to the user.
    /// RequiredValues:
    ///   CompanyEntityId - The ExternalEntityId of the company being created
    ///   Company - The company as json
    /// ResultValues:
    ///   Company - The created company as json and including any additional values added by the DAL
    /// </remarks>
    [Name(EntityActivityTasks.CreateCompany)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.MessagePayload)]
    [ResultValues(EntityActivityValues.Company)]
    public class CreateCompanyActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request);
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var companyEntityId = request.Values[EntityActivityValues.EntityId];
            var company = EntityJsonSerializer.DeserializeCompanyEntity(
                companyEntityId,
                request.Values[EntityActivityValues.MessagePayload]);

            // Add the new company
            this.Repository.AddCompany(externalContext, company);

            // Get the creator user and add an association to it for the new company
            // Note: "company" will be replaced later with a constant, probably something like UserEntity.CompanyAssociationName
            // var userId = new EntityId(request.Values["AuthUserId"]);
            var user = this.Repository.GetUser(internalContext, request.Values[EntityActivityValues.AuthUserId]);
            this.Repository.AssociateEntities(internalContext, user.ExternalEntityId, "company", new HashSet<IEntity> { company });

            // Return a result with the added company as the output
            // The added company will have additional data assigned by
            // the DAL such as the EntityId
            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Company, company.SerializeToJson() }
            });
        }
    }
}
