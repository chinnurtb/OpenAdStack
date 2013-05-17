// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlobEntityFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Rhino.Mocks;
using Utilities.Serialization;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test fixture for BlobEntity class.</summary>
    [TestClass]
    public class BlobEntityFixture
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
                EntityCategory = new EntityProperty { Name = "EntityCategory", Value = BlobEntity.BlobEntityCategory },
                CreateDate = new EntityProperty { Name = "CreateDate", Value = DateTime.Now },
                LastModifiedDate = new EntityProperty { Name = "LastModifiedDate", Value = DateTime.Now },
                LocalVersion = new EntityProperty { Name = "LocalVersion", Value = 1 },
                Key = MockRepository.GenerateStub<IStorageKey>()
            };
        }
        
        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIRawEntity()
        {
            var blobEntity = new BlobEntity(this.wrappedEntity);

            Assert.AreSame(this.wrappedEntity, blobEntity.WrappedEntity);
            Assert.AreEqual(null, blobEntity.BlobData.Value);

            var entityBase = EntityWrapperBase.BuildWrappedEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, EntityWrapperBase.SafeUnwrapEntity(entityBase));
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromLegacyIEntity()
        {
            var blobEntityId = new EntityId();
            var objToBlob = "{\"json1\" : \"val1\" }";
            var legacyBlobEntity = new BlobEntity(blobEntityId);

            // Serialize the object to a byte array containing the xml serialized object representation.
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(string));
                serializer.WriteObject(stream, objToBlob);
                legacyBlobEntity.BlobBytes = stream.ToArray();
            }

            var blobEntity = new BlobEntity(legacyBlobEntity);
            Assert.AreSame(legacyBlobEntity.WrappedEntity, blobEntity.WrappedEntity);
            Assert.AreEqual(null, blobEntity.BlobData.Value);
            Assert.IsNotNull(blobEntity.BlobBytes);
        }

        /// <summary>Test we can construct and validate from entityId.</summary>
        [TestMethod]
        public void ConstructFromEntityId()
        {
            var blobEntityId = new EntityId();
            var blobEntity = new BlobEntity(blobEntityId);
            Assert.IsNotNull(blobEntity.WrappedEntity);
            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(null, blobEntity.BlobData.Value);
        }

        /// <summary>Test the factory method to construct from a json string.</summary>
        [TestMethod]
        public void RoundtripSerializeJsonString()
        {
            var blobEntityId = new EntityId();
            var objToBlob = "{\"json1\" : \"val1\" }";
            var blobEntity = BlobEntity.BuildBlobEntity(blobEntityId, objToBlob);
            var blobToObj = blobEntity.DeserializeBlob<string>();

            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob, blobToObj);
        }

        /// <summary>Test the factory method to construct from a non-json string.</summary>
        [TestMethod]
        public void RoundtripSerializeNonJsonString()
        {
            var blobEntityId = new EntityId();
            var objToBlob = "OnnaStick";
            var blobEntity = BlobEntity.BuildBlobEntity(blobEntityId, objToBlob);
            var blobToObj = blobEntity.DeserializeBlob<string>();

            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob, blobToObj);
        }

        /// <summary>Test the factory method to construct from a serializable object and subsequently deserialize.</summary>
        [TestMethod]
        public void RoundtripSerializeObject()
        {
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "OnnaStick" };
            var blobEntity = BlobEntity.BuildBlobEntity(blobEntityId, objToBlob);
            var blobToObj = blobEntity.DeserializeBlob<TestBlobType>();

            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob.Foo, blobToObj.Foo);
            Assert.AreEqual(objToBlob.Bar, blobToObj.Bar);
        }

        /// <summary>Test we deserialized correctly an object that was built as a json string but deserialized to target type.</summary>
        [TestMethod]
        public void DeserializeObjectPreviouslyBuiltAsJsonString()
        {
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "OnnaStick" };
            var objJson = JsonConvert.SerializeObject(objToBlob);
            var blobEntity = BlobEntity.BuildBlobEntity(blobEntityId, objJson);
            var blobToObj = blobEntity.DeserializeBlob<TestBlobType>();

            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob.Foo, blobToObj.Foo);
            Assert.AreEqual(objToBlob.Bar, blobToObj.Bar);
        }

        /// <summary>Test we deserialized correctly an object that was saved with the string constructor instead of the factory method.</summary>
        [TestMethod]
        public void RoundtripDeserializeConstructedObject()
        {
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "OnnaStick" };
            var objJson = JsonConvert.SerializeObject(objToBlob);
            var blobEntity = new BlobEntity(blobEntityId, objJson);
            var blobToObj = blobEntity.DeserializeBlob<TestBlobType>();

            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob.Foo, blobToObj.Foo);
            Assert.AreEqual(objToBlob.Bar, blobToObj.Bar);
        }

        /// <summary>Migration Only: Test we correctly deserialize an object that was serialized as Xml.</summary>
        [TestMethod]
        public void DeserializeObjectPreviouslySerializedAsXml()
        {
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "OnnaStick" };
            var blobEntity = new BlobEntity(blobEntityId);

            // Serialize the object to a byte array containing the xml serialized object representation.
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(TestBlobType));
                serializer.WriteObject(stream, objToBlob);
                blobEntity.BlobBytes = stream.ToArray();
            }

            var blobToObj = blobEntity.DeserializeBlobFromXml<TestBlobType>();
            Assert.AreEqual(blobEntityId, (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(objToBlob.Foo, blobToObj.Foo);
            Assert.AreEqual(objToBlob.Bar, blobToObj.Bar);
        }

        /// <summary>Test that a blob entity throws if deserialized with with no blob bytes.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SerializeNullBytesFails()
        {
            var blobEntityId = new EntityId();
            BlobEntity.BuildBlobEntity<object>(blobEntityId, null);
        }

        /// <summary>Test that a blob entity throws if deserialized with with no blob bytes.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeNullBytesFails()
        {
            var blobEntityId = new EntityId();
            var blobEntity = new BlobEntity(blobEntityId);
            blobEntity.DeserializeBlob<TestBlobType>();
        }

        /// <summary>Test that a blob entity throws if deserialized with with no blob bytes.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsJsonException))]
        public void DeserializeWrongTypeFails()
        {
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "OnnaStick" };
            var objJson = JsonConvert.SerializeObject(objToBlob);
            var blobEntity = new BlobEntity(blobEntityId, objJson);
            blobEntity.DeserializeBlob<List<string>>();
        }

        /// <summary>Validate that entity construction fails if category is not Blob.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailValidationIfCategoryPropertyNotBlob()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new BlobEntity(this.wrappedEntity);
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
