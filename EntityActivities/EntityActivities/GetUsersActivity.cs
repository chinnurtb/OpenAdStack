// -----------------------------------------------------------------------
// <copyright file="GetUsersActivity.cs" company="Rare Crowds Inc">
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