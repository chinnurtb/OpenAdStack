//-----------------------------------------------------------------------
// <copyright file="GetCompaniesForUserActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
    /// Activity for getting companies for a user
    /// </summary>
    /// <remarks>
    /// Gets all companies associated with a user
    /// RequiredValues:
    ///   UserId - UserId of the user to get companies for
    /// ResultValues:
    ///   Companies - List of companies as a json list
    /// </remarks>
    [Name(EntityActivityTasks.GetCompaniesForUser)]
    [ResultValues(EntityActivityValues.Companies)]
    public class GetCompaniesForUserActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var userId = request.Values[EntityActivityValues.AuthUserId];

            UserEntity user = null;

            try
            {
                // Get the user
                user = this.Repository.GetUser(internalContext, userId);
            }
            catch (ArgumentException)
            {
                return UserNotFoundError(userId);
            }

            var companiesToReturnList = EntityActivityUtilities.GetCompaniesForUser(this.AccessHandler, this.Repository, externalContext, user);

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Companies, companiesToReturnList.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }
    }
}
