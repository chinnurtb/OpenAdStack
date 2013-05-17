// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureBlobStorageKeyFactory.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
        public IStorageKey BuildNewStorageKey(string storageAccountName, EntityId companyExternalId, IRawEntity rawEntity)
        {
            if ((string)rawEntity.EntityCategory != BlobPropertyEntity.BlobPropertyEntityCategory)
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
        public IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IRawEntity rawEntity)
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