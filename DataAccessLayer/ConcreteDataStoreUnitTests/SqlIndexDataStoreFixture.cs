//-----------------------------------------------------------------------
// <copyright file="SqlIndexDataStoreFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Diagnostics;
using Microsoft.SqlServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>
    /// Unit test fixture for SqlIndexDataStore
    /// </summary>
    [TestClass]
    public class SqlIndexDataStoreFixture
    {
        /// <summary>ISqlStore stub for testing.</summary>
        private ISqlStore sqlStore;

        /// <summary>Sql index store for testing.</summary>
        private SqlIndexDataStore indexStore;

        /// <summary>Strorage account for testing.</summary>
        private string storageAccount;

        /// <summary>Table name for testing.</summary>
        private string tableName;

        /// <summary>Partition for testing.</summary>
        private string partition;

        /// <summary>Table row id for testing.</summary>
        private EntityId rowId;

        /// <summary>Local version for testing.</summary>
        private int localVersion;

        /// <summary>Version time stamp for testing.</summary>
        private DateTime? versionTimestamp;

        /// <summary>external entity id for testing.</summary>
        private EntityId externalEntityId;

        /// <summary>storage type for testing.</summary>
        private string storageType;

        /// <summary>entity create date for testing.</summary>
        private DateTime createDate;

        /// <summary>entity last modified date for testing.</summary>
        private DateTime lastModifiedDate;

        /// <summary>entity last modified user for testing.</summary>
        private string lastModifiedUser;

        /// <summary>schema version for testing.</summary>
        private int schemaVersion;

        /// <summary>entity external name for testing.</summary>
        private string externalName;

        /// <summary>entity category for testing.</summary>
        private string entityCategory;

        /// <summary>entity external type for testing.</summary>
        private string externalType;

        /// <summary>assocation name for testing.</summary>
        private string associationName;

        /// <summary>assocation target id for testing.</summary>
        private EntityId targetId1;

        /// <summary>assocation target id for testing.</summary>
        private EntityId targetId2;

        /// <summary>assocation type for testing.</summary>
        private AssociationType associationType;

        /// <summary>assocation target external category for testing.</summary>
        private string associationTargetExternalCategory;

        /// <summary>assocation target external type for testing.</summary>
        private string associationTargetExternalType;

        /// <summary>assocation details for testing.</summary>
        private string associationDetails;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new List<ILogger> { MockRepository.GenerateStub<ILogger>() });
            this.sqlStore = MockRepository.GenerateStub<ISqlStore>();
            this.indexStore = new SqlIndexDataStore(this.sqlStore);
            
            // Inialize entity property fields for testing
            this.externalEntityId = new EntityId();
            this.storageType = "AzureTable";
            this.createDate = DateTime.UtcNow;
            this.lastModifiedDate = this.createDate.AddDays(1);
            this.lastModifiedUser = "someuser";
            this.schemaVersion = 1;
            this.externalName = "entityname";
            this.entityCategory = "entitycategory";
            this.externalType = "externaltype";

            // Initialize default key fields for testing
            this.storageAccount = "storageaccount";
            this.tableName = "tablename";
            this.partition = "partition";
            this.rowId = new EntityId();
            this.localVersion = 1;
            this.versionTimestamp = this.lastModifiedDate;

            // Initialize default association fields for testing
            this.associationName = "assoc";
            this.targetId1 = new EntityId();
            this.targetId2 = new EntityId();
            this.associationType = AssociationType.Relationship;
            this.associationTargetExternalCategory = "targetcategory";
            this.associationTargetExternalType = "targettype";
            this.associationDetails = "some details";
        }

        /// <summary>Happy path GetStorageKey for versioned key.</summary>
        [TestMethod]
        public void GetStorageKey()
        {
            var keyResult = this.BuildTestKeyResult();

            IList<SqlParameter> sqlParams = null;
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityKeyFieldsForVersion"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => sqlParams = (IList<SqlParameter>)call.Arguments[1])
                .Return(keyResult);

            var key = (AzureStorageKey)this.indexStore.GetStorageKey(this.externalEntityId, this.storageAccount);
            Assert.AreEqual(DBNull.Value, sqlParams.Single(p => p.ParameterName == "@Version").Value);
            Assert.AreEqual(this.storageAccount, key.StorageAccountName);
            Assert.AreEqual(this.tableName, key.TableName);
            Assert.AreEqual(this.partition, key.Partition);
            Assert.AreEqual(this.rowId, key.RowId);
            Assert.AreEqual(this.localVersion, key.LocalVersion);
            Assert.AreEqual(this.versionTimestamp, key.VersionTimestamp);
        }

        /// <summary>Test GetStorageKey returns null if entity not found.</summary>
        [TestMethod]
        public void GetStorageKeyNotFoundReturnsNull()
        {
            var queryResult = new QueryResult();
            queryResult.AddRecordSet();

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityKeyFieldsForVersion"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(queryResult);

            var key = (AzureStorageKey)this.indexStore.GetStorageKey(this.externalEntityId, this.storageAccount);
            Assert.IsNull(key);
        }

        /// <summary>Test GetStorageKey throws if key found but account doesn't match.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void GetStorageKeyAccountMismatch()
        {
            var keyResult = this.BuildTestKeyResult();

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityKeyFieldsForVersion"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(keyResult);

            this.indexStore.GetStorageKey(this.externalEntityId, "differentaccount");
        }

        /// <summary>Test GetStorageKey throws if multiple matching keys found.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void GetStorageKeyMultipleMatches()
        {
            var keyResult = this.BuildTestKeyResult();
            keyResult.AddRecord(this.BuildTestKeyResult().GetSingleRecord(0, 0), 0);

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityKeyFieldsForVersion"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(keyResult);

            this.indexStore.GetStorageKey(this.externalEntityId, this.storageAccount);
        }

        /// <summary>Happy-path GetEntity.</summary>
        [TestMethod]
        public void GetEntity()
        {
            var entityResult = this.BuildTestEntityResult(true);

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(entityResult);

            var entity = this.indexStore.GetEntity(this.externalEntityId, this.storageAccount, null);
            
            // Assert entity properties
            Assert.AreEqual(this.externalEntityId, (EntityId)entity.ExternalEntityId);
            Assert.AreEqual(this.localVersion, (int)entity.LocalVersion);
            Assert.AreEqual(this.createDate, (DateTime)entity.CreateDate);
            Assert.AreEqual(this.lastModifiedDate, (DateTime)entity.LastModifiedDate);
            Assert.AreEqual(this.externalName, (string)entity.ExternalName);
            Assert.AreEqual(this.entityCategory, (string)entity.EntityCategory);
            Assert.AreEqual(this.externalType, (string)entity.ExternalType);
            Assert.AreEqual(this.lastModifiedUser, (string)entity.LastModifiedUser);
            Assert.AreEqual(this.schemaVersion, (int)entity.SchemaVersion);

            // Assert Key
            var key = (AzureStorageKey)entity.Key;
            Assert.AreEqual(this.storageAccount, key.StorageAccountName);
            Assert.AreEqual(this.tableName, key.TableName);
            Assert.AreEqual(this.partition, key.Partition);
            Assert.AreEqual(this.rowId, key.RowId);
            Assert.AreEqual(this.localVersion, key.LocalVersion);
            Assert.AreEqual(this.versionTimestamp, key.VersionTimestamp);

            // Assert associations
            Assert.AreEqual(2, entity.Associations.Count);
            var assoc1 = entity.Associations[0];
            Assert.AreEqual(this.associationName, assoc1.ExternalName);
            Assert.AreEqual(this.targetId1, assoc1.TargetEntityId);
            Assert.AreEqual(this.associationType, assoc1.AssociationType);
            Assert.AreEqual(this.associationTargetExternalCategory, assoc1.TargetEntityCategory);
            Assert.AreEqual(this.associationTargetExternalType, assoc1.TargetExternalType);
            Assert.AreEqual(this.associationDetails, assoc1.Details);
            var assoc2 = entity.Associations[1];
            Assert.AreEqual(this.associationName, assoc2.ExternalName);
            Assert.AreEqual(this.targetId2, assoc2.TargetEntityId);
            Assert.AreEqual(this.associationType, assoc2.AssociationType);
            Assert.AreEqual(this.associationTargetExternalCategory, assoc2.TargetEntityCategory);
            Assert.AreEqual(this.associationTargetExternalType, assoc2.TargetExternalType);
            Assert.AreEqual(this.associationDetails, assoc2.Details);
        }

        /// <summary>Happy-path GetEntity at version.</summary>
        [TestMethod]
        public void GetEntityAtVersion()
        {
            this.localVersion = 2;
            var keyResult = this.BuildTestKeyResult();

            IList<SqlParameter> sqlParams = null;
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityKeyFieldsForVersion"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => sqlParams = (IList<SqlParameter>)call.Arguments[1])
                .Return(keyResult);

            var entity = this.indexStore.GetEntity(this.externalEntityId, this.storageAccount, this.localVersion);

            Assert.AreEqual(this.localVersion, (int)sqlParams[1].Value);

            // Assert entity properties
            Assert.AreEqual(this.externalEntityId, (EntityId)entity.ExternalEntityId);
            Assert.AreEqual(this.localVersion, (int)entity.LocalVersion);

            // Assert Key
            var key = (AzureStorageKey)entity.Key;
            Assert.AreEqual(this.storageAccount, key.StorageAccountName);
            Assert.AreEqual(this.tableName, key.TableName);
            Assert.AreEqual(this.partition, key.Partition);
            Assert.AreEqual(this.rowId, key.RowId);
            Assert.AreEqual(this.localVersion, key.LocalVersion);
            Assert.AreEqual(this.versionTimestamp, key.VersionTimestamp);
        }

        /// <summary>GetEntity with no associations succeeds.</summary>
        [TestMethod]
        public void GetEntityNoAssociations()
        {
            var entityResult = this.BuildTestEntityResult(false);

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(entityResult);

            var entity = this.indexStore.GetEntity(this.externalEntityId, this.storageAccount, null);

            // Assert associations
            Assert.AreEqual(0, entity.Associations.Count);
        }

        /// <summary>GetEntity with missing nullable properties in index.</summary>
        [TestMethod]
        public void GetEntityWithNullableIndexProperties()
        {
            // Set some nullable properties
            this.externalName = null;
            this.entityCategory = null;
            this.externalType = null;
            this.lastModifiedUser = null;
            this.associationTargetExternalCategory = null;
            this.associationTargetExternalType = null;
            this.associationDetails = null;
            this.schemaVersion = -1; // forces it to be null in the result set

            var entityResult = this.BuildTestEntityResult(true);

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(entityResult);

            var entity = this.indexStore.GetEntity(this.externalEntityId, this.storageAccount, null);
            
            // Assert entity properties
            Assert.IsNull((string)entity.ExternalName);
            Assert.IsNull((string)entity.EntityCategory);
            Assert.IsNull((string)entity.ExternalType);
            Assert.IsNull((string)entity.LastModifiedUser);

            // Assert associations
            Assert.AreEqual(2, entity.Associations.Count);
            var assoc1 = entity.Associations[0];
            Assert.IsNull(assoc1.TargetEntityCategory);
            Assert.IsNull(assoc1.TargetExternalType);
            Assert.IsNull(assoc1.Details);
        }

        /// <summary>Happy-path SaveEntity.</summary>
        [TestMethod]
        public void SaveEntity()
        {
            IList<SqlParameter> sqlParameters = null;
            Action<IList<SqlParameter>> captureArgs = p => sqlParameters = p; 
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => captureArgs((IList<SqlParameter>)call.Arguments[1]))
                .Return(null);

            var entity = this.BuildTestEntity(true);

            this.indexStore.SaveEntity(entity);

            Assert.AreEqual((Guid)this.externalEntityId, (Guid)sqlParameters.Single(p => p.ParameterName == "@ExternalEntityId").Value);
            Assert.AreEqual(this.externalName, (string)sqlParameters.Single(p => p.ParameterName == "@ExternalName").Value);
            Assert.AreEqual(this.externalType, (string)sqlParameters.Single(p => p.ParameterName == "@ExternalType").Value);
            Assert.AreEqual(this.entityCategory, (string)sqlParameters.Single(p => p.ParameterName == "@EntityCategory").Value);
            Assert.AreEqual(1, (int)sqlParameters.Single(p => p.ParameterName == "@Active").Value);
            Assert.AreEqual(this.localVersion, (int)sqlParameters.Single(p => p.ParameterName == "@Version").Value);
            Assert.AreEqual(this.lastModifiedDate, (DateTime)sqlParameters.Single(p => p.ParameterName == "@TimeStamp").Value);
            Assert.AreEqual(this.storageAccount, (string)sqlParameters.Single(p => p.ParameterName == "@StorageAccountName").Value);
            Assert.AreEqual(this.lastModifiedUser, (string)sqlParameters.Single(p => p.ParameterName == "@LastModifiedUser").Value);
            Assert.AreEqual(this.schemaVersion, (int)sqlParameters.Single(p => p.ParameterName == "@SchemaVersion").Value);

            var associationList = (List<SqlDataRecord>)sqlParameters.Single(p => p.ParameterName == "@AssociationList").Value;
            Assert.AreEqual(2, associationList.Count);
            Assert.AreEqual(this.associationName, associationList[0].GetString(0));
            Assert.AreEqual((Guid)this.targetId1, associationList[0].GetGuid(1));
            Assert.AreEqual(Association.StringFromAssociationType(this.associationType), associationList[0].GetString(2));
            Assert.AreEqual(this.associationDetails, associationList[0].GetString(3));
        }
        
        /// <summary>SaveEntity with no associations succeeds.</summary>
        [TestMethod]
        public void SaveEntityNoAssociations()
        {
            IList<SqlParameter> sqlParameters = null;
            Action<IList<SqlParameter>> captureArgs = p => sqlParameters = p;
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => captureArgs((IList<SqlParameter>)call.Arguments[1]))
                .Return(null);

            var entity = this.BuildTestEntity(false);

            this.indexStore.SaveEntity(entity);

            var associationList = (List<SqlDataRecord>)sqlParameters.Single(p => p.ParameterName == "@AssociationList").Value;
            Assert.IsNull(associationList);
        }

        /// <summary>SaveEntity with nullable values succeeds.</summary>
        [TestMethod]
        public void SaveEntityWithNullable()
        {
            IList<SqlParameter> sqlParameters = null;
            Action<IList<SqlParameter>> captureArgs = p => sqlParameters = p;
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => captureArgs((IList<SqlParameter>)call.Arguments[1]))
                .Return(null);

            this.externalName = null;
            this.externalType = null;
            this.entityCategory = null;
            this.associationDetails = null;
            this.lastModifiedUser = null;
            var entity = this.BuildTestEntity(true);

            this.indexStore.SaveEntity(entity);

            Assert.AreEqual(DBNull.Value, sqlParameters.Single(p => p.ParameterName == "@ExternalName").Value);
            Assert.AreEqual(DBNull.Value, sqlParameters.Single(p => p.ParameterName == "@ExternalType").Value);
            Assert.AreEqual(DBNull.Value, sqlParameters.Single(p => p.ParameterName == "@EntityCategory").Value);
            Assert.AreEqual(DBNull.Value, sqlParameters.Single(p => p.ParameterName == "@LastModifiedUser").Value);

            var associationList = (List<SqlDataRecord>)sqlParameters.Single(p => p.ParameterName == "@AssociationList").Value;
            Assert.AreEqual(2, associationList.Count);
            Assert.IsTrue(associationList[0].IsDBNull(3));
        }

        /// <summary>SaveEntity should rethrow stale version exception.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void SaveEntityRethrowsStaleVersion()
        {
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .Throw(new DataAccessStaleEntityException());

            var entity = this.BuildTestEntity(false);

            this.indexStore.SaveEntity(entity);
        }

        /// <summary>SaveEntity should rethrow data access exception.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SaveEntityRethrowsDataAccessException()
        {
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityIndexEntry"), Arg<IList<SqlParameter>>.Is.Anything))
                .Throw(new DataAccessException());

            var entity = this.BuildTestEntity(false);

            this.indexStore.SaveEntity(entity);
        }

        /// <summary>Happy-path GetEntityInfoByCategory.</summary>
        [TestMethod]
        public void GetEntityInfoByCategory()
        {
            var id1 = new EntityId();
            var id2 = new EntityId();
            var queryResult = new QueryResult();
            queryResult.AddRecordSet();
            var queryRecord1 = new QueryRecord(
                new Dictionary<string, object> { { "ExternalEntityId", (Guid)id1 }, { "EntityCategory", "Category" }, { "ExternalName", "name1" }, { "ExternalType", "type1" } });
            var queryRecord2 = new QueryRecord(
                new Dictionary<string, object> { { "ExternalEntityId", (Guid)id2 }, { "EntityCategory", "Category" }, { "ExternalName", "name2" }, { "ExternalType", "type2" } });
            queryResult.AddRecord(queryRecord1, 0);
            queryResult.AddRecord(queryRecord2, 0);

            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("GetEntityInfoByCategory"), Arg<IList<SqlParameter>>.Is.Anything))
                .Return(queryResult);

            var entities = this.indexStore.GetEntityInfoByCategory("Category");

            var entity1 = entities.Single(e => (EntityId)e.ExternalEntityId == id1);
            var entity2 = entities.Single(e => (EntityId)e.ExternalEntityId == id2);
            Assert.AreEqual("Category", (string)entity1.EntityCategory);
            Assert.AreEqual("name1", (string)entity1.ExternalName);
            Assert.AreEqual("type1", (string)entity1.ExternalType);
            Assert.AreEqual("Category", (string)entity2.EntityCategory);
            Assert.AreEqual("name2", (string)entity2.ExternalName);
            Assert.AreEqual("type2", (string)entity2.ExternalType);
        }

        /// <summary>Happy-path SetEntityStatus.</summary>
        [TestMethod]
        public void SetEntityStatus()
        {
            var result = new QueryResult();
            result.AddRecordSet();
            result.AddRecord(new QueryRecord(new Dictionary<string, object> { { "UpdatedEntityCount", 2 } }), 0);

            IList<SqlParameter> sqlParameters = null;
            Action<IList<SqlParameter>> captureArgs = p => sqlParameters = p;
            this.sqlStore.Stub(f => f.ExecuteStoredProcedure(
                Arg<string>.Is.Equal("UpdateEntityStatus"), Arg<IList<SqlParameter>>.Is.Anything))
                .WhenCalled(call => captureArgs((IList<SqlParameter>)call.Arguments[1]))
                .Return(result);

            var entityIds = new HashSet<EntityId> { new EntityId(), new EntityId() };
            
            this.indexStore.SetEntityStatus(entityIds, true);
            Assert.AreEqual(1, (int)sqlParameters.Single(p => p.ParameterName == "@Active").Value);
            var entityIdList = (List<SqlDataRecord>)sqlParameters.Single(p => p.ParameterName == "@EntityIdList").Value;
            var actualEntityIds = entityIdList.Select(r => new EntityId(r.GetGuid(0))).ToList();
            Assert.IsTrue(actualEntityIds.SequenceEqual(entityIds));

            this.indexStore.SetEntityStatus(entityIds, false);
            Assert.AreEqual(0, (int)sqlParameters.Single(p => p.ParameterName == "@Active").Value);
        }

        /// <summary>Happy-path test of QueryRecord</summary>
        [TestMethod]
        public void QueryRecordSuccess()
        {
            var guid = Guid.NewGuid();
            var date = DateTime.UtcNow;
            var record = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "stringrecord", "stringvalue" },
                            { "intrecord", 1 },
                            { "guidrecord", guid },
                            { "daterecord", date },
                            { "boolrecord", true },
                            { "nullable", null },
                        });

            Assert.IsTrue(record.HasField("stringrecord"));
            Assert.IsFalse(record.HasField("notfound"));
            Assert.AreEqual("stringvalue", record.GetValue<string>("stringrecord"));
            Assert.AreEqual(1, record.GetValue<int>("intrecord"));
            Assert.AreEqual(guid, record.GetValue<Guid>("guidrecord"));
            Assert.AreEqual(date, record.GetValue<DateTime>("daterecord"));
            Assert.AreEqual(true, record.GetValue<bool>("boolrecord"));
            Assert.AreEqual(null, record.GetValue<string>("nullable"));
            Assert.IsTrue(record.Match("stringrecord", "stringvalue"));
            Assert.IsFalse(record.Match("stringrecord", "notthevalue"));
        }

        /// <summary>QueryRecord Null value for non-nullable type param fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryRecordNonNullableThrows()
        {
            var record = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "nonnullable", null },
                        });

            record.GetValue<DateTime>("nonnullable");
        }

        /// <summary>QueryRecord Type mismatch throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryRecordTypeMismatchThrows()
        {
            var record = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "stringrecord", "string value" },
                        });

            record.GetValue<DateTime>("stringrecord");
        }

        /// <summary>Happy-path test of QueryResult</summary>
        [TestMethod]
        public void QueryResultSuccess()
        {
            var record1 = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "stringrecord", "stringvalue" },
                            { "intrecord", 1 }
                        });
            var record2 = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "stringrecord", "stringvalue" },
                            { "intrecord", 1 }
                        });
            var result = new QueryResult();
            Assert.IsTrue(result.IsEmpty());

            result.AddRecordSet();
            result.AddRecordSet();
            Assert.AreEqual(0, result.GetRecords(0).Count);
            Assert.AreEqual(0, result.GetRecords(1).Count);

            // Record sets but no records should be considered empty
            Assert.IsTrue(result.IsEmpty());
            
            // Populate the second record set
            result.AddRecord(record2, 1);

            // Should not be considered empty
            Assert.IsFalse(result.IsEmpty());

            // Populate first record set
            result.AddRecord(record1, 0);

            Assert.AreEqual(1, result.GetRecords(0).Count);
            Assert.AreEqual(1, result.GetRecords(1).Count);
            Assert.AreEqual("stringvalue", result.GetSingleRecord(0, 0).GetValue<string>("stringrecord"));
            Assert.AreEqual("stringvalue", result.GetSingleRecord(1, 0).GetValue<string>("stringrecord"));

            // Match by one field, get value by another
            Assert.AreEqual(1, result.GetMatchingRecord(0, "stringrecord", "stringvalue").GetValue<int>("intrecord"));
        }

        /// <summary>QueryResult GetRecords throws if empty</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetRecordsThrowsIfEmpty()
        {
            var result = new QueryResult();
            result.GetRecords(0);
        }

        /// <summary>QueryResult GetRecords throws if requested record set not present</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetRecordsThrowsIfRecordSetNotPresent()
        {
            var result = new QueryResult();
            result.AddRecordSet();
            result.AddRecord(new QueryRecord(new Dictionary<string, object>()), 0);
            result.GetRecords(1);
        }

        /// <summary>QueryResult GetSingleRecords throws if empty</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetSingleRecordThrowsIfEmpty()
        {
            var result = new QueryResult();
            result.GetSingleRecord(0, 0);
        }

        /// <summary>QueryResult GetSingleRecords throws if recordset empty</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetSingleRecordThrowsIfRecordSetEmpty()
        {
            var result = new QueryResult();
            result.AddRecordSet();
            result.GetSingleRecord(0, 0);
        }

        /// <summary>QueryResult GetSingleRecords throws if requested recordset not present</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetSingleRecordThrowsIfRecordSetNotPresent()
        {
            var result = new QueryResult();
            result.AddRecordSet();
            result.AddRecord(new QueryRecord(new Dictionary<string, object>()), 0);
            result.GetSingleRecord(1, 0);
        }

        /// <summary>QueryResult GetSingleRecords throws if requested record not present</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void QueryResultGetSingleRecordThrowsIfRecordNotPresent()
        {
            var result = new QueryResult();
            result.AddRecordSet();
            result.AddRecord(new QueryRecord(new Dictionary<string, object>()), 0);
            result.GetSingleRecord(0, 1);
        }

        /// <summary>Build a default entity for testing.</summary>
        /// <param name="includeAssociations">True to include associations.</param>
        /// <returns>The entity.</returns>
        private IRawEntity BuildTestEntity(bool includeAssociations)
        {
            var key = new AzureStorageKey(this.storageAccount, this.tableName, this.partition, new EntityId());

            var entity = new Entity
                {
                    ExternalEntityId = this.externalEntityId,
                    CreateDate = this.createDate,
                    LastModifiedDate = this.lastModifiedDate,
                    LastModifiedUser = this.lastModifiedUser,
                    EntityCategory = this.entityCategory,
                    ExternalName = this.externalName,
                    ExternalType = this.externalType,
                    Key = key,
                    LocalVersion = this.localVersion,
                    SchemaVersion = this.schemaVersion
                };

            if (!includeAssociations)
            {
                return entity;
            }

            entity.Associations.Add(
                new Association
                    {
                        AssociationType = this.associationType,
                        ExternalName = this.associationName,
                        Details = this.associationDetails,
                        TargetEntityId = this.targetId1,
                        TargetEntityCategory = this.associationTargetExternalCategory,
                        TargetExternalType = this.associationTargetExternalType
                    });
            entity.Associations.Add(
                new Association
                    {
                        AssociationType = this.associationType,
                        ExternalName = this.associationName,
                        Details = this.associationDetails,
                        TargetEntityId = this.targetId2,
                        TargetEntityCategory = this.associationTargetExternalCategory,
                        TargetExternalType = this.associationTargetExternalType
                    });
            return entity;
        }

        /// <summary>Build a default query result with entity data for testing.</summary>
        /// <param name="includeAssociations">True to include associations.</param>
        /// <returns>The query result.</returns>
        private QueryResult BuildTestEntityResult(bool includeAssociations)
        {
            var queryResult = new QueryResult();
            queryResult.AddRecordSet();
            queryResult.AddRecordSet();
            queryResult.AddRecordSet();

            // Entity properties are in first record set
            var properties = new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "ExternalEntityId", (Guid)this.externalEntityId },
                            { "StorageType", this.storageType },
                            { "Version", this.localVersion },
                            { "CreateDate", this.createDate },
                            { "HomeStorageAccountName", this.storageAccount },
                            { "LastModifiedDate", this.lastModifiedDate },
                            { "ExternalName", this.externalName },
                            { "EntityCategory", this.entityCategory },
                            { "ExternalType", this.externalType },
                            { "LastModifiedUser", this.lastModifiedUser },
                            { "SchemaVersion", this.schemaVersion == -1 ? null : (object)this.schemaVersion },
                            { "Active", true },
                        });
            queryResult.AddRecord(properties, 0);

            // Key is in second record set
            var key = this.BuildTestKeyResult().GetSingleRecord(0, 0);
            queryResult.AddRecord(key, 1);

            if (!includeAssociations)
            {
                return queryResult;
            }

            // Associations are in third record set
            var association1 = new QueryRecord(
                new Dictionary<string, object>
                    {
                        { "ExternalName", this.associationName },
                        { "TargetEntityId", (Guid)this.targetId1 },
                        { "AssociationType", Association.StringFromAssociationType(this.associationType) },
                        { "TargetEntityCategory", this.associationTargetExternalCategory },
                        { "TargetExternalType", this.associationTargetExternalType },
                        { "Details", this.associationDetails },
                    });
            var association2 = new QueryRecord(
                new Dictionary<string, object>
                    {
                        { "ExternalName", this.associationName },
                        { "TargetEntityId", (Guid)this.targetId2 },
                        { "AssociationType", Association.StringFromAssociationType(this.associationType) },
                        { "TargetEntityCategory", this.associationTargetExternalCategory },
                        { "TargetExternalType", this.associationTargetExternalType },
                        { "Details", this.associationDetails },
                    });

            queryResult.AddRecord(association1, 2);
            queryResult.AddRecord(association2, 2);
            
            return queryResult;
        }

        /// <summary>Build a default query result with key fields for testing.</summary>
        /// <returns>The query result.</returns>
        private QueryResult BuildTestKeyResult()
        {
            var queryResult = new QueryResult();
            queryResult.AddRecordSet();
            queryResult.AddRecord(
                new QueryRecord(
                    new Dictionary<string, object>
                        {
                            { "StorageAccountName", this.storageAccount },
                            { "TableName", this.tableName },
                            { "Partition", this.partition },
                            { "RowId", (Guid)this.rowId },
                            { "LocalVersion", this.localVersion },
                            { "VersionTimestamp", this.versionTimestamp },
                        }),
                0);
            return queryResult;
        }
    }
}
