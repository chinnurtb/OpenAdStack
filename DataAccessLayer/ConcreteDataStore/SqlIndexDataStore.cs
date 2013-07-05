//-----------------------------------------------------------------------
// <copyright file="SqlIndexDataStore.cs" company="Rare Crowds Inc.">
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DataAccessLayer;
using Diagnostics;
using Microsoft.SqlServer.Server;

namespace ConcreteDataStore
{
    /// <summary>
    /// Sql backed store for Index data. IIndexStore is the only interface that non-test callers of this assembly
    /// can use. That is what will be returned from SqlIndexStoreFactory.
    /// </summary>
    internal class SqlIndexDataStore : IIndexStore
    {
        /// <summary>
        /// A map of key field names (from IStorageKey.KeyFields) to SqlParameters
        /// </summary>
        private readonly Dictionary<string, Func<string, SqlParameter>> keyFieldMap = 
            new Dictionary<string, Func<string, SqlParameter>>
                {
                    { AzureStorageKey.TableNameFieldName, 
                        v => new SqlParameter("@Tablename", SqlDbType.VarChar, 120) { Value = v } },
                    { AzureStorageKey.PartitionFieldName, 
                        v => new SqlParameter("@Partition", SqlDbType.VarChar, 120) { Value = v } },
                    { AzureStorageKey.RowIdFieldName, 
                        v => new SqlParameter("@RowId", SqlDbType.UniqueIdentifier) { Value = (Guid)new EntityId(v) } },
                    { AzureBlobStorageKey.BlobMarkerFieldName, 
                        v => new SqlParameter("@Tablename", SqlDbType.VarChar, 120) { Value = v } },
                    { AzureBlobStorageKey.ContainerFieldName, 
                        v => new SqlParameter("@Partition", SqlDbType.VarChar, 120) { Value = v } },
                    { AzureBlobStorageKey.BlobIdFieldName, 
                        v => new SqlParameter("@RowId", SqlDbType.UniqueIdentifier) { Value = (Guid)new EntityId(v) } },
                };

        /// <summary>This is a table name key field override value for a blob.</summary>
        private const string BlobTableNameOverride = "**AzureBlob**";

        /// <summary>Initializes a new instance of the <see cref="SqlIndexDataStore"/> class.</summary>
        /// <param name="sqlStore">The low-level sql store object.</param>
        public SqlIndexDataStore(ISqlStore sqlStore)
        {
            this.SqlStore = sqlStore;
        }

        /// <summary>Gets the SqlStore.</summary>
        internal ISqlStore SqlStore { get; private set; }

        ////
        // Begin IIndexStore Members
        ////

        /// <summary>Get the key fields from the index given an external entity Id.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <returns>Storage key for this entity.</returns>
        public IStorageKey GetStorageKey(EntityId externalEntityId, string storageAccountName)
        {
            var commandName = "GetEntityKeyFieldsForVersion";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)externalEntityId },
                    new SqlParameter("@Version", SqlDbType.Int) { Value = DBNull.Value }
                };

            var resultSets = this.SqlStore.ExecuteStoredProcedure(commandName, parameters);
            if (resultSets.IsEmpty())
            {
                return null;
            }

            var keyFields = resultSets.GetMatchingRecord(0, "StorageAccountName", storageAccountName);
            return BuildStorageKey(storageAccountName, keyFields);
        }

        /// <summary>Get index data for an entity.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="version">The entity version to get. Null for current.</param>
        /// <returns>
        /// A partially populated entity with the key only if version is supplied.
        /// If current version is requested, current associations and indexed properties will be included.
        /// </returns>
        public IEntity GetEntity(EntityId externalEntityId, string storageAccountName, int? version)
        {
            return version == null 
                ? this.GetIndexEntity(externalEntityId, storageAccountName) 
                : this.GetIndexEntity(externalEntityId, storageAccountName, version);
        }

        /// <summary>Save a reference to an entity in the index store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        /// <exception cref="DataAccessStaleEntityException">Throws if index save fails because incoming entity is stale.</exception>
        /// <exception cref="DataAccessException">Throws if index save fails.</exception>
        public void SaveEntity(IEntity rawEntity, bool isUpdate = false)
        {
            try
            {
                this.SaveEntityIndexEntry(rawEntity, true);
            }
            catch (DataAccessStaleEntityException e)
            {
                var msgFormat = "Unable to save stale entity in index. ExternalEntityId: {0}, Detail: {1}";
                LogManager.Log(LogLevels.Information, false, msgFormat.FormatInvariant(rawEntity.ExternalEntityId, e.ToString()));
                throw;
            }
            catch (DataAccessException e)
            {
                var msgFormat = "Unable to save entity in index. Exception: {0}, Detail: {1}";
                LogManager.Log(LogLevels.Error, false, msgFormat.FormatInvariant(rawEntity.ExternalEntityId, e.ToString()));
                throw;
            }
        }

        /// <summary>Retrieve a list of entities of a given entity category.</summary>
        /// <param name="entityCategory">The entity category.</param>
        /// <returns>A list of minimally populated raw entities.</returns>
        public IList<IEntity> GetEntityInfoByCategory(string entityCategory)
        {
            var commandName = "GetEntityInfoByCategory";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@EntityCategory", SqlDbType.NVarChar, 120) { Value = entityCategory }
                };

            var entities = this.SqlStore.ExecuteStoredProcedure(commandName, parameters).GetRecords(0);
            return entities.Select(BuildInfoEntity).ToList();
        } 

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        public void SetEntityStatus(HashSet<EntityId> entityIds, bool active)
        {
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Active", SqlDbType.Bit) { Value = active ? 1 : 0 },
                };

            var entityIdList = new List<SqlDataRecord>();
            foreach (var entityId in entityIds)
            {
                var record = new SqlDataRecord(new[]
                    {
                        new SqlMetaData("ExternalEntityId", SqlDbType.UniqueIdentifier)
                    });

                record.SetGuid(0, entityId);
                entityIdList.Add(record);
            }

            var entityIdsParam = new SqlParameter("@EntityIdList", SqlDbType.Structured);
            entityIdsParam.TypeName = "dbo.EntityIdListParam";
            entityIdsParam.Value = entityIds.Count > 0 ? entityIdList : null;
            parameters.Add(entityIdsParam);

            var result = this.SqlStore.ExecuteStoredProcedure("UpdateEntityStatus", parameters);

            var count = result.GetSingleRecord(0, 0).GetValue<int>("UpdatedEntityCount");
            if (count != entityIds.Count)
            {
                var msg = "Status could not be updated for all entities. Expected {0}, Updated {1}".FormatInvariant(
                        entityIds.Count, count);
                LogManager.Log(LogLevels.Warning, msg);
            }
        }

        ////
        // End IIndexStore Members
        ////

        /// <summary>Convert a Sql date to UTC.</summary>
        /// <param name="sqlTime">The sql date.</param>
        /// <returns>A UTC value.</returns>
        private static DateTime SqlDateToUtc(DateTime sqlTime)
        {
            return new DateTime(sqlTime.Ticks, DateTimeKind.Utc);
        }

        /// <summary>Determine if a value for a sql parameter needs to be DBNull.</summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The original value or DBNull.Value if null.</returns>
        private static object BuildParameterValue(object value)
        {
            return value ?? DBNull.Value;
        }

        /// <summary>Build a storage key from the key fields.</summary>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="keyFields">The record with the key fields.</param>
        /// <returns>A storage key.</returns>
        private static IStorageKey BuildStorageKey(string storageAccountName, QueryRecord keyFields)
        {
            // TODO: Consider how to encapsulate this in a single storage key factory
            // TODO: with mapped rather than named fields (e.g. - change the table to have keyfield1, keyfield2, etc)
            var tableName = keyFields.GetValue<string>("TableName");
            var partition = keyFields.GetValue<string>("Partition");
            var rowId = new EntityId(keyFields.GetValue<Guid>("RowId"));

            // Make sure dates round-trip as utc
            DateTime? versionTimestamp = null;
            if (keyFields.HasField("VersionTimestamp"))
            {
                versionTimestamp = SqlDateToUtc(keyFields.GetValue<DateTime>("VersionTimestamp"));
            }

            var localVersion = keyFields.GetValue<int>("LocalVersion");

            IStorageKey storageKey;

            if (tableName == BlobTableNameOverride)
            {
                storageKey = new AzureBlobStorageKey(
                    storageAccountName, partition, rowId, localVersion, versionTimestamp);
            }
            else
            {
                storageKey = new AzureStorageKey(
                    storageAccountName, tableName, partition, rowId, localVersion, versionTimestamp);
            }

            return storageKey;
        }

        /// <summary>Get index data for an entity.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="resultSets">The query results.</param>
        /// <returns>A partially populated entity with the index data (Most IRawEntity properties, a key, and associations).</returns>
        private static IEntity BuildIndexEntity(Guid externalEntityId, string storageAccountName, QueryResult resultSets)
        {
            // Get the entity properties
            var entity = new Entity();
            var entityRecord = resultSets.GetSingleRecord();
            entity.ExternalEntityId = externalEntityId;
            entity.LocalVersion = entityRecord.GetValue<int>("Version");
            entity.CreateDate = entityRecord.GetValue<DateTime>("CreateDate");
            entity.LastModifiedDate = entityRecord.GetValue<DateTime>("LastModifiedDate");
            entity.ExternalName = entityRecord.GetValue<string>("ExternalName");
            entity.EntityCategory = entityRecord.GetValue<string>("EntityCategory");
            entity.ExternalType = entityRecord.GetValue<string>("ExternalType");
            entity.LastModifiedUser = entityRecord.GetValue<string>("LastModifiedUser");
            if (entityRecord.HasField("SchemaVersion"))
            {
                entity.SchemaVersion = entityRecord.GetValue<int>("SchemaVersion");
            }

            // Get the key fields
            var keyFields = resultSets.GetMatchingRecord(1, "StorageAccountName", storageAccountName);
            entity.Key = BuildStorageKey(storageAccountName, keyFields);

            // Get associations
            var associations = resultSets.GetRecords(2);
            if (!associations.Any())
            {
                return entity;
            }

            foreach (var associationRow in associations)
            {
                entity.Associations.Add(new Association
                    {
                        ExternalName = associationRow.GetValue<string>("ExternalName"),
                        TargetEntityId = associationRow.GetValue<Guid>("TargetEntityId"),
                        AssociationType = Association.AssociationTypeFromString(associationRow.GetValue<string>("AssociationType")),
                        TargetEntityCategory = associationRow.GetValue<string>("TargetEntityCategory"),
                        TargetExternalType = associationRow.GetValue<string>("TargetExternalType"),
                        Details = associationRow.HasField("Details") ? associationRow.GetValue<string>("Details") : null
                    });
            }

            return entity;
        }

        /// <summary>Get index data for an entity.</summary>
        /// <param name="entityInfo">The query record for the entity.</param>
        /// <returns>A partially populated entity with the entity info.</returns>
        private static IEntity BuildInfoEntity(QueryRecord entityInfo)
        {
            // Get the entity properties
            var entity = new Entity();
            var entityRecord = entityInfo;
            entity.ExternalEntityId = entityRecord.GetValue<Guid>("ExternalEntityId");
            entity.ExternalName = entityRecord.GetValue<string>("ExternalName");
            entity.EntityCategory = entityRecord.GetValue<string>("EntityCategory");
            entity.ExternalType = entityRecord.GetValue<string>("ExternalType");
            return entity;
        }

        /// <summary>Get index data for an entity.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <returns>A partially populated entity with key, current associations and indexed properties included.</returns>
        private IEntity GetIndexEntity(EntityId externalEntityId, string storageAccountName)
        {
            var commandName = "GetEntityIndexEntry";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)externalEntityId }
                };

            var resultSets = this.SqlStore.ExecuteStoredProcedure(commandName, parameters);
            if (resultSets.IsEmpty())
            {
                return null;
            }

            return BuildIndexEntity(externalEntityId, storageAccountName, resultSets);
        }

        /// <summary>Get index data for an entity at a specific version.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="version">The entity version to get. Null for current.</param>
        /// <returns>A partially populated entity with the key.</returns>
        private Entity GetIndexEntity(EntityId externalEntityId, string storageAccountName, int? version)
        {
            var commandName = "GetEntityKeyFieldsForVersion";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)externalEntityId },
                    new SqlParameter("@Version", SqlDbType.Int) { Value = version }
                };

            var resultSets = this.SqlStore.ExecuteStoredProcedure(commandName, parameters);
            if (resultSets.IsEmpty())
            {
                return null;
            }

            // Get the key fields
            var keyFields = resultSets.GetMatchingRecord(0, "StorageAccountName", storageAccountName);
            return new Entity
                {
                    ExternalEntityId = externalEntityId,
                    LocalVersion = version.Value,
                    Key = BuildStorageKey(storageAccountName, keyFields)
                };
        }

        /// <summary>Add a new index entry for an entity.</summary>
        /// <param name="entity">The entity to add/update in the index.</param>
        /// <param name="active">True if the entity should be marked active.</param>
        private void SaveEntityIndexEntry(IEntity entity, bool active)
        {
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)entity.ExternalEntityId },
                    new SqlParameter("@ExternalName", SqlDbType.NVarChar, 120) { Value = BuildParameterValue((string)entity.ExternalName) },
                    new SqlParameter("@ExternalType", SqlDbType.NVarChar, 120) { Value = BuildParameterValue((string)entity.ExternalType) },
                    new SqlParameter("@EntityCategory", SqlDbType.NVarChar, 120) { Value = BuildParameterValue((string)entity.EntityCategory) },
                    new SqlParameter("@LastModifiedUser", SqlDbType.NVarChar, 120) { Value = BuildParameterValue((string)entity.LastModifiedUser) },
                    new SqlParameter("@SchemaVersion", SqlDbType.Int) { Value = BuildParameterValue((int)entity.SchemaVersion) },
                    new SqlParameter("@Active", SqlDbType.Bit) { Value = active ? 1 : 0 },
                    new SqlParameter("@Version", SqlDbType.Int) { Value = (int)entity.LocalVersion },
                    new SqlParameter("@TimeStamp", SqlDbType.DateTime) { Value = (DateTime)entity.LastModifiedDate },
                    new SqlParameter("@StorageAccountName", SqlDbType.VarChar, 120) { Value = entity.Key.StorageAccountName }
                };
            parameters.AddRange(entity.Key.KeyFields.Select(keyField => this.keyFieldMap[keyField.Key](keyField.Value)));

            var associationList = new List<SqlDataRecord>();
            foreach (var association in entity.Associations)
            {
                var record = new SqlDataRecord(new[]
                    {
                        new SqlMetaData("ExternalName", SqlDbType.NVarChar, 120),
                        new SqlMetaData("TargetEntityId", SqlDbType.UniqueIdentifier), 
                        new SqlMetaData("AssociationType", SqlDbType.VarChar, 15), 
                        new SqlMetaData("Details", SqlDbType.NVarChar, 240)
                    });
                
                record.SetString(0, association.ExternalName);
                record.SetGuid(1, association.TargetEntityId);
                record.SetString(2, Association.StringFromAssociationType(association.AssociationType));
                if (association.Details != null)
                {
                    record.SetString(3, association.Details);
                }

                associationList.Add(record);
            }

            var associations = new SqlParameter("@AssociationList", SqlDbType.Structured);
            associations.TypeName = "dbo.AssociationListParam";
            associations.Value = associationList.Count > 0 ? associationList : null;
            parameters.Add(associations);

            this.SqlStore.ExecuteStoredProcedure("UpdateEntityIndexEntry", parameters);
        }
    }
}
