// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityStore.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>Interface for accessing Entity stores independent of underlying technology.</summary>
    public interface IEntityStore
    {
        /// <summary>Get a raw entity given a storage key.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="key">An IStorageKey key.</param>
        /// <returns>An entity that is normalized but not serialized.</returns>
        IEntity GetEntityByKey(RequestContext context, IStorageKey key);
        
        /// <summary>Do the setup work in a datastore needed to add a new company (does not save a company entity).</summary>
        /// <param name="externalName">External name of company.</param>
        /// <returns>A partial storage key with any key fields bound to the new company populated.</returns>
        IStorageKey SetupNewCompany(string externalName);

        /// <summary>Save an entity in the entity store.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        /// <returns>True if save successful.</returns>
        [SuppressMessage("Microsoft.Design", "CA1026", Justification = "Not targeted for non-C# languages.")]
        bool SaveEntity(RequestContext context, IEntity rawEntity, bool isUpdate = false);

        /// <summary>Get the user entities with a given UserId.</summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyKey">The key for the company holding the user.</param>
        /// <returns>The user entities.</returns>
        HashSet<IEntity> GetUserEntitiesByUserId(string userId, IStorageKey companyKey);

        /// <summary>Remove and entity from entity store.</summary>
        /// <param name="storageKey">The storage key of the entity to remove.</param>
        void RemoveEntity(IStorageKey storageKey);
    }
}
