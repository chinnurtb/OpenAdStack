//-----------------------------------------------------------------------
// <copyright file="UserMessageVerify.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using System.Text;
using Activities;
using DataAccessLayer;
using Diagnostics;
using EntityActivities;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Confirms the user by setting the UserId to the AuthUser token
    /// </summary>
    [Name(EntityActivityTasks.UserMessageVerify)]
    public class UserMessageVerify : MessagePostActivityBase
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values from message post body</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var userEntityId = request.Values[EntityActivityValues.EntityId];
            var userId = request.Values[EntityActivityValues.AuthUserId];

            // GET User from DAL
            // TODO move this to base class for reuse in other activities, common pattern
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var user = Repository.TryGetEntity(internalContext, userEntityId) as UserEntity;
            if (user == null)
            {
                return EntityNotFoundError(userEntityId);
            }

            // end TODO

            // Verify UserID and EntityID are the same to ensure that the user is not already registered, a bit of a defense against account hijacking.
            if (user.UserId.Value.SerializationValue != user.ExternalEntityId.Value.SerializationValue)
            {
                return this.ErrorResult(
                ActivityErrorId.GenericError,
                "User already registered: '{0}'",
                userEntityId);
            }

            // Verify User last Mod Date < 72 hours
            var lastModDate = DateTime.Parse(user.LastModifiedDate.Value.SerializationValue, CultureInfo.CurrentCulture);
            var hoursSinceModification = ((DateTime.UtcNow - lastModDate).Days * 24) + (DateTime.UtcNow - lastModDate).Hours;
            if (hoursSinceModification >= 72)
            {
                return this.ErrorResult(
                ActivityErrorId.GenericError,
                "User cannot be registered: '{0}'",
                userEntityId);
            }

            // Set UserId == AuthUserId
            user.UserId = userId;

            // Save User
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request);
            Repository.SaveUser(externalContext, user);

            // Remove temporary user verify access
            var removeAccessList = new List<string>
                    {
                        string.Format(CultureInfo.InvariantCulture, "USER:{0}:#:POST:VERIFY", userEntityId),
                    };

            if (!this.UserAccessRepository.RemoveUserAccessList(userEntityId, removeAccessList))
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Unable to remove default access for user entity Id: {0}",
                    userEntityId);
                LogManager.Log(LogLevels.Error, msg);
            }

            return this.SuccessResult();
        }
    }
}
