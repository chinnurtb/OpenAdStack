// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIndexStore.cs" company="Rare Crowds Inc">
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
    /// <summary>Interface for accessing Index stores independent of underlying technology.</summary>
    public interface IIndexStore
    {
        /// <summary>Get the key fields from the index given an external entity Id.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <returns>dictionary of key fields for this entity store type.</returns>
        IStorageKey GetStorageKey(EntityId externalEntityId, string storageAccountName);

        /// <summary>Retrieve a list of entities of a given entity category.</summary>
        /// <param name="entityCategory">The entity category.</param>
        /// <returns>A list of minimally populated raw entities.</returns>
        IList<IEntity> GetEntityInfoByCategory(string entityCategory);

        /// <summary>Get index data for an entity at a specific version.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="version">The entity version to get. Null for current.</param>
        /// <returns>A partially populated entity with the key.</returns>
        IEntity GetEntity(EntityId externalEntityId, string storageAccountName, int? version);
 
        /// <summary>Save a reference to an entity in the index store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        /// <exception cref="DataAccessStaleEntityException">Throws if index save fails because incoming entity is stale.</exception>
        /// <exception cref="DataAccessException">Throws if index save fails.</exception>
        [SuppressMessage("Microsoft.Design", "CA1026", Justification = "Not targeted for non-C# languages.")]
        void SaveEntity(IEntity rawEntity, bool isUpdate = false);

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        void SetEntityStatus(HashSet<EntityId> entityIds, bool active);
    }
}
