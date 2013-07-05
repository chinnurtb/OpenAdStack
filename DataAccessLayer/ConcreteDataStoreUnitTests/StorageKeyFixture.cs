// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StorageKeyFixture.cs" company="Rare Crowds Inc">
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

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for storage key abstractions</summary>
    [TestClass]
    public class StorageKeyFixture
    {
        /// <summary>Test construction for Xml storage key abstration</summary>
        [TestMethod]
        public void XmlStorageKeyConstructor()
        {
            var interfaceKey = new XmlStorageKey("acct", "tn", "part", new EntityId(1)) as IStorageKey;
            Assert.IsNotNull(interfaceKey);
            var key = (XmlStorageKey)interfaceKey;
            Assert.AreEqual("acct", key.StorageAccountName);
            Assert.AreEqual("tn", key.TableName);
            Assert.AreEqual("part", key.Partition);
            Assert.AreEqual(new EntityId(1), key.RowId);
        }

        /// <summary>Test construction for Azure storage key abstraction</summary>
        [TestMethod]
        public void AzureStorageKeyConstructor()
        {
            var entityId = new EntityId(1);
            var interfaceKey = new AzureStorageKey("acct", "tn", "part", entityId) as IStorageKey;
            Assert.IsNotNull(interfaceKey);
            var key = (AzureStorageKey)interfaceKey;
            Assert.AreEqual("acct", key.StorageAccountName);
            Assert.AreEqual("tn", key.TableName);
            Assert.AreEqual("part", key.Partition);
            Assert.AreEqual(entityId, key.RowId);
            Assert.AreEqual(0, key.LocalVersion);
            Assert.AreEqual(null, key.VersionTimestamp);
        }

        /// <summary>Test that IsEqual method of AzureStorageKey</summary>
        [TestMethod]
        public void AzureKeyIsEqual()
        {
            var key = new AzureStorageKey("acc", "tab", "par", new EntityId(), 0, DateTime.UtcNow);
            var key2 = new AzureStorageKey(key);
            Assert.IsTrue(key.IsEqual(key2));
            key2 = new AzureStorageKey(key) { LocalVersion = 1 };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureStorageKey(key) { Partition = "other" };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureStorageKey(key) { RowId = new EntityId() };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureStorageKey(key) { StorageAccountName = "other" };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureStorageKey(key) { TableName = "other" };
            Assert.IsFalse(key.IsEqual(key2));

            // timestamp is not included in comparison
            key2 = new AzureStorageKey(key) { VersionTimestamp = DateTime.UtcNow.AddDays(1) };
            Assert.IsTrue(key.IsEqual(key2));
        }

        /// <summary>Test KeyFields property of AzureStorageKey.</summary>
        [TestMethod]
        public void AzureStorageKeyGetKeyFields()
        {
            var entityId = new EntityId(1);
            var interfaceKey = new AzureStorageKey("acct", "tn", "part", entityId) as IStorageKey;
            var keyFields = interfaceKey.KeyFields;
            Assert.AreEqual(keyFields[AzureStorageKey.TableNameFieldName], "tn");
            Assert.AreEqual(keyFields[AzureStorageKey.PartitionFieldName], "part");
            Assert.AreEqual(keyFields[AzureStorageKey.RowIdFieldName], (string)entityId);
        }

        /// <summary>Test construction for Azure blob storage key abstraction</summary>
        [TestMethod]
        public void AzureBlobStorageKeyConstructor()
        {
            var interfaceKey = new AzureBlobStorageKey("acct", "container", "blobId") as IStorageKey;
            Assert.IsNotNull(interfaceKey);
            var key = (AzureBlobStorageKey)interfaceKey;
            Assert.AreEqual("acct", key.StorageAccountName);
            Assert.AreEqual("container", key.ContainerName);
            Assert.AreEqual("blobId", key.BlobId);
            Assert.AreEqual(0, key.LocalVersion);
            Assert.AreEqual(null, key.VersionTimestamp);
        }

        /// <summary>Test that IsEqual method of AzureBlobStorageKey</summary>
        [TestMethod]
        public void AzureBlobKeyIsEqual()
        {
            var key = new AzureBlobStorageKey("acct", "container", "blobId");
            var key2 = new AzureBlobStorageKey(key);
            Assert.IsTrue(key.IsEqual(key2));
            key2 = new AzureBlobStorageKey(key) { LocalVersion = 1 };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureBlobStorageKey(key) { ContainerName = "other" };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureBlobStorageKey(key) { BlobId = new EntityId() };
            Assert.IsFalse(key.IsEqual(key2));
            key2 = new AzureBlobStorageKey(key) { StorageAccountName = "other" };
            Assert.IsFalse(key.IsEqual(key2));

            // timestamp is not included in comparison
            key2 = new AzureBlobStorageKey(key) { VersionTimestamp = DateTime.UtcNow.AddDays(1) };
            Assert.IsTrue(key.IsEqual(key2));
        }

        /// <summary>Test KeyFields property of AzureBlobStorageKey.</summary>
        [TestMethod]
        public void AzureBlobStorageKeyGetKeyFields()
        {
            var interfaceKey = new AzureBlobStorageKey("acct", "container", "blobId") as IStorageKey;
            var keyFields = interfaceKey.KeyFields;
            Assert.AreEqual(keyFields[AzureBlobStorageKey.BlobMarkerFieldName], AzureBlobStorageKey.AzureBlobMarker);
            Assert.AreEqual(keyFields[AzureBlobStorageKey.ContainerFieldName], "container");
            Assert.AreEqual(keyFields[AzureBlobStorageKey.BlobIdFieldName], "blobId");
        }

        /// <summary>Test construction for S3 storage key abstraction</summary>
        [TestMethod]
        public void S3StorageKeyConstructor()
        {
            var interfaceKey = new S3StorageKey("acct", "tbd") as IStorageKey;
            Assert.IsNotNull(interfaceKey);
            var key = (S3StorageKey)interfaceKey;
            Assert.AreEqual("acct", key.StorageAccountName);
            Assert.AreEqual("tbd", key.TbdS3);
        }
    }
}
