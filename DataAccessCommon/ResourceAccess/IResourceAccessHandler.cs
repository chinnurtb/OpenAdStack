//-----------------------------------------------------------------------
// <copyright file="IResourceAccessHandler.cs" company="Rare Crowds Inc.">
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

using DataAccessLayer;

namespace ResourceAccess
{
    /// <summary>Handler interface for resource access checking.</summary>
    public interface IResourceAccessHandler
    {
        /// <summary>Check whether a user has access to a resource.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>True if access is granted.</returns>
        bool CheckAccess(CanonicalResource canonicalResource, EntityId userEntityId);

        /// <summary>Check if a resource has global access rights.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <returns>True if global access is granted.</returns>
        bool CheckGlobalAccess(CanonicalResource canonicalResource);

        /// <summary>Return an array of EnityId's based on the user's access list</summary>
        /// <param name="userEntityId">The user's entity id</param>
        /// <returns>Array of Company Ids.</returns>
        EntityId[] GetUserCompanyByAccessList(EntityId userEntityId);
    }
}