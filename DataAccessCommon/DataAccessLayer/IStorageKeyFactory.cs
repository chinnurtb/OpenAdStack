// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStorageKeyFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>Interface definition for storage key factories</summary>
    public interface IStorageKeyFactory
    {
        /// <summary>Build a new storage key for an entity that will be inserted to the entity store.</summary>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="companyExternalId">External Id of company associated with this entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        IStorageKey BuildNewStorageKey(string storageAccountName, EntityId companyExternalId, IRawEntity rawEntity);

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IRawEntity rawEntity);

        /// <summary>Build a storage key from the serialized representation.</summary>
        /// <param name="serializedKey">The serialized key.</param>
        /// <returns>The storage key.</returns>
        IStorageKey DeserializeKey(string serializedKey);

        /// <summary>Build a serialized representation of the key.</summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A string serialized form of the blob.</returns>
        string SerializeBlobKey(IStorageKey key);
    }
}
