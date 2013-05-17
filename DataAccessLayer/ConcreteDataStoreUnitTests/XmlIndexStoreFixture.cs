// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlIndexStoreFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for XmlDataStore</summary>
    [TestClass]
    public class XmlIndexStoreFixture
    {
        /// <summary>minimum index store</summary>
        private XmlIndexDataStore minIndexStore;

        /// <summary>Xml storage account name</summary>
        private string xmlStorageAccountName = "MyXmlStorageAccount";

        /// <summary>Name of an entity table</summary>
        private string entityTableName = "CompanyFoo";

        /// <summary>Azure storage account name</summary>
        private string azureStorageAccountName = "MyAzureStorageAccount";

        /// <summary>Table storage partition value</summary>
        private string partitionValue = "FooCampaign1";

        /// <summary>A RowId value</summary>
        private EntityId rowIdValue1 = 10;

        /// <summary>S3 storage account name</summary>
        private string awsStorageAccountName = "MyS3StorageAccount";

        /// <summary>Index store factory stub.</summary>
        private IIndexStoreFactory indexStoreFactory;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Create the index store
            var indexXml = ResourceHelper.LoadXmlResource(@"MinimalIndexStore.xml");
            this.minIndexStore = new XmlIndexDataStore();
            this.minIndexStore.LoadFromXml(indexXml);
            
            // Create the index store factory stub
            this.indexStoreFactory = MockRepository.GenerateStub<IIndexStoreFactory>();
            this.indexStoreFactory.Stub(f => f.GetIndexStore()).Return(this.minIndexStore);
        }

        /// <summary>Default constructor</summary>
        [TestMethod]
        public void DefaultConstructor()
        {
            var store = new XmlIndexDataStore();
            Assert.IsNotNull(store);
            Assert.IsInstanceOfType(store.DataSet, typeof(XmlIndexDataSet));
        }

        /// <summary>Initialize index store from an xml string.</summary>
        [TestMethod]
        public void LoadIndexStoreFromXmlString()
        {
            var dataSet = (XmlIndexDataSet)this.minIndexStore.DataSet;
            var companyInfo = dataSet.Company.Single();
            Assert.AreEqual("MyCompany", companyInfo.Name);
        }

        /// <summary>Initialize index store from an xml file.</summary>
        [TestMethod]
        [DeploymentItem(@"resources\indexstorebackingfile.xml")]
        public void LoadIndexStoreFromBackingFile()
        {
            var store = new XmlIndexDataStore();
            store.LoadFromFile(@"indexstorebackingfile.xml");
            Assert.IsTrue(store.DataSet.Tables.Count > 0);
        }

        /// <summary>Verify reloading the index datastore is destructive.</summary>
        [TestMethod]
        public void LoadIndexIsDestructive()
        {
            // Verify intital number of elements
            var dataSet = (XmlIndexDataSet)this.minIndexStore.DataSet;
            Assert.AreEqual(1, dataSet.Company.Count);
            
            // Add a new element and verify
            var newCompany = dataSet.Company.NewCompanyRow();
            newCompany.xId = (EntityId)100;
            dataSet.Company.AddCompanyRow(newCompany);
            Assert.AreEqual(2, dataSet.Company.Count);
            Assert.IsNotNull(dataSet.Company.Single(e => e.xId == newCompany.xId));

            // Reload
            var dataXml = ResourceHelper.LoadXmlResource(@"MinimalIndexStore.xml");
            this.minIndexStore.LoadFromXml(dataXml);
            dataSet = (XmlIndexDataSet)this.minIndexStore.DataSet;
            
            // Assert we have initial number of elements
            Assert.AreEqual(1, dataSet.Company.Count);
        }

        /// <summary>Get the key fields for an entity homed in an Xml data store.</summary>
        [TestMethod]
        public void GetXmlKeyFieldsFromExternalId()
        {
            var indexStore = this.minIndexStore as IIndexStore;
            var keyFields = (XmlStorageKey)indexStore.GetStorageKey(1, string.Empty);

            // Assert we have the expected key elements
            Assert.AreEqual(this.xmlStorageAccountName, keyFields.StorageAccountName);
            Assert.AreEqual(this.entityTableName, keyFields.TableName);
            Assert.AreEqual(this.partitionValue, keyFields.Partition);
            Assert.AreEqual(this.rowIdValue1, keyFields.RowId);
        }

        /// <summary>Get the key fields for an entity homed in an Azure Table data store.</summary>
        [TestMethod]
        public void GetAzureKeyFieldsFromExternalId()
        {
            // Get the key fields for an entity homed in an Azure data store
            var indexStore = this.minIndexStore as IIndexStore;
            var keyFields = (AzureStorageKey)indexStore.GetStorageKey(2, string.Empty);

            // Assert we have the expected key elements
            Assert.AreEqual(this.azureStorageAccountName, keyFields.StorageAccountName);
            Assert.AreEqual(this.entityTableName, keyFields.TableName);
            Assert.AreEqual(this.partitionValue, keyFields.Partition);
            Assert.AreEqual(this.rowIdValue1, keyFields.RowId);
        }

        /// <summary>Make sure we return an empty dictionary if the key fields for an entity are not found.</summary>
        [TestMethod]
        public void GetAzureKeyFieldsFromExternalIdReturnEmptyDictionaryIfNotFound()
        {
            // Get the key fields for an entity homed in an Azure data store
            var indexStore = this.minIndexStore as IIndexStore;
            var idNotFound = new EntityId(123);
            var keyFields = (AzureStorageKey)indexStore.GetStorageKey(idNotFound, string.Empty);

            // Assert we have a null key
            Assert.IsNull(keyFields);
        }

        /// <summary>Get key fields for an entity homed in an S3 Table data store.</summary>
        [TestMethod]
        public void GetS3KeyFieldsFromExternalId()
        {
            var indexStore = this.minIndexStore as IIndexStore;
            var keyFields = (S3StorageKey)indexStore.GetStorageKey(3, string.Empty);

            // Assert we have the expected key elements
            Assert.AreEqual(this.awsStorageAccountName, keyFields.StorageAccountName);
            Assert.AreEqual(this.entityTableName, keyFields.TbdS3);
        }

        /// <summary>Verify we get the key fields for the home data store.</summary>
        [TestMethod]
        public void GetHomeKeyFieldsFromExternalIdIfExistsInMultipleStores()
        {
            var indexStore = this.minIndexStore as IIndexStore;
            var keyFields = (XmlStorageKey)indexStore.GetStorageKey(4, string.Empty);

            // We should see results for the home storage account
            Assert.AreEqual(this.xmlStorageAccountName, keyFields.StorageAccountName);
            Assert.AreEqual(11, keyFields.RowId);
        }
        
        /// <summary>Key field tables cannot have a duplicate external entity id in the same data store.</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ExternalIdUniquePerStorageAccount()
        {
            // Add a duplicate entry to the XmlKeyFields table
            var indexStore = this.minIndexStore as IIndexStore;
            var indexDataSet = (XmlIndexDataSet)this.minIndexStore.DataSet;
            indexDataSet.XmlKeyFields.AddXmlKeyFieldsRow(
                (EntityId)4, this.xmlStorageAccountName, this.entityTableName, this.partitionValue, (EntityId)11, 0);

            indexStore.GetStorageKey(4, string.Empty);
        }

        /// <summary>Key field tables cannot have a duplicate external entity id in the same data store.</summary>
        [TestMethod]
        public void InvalidStorageType()
        {
            // this.indexStore.Stub(f => f.GetKeyFieldsFromExternalId(Arg<EntityId>.Is.Anything)).Return(this.keyFields);
            // Add a duplicate entry to the XmlKeyFields table
            var indexStore = this.minIndexStore as IIndexStore;
            var key = indexStore.GetStorageKey(5, string.Empty);
            Assert.IsNull(key);
        }

        /// <summary>Test saving index entry for a new xml entity.</summary>
        [TestMethod]
        public void SaveEntityForXmlEntityStore()
        {
            // Create entity
            var companyName = "Megatherium Trust";
            var companyExternalId = new EntityId();
            var key = new XmlStorageKey(this.xmlStorageAccountName, this.entityTableName, "SomePartition", new EntityId());
            var createDate = DateTime.Now;
            var entity = new XmlRawEntity
            {
                Key = key,
                ExternalEntityId = companyExternalId,
                EntityCategory = "Company",
                ExternalName = companyName,
                ExternalType = "MegaCompany",
                LocalVersion = 0,
                CreateDate = createDate,
                LastModifiedDate = createDate,
                Fields = "someserializeddata"
            };

            // Save the index entry for the entity
            var indexStore = (IIndexStore)this.minIndexStore;
            indexStore.SaveEntity(entity);

            var savedKey = (XmlStorageKey)indexStore.GetStorageKey(companyExternalId, string.Empty);

            // Assert retrieved index data matches saved data
            // TODO: Implement equality operators for storage keys
            Assert.AreEqual(key.StorageAccountName, savedKey.StorageAccountName);
            Assert.AreEqual(key.TableName, savedKey.TableName);
            Assert.AreEqual(key.Partition, savedKey.Partition);
            Assert.AreEqual(key.RowId, savedKey.RowId);

            // Assert remaining index table info directly
            var indexDataSet = (XmlIndexDataSet)this.minIndexStore.DataSet;
            var indexRow = indexDataSet.EntityId.Single(r => r.xId == (Guid)companyExternalId);

            Assert.AreEqual(key.StorageAccountName, indexRow.HomeStorageAccountName);
            Assert.AreEqual("Xml", indexRow.StorageType);
            Assert.AreEqual(0, indexRow.Version);
            Assert.AreEqual(false, indexRow.WriteLock);
            Assert.AreEqual(createDate.ToUniversalTime(), indexRow.LastModifiedDate);
            Assert.AreEqual(createDate.ToUniversalTime(), indexRow.CreateDate);
        }
    }
}
