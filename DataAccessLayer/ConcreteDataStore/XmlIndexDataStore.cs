// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlIndexDataStore.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// DataSet based store for Index data. IIndexStore is the only interface that non-test callers of this assembly
    /// can use. That is what will be returned from XmlIndexStoreFactory.
    /// </summary>
    internal class XmlIndexDataStore : XmlDataStoreBase, IIndexStore
    {
        /// <summary>singleton lock object.</summary>
        private static readonly object LockObj = new object();

        /// <summary>The one and only data store object.</summary>
        private static XmlIndexDataStore indexDataStore;

        /// <summary>singleton initialization flag.</summary>
        private static bool initialized = false;

        /// <summary>Initializes a new instance of the <see cref="XmlIndexDataStore"/> class.</summary>
        public XmlIndexDataStore() : base(new XmlIndexDataSet())
        {
        }

        /// <summary>Gets IndexDataSet.</summary>
        private XmlIndexDataSet IndexDataSet
        {
            get { return (XmlIndexDataSet)this.DataSet; }
        }

        /// <summary>Gets or initializes a singleton data store instance.</summary>
        /// <param name="backingFile">The backing file.</param>
        /// <returns>The data store object.</returns>
        public static XmlIndexDataStore GetInstance(string backingFile)
        {
            if (!initialized)
            {
                lock (LockObj)
                {
                    if (!initialized)
                    {
                        indexDataStore = new XmlIndexDataStore();
                        indexDataStore.LoadFromFile(backingFile);

                        // Make sure flag initialization isn't reordered by the compiler.
                        System.Threading.Thread.MemoryBarrier();
                        initialized = true;
                    }
                }
            }

            return indexDataStore;
        }

        ////
        // Begin IIndexStore Members
        ////
        
        /// <summary>Get the key fields from the index given an external entity Id.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <returns>Storage key for this entity.</returns>
        public IStorageKey GetStorageKey(EntityId externalEntityId, string storageAccountName)
        {
            // The fact that we are an Xml DataSet index doesn't mean the entity store is Xml.
            // Get the storage type
            var indexRecord = this.IndexDataSet.EntityId.SingleOrDefault(r => r.xId == (Guid)externalEntityId);

            // Might be checking if a record exists
            if (indexRecord == null)
            {
                return null;
            }

            var storageType = indexRecord.StorageType;

            // Get the fields based on the storage type
            var keyFields = this.GetKeyFieldsByStorageType(externalEntityId, storageType);

            return keyFields;
        }

        /// <summary>
        /// Retrieve a list of entities of a given entity category.
        /// </summary>
        /// <param name="entityCategory">The entity category.</param>
        /// <returns>
        /// A list of minimally populated raw entities.
        /// </returns>
        public IList<IRawEntity> GetEntityInfoByCategory(string entityCategory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get index data for an entity at a specific version.
        /// </summary>
        /// <param name="externalEntityId">The external entity id.</param><param name="storageAccountName">The storage account name.</param><param name="version">The entity version to get. Null for current.</param>
        /// <returns>
        /// A partially populated entity with the key.
        /// </returns>
        public IRawEntity GetEntity(EntityId externalEntityId, string storageAccountName, int? version)
        {
            throw new NotImplementedException();
        }

        /// <summary>Save a reference to an entity in the index store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        public void SaveEntity(IRawEntity rawEntity, bool isUpdate = false)
        {
            if (isUpdate)
            {
                this.UpdateIndexEntry(rawEntity);
            }

            // The fact that we are an Xml DataSet index doesn't mean the entity store is Xml.
            // Save the key fields for the specific data store type
            var storageType = this.IndexDataSet.StorageAccount
                .Single(r => r.StorageAccountName == rawEntity.Key.StorageAccountName).StorageType;
            this.SaveKeyFieldsByStorageType(rawEntity, storageType);

            // TODO: Harden this if the entity already exists in the index
            // Create the entity index row object
            var xmlEntityIndexRow = this.IndexDataSet.EntityId.NewEntityIdRow();
            xmlEntityIndexRow.xId = rawEntity.ExternalEntityId;
            xmlEntityIndexRow.HomeStorageAccountName = rawEntity.Key.StorageAccountName;
            xmlEntityIndexRow.StorageType = storageType;
            xmlEntityIndexRow.CreateDate = rawEntity.CreateDate;
            xmlEntityIndexRow.LastModifiedDate = rawEntity.LastModifiedDate;
            xmlEntityIndexRow.Version = rawEntity.LocalVersion;
            xmlEntityIndexRow.WriteLock = false;
            
            // Add the row objects
            this.IndexDataSet.EntityId.AddEntityIdRow(xmlEntityIndexRow);
            this.Commit();
        }

        /// <summary>
        /// Set an entity status to active or inactive.
        /// </summary>
        /// <param name="entityIds">The entity ids.</param><param name="active">True to set active, false for inactive.</param>
        public void SetEntityStatus(HashSet<EntityId> entityIds, bool active)
        {
            throw new NotImplementedException();
        }

        ////
        // End IIndexStore Members
        ////

        ////
        // Begin XmlDataStoreBase Overrides
        ////

        /// <summary>Load data store from xml string.</summary>
        /// <param name="xmlData">An xml string conforming to the XmlDataStore schema.</param>
        public override void LoadFromXml(string xmlData)
        {
            this.ReadTableData(xmlData);
        }

        ////
        // End XmlDataStoreBase Overrides
        ////
        
        /// <summary>We just need to update the index metadata (not the keys fields).</summary>
        /// <param name="rawEntity">The raw entity.</param>
        private void UpdateIndexEntry(IRawEntity rawEntity)
        {
            var dataSet = this.IndexDataSet;
            var entityIdRow = (from entity in dataSet.EntityId
                               where entity.xId == (Guid)rawEntity.ExternalEntityId
                               select entity).Single();
            entityIdRow.Version = rawEntity.LocalVersion;
            entityIdRow.LastModifiedDate = rawEntity.LastModifiedDate;
            this.Commit();
        }

        /// <summary>Get the keyfields of an entity based on the data store type.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageType">The storage type.</param>
        /// <returns>IStorageKey of the type for the entity store.</returns>
        private IStorageKey GetKeyFieldsByStorageType(EntityId externalEntityId, string storageType)
        {
            // TODO: define an enum for these if they are needed in an AzureSql index
            if (storageType == "Xml")
            {
                return this.GetXmlKeyFields(externalEntityId);
            }

            if (storageType == "AzureTable")
            {
                return this.GetAzureKeyFields(externalEntityId);
            }

            if (storageType == "S3Table")
            {
                return this.GetS3KeyFields(externalEntityId);
            }

            return null;
        }

        /// <summary>Get key fields for an Xml DataSet based data store.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <returns>Dictionary of key fields.</returns>
        private XmlStorageKey GetXmlKeyFields(Guid externalEntityId)
        {
            var dataSet = this.IndexDataSet;
            var dataTable = dataSet.XmlKeyFields;
            var result = (from entity in dataSet.EntityId
                          join keyFieldsRow in dataTable on
                              new { Id = entity.xId, Acc = entity.HomeStorageAccountName } equals
                              new { Id = keyFieldsRow.xId, Acc = keyFieldsRow.StorageAccountName }
                          where entity.xId == externalEntityId
                          select keyFieldsRow).Single();

            return new XmlStorageKey(result.StorageAccountName, result.TableName, result.Partition, result.RowId);
        }

        /// <summary>Get key fields for an Azure Table based data store.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <returns>Dictionary of key fields.</returns>
        private AzureStorageKey GetAzureKeyFields(Guid externalEntityId)
        {
            var dataSet = this.IndexDataSet;
            var dataTable = dataSet.AzureKeyFields;
            var result = (from entity in dataSet.EntityId
                          join keyFieldsRow in dataTable on
                              new { Id = entity.xId, Acc = entity.HomeStorageAccountName } equals
                              new { Id = keyFieldsRow.xId, Acc = keyFieldsRow.StorageAccountName }
                          where entity.xId == externalEntityId
                          select keyFieldsRow).Single();

            return new AzureStorageKey(result.StorageAccountName, result.TableName, result.Partition, result.RowId);
        }

        /// <summary>Get key fields for an S3 Table based data store.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <returns>Dictionary of key fields.</returns>
        private S3StorageKey GetS3KeyFields(Guid externalEntityId)
        {
            var dataSet = this.IndexDataSet;
            var dataTable = dataSet.S3KeyFields;
            var result = (from entity in dataSet.EntityId
                          join keyFieldsRow in dataTable on
                              new { Id = entity.xId, Acc = entity.HomeStorageAccountName } equals
                              new { Id = keyFieldsRow.xId, Acc = keyFieldsRow.StorageAccountName }
                          where entity.xId == externalEntityId
                          select keyFieldsRow).Single();

            return new S3StorageKey(result.StorageAccountName, result.TBDS3);
        }

        /// <summary>Save the key fields for the corresponding type of data store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="storageType">The storage type.</param>
        private void SaveKeyFieldsByStorageType(IRawEntity rawEntity, string storageType)
        {
            // TODO: define an enum for these
            if (storageType == "Xml")
            {
                this.SaveXmlKeyFields(rawEntity);
            }

            if (storageType == "AzureTable")
            {
                this.SaveAzureKeyFields(rawEntity);
            }

            if (storageType == "S3Table")
            {
                this.SaveS3KeyFields(rawEntity);
            }
        }

        /// <summary>Save key fields for an Xml data store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        private void SaveXmlKeyFields(IRawEntity rawEntity)
        {
            // Create the keyfields row object
            var xmlRawEntity = (XmlRawEntity)rawEntity;
            var xmlKey = (XmlStorageKey)xmlRawEntity.Key;
            var xmlKeyFieldsRow = this.IndexDataSet.XmlKeyFields.NewXmlKeyFieldsRow();
            xmlKeyFieldsRow.xId = rawEntity.ExternalEntityId;
            xmlKeyFieldsRow.StorageAccountName = xmlKey.StorageAccountName;
            xmlKeyFieldsRow.TableName = xmlKey.TableName;
            xmlKeyFieldsRow.Partition = xmlKey.Partition;
            xmlKeyFieldsRow.RowId = xmlKey.RowId;
            xmlKeyFieldsRow.LocalVersion = rawEntity.LocalVersion;

            // Add the row objects
            this.IndexDataSet.XmlKeyFields.AddXmlKeyFieldsRow(xmlKeyFieldsRow);
        }

        /// <summary>Save key fields for an Azure Table data store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        private void SaveAzureKeyFields(IRawEntity rawEntity)
        {
            // Create the keyfields row object
            var storageKey = (AzureStorageKey)rawEntity.Key;
            var azureKeyFieldsRow = this.IndexDataSet.AzureKeyFields.NewAzureKeyFieldsRow();
            azureKeyFieldsRow.xId = rawEntity.ExternalEntityId;
            azureKeyFieldsRow.StorageAccountName = storageKey.StorageAccountName;
            azureKeyFieldsRow.TableName = storageKey.TableName;
            azureKeyFieldsRow.Partition = storageKey.Partition;
            azureKeyFieldsRow.RowId = storageKey.RowId;
            azureKeyFieldsRow.LocalVersion = rawEntity.LocalVersion;

            // Add the row objects
            this.IndexDataSet.AzureKeyFields.AddAzureKeyFieldsRow(azureKeyFieldsRow);
        }

        /// <summary>Save key fields for an S3 data store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        private void SaveS3KeyFields(IRawEntity rawEntity)
        {
            // TODO: Implement an S3RawEntity and populate the rest of the fields
            var aws3KeyFieldsRow = this.IndexDataSet.S3KeyFields.NewS3KeyFieldsRow();
            aws3KeyFieldsRow.StorageAccountName = rawEntity.Key.StorageAccountName;
        }
    }
}
