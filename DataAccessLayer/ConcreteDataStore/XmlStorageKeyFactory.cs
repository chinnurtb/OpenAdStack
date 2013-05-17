// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlStorageKeyFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Factory implementation for Xml data store storage keys</summary>
    internal class XmlStorageKeyFactory : IStorageKeyFactory
    {
        /// <summary>Initializes a new instance of the <see cref="XmlStorageKeyFactory"/> class.</summary>
        /// <param name="indexFactory">The index store factory.</param>
        /// <param name="keyRuleFactory">The key rule factory.</param>
        public XmlStorageKeyFactory(IIndexStoreFactory indexFactory, IKeyRuleFactory keyRuleFactory)
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
            // TODO: this is just an initial pass at this - it needs more scrutiny when the re-partitioning
            // TODO: and key rule implementation are being done
            string tableName;

            // TODO: categories should be an enum or something
            if ((string)rawEntity.EntityCategory == "Company")
            {
                var partialKey = (XmlStorageKey)rawEntity.Key;
                tableName = partialKey.TableName;
            }
            else
            {
                var indexStore = this.IndexStoreFactory.GetIndexStore();
                var companyKeys = (XmlStorageKey)indexStore.GetStorageKey(companyExternalId, string.Empty);
                tableName = companyKeys.TableName;
            }

            // TODO: These string parameters need to be enums as well
            var partitionRule = this.KeyRuleFactory.GetKeyRule(rawEntity, "Xml", "Partition");
            var partition = partitionRule.GenerateKeyField(rawEntity);

            return new XmlStorageKey(storageAccountName, tableName, partition, new EntityId());
        }

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IRawEntity rawEntity)
        {
            return null;
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
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }
    }
}
