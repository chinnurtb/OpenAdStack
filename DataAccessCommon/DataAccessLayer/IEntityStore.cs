// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityStore.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
        IRawEntity GetEntityByKey(RequestContext context, IStorageKey key);
        
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
        bool SaveEntity(RequestContext context, IRawEntity rawEntity, bool isUpdate = false);

        /// <summary>Get the user entities with a given UserId.</summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyKey">The key for the company holding the user.</param>
        /// <returns>The user entities.</returns>
        HashSet<IRawEntity> GetUserEntitiesByUserId(string userId, IStorageKey companyKey);

        /// <summary>Remove and entity from entity store.</summary>
        /// <param name="storageKey">The storage key of the entity to remove.</param>
        void RemoveEntity(IStorageKey storageKey);
    }
}
