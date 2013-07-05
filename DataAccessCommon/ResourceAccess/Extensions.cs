// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Diagnostics;

namespace ResourceAccess
{
    /// <summary>Extension methods for IResourceAccessHandler</summary>
    public static class Extensions
    {
        /// <summary>Check access for a given resource and user id.</summary>
        /// <param name="accessHandler">The resource access handler instance.</param>
        /// <param name="repository">The entity repository instance.</param>
        /// <param name="resource">The canonical resource.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>True if access is granted.</returns>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if user not found.</exception>
        public static bool CheckAccessByUserId(this IResourceAccessHandler accessHandler, IEntityRepository repository, CanonicalResource resource, string userId)
        {
            try
            {
                var user = repository.GetUser(new RequestContext(), userId);
                return accessHandler.CheckAccess(resource, user.ExternalEntityId);
            }
            catch (ArgumentException ex)
            {
                var msg = "User Id not found: {0}".FormatInvariant(userId);
                LogManager.Log(LogLevels.Warning, msg);
                throw new DataAccessEntityNotFoundException(msg, ex);
            }
        }
    }
}
