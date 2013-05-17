//-----------------------------------------------------------------------
// <copyright file="SaveUserActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;

namespace EntityActivities
{
    using System;

    /// <summary>
    /// Activity for saving users
    /// </summary>
    /// <remarks>
    /// Creates/Updates a user
    /// RequiredValues:
    ///   UserEntityId - The ExternalEntityId
    ///   User - The new/updated user as json
    /// ResultValues
    ///   User - The saved user as json and including any additional values added by the DAL
    /// </remarks>
    [Name(EntityActivityTasks.SaveUser)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.MessagePayload)]
    [ResultValues(EntityActivityValues.User)]
    public class SaveUserActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var user = EntityJsonSerializer.DeserializeUserEntity(
                new EntityId(request.Values[EntityActivityValues.EntityId]),
                request.Values[EntityActivityValues.MessagePayload]);

            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request);

            // Check for an existing user
            var existingUserEntity =
                this.Repository.TryGetEntity(externalContext, request.Values[EntityActivityValues.EntityId]) as UserEntity;

            // only do if user doesn't exist, so we don't overwrite previous values
            if (existingUserEntity == null)
            {
                var newUserEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
                var userEntityIdString = newUserEntityId.ToString();

                if (user.GetUserType() == UserType.AppNexusApp)
                {
                    // For AppNexus App users the UserId must be included
                    if (string.IsNullOrWhiteSpace(user.UserId))
                    {
                        return ErrorResult(ActivityErrorId.MissingRequiredInput, "Missing AppNexus App UserId ");
                    }
                }
                else
                {
                    // For non-AppNexus App users, the initial UserId is the EntityId
                    user.UserId = userEntityIdString;
                }

                // Add default permissions for a new user
                // Post access for user verify and read access for user entity
                var defaultAccessList = new List<string>
                    {
                        "USERVERIFICATION.HTML:#:GET:*",
                        "USER:*:#:GET:PROPERTIES",
                        "USER:{0}:#:POST:VERIFY".FormatInvariant(userEntityIdString),
                        "USER:{0}:#:GET:*".FormatInvariant(userEntityIdString),
                    };

                // add any other permissions passed in
                var additionalPermissions = user.AccessList.ToString().Split(
                    new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                defaultAccessList.AddRange(additionalPermissions);

                if (!this.UserAccessRepository.AddUserAccessList(newUserEntityId, defaultAccessList))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Unable to grant default access for user entity Id: {0}",
                        userEntityIdString);
                }

                var acl = new[]
                {
                    "USER:{0}:#:POST:VERIFY".FormatInvariant(userEntityIdString),
                    "USER:{0}:#:GET:*".FormatInvariant(userEntityIdString),
                    (string)user.AccessList
                };
                user.AccessList = string.Join("|", acl);

                // Submit followup request for new AppNexus users
                // TODO: Move this to user.html instead of breaking encapsulation here??
                if (user.GetUserType() == UserType.AppNexusApp)
                {
                    var newAppUserRequest = CreateRequestFromContext(
                        externalContext,
                        AppNexusUtilities.AppNexusActivityTasks.NewAppUser,
                        new Dictionary<string, string>
                        {
                            { EntityActivityValues.EntityId, user.ExternalEntityId.ToString() }
                        });
                    if (!this.SubmitRequest(newAppUserRequest, true))
                    {
                        LogManager.Log(
                            LogLevels.Error,
                            "Unable to submit new AppNexus app user request for user {0}",
                            user.ExternalEntityId);
                    }
                }
            }
            else
            {
                var userEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
                user.UserId = existingUserEntity.UserId;

                // update the access list, remove all existing access settings, then add values in this user request update
                this.UserAccessRepository.RemoveUserAccessList(
                    userEntityId, this.UserAccessRepository.GetUserAccessList(userEntityId));

                var userPermissions = user.AccessList.ToString().Split(
                    new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                var accessList = new List<string>();
                accessList.AddRange(userPermissions);

                if (!this.UserAccessRepository.AddUserAccessList(userEntityId, accessList))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Unable to update access for user entity Id: {0}",
                        user.UserId.ToString());
                }
            }

            // Save the user
            this.Repository.SaveUser(externalContext, user);

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.User, user.SerializeToJson() }
            });
        }
    }
}
