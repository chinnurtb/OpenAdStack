// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureBlobStore.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.IO;
using DataAccessLayer;
using Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace ConcreteDataStore
{
    /// <summary>Blob data store for Azure Blob.</summary>
    internal class AzureBlobStore : IBlobStore
    {
        /// <summary>
        /// Name of the metadata value indicating property type
        /// of the blob's content.
        /// </summary>
        internal const string PropertyTypeMetadataValue = "PROPERTYTYPE";

        /// <summary>
        /// Name of the metadata value indicating external entity id
        /// of the blob entity.
        /// </summary>
        internal const string ExternalEntityIdMetadataValue = "EXTERNALENTITYID";

        /// <summary>
        /// Name of the metadata value indicating whether the
        /// blob's content is compressed.
        /// </summary>
        internal const string CompressedMetadataValue = "COMPRESSED";

        /// <summary>
        /// Name of the metadata value specifying the company
        /// for storage auditing purposes.
        /// </summary>
        internal const string CompanyMetadataValue = "COMPANY";

        /// <summary>Initializes a new instance of the AzureBlobStore class.</summary>
        /// <param name="connectionString">Cloud storage account connection string</param>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        public AzureBlobStore(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Specify a retry backoff of 10 seconds max instead of using default values.
            blobClient.RetryPolicy = RetryPolicies.RetryExponential(
                3, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 3));
            this.BlobClient = blobClient;
        }

        /// <summary>Gets or sets Blob container used to persist the entries</summary>
        public CloudBlobClient BlobClient { get; set; }

        /// <summary>Get a blob entity given a storage key.</summary>
        /// <param name="key">An IStorageKey key.</param>
        /// <returns>A blob entity that is not deserialized.</returns>
        public IEntity GetBlobByKey(IStorageKey key)
        {
            var blobKey = key as AzureBlobStorageKey;
            if (blobKey == null)
            {
                throw new DataAccessException("Invalid blob storage key.");
            }

            // Build a raw entity
            var entity = new Entity();
            entity.EntityCategory = BlobPropertyEntity.CategoryName;
            entity.Key = key;
            var blobEntity = new BlobPropertyEntity(entity);

            try
            {
                // Get the blob data
                var container = this.BlobClient.GetContainerReference(blobKey.ContainerName);
                var blob = container.GetBlobReference(blobKey.BlobId);
                ReadAllBytes(blob, ref blobEntity);
            }
            catch (Exception e)
            {
                var msg = "Unable to read bytes for blob Id (not entity Id) '{0}'"
                    .FormatInvariant(blobKey.BlobId);
                LogManager.Log(LogLevels.Error, msg);
                throw new DataAccessException(msg, e);
            }

            return blobEntity;
        }

        /// <summary>Save an entity in the entity store.</summary>
        /// <param name="rawEntity">The raw entity.</param>
        /// <param name="company">The company (for storage auditing).</param>
        public void SaveBlob(IEntity rawEntity, string company)
        {
            var blobEntity = new BlobPropertyEntity(rawEntity);
            var blobKey = rawEntity.Key as AzureBlobStorageKey;
            if (blobKey == null)
            {
                throw new DataAccessException("Invalid blob storage key.");
            }

            try
            {
                var container = this.BlobClient.GetContainerReference(blobKey.ContainerName);
                container.CreateIfNotExist();
                var blob = container.GetBlobReference(blobKey.BlobId);
                WriteAllBytes(blob, blobEntity, company);
            }
            catch (Exception e)
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Unable to write bytes for blob entity Id '{0}' with blob Id '{1}'",
                    (EntityId)rawEntity.ExternalEntityId,
                    blobKey.BlobId);
                LogManager.Log(LogLevels.Error, msg);
                throw new DataAccessException(msg, e);
            }

            rawEntity.CreateDate = DateTime.UtcNow;
            rawEntity.LastModifiedDate = rawEntity.CreateDate;
            rawEntity.LocalVersion = 0;
        }

        /// <summary>Gets a storage key factory for this blob store.</summary>
        /// <returns>An IStorageKeyFactory</returns>
        public IStorageKeyFactory GetStorageKeyFactory()
        {
            return new AzureBlobStorageKeyFactory();
        }

        /// <summary>Writes the content to the entry</summary>
        /// <param name="blob">The blob object.</param>
        /// <param name="blobEntity">The blob entity.</param>
        /// <param name="company">The company (for storage auditing).</param>
        private static void WriteAllBytes(CloudBlob blob, BlobPropertyEntity blobEntity, string company)
        {
            var content = (byte[])blobEntity.BlobBytes.Value;
            var propertyType = (string)blobEntity.BlobPropertyType;
            var externalEntityId = blobEntity.ExternalEntityId.Value.ToString();

            // Always compress
            blob.UploadByteArray(content.Deflate());
            blob.Metadata[CompressedMetadataValue] = true.ToString(CultureInfo.InvariantCulture);
            blob.Metadata[CompanyMetadataValue] = !string.IsNullOrWhiteSpace(company) ? company : "UNKNOWN";
            blob.Metadata[PropertyTypeMetadataValue] = !string.IsNullOrWhiteSpace(propertyType) ? propertyType : "UNKNOWN";
            blob.Metadata[ExternalEntityIdMetadataValue] = !string.IsNullOrWhiteSpace(externalEntityId) ? externalEntityId : "UNKNOWN";
            blob.SetMetadata();
        }

        /// <summary>Reads the content from the entry</summary>
        /// <param name="blob">The blob object.</param>
        /// <param name="blobEntity">The blob entity to populate.</param>
        private static void ReadAllBytes(CloudBlob blob, ref BlobPropertyEntity blobEntity)
        {
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStream(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var bytes = stream.ToArray();

                blobEntity.BlobPropertyType = blob.Metadata[PropertyTypeMetadataValue];

                // Determine if it has been compressed (for backward compatibility)
                var compressedMetadataField = blob.Metadata[CompressedMetadataValue];
                var compressed = !string.IsNullOrEmpty(compressedMetadataField) && bool.Parse(compressedMetadataField);

                blobEntity.BlobBytes = compressed ? bytes.Inflate() : bytes;
            }
        }
    }
}
