// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureBlobStorageKeyFactory.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using Diagnostics;
using Utilities.Serialization;

namespace ConcreteDataStore
{
    /// <summary>
    /// Blob storage key factory for Azure
    /// </summary>
    internal class AzureBlobStorageKeyFactory : IStorageKeyFactory
    {
        /// <summary>Build a new storage key for an entity that will be inserted to the entity store.</summary>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="companyExternalId">External Id of company associated with this entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildNewStorageKey(string storageAccountName, EntityId companyExternalId, IEntity rawEntity)
        {
            if ((string)rawEntity.EntityCategory != BlobPropertyEntity.CategoryName)
            {
                throw new DataAccessException("Cannot build blob storage key for non-BlobPropertyEntity");
            }

            // TODO: for now they all get the same container name
            return new AzureBlobStorageKey(storageAccountName, "entityblobassociations", new EntityId());
        }

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        public IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IEntity rawEntity)
        {
            throw new NotImplementedException();
        }

        /// <summary>Build a storage key from the serialized representation.</summary>
        /// <param name="serializedKey">The serialized key.</param>
        /// <returns>The storage key.</returns>
        public IStorageKey DeserializeKey(string serializedKey)
        {
            if (serializedKey == null)
            {
                var msg = "AzureStorageBlobKey is null.";
                LogManager.Log(LogLevels.Error, msg);
                throw new DataAccessException(msg);
            }

            var keyFields = AppsJsonSerializer.DeserializeObject<AzureBlobStorageKeySerialized>(serializedKey);
            var key = new AzureBlobStorageKey(
                keyFields.StorageAccountName, 
                keyFields.ContainerName,
                keyFields.BlobId,
                keyFields.LocalVersion,
                keyFields.VersionTimestamp);

            if (key.BlobId == null || key.ContainerName == null || key.StorageAccountName == null)
            {
                var msg = "Serialized value is not an AzureStorageBlobKey.";
                LogManager.Log(LogLevels.Error, msg);
                throw new DataAccessException(msg);
            }

            return key;
        }

        /// <summary>Build a serialized representation of the key.</summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A string serialized form of the blob.</returns>
        public string SerializeBlobKey(IStorageKey key)
        {
            var blobKey = key as AzureBlobStorageKey;
            
            if (blobKey == null)
            {
                var msg = "Key is null or not an AzureStorageBlobKey.";
                LogManager.Log(LogLevels.Error, msg);
                throw new DataAccessException(msg);
            }

            var keyFields = new AzureBlobStorageKeySerialized
                {
                    ContainerName = blobKey.ContainerName,
                    BlobId = blobKey.BlobId,
                    LocalVersion = blobKey.LocalVersion,
                    StorageAccountName = blobKey.StorageAccountName,
                    VersionTimestamp = blobKey.VersionTimestamp
                };

            return AppsJsonSerializer.SerializeObject(keyFields);
        }

        /// <summary>
        /// Json Serialization class for blob storage key
        /// </summary>
        private class AzureBlobStorageKeySerialized
        {
            /// <summary>Gets or sets ContainerName.</summary>
            public string ContainerName { get; set; }

            /// <summary>Gets or sets BlobId.</summary>
            public string BlobId { get; set; }

            /// <summary>Gets or sets StorageAccountName (e.g. - account).</summary>
            public string StorageAccountName { get; set; }

            /// <summary>Gets or sets Version Timestamp.</summary>
            public DateTime? VersionTimestamp { get; set; }

            /// <summary>Gets or sets LocalVersion.</summary>
            public int LocalVersion { get; set; }
        }
    }
}