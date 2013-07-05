// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StorageKeyFactoryFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>
    /// Test fixture for storage key factories
    /// </summary>
    [TestClass]
    public class StorageKeyFactoryFixture
    {
        /// <summary>test value for storage account name</summary>
        private string expectedStorageAccountName = "MyXmlStorageAccount";

        /// <summary>test value for table name</summary>
        private string tableName = "CompanyFoo";

        /// <summary>test value for partition name</summary>
        private string partition = "SomethingClever";

        /// <summary>test value for row id</summary>
        private EntityId rowId = 10;

        /// <summary>index store test stub</summary>
        private IIndexStore indexStore;

        /// <summary>index store factory test stub</summary>
        private IIndexStoreFactory indexStoreFactory;

        /// <summary>key rule test stub</summary>
        private IKeyRule keyRule;

        /// <summary>partition value for testing</summary>
        private string partitionValue = "CompanyPartition";

        /// <summary>key rule factory test stub</summary>
        private IKeyRuleFactory keyRuleFactory;

        /// <summary>create date value for testing</summary>
        private DateTime createDate = DateTime.Now;

        /// <summary>external company id for testing</summary>
        private EntityId companyExternalId = new EntityId();

        /// <summary>Azure storage key for testing.</summary>
        private AzureStorageKey azureStorageKey;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Create index store stub and factory stub
            this.indexStore = MockRepository.GenerateStub<IIndexStore>();
            this.indexStoreFactory = MockRepository.GenerateStub<IIndexStoreFactory>();
            this.indexStoreFactory.Stub(f => f.GetIndexStore()).Return(this.indexStore);

            // Create key rule stub to return partition
            this.keyRule = MockRepository.GenerateStub<IKeyRule>();
            this.keyRule.Stub(f => f.GenerateKeyField(Arg<IEntity>.Is.Anything)).Return(this.partitionValue);

            // Create key rule factory stub to return key rule
            this.keyRuleFactory = MockRepository.GenerateStub<IKeyRuleFactory>();
            this.keyRuleFactory.Stub(
                f => f.GetKeyRule(Arg<IEntity>.Is.Anything, Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .Return(this.keyRule);

            this.azureStorageKey = new AzureStorageKey(this.expectedStorageAccountName, this.tableName, this.partition, this.rowId);
        }

        /// <summary>Test Xml key factory injection construction</summary>
        [TestMethod]
        public void XmlInjectionConstructor()
        {
            // Call key factory constructor with data store
            var keyFactory = new XmlStorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            // Assert that it is using the correct data store
            Assert.IsNotNull(keyFactory);
            Assert.AreSame(this.indexStoreFactory, keyFactory.IndexStoreFactory);
            Assert.AreSame(this.keyRuleFactory, keyFactory.KeyRuleFactory);
        }

        /// <summary>Test Azure key factory injection construction</summary>
        [TestMethod]
        public void AzureInjectionConstructor()
        {
            // Call key factory constructor with data store
            var keyFactory = new AzureStorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            // Assert that it is using the correct data store
            Assert.IsNotNull(keyFactory);
            Assert.AreSame(this.indexStoreFactory, keyFactory.IndexStoreFactory);
            Assert.AreSame(this.keyRuleFactory, keyFactory.KeyRuleFactory);
        }

        /// <summary>Test S3 key factory injection construction</summary>
        [TestMethod]
        public void S3InjectionConstructor()
        {
            // Call key factory constructor with data store
            var keyFactory = new S3StorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            // Assert that it is using the correct data store
            Assert.IsNotNull(keyFactory);
            Assert.AreSame(this.indexStoreFactory, keyFactory.IndexStoreFactory);
            Assert.AreSame(this.keyRuleFactory, keyFactory.KeyRuleFactory);
        }

        /// <summary>Test to build a new Xml storage key for a company.</summary>
        [TestMethod]
        public void XmlBuildNewCompanyStorageKey()
        {
            // Create entity
            var entity = this.BuildNewEntity<XmlRawEntity>();

            // Create concrete IStorageKeyFactory using the stubbed key rule stack
            var keyFactory = new XmlStorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            // Create entity key - these will be a little different because we partially populate the entity key
            // with table name. It would not be available in the index yet at this point.
            var partialKey = new XmlStorageKey(this.expectedStorageAccountName, this.tableName, string.Empty, Guid.Empty);
            entity.Key = partialKey;
            var key = (XmlStorageKey)keyFactory.BuildNewStorageKey(this.expectedStorageAccountName, this.companyExternalId, entity);

            Assert.AreEqual(this.expectedStorageAccountName, key.StorageAccountName);
            Assert.AreEqual(partialKey.TableName, key.TableName);
            Assert.AreEqual(this.partitionValue, key.Partition);
            Assert.AreNotEqual(Guid.Empty, (Guid)key.RowId);
        }

        /// <summary>Test to build a new Azure storage key for a company.</summary>
        [TestMethod]
        public void AzureBuildNewCompanyStorageKey()
        {
            // Create entity
            var entity = this.BuildNewEntity<Entity>();

            // Create concrete IStorageKeyFactory using the stubbed key rule stack
            var keyFactory = new AzureStorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            // Create entity key - these will be a little different because we partially populate the entity key
            // with table name. It would not be available in the index yet at this point.
            var partialKey = new AzureStorageKey(this.expectedStorageAccountName, this.tableName, string.Empty, Guid.Empty);
            entity.Key = partialKey;
            var key = (AzureStorageKey)keyFactory.BuildNewStorageKey(this.expectedStorageAccountName, this.companyExternalId, entity);

            Assert.AreEqual(this.expectedStorageAccountName, key.StorageAccountName);
            Assert.AreEqual(partialKey.TableName, key.TableName);
            Assert.AreEqual(this.partitionValue, key.Partition);
            Assert.AreNotEqual(Guid.Empty, (Guid)key.RowId);
        }

        /// <summary>Test to build a new Azure storage key for an entity other than a company.</summary>
        [TestMethod]
        public void AzureBuildNewEntityStorageKey()
        {
            // Create entity
            var entity = this.BuildNewEntity<Entity>();
            entity.EntityCategory = "SomethingOtherThanCompany";

            this.indexStore.Stub(f => f.GetStorageKey(Arg<EntityId>.Is.Anything, Arg<string>.Is.Anything)).Return(this.azureStorageKey);

            // Create concrete IStorageKeyFactory using the stubbed key rule stack
            var keyFactory = new AzureStorageKeyFactory(this.indexStoreFactory, this.keyRuleFactory);

            var key = (AzureStorageKey)keyFactory.BuildNewStorageKey(this.expectedStorageAccountName, this.companyExternalId, entity);

            Assert.AreEqual(this.expectedStorageAccountName, key.StorageAccountName);
            Assert.AreEqual(this.tableName, key.TableName);
            Assert.AreEqual(this.partitionValue, key.Partition);
            Assert.AreNotEqual(Guid.Empty, (Guid)key.RowId);
        }

        /// <summary>Build a raw entity with the base interface members needed by these tests populated</summary>
        /// <typeparam name="T">Concrete IEntity</typeparam>
        /// <returns>IEntity derived entity</returns>
        private IEntity BuildNewEntity<T>() where T : IEntity, new()
        {
            var entity = new T
            {
                ExternalEntityId = this.companyExternalId,
                EntityCategory = "Company",
                CreateDate = this.createDate,
                LastModifiedDate = this.createDate,
            };

            return entity;
        }
    }
}
