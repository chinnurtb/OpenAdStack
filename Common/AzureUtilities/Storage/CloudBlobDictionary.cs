//-----------------------------------------------------------------------
// <copyright file="CloudBlobDictionary.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Utilities.Storage;

namespace AzureUtilities.Storage
{
    /// <summary>Generic persistent collection of <typeparamref name="TValue"/> using Azure CloudBlobs</summary>
    /// <typeparam name="TValue">Value type. Must be a valid data contract</typeparam>
    /// <remarks>
    /// <para>This IDictionary implementation exclusively stores its data in an Azure CloudBlobContainer.</para>
    /// <para>
    /// Users of this class must be mindful that every operation performed on this dictionary will incure
    /// Azure Storage transaction costs.
    /// </para>
    /// <para>
    /// This should only be used when dealing with values that must be persisted at all times in case of a system
    /// failure or situations where multiple role instances/threads need to access the same collection of values.
    /// </para>
    /// </remarks>
    public sealed class CloudBlobDictionary<TValue> :
        AbstractPersistentDictionary<TValue>,
        IPersistentDictionary<TValue>
    {
        /// <summary>Blob container used to persist the entries</summary>
        private readonly CloudBlobContainer Container;

        /// <summary>Initializes a new instance of the CloudBlobDictionary class.</summary>
        /// <param name="connectionString">Cloud storage account connection string</param>
        /// <param name="containerAddress">Blob container address</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        internal CloudBlobDictionary(string connectionString, string containerAddress, bool raw)
            : base(raw)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            this.Container = blobClient.GetContainerReference(containerAddress);
            this.Container.CreateIfNotExist();
        }

        /// <summary>Gets the name of the blob container used to store entries</summary>
        public override string StoreName
        {
            get { return this.Container.Name; }
        }

        /// <summary>Gets the number of elements</summary>
        public override int Count
        {
            get { return this.Container.ListBlobs().Count(); }
        }

        /// <summary>Gets the names of the value entries</summary>
        public override ICollection<string> Keys
        {
            get { return new List<string>(this.Container.ListBlobs().OfType<CloudBlob>().Select(b => b.Name)); }
        }

        /// <summary>Determines whether the dictionary contains an entry with the sepecified key</summary>
        /// <param name="key">The key of the value entry to locate</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null</exception>
        /// <returns>True if an entry with the key is found; otherwise, false.</returns>
        public override bool ContainsKey(string key)
        {
            var blob = this.Container.GetBlobReference(key);
            return blob.Exists();
        }

        /// <summary>Returns an enumerator that iterates through the entry value.</summary>
        /// <returns>An IEnumerator&lt;WorkItemScheduleEntry&gt; that can be used to iterate through the entry value.</returns>
        public override IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return this.Container
                .ListBlobs()
                .OfType<CloudBlob>()
                .Select(b => new CloudBlobDictionaryEntry(b))
                .Select(e =>
                    new KeyValuePair<string, TValue>(
                        e.Name,
                        (TValue)this.Serializer.ReadObject(new MemoryStream(e.ReadAllBytes()))))
                .GetEnumerator();
        }

        /// <summary>
        /// Gets the blob store entry for the specified key
        /// </summary>
        /// <param name="key">Key to get the blob for</param>
        /// <param name="mustExist">Whether the blob must already exist</param>
        /// <param name="checkETag">Whether to check that the cached ETag is current</param>
        /// <param name="updateETag">Whether to update the cached ETag</param>
        /// <returns>The blob store entry</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// <paramref name="mustExist"/> is true and no blob exists for the specified key
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="checkETag"/> is true and the ETag does not match the one cached
        /// </exception>
        protected override IPersistentDictionaryEntry GetEntry(string key, bool mustExist, bool checkETag, bool updateETag)
        {
            var blob = this.Container.GetBlobReference(key);
            try
            {
                blob.FetchAttributes();
                
                if (string.IsNullOrEmpty(blob.Properties.ETag))
                {
                    this.ETags.Remove(key);
                }

                if (checkETag && this.ETags.ContainsKey(key) && blob.Properties.ETag != this.ETags[key])
                {
                    throw new InvalidOperationException(
                        "The blob for '{0}' has been modified. Expected ETag: {1} Current ETag: {2}"
                        .FormatInvariant(key, this.ETags[key], blob.Properties.ETag));
                }

                if (updateETag)
                {
                    this.ETags[key] = blob.Properties.ETag;
                }
            }
            catch (StorageClientException sce)
            {
                if (sce.ErrorCode != StorageErrorCode.ResourceNotFound)
                {
                    throw new System.IO.IOException(
                        "Unable to get element '{0}' of '{1}'"
                        .FormatInvariant(key, this.StoreName),
                        sce);
                }

                if (mustExist)
                {
                    throw new KeyNotFoundException(key, sce);
                }
            }

            return new CloudBlobDictionaryEntry(blob);
        }

        /// <summary>Removes the entry with the specified key</summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>
        /// True if the entry is successfully removed; otherwise, false.
        /// Also returns false if the entry was not found.
        /// </returns>
        protected override bool RemoveEntry(string key)
        {
            var blob = this.Container.GetBlobReference(key);
            return blob.DeleteIfExists();
        }

        /// <summary>Removes all value entries</summary>
        /// <remarks>Deletes the container and recreates it</remarks>
        protected override void ClearEntries()
        {
            this.Container.Delete();
            this.Container.Create();
        }

        /// <summary>Deletes the blob container where the dictionary is stored</summary>
        protected override void DeleteStore()
        {
            this.Container.Delete();
        }
    }
}
