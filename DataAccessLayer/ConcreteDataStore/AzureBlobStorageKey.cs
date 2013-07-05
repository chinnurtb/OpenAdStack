// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureBlobStorageKey.cs" company="Rare Crowds Inc.">
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
using System.Collections.Generic;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Storage Key for blob reference</summary>
    internal class AzureBlobStorageKey : IStorageKey
    {
        /// <summary>Blob marker constant for key fields.</summary>
        public const string AzureBlobMarker = "**AzureBlob**";

        /// <summary>Field name for Azure Blob marker (only used to identify key fields collection as 
        /// belonging to an azure blob)</summary>
        public const string BlobMarkerFieldName = "AzureBlobMarker";

        /// <summary>Field name for Azure Blob Container</summary>
        public const string ContainerFieldName = "AzureBlobContainer";
        
        /// <summary>Field name for Azure Blob Id</summary>
        public const string BlobIdFieldName = "AzureBlobId";

        /// <summary>Initializes a new instance of the <see cref="AzureBlobStorageKey"/> class.</summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobId">The blob id.</param>
        public AzureBlobStorageKey(string accountId, string containerName, string blobId)
            : this(accountId, containerName, blobId, 0, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AzureBlobStorageKey"/> class.</summary>
        /// <param name="existingKey">An existing key to copy.</param>
        public AzureBlobStorageKey(AzureBlobStorageKey existingKey)
            : this(existingKey.StorageAccountName, existingKey.ContainerName, existingKey.BlobId, existingKey.LocalVersion, existingKey.VersionTimestamp)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AzureBlobStorageKey"/> class.</summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobId">The blob id.</param>
        /// <param name="localVersion">The entity version local to this storage account.</param>
        /// <param name="versionTimestamp">The version timestamp.</param>
        public AzureBlobStorageKey(
            string accountId, string containerName, string blobId, int localVersion, DateTime? versionTimestamp) 
        {
            this.StorageAccountName = accountId;
            this.ContainerName = containerName;
            this.BlobId = blobId;
            this.LocalVersion = localVersion;
            this.VersionTimestamp = versionTimestamp;
        }

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

        /// <summary>Gets a map of key field name/value pairs.</summary>
        public IDictionary<string, string> KeyFields
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { BlobMarkerFieldName, AzureBlobMarker },
                    { ContainerFieldName, this.ContainerName },
                    { BlobIdFieldName, this.BlobId },
                };
            }
        }

        /// <summary>Interface method to determine equality of keys.</summary>
        /// <param name="otherKey">The key to compare with this key.</param>
        /// <returns>True if the keys refer to the same storage entity.</returns>
        public bool IsEqual(IStorageKey otherKey)
        {
            var otherAzureKey = otherKey as AzureBlobStorageKey;
            return otherAzureKey != null
                && otherAzureKey.StorageAccountName == this.StorageAccountName
                && otherAzureKey.LocalVersion == this.LocalVersion
                && otherAzureKey.ContainerName == this.ContainerName
                && otherAzureKey.BlobId == this.BlobId;
        }
    }
}
