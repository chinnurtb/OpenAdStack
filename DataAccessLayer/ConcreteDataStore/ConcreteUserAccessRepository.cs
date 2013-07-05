//-----------------------------------------------------------------------
// <copyright file="ConcreteUserAccessRepository.cs" company="Rare Crowds Inc.">
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
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Concrete implementation of IUserAccessRepository
    /// </summary>
    internal class ConcreteUserAccessRepository : IUserAccessRepository
    {
        /// <summary>Initializes a new instance of the <see cref="ConcreteUserAccessRepository"/> class.</summary>
        /// <param name="userAccessStoreFactory">The user access store factory.</param>
        public ConcreteUserAccessRepository(IUserAccessStoreFactory userAccessStoreFactory)
        {
            this.UserAccessStoreFactory = userAccessStoreFactory;
        }

        /// <summary>Gets UserAccessStoreFactory.</summary>
        internal IUserAccessStoreFactory UserAccessStoreFactory { get; private set; }

        /// <summary>Get a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>A collection of access descriptors.</returns>
        public IEnumerable<string> GetUserAccessList(EntityId userEntityId)
        {
            var userAccessStore = this.UserAccessStoreFactory.GetUserAccessStore();
            return userAccessStore.GetUserAccessList(userEntityId);
        }

        /// <summary>Add a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        public bool AddUserAccessList(EntityId userEntityId, IEnumerable<string> accessList)
        {
            var userAccessStore = this.UserAccessStoreFactory.GetUserAccessStore();
            return userAccessStore.AddUserAccessList(userEntityId, accessList);
        }

        /// <summary>Remove a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        public bool RemoveUserAccessList(EntityId userEntityId, IEnumerable<string> accessList)
        {
            var userAccessStore = this.UserAccessStoreFactory.GetUserAccessStore();
            return userAccessStore.RemoveUserAccessList(userEntityId, accessList);
        }
    }
}
