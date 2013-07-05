//-----------------------------------------------------------------------
// <copyright file="GetUserByEntityIdActivity.cs" company="Rare Crowds Inc">
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

using Activities;
using DataAccessLayer;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting users by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the user with the specified EntityId
    /// RequiredValues:
    ///   UserEntityId - ExternalEntityId of the user to get
    /// ResultValues:
    ///   User - The user as json
    /// </remarks>
    [Name("GetUserByEntityId"), RequiredValues("EntityId"), ResultValues("User")]
    public class GetUserByEntityIdActivity : GetEntityByEntityIdActivityBase
    {
        /// <summary>
        /// Gets the expected EntityCategory of the returned entity
        /// </summary>
        protected override string EntityCategory
        {
            get { return UserEntity.UserEntityCategory; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return "User"; }
        }

        /// <summary>
        /// Create a return json string for the User entity type
        /// </summary>
        /// <param name="entity">the entity</param>
        /// <returns>a json string</returns>
        protected override string CreateJsonResult(IEntity entity)
        {
            UserEntity companyEntity = new UserEntity(entity);
            return companyEntity.SerializeToJson();
        }
    }
}
