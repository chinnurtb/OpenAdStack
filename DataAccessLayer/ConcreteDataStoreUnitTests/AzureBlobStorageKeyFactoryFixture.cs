//-----------------------------------------------------------------------
// <copyright file="AzureBlobStorageKeyFactoryFixture.cs" company="Rare Crowds Inc.">
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
using ConcreteDataStore;
using DataAccessLayer;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>
    /// Test fixture class for AzureBlobStorageKeyFactoryFixture
    /// </summary>
    [TestClass]
    public class AzureBlobStorageKeyFactoryFixture
    {
        /// <summary>
        /// Per-test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateStub<ILogger>() });
        }

        /// <summary>Happy-path round-trip serialize blob storage key.</summary>
        [TestMethod]
        public void RoundtripSerializeKey()
        {
            var factory = new AzureBlobStorageKeyFactory();
            var entity = new BlobPropertyEntity(new EntityId());
            var key = factory.BuildNewStorageKey("account", new EntityId(), entity) as AzureBlobStorageKey;
            var serializedKey = factory.SerializeBlobKey(key);
            var roundtripKey = factory.DeserializeKey(serializedKey);
            Assert.IsTrue(key.IsEqual(roundtripKey));
            Assert.IsNull(roundtripKey.VersionTimestamp);
        }

        /// <summary>Build updated storage key not supported for blobs.</summary>
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void BuildUpdatedStorageKeyNotSupportedForBlob()
        {
            var factory = new AzureBlobStorageKeyFactory();
            var entity = new BlobPropertyEntity(new EntityId());
            var key = factory.BuildNewStorageKey("account", new EntityId(), entity) as AzureBlobStorageKey;
            factory.BuildUpdatedStorageKey(key, entity);
        }

        /// <summary>Serialize null key throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SerializeKeyNull()
        {
            var factory = new AzureBlobStorageKeyFactory();
            factory.SerializeBlobKey(null);
        }

        /// <summary>Deserialize null key throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeNullKey()
        {
            var factory = new AzureBlobStorageKeyFactory();
            factory.DeserializeKey(null);
        }

        /// <summary>Deserialize key with no container throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeInvalidKeyNoContainer()
        {
            var factory = new AzureBlobStorageKeyFactory();
            factory.DeserializeKey("{\"BlobId\":\"123\",\"StorageAccountName\":\"account\",\"VersionTimestamp\":null,\"LocalVersion\":0}");
        }

        /// <summary>Deserialize key with no blob id throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeInvalidKeyNoBlobId()
        {
            var factory = new AzureBlobStorageKeyFactory();
            factory.DeserializeKey("{\"ContainerName\":\"container\",\"StorageAccountName\":\"account\",\"VersionTimestamp\":null,\"LocalVersion\":0}");
        }

        /// <summary>Deserialize key with no account name throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeInvalidKeyNoAccountName()
        {
            var factory = new AzureBlobStorageKeyFactory();
            factory.DeserializeKey("{\"ContainerName\":\"container\",\"BlobId\":\"123\",\"VersionTimestamp\":null,\"LocalVersion\":0}");
        }

        /// <summary>
        /// An outgoing property that was persisted with the blob ref marker but is not will
        /// be optimistically treated as a normal property.
        /// </summary>
        [TestMethod]
        public void InvalidOutgoingBlobRefsTreatedAsNormalProperties()
        {
            var factory = new AzureBlobStorageKeyFactory();
            var key = factory.BuildNewStorageKey("account", new EntityId(), new BlobPropertyEntity(new EntityId())) as AzureBlobStorageKey;
            var serializedKey = factory.SerializeBlobKey(key);
            var deserializedKey = factory.DeserializeKey(serializedKey) as AzureBlobStorageKey;

            Assert.AreEqual(key.StorageAccountName, deserializedKey.StorageAccountName);
            Assert.AreEqual(key.BlobId, deserializedKey.BlobId);
            Assert.AreEqual(key.ContainerName, deserializedKey.ContainerName);
            Assert.AreEqual(key.LocalVersion, deserializedKey.LocalVersion);
            Assert.AreEqual(key.VersionTimestamp, deserializedKey.VersionTimestamp);
        }
    }
}
