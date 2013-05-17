// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureStorageKeyFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Factory implementation for Azure Table data store storage keys</summary>
    internal class AzureStorageKeyFactory : IStorageKeyFactory
    {
        /// <summary>Initializes a new instance of the <see cref="AzureStorageKeyFactory"/> class.</summary>
        /// <param name="indexFactory">The index store factory.</param>
        /// <param name="keyRuleFactory">The key rule factory.</param>
        public AzureStorageKeyFactory(IIndexStoreFactory indexFactory, IKeyRuleFactory keyRuleFactory)
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
            if ((string)rawEntity.EntityCategory == BlobPropertyEntity.BlobPropertyEntityCategory)
            {
                throw new DataAccessException("BlobPropertyEntity not supported by this key Factory.");
            }

            // TODO: this is just an initial pass at this - it needs more scrutiny when the re-partitioning
            // TODO: and key rule implementation are being done
            string tableName;

            // TODO: categories should be an enum or something
            if ((string)rawEntity.EntityCategory == CompanyEntity.CompanyEntityCategory)
            {
                // If this is a new company entity the table will be populated 
                var partialKey = (AzureStorageKey)rawEntity.Key;
                tableName = partialKey.TableName;
            }
            else
            {
                // Otherwise we get the table name from the company
                var indexStore = this.IndexStoreFactory.GetIndexStore();
                var companyKeys = (AzureStorageKey)indexStore.GetStorageKey(companyExternalId, storageAccountName);
                tableName = companyKeys.TableName;
            }

            // TODO: These string parameters need to be enums as well
            var partition = this.GetPartition(rawEntity);

            return new AzureStorageKey(storageAccountName, tableName, partition, new EntityId());
        }

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IRawEntity rawEntity)
        {
            var azureKey = existingKey as AzureStorageKey;
            if (azureKey == null)
            {
                throw new DataAccessException("BuildUpdatedStorageKey called without a valid Azure storage key");
            }
            
            // Generate new rowkey and partition values
            // but leave the rest of the key in tact
            var newKey = new AzureStorageKey(azureKey);
            newKey.RowId = new EntityId();
            newKey.Partition = this.GetPartition(rawEntity);
            
            return newKey;
        }

        /// <summary>Build a storage key from the serialized representation.</summary>
        /// <param name="serializedKey">The serialized key.</param>
        /// <returns>The storage key.</returns>
        public IStorageKey DeserializeKey(string serializedKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>Build a serialized representation of the key.</summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A string serialized form of the blob.</returns>
        public string SerializeBlobKey(IStorageKey key)
        {
            throw new NotImplementedException();
        }

        /// <summary>Get a partition based on the entity characteristics.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <returns>The partition value.</returns>
        private string GetPartition(IRawEntity rawEntity)
        {
            var partitionRule = this.KeyRuleFactory.GetKeyRule(rawEntity, "AzureTable", "Partition");
            var partition = partitionRule.GenerateKeyField(rawEntity);
            return partition;
        }
    }
}
