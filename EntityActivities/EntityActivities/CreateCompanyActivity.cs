//-----------------------------------------------------------------------
// <copyright file="CreateCompanyActivity.cs" company="Rare Crowds Inc">
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
