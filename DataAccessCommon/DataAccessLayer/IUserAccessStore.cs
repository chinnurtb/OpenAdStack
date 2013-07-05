//-----------------------------------------------------------------------
// <copyright file="IUserAccessStore.cs" company="Rare Crowds Inc.">
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

using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Interface for User Access data store</summary>
    public interface IUserAccessStore
    {
        /// <summary>Get a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>A collection of access descriptors.</returns>
        IEnumerable<string> GetUserAccessList(EntityId userEntityId);

        /// <summary>Add a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        bool AddUserAccessList(EntityId userEntityId, IEnumerable<string> accessList);

        /// <summary>Remove a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        bool RemoveUserAccessList(EntityId userEntityId, IEnumerable<string> accessList);
    }
}