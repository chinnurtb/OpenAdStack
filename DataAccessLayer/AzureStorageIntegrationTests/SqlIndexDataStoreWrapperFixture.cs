// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlIndexDataStoreWrapperFixture.cs" company="Rare Crowds Inc.">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AzureStorageIntegrationTests
{
    /// <summary>Test fixture for SqlIndexDataStore</summary>
    [TestClass]
    public class SqlIndexDataStoreWrapperFixture
    {
        /// <summary>Concrete sql store for testing.</summary>
        private ConcreteSqlStore sqlStore;

        /// <summary>SqlIndexDataStore for testing.</summary>
        private SqlIndexDataStore indexDatastore;

        /// <summary>Storage key for testing.</summary>
        private AzureStorageKey storageKey;

        /// <summary>Storage account for testing.</summary>
        private string storageAccount;

        /// <summary>Table name for testing.</summary>
        private string tableName;

        /// <summary>Partition for testing.</summary>
        private string partition;

        /// <summary>Row Id for testing.</summary>
        private EntityId rowId;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.sqlStore = new ConcreteSqlStore(ConfigurationManager.AppSettings["Index.ConnectionString"]);
            this.indexDatastore = new SqlIndexDataStore(this.sqlStore);
            this.storageAccount = ConcreteEntityRepository.DefaultStorageAccount;
            this.tableName = "CompanyFoo";
            this.partition = "FooPartition";
            this.rowId = new EntityId();
            this.storageKey = new AzureStorageKey(this.storageAccount, this.tableName, this.partition, this.rowId);
        }

        /// <summary>Test we can get entity info from index by entity category.</summary>
        [TestMethod]
        public void GetEntityInfoByCategory()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);

            var entities = this.indexDatastore.GetEntityInfoByCategory(PartnerEntity.PartnerEntityCategory);
            Assert.AreEqual(1, entities.Count(e => (EntityId)e.ExternalEntityId == externalEntityId));
        }

        /// <summary>Test we can round-trip save a new entity index entry.</summary>
        [TestMethod]
        public void RoundtripAzureIndexEntry()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
            var entityKey = (AzureStorageKey)this.indexDatastore.GetStorageKey(externalEntityId, this.storageKey.StorageAccountName);

            Assert.AreEqual(0, entityKey.LocalVersion);
            AssertKeysEqual(this.storageKey, entityKey);
            AssertRoundTripTimestampsAreEqual(timeStamp, entityKey.VersionTimestamp.Value);
        }

        /// <summary>Test we can round-trip save a new blob index entry.</summary>
        [TestMethod]
        public void RoundtripAzureBlobIndexEntry()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;

            var blobStorageKey = new AzureBlobStorageKey(this.storageAccount, "containerName", new EntityId());
            this.SaveNewEntity(externalEntityId, timeStamp, blobStorageKey);
            var entityKey = (AzureBlobStorageKey)this.indexDatastore.GetStorageKey(externalEntityId, this.storageKey.StorageAccountName);

            // Assert we are correctly round-tripping the date as UTC
            AssertRoundTripTimestampsAreEqual(timeStamp, entityKey.VersionTimestamp.Value);

            Assert.AreEqual(0, entityKey.LocalVersion);
            Assert.AreEqual(blobStorageKey.BlobId, entityKey.BlobId);
            Assert.AreEqual(blobStorageKey.ContainerName, entityKey.ContainerName);
        }
        
        /// <summary>Test fail for an unrecognized key type.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void UnrecognizedKey()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            
            var key = MockRepository.GenerateStub<IStorageKey>();
            key.Stub(f => f.KeyFields).Return(new Dictionary<string, string>());
            this.SaveNewEntity(externalEntityId, timeStamp, key);
        }

        /// <summary>Test we can update an existing index entry and get the current version.</summary>
        [TestMethod]
        public void RoundtripUpdateEntity()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var updatedTimeStamp = timeStamp.AddDays(1);

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
            this.storageKey.RowId = new EntityId();
            this.UpdateEntity(externalEntityId, updatedTimeStamp, 1, this.storageKey);

            var entityKey = (AzureStorageKey)this.indexDatastore.GetStorageKey(externalEntityId, this.storageAccount);
            
            Assert.AreEqual(1, entityKey.LocalVersion);
            AssertRoundTripTimestampsAreEqual(updatedTimeStamp, entityKey.VersionTimestamp.Value);
            Assert.AreEqual(this.storageKey.RowId, entityKey.RowId);
            
            // IndexStore should get keys with latest version
            entityKey = (AzureStorageKey)this.indexDatastore.GetStorageKey(externalEntityId, this.storageKey.StorageAccountName);
            Assert.AreEqual(1, entityKey.LocalVersion);
        }

        /// <summary>Test inserting duplicate key fields (same xId, version, account) fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException), AllowDerivedTypes = true)]
        public void InsertAzureKeyFieldsDuplicateKeyFails()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var version = 0;

            // Create key fields with duplicate version
            this.InsertKeyFields(externalEntityId, version, timeStamp, this.storageKey);
            this.storageKey.RowId = new EntityId();
            this.InsertKeyFields(externalEntityId, version, timeStamp.AddDays(1), this.storageKey);
        }

        /// <summary>Save new index entry fails if key fields cannot be added.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException), AllowDerivedTypes = true)]
        public void SaveNewEntityFailsIfKeysCannotBeAdded()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var version = 0;

            // Create an existing key fields row at version 0 - this should cause SaveNewEntity to fail
            this.InsertKeyFields(externalEntityId, version, timeStamp, this.storageKey);
            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
        }

        /// <summary>Save duplicate index entry fails if entity already exists even if key fields can be added.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException), AllowDerivedTypes = true)]
        public void SaveNewEntityFailsIfDuplicate()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
            
            // Remove key fields so the failure doesn't come from there.
            this.RemoveKeyFields(externalEntityId, 0);
            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
        }

        /// <summary>Save new index entry rolls back inserted key fields if updating fails.</summary>
        [TestMethod]
        public void SaveNewRollsBackKeyFieldsIfInsertFails()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;

            // Add a pre-existing index entry just to force a failure of the index insert but not the
            // key fields insert.
            this.InsertIndexEntry(externalEntityId, 0, timeStamp);

            try
            {
                this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
                Assert.Fail();
            }
            catch (DataAccessException)
            {
            }

            Assert.IsNull(this.GetKeyFields(externalEntityId, 0));
        }

        /// <summary>Update index entry fails if key fields cannot be added.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException), AllowDerivedTypes = true)]
        public void UpdateEntityFailsIfKeyFieldsCannotBeAdded()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var version = 1;

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);

            // Create an existing key fields row at version 1 - this should cause update to fail
            this.InsertKeyFields(externalEntityId, version, timeStamp, this.storageKey);
            this.UpdateEntity(externalEntityId, timeStamp.AddDays(1), version, this.storageKey);
        }

        /// <summary>Update index entry fails if requested version is not the next in sequence.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void UpdateEntityFailsIfVersionNotSequential()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var version = 7;

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
            
            // Update the entity with a version other than the next version (1)
            this.UpdateEntity(externalEntityId, timeStamp.AddDays(1), version, this.storageKey);
        }

        /// <summary>Update index entry rolls back inserted key fields if updating fails.</summary>
        [TestMethod]
        public void UpdateEntityRollsBackKeyFieldsIfUpdateFails()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var version = 7;

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);

            try
            {
                // Update the entity with a version other than the next version (1)
                this.UpdateEntity(externalEntityId, timeStamp.AddDays(1), version, this.storageKey);
                Assert.Fail();
            }
            catch (DataAccessException)
            {
            }

            Assert.IsNull(this.GetKeyFields(externalEntityId, version));
        }

        /// <summary>Test that SaveEntity fails when the entity as been updated already.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void UpdateStaleEntityFails()
        {
            var timeStamp = DateTime.UtcNow;
            var entityId = new EntityId();

            this.SaveNewEntity(entityId, timeStamp, this.storageKey);

            this.storageKey.RowId = new EntityId();

            // The first update should succeed, the second should fail
            try
            {
                this.UpdateEntity(entityId, timeStamp.AddDays(1), 1, this.storageKey);
            }
            catch (DataAccessException)
            {
                Assert.Fail("The first update should not fail.");                
            }

            this.UpdateEntity(entityId, timeStamp.AddDays(1), 1, this.storageKey);
        }

        /// <summary>Test we can get the entity current version with multiple key field entries for different
        /// storage accounts.</summary>
        [TestMethod]
        public void RoundtripUpdateEntityMultipleStorageAccts()
        {
            var externalEntityId = new EntityId();
            var timeStamp = DateTime.UtcNow;
            var updatedTimeStamp = timeStamp.AddDays(1);

            this.SaveNewEntity(externalEntityId, timeStamp, this.storageKey);
            this.storageKey.RowId = new EntityId();
            this.UpdateEntity(externalEntityId, updatedTimeStamp, 1, this.storageKey);

            // Set up key fields record with different storage account but same version
            var altAcctKey = CloneAzureStorageKey(this.storageKey);
            altAcctKey.StorageAccountName = "DifferentAcct";
            this.InsertKeyFields(externalEntityId, 1, updatedTimeStamp, altAcctKey);

            var entityKey = (AzureStorageKey)this.indexDatastore.GetStorageKey(externalEntityId, this.storageAccount);

            // The returned key fields should be for the same account as referenced in the entity index by default
            Assert.AreEqual(this.storageKey.StorageAccountName, entityKey.StorageAccountName);
            Assert.AreEqual(1, entityKey.LocalVersion);
            Assert.AreEqual(this.storageKey.RowId, entityKey.RowId);
            AssertRoundTripTimestampsAreEqual(updatedTimeStamp, entityKey.VersionTimestamp.Value);
        }

        /// <summary>Test we fail with a DataAccessException if we cannot connect.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void GetEntityConnectFail()
        {
            var badStore = new ConcreteSqlStore(
                "Data Source=.\\SQLEXPRESS;Initial Catalog=IndexDatastore;Integrated Security=False;User ID=lucyAppUser;Password=baddog");
            var index = new SqlIndexDataStore(badStore);
            index.GetEntity(new EntityId(), "somestorageaccount", null);
        }

        /// <summary>Test we can round-trip an entity index entry.</summary>
        [TestMethod]
        public void SaveNewAndUpdateEntityIndexEntry()
        {
            var timeStamp = DateTime.UtcNow;

            var target1Key = new AzureStorageKey("DefaultAzureStorageAccount", "dontcare", "dontcare", new EntityId(), 0, null);
            var target1 = new Entity
                {
                    ExternalEntityId = new EntityId(),
                    CreateDate = timeStamp,
                    LastModifiedDate = timeStamp,
                    ExternalName = "foo",
                    ExternalType = "footype",
                    EntityCategory = "Partner",
                    LocalVersion = 0,
                    SchemaVersion = 1,
                    Key = target1Key
                };
            this.indexDatastore.SaveEntity(target1);

            var target2Key = new AzureStorageKey("DefaultAzureStorageAccount", "dontcare", "dontcare", new EntityId(), 0, null);
            var target2 = new Entity
            {
                ExternalEntityId = new EntityId(),
                CreateDate = timeStamp,
                LastModifiedDate = timeStamp,
                ExternalName = "foo",
                ExternalType = "footype",
                EntityCategory = "Partner",
                LocalVersion = 0,
                SchemaVersion = 1,
                Key = target2Key
            };
            this.indexDatastore.SaveEntity(target2);

            var entityKey = new AzureStorageKey("DefaultAzureStorageAccount", "dontcare", "dontcare", new EntityId(), 0, null);
            var entity = new Entity
                {
                    ExternalEntityId = new EntityId(),
                    CreateDate = timeStamp,
                    LastModifiedDate = timeStamp,
                    ExternalName = "foo",
                    ExternalType = "footype",
                    EntityCategory = "Partner",
                    LastModifiedUser = "someuser",
                    LocalVersion = 0,
                    SchemaVersion = 1,
                    Key = entityKey
                };

            entity.Associations.Add(new Association { ExternalName = "assoc1", AssociationType = AssociationType.Relationship, TargetEntityId = target1.ExternalEntityId });
            this.indexDatastore.SaveEntity(entity);
            var roundtripEntity = this.indexDatastore.GetEntity(entity.ExternalEntityId, "DefaultAzureStorageAccount", null);

            Assert.IsNotNull(roundtripEntity.Key);
            Assert.AreEqual(1, roundtripEntity.Associations.Count);
            Assert.AreEqual(entity.ExternalEntityId, roundtripEntity.ExternalEntityId);
            Assert.AreEqual(entity.ExternalName, roundtripEntity.ExternalName);
            Assert.AreEqual(entity.ExternalType, roundtripEntity.ExternalType);
            Assert.AreEqual(entity.EntityCategory, roundtripEntity.EntityCategory);
            Assert.AreEqual(entity.SchemaVersion, roundtripEntity.SchemaVersion);
            Assert.AreEqual(entity.LastModifiedUser, roundtripEntity.LastModifiedUser);
            AssertRoundTripTimestampsAreEqual(entity.CreateDate, roundtripEntity.CreateDate);
            AssertRoundTripTimestampsAreEqual(entity.LastModifiedDate, roundtripEntity.LastModifiedDate);

            entity.LocalVersion = 1;
            entity.Associations.Add(new Association { ExternalName = "assoc2", AssociationType = AssociationType.Relationship, TargetEntityId = target2.ExternalEntityId });
            this.indexDatastore.SaveEntity(entity);
            roundtripEntity = this.indexDatastore.GetEntity(entity.ExternalEntityId, "DefaultAzureStorageAccount", null);
            Assert.IsNotNull(roundtripEntity.Key);
            Assert.AreEqual(2, roundtripEntity.Associations.Count);
        }

        /// <summary>Test that GetKeyFields is benign for keys not found.</summary>
        [TestMethod]
        public void EntityKeysNotFound()
        {
            var entityKey = this.indexDatastore.GetStorageKey(new EntityId(), "acct") as AzureStorageKey;
            Assert.IsNull(entityKey);
        }

        /// <summary>Test that GetEntity is benign for keys not found.</summary>
        [TestMethod]
        public void EntityNotFound()
        {
            var entity = this.indexDatastore.GetEntity(new EntityId(), "acct", null) as AzureStorageKey;
            Assert.IsNull(entity);
        }

        /// <summary>Assert that a timestamp round-tripped through sql is equal to an expected timestamp.</summary>
        /// <param name="expected">The expected timestamp.</param>
        /// <param name="actual">The actual timestamp.</param>
        private static void AssertRoundTripTimestampsAreEqual(DateTime expected, DateTime actual)
        {
            Assert.AreEqual(expected.Ticks, actual.Ticks, (double)20000);
        }

        /// <summary>Clone a key</summary>
        /// <param name="key">The source key.</param>
        /// <returns>The clone key.</returns>
        private static AzureStorageKey CloneAzureStorageKey(AzureStorageKey key)
        {
            return new AzureStorageKey(key.StorageAccountName, key.TableName, key.Partition, key.RowId);
        }

        /// <summary>Assert that two azure storage keys have the same value.</summary>
        /// <param name="expectedKey">The expected key.</param>
        /// <param name="actualKey">The actual key.</param>
        private static void AssertKeysEqual(AzureStorageKey expectedKey, AzureStorageKey actualKey)
        {
            Assert.AreEqual(expectedKey.StorageAccountName, actualKey.StorageAccountName);
            Assert.AreEqual(expectedKey.TableName, actualKey.TableName);
            Assert.AreEqual(expectedKey.Partition, actualKey.Partition);
            Assert.AreEqual(expectedKey.RowId, actualKey.RowId);
        }

        /// <summary>Add a new index entry for an entity.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="timeStamp">Timpstamp for create.</param>
        /// <param name="key">Storage Key.</param>
        private void SaveNewEntity(Guid externalEntityId, DateTime timeStamp, IStorageKey key)
        {
            var rawEntity = new Entity
            {
                ExternalEntityId = externalEntityId,
                ExternalName = "name",
                EntityCategory = "Partner",
                ExternalType = "type",
                CreateDate = timeStamp,
                LastModifiedDate = timeStamp,
                LocalVersion = 0,
                SchemaVersion = 1,
                Key = key
            };

            this.indexDatastore.SaveEntity(rawEntity);
        }

        /// <summary>Add a new index entry for an entity.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="timeStamp">Timpstamp for modification.</param>
        /// <param name="version">The new version of the entity.</param>
        /// <param name="key">The storage key.</param>
        private void UpdateEntity(Guid externalEntityId, DateTime timeStamp, int version, IStorageKey key)
        {
            var rawEntity = new Entity
            {
                ExternalEntityId = externalEntityId,
                ExternalName = "name",
                EntityCategory = "Partner",
                ExternalType = "type",
                CreateDate = timeStamp,
                LastModifiedDate = timeStamp,
                LocalVersion = version,
                SchemaVersion = 1,
                Key = key
            };

            this.indexDatastore.SaveEntity(rawEntity, true);
        }

        /// <summary>Remove a key fields record</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="localVersion">The local version.</param>
        private void RemoveKeyFields(EntityId externalEntityId, int localVersion)
        {
            var cmd =
                @"DELETE FROM IndexDatastore.dbo.AzureKeyFieldsVersioned WHERE xId = '{0}' AND LocalVersion = '{1}'"
                .FormatInvariant(
                ((Guid)externalEntityId),
                localVersion);

            this.ExecuteSqlCommand(cmd);
        }

        /// <summary>Get the key fields at a specific version for an entity id.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="localVersion">The local version.</param>
        /// <returns>The key fields as a dictionary.</returns>
        private Dictionary<string, object> GetKeyFields(EntityId externalEntityId, int localVersion)
        {
            var cmd =
                @"SELECT * FROM IndexDatastore.dbo.AzureKeyFieldsVersioned WHERE xId = '{0}' AND LocalVersion = '{1}'"
                .FormatInvariant(
                ((Guid)externalEntityId),
                localVersion);

            var results = this.ExecuteSqlCommand(cmd);

            if (results.Count == 0)
            {
                return null;
            }

            return results.Single();
        }

        /// <summary>Insert a key fields record</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="localVersion">The local version.</param>
        /// <param name="versionTimestamp">The version timestamp.</param>
        /// <param name="insertStorageKey">The storage key.</param>
        private void InsertKeyFields(EntityId externalEntityId, int localVersion, DateTime? versionTimestamp, AzureStorageKey insertStorageKey)
        {
            var timestamp = new SqlParameter("@VersionTimestamp", SqlDbType.DateTime) { Value = DBNull.Value };

            if (versionTimestamp.HasValue)
            {
                timestamp = new SqlParameter("@VersionTimestamp", SqlDbType.DateTime) { Value = versionTimestamp };
            }

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)externalEntityId },
                    new SqlParameter("@StorageAccountName", SqlDbType.VarChar, 120) { Value = insertStorageKey.StorageAccountName },
                    new SqlParameter("@Tablename", SqlDbType.VarChar, 120) { Value = insertStorageKey.TableName },
                    new SqlParameter("@Partition", SqlDbType.VarChar, 120) { Value = insertStorageKey.Partition },
                    new SqlParameter("@RowId", SqlDbType.UniqueIdentifier) { Value = (Guid)insertStorageKey.RowId },
                    new SqlParameter("@LocalVersion", SqlDbType.Int) { Value = localVersion },
                    timestamp,
                };

            this.sqlStore.ExecuteStoredProcedure("KeyFields_InsertAzureKeyFields", parameters);
        }

        /// <summary>Insert an entity index record.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="version">The version.</param>
        /// <param name="timeStamp">The time stamp.</param>
        private void InsertIndexEntry(EntityId externalEntityId, int version, DateTime timeStamp)
        {
            var cmd =
                @"INSERT INTO IndexDatastore.dbo.EntityId (xId, StorageType, Version, CreateDate, LastModifiedDate, WriteLock, HomeStorageAccountName)
	              VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')"
                .FormatInvariant(
                ((Guid)externalEntityId),
                "AzureTable",
                version,
                timeStamp,
                timeStamp,
                0,
                "DefaultAzureStorageAccount");

            this.ExecuteSqlCommand(cmd);
        }

        /// <summary>Remove a key fields record</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        private void RemoveIndexEntry(EntityId externalEntityId)
        {
            var cmd =
                @"DELETE FROM IndexDatastore.dbo.EntityId WHERE xId = '{0}'"
                .FormatInvariant(
                ((Guid)externalEntityId));

            this.ExecuteSqlCommand(cmd);
        }

        /// <summary>Execute a sql query</summary>
        /// <param name="commandText">The command text (query).</param>
        /// <returns>A list of dictinoaries representation of the record set.</returns>
        private IList<Dictionary<string, object>> ExecuteSqlCommand(string commandText)
        {
            var connection = new SqlConnection(this.sqlStore.ConnectionString);
            var command = new SqlCommand();
            command.Connection = connection;
            command.CommandText = commandText;
            command.CommandType = CommandType.Text;

            var resultRows = new List<Dictionary<string, object>>();
            SqlDataReader reader = null;

            try
            {
                connection.Open();
                reader = command.ExecuteReader();

                // Realize the reader as a List<Dictionary<string, object>>
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                    }

                    resultRows.Add(row);
                }
            }
            finally
            {
                // Make sure we close the reader and connection
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }

                connection.Close();
            }

            return resultRows;
        }
    }
}
