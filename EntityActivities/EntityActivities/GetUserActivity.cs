//-----------------------------------------------------------------------
// <copyright file="GetUserActivity.cs" company="Rare Crowds Inc">
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
    using System.Linq;

    /// <summary>
    /// Activity for getting a user by their external id (entityid)
    /// </summary>
    /// <remarks>
    /// Gets the user with the specified xId
    /// RequiredValues
    ///   EntityId - The EntityId for the user
    /// ResultValues
    ///   User - The user as json
    /// </remarks>
    [Name(EntityActivityTasks.GetUser)]
    [RequiredValues(EntityActivityValues.EntityId)]
    [ResultValues(EntityActivityValues.User)]
    public class GetUserActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var entityId = request.Values[EntityActivityValues.EntityId];
            var userId = request.Values[EntityActivityValues.AuthUserId];
            var context = CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);
            var includeAccessList = GetAccessList(request);
            
            UserEntity user = null;

            if (!string.IsNullOrEmpty(entityId))
            {
                // Get the user
                user = this.Repository.TryGetEntity(context, entityId) as UserEntity;
                if (user == null)
                {
                    return EntityNotFoundError(entityId);
                }
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    // Get the user
                    user = this.Repository.GetUser(context, userId);
                }
                catch (ArgumentException)
                {
                    return EntityNotFoundError(entityId);
                }
            }

            if (includeAccessList)
            {
                var accessList = this.UserAccessRepository.GetUserAccessList(entityId);
 
                // iterate the list and create a string
                string completeAccessList = accessList.Aggregate(string.Empty, (current, accessItem) => current + (accessItem + "|"));
                user.Properties.Add(new EntityProperty("AccessList", completeAccessList));
            }

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.User, user.SerializeToJson(new EntitySerializationFilter(request.QueryValues)) }
            });
        }

        /// <summary>Gets whether to include the Access List from the request.</summary>
        /// <param name="request">The ActivityRequest</param>
        /// <returns>True if the Access List should be included; otherwise, false.</returns>
        private static bool GetAccessList(ActivityRequest request)
        {
            var entityQueries = new EntityActivityQuery(request.QueryValues);
            var requestParam = request.Values.ContainsKey(EntityActivityValues.AccessList) && request.Values[EntityActivityValues.AccessList].ToUpperInvariant() == "INCLUDE";
            return requestParam || entityQueries.ContainsFlag("WithAccessList");
        }
    }
}
