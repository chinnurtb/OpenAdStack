// --------------------------------------------------------------------------------------------------------------------
// <copyright file="S3StorageKeyFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Implementation of a storage key for an S3 based store.
    /// </summary>
    internal class S3StorageKeyFactory : IStorageKeyFactory
    {
        /// <summary>Initializes a new instance of the <see cref="S3StorageKeyFactory"/> class.</summary>
        /// <param name="indexFactory">The index store factory.</param>
        /// <param name="keyRuleFactory">The key rule factory.</param>
        public S3StorageKeyFactory(IIndexStoreFactory indexFactory, IKeyRuleFactory keyRuleFactory)
        {
            this.IndexStoreFactory = indexFactory;
            this.KeyRuleFactory = keyRuleFactory;
        }

        /// <summary>Gets IndexStoreFactory.</summary>
        public IIndexStoreFactory IndexStoreFactory { get; private set; }

        /// <summary>Gets KeyRuleFactory.</summary>
        public IKeyRuleFactory KeyRuleFactory { get; private set; }

        /// <summary>Build a new storage key for an entity that will be inserted to the entity store.</summary>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="companyExternalId">External Id of company associated with this entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildNewStorageKey(string storageAccountName, EntityId companyExternalId, IRawEntity rawEntity)
        {
            throw new NotImplementedException();
        }

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IRawEntity rawEntity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a storage key from the serialized representation.
        /// </summary>
        /// <param name="serializedKey">The serialized key.</param>
        /// <returns>
        /// The storage key.
        /// </returns>
        public IStorageKey DeserializeKey(string serializedKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a serialized representation of the key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>
        /// A string serialized form of the blob.
        /// </returns>
        public string SerializeBlobKey(IStorageKey key)
        {
            throw new NotImplementedException();
        }
    }
}
