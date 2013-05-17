// -----------------------------------------------------------------------
// <copyright file="GetUsersActivity.cs" company="Rare Crowds Inc">
// Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting all users
    /// </summary>
    /// <remarks>
    /// Gets all the users
    /// ResultValues
    ///   Users - List of users as json list
    /// </remarks>
    [Name(EntityActivityTasks.GetUsers)]
    [ResultValues(EntityActivityValues.Users)]
    public class GetUsersActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);

            // Get the users
            var users = this.Repository.GetAllUsers(externalContext);

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Users, users.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }
    }
}