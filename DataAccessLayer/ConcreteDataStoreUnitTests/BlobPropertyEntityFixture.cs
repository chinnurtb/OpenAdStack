// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlobPropertyEntityFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for BlobPropertyEntity class.</summary>
    [TestClass]
    public class BlobPropertyEntityFixture
    {
        /// <summary>Entity object with blob properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty { Name = "ExternalEntityId", Value = new EntityId() },
                EntityCategory = new EntityProperty { Name = "EntityCategory", Value = BlobPropertyEntity.BlobPropertyEntityCategory },
                CreateDate = new EntityProperty { Name = "CreateDate", Value = DateTime.Now },
                LastModifiedDate = new EntityProperty { Name = "LastModifiedDate", Value = DateTime.Now },
                LocalVersion = new EntityProperty { Name = "LocalVersion", Value = 1 },
                Key = MockRepository.GenerateStub<IStorageKey>()
            };
        }
        
        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var blobEntity = new BlobPropertyEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, blobEntity.WrappedEntity);
            Assert.AreEqual(null, blobEntity.BlobBytes.Value);
        }

        /// <summary>Test we can construct and validate from entityId.</summary>
        [TestMethod]
        public void ConstructFromEntityId()
        {
            var blobEntityId = new EntityId();
            var blobEntity = new BlobPropertyEntity(blobEntityId);
            Assert.IsNotNull(blobEntity.WrappedEntity);
            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(null, blobEntity.BlobBytes.Value);
        }

        /// <summary>Test we can construct and byte array.</summary>
        [TestMethod]
        public void ConstructFromBytes()
        {
            var blobEntityId = new EntityId();
            var blobBytes = new byte[] { 1 };
            var blobEntity = new BlobPropertyEntity(blobEntityId, blobBytes);
            Assert.IsNotNull(blobEntity.WrappedEntity);
            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(blobBytes[0], ((byte[])blobEntity.BlobBytes)[0]);
            Assert.AreEqual(string.Empty, (string)blobEntity.BlobPropertyType);
        }

        /// <summary>Test we can construct and byte array.</summary>
        [TestMethod]
        public void ConstructFromBytesAndPropertyType()
        {
            var blobEntityId = new EntityId();
            var blobBytes = new byte[] { 1 };
            var blobEntity = new BlobPropertyEntity(blobEntityId, blobBytes, PropertyType.String);
            Assert.IsNotNull(blobEntity.WrappedEntity);
            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(blobBytes[0], ((byte[])blobEntity.BlobBytes)[0]);
            Assert.AreEqual("String", (string)blobEntity.BlobPropertyType);
        }

        /// <summary>Validate that entity construction fails if category is not Blob.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailValidationIfCategoryPropertyNotBlob()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new BlobPropertyEntity(this.wrappedEntity);
        }

        /// <summary>Simple blob interface testing type...not trying to test PersistentDictionary here.</summary>
        [DataContract]
        internal class TestBlobType
        {
            /// <summary>Gets or sets Foo.</summary>
            [DataMember]
            public int Foo { get; set; }

            /// <summary>Gets or sets Bar.</summary>
            [DataMember]
            public string Bar { get; set; }
        }
    }
}
