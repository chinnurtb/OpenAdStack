// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIndexStore.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
        IList<IRawEntity> GetEntityInfoByCategory(string entityCategory);

        /// <summary>Get index data for an entity at a specific version.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="version">The entity version to get. Null for current.</param>
        /// <returns>A partially populated entity with the key.</returns>
        IRawEntity GetEntity(EntityId externalEntityId, string storageAccountName, int? version);
 
        /// <summary>Save a reference to an entity in the index store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        /// <exception cref="DataAccessStaleEntityException">Throws if index save fails because incoming entity is stale.</exception>
        /// <exception cref="DataAccessException">Throws if index save fails.</exception>
        [SuppressMessage("Microsoft.Design", "CA1026", Justification = "Not targeted for non-C# languages.")]
        void SaveEntity(IRawEntity rawEntity, bool isUpdate = false);

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        void SetEntityStatus(HashSet<EntityId> entityIds, bool active);
    }
}
