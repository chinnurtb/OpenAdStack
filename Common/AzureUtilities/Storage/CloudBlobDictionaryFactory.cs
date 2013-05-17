//-----------------------------------------------------------------------
// <copyright file="CloudBlobDictionaryFactory.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Utilities.Storage;

namespace AzureUtilities.Storage
{
    /// <summary>
    /// Factory for CloudBlobDictionaryFactory
    /// </summary>
    public class CloudBlobDictionaryFactory : IPersistentDictionaryFactory
    {
        /// <summary>Initializes a new instance of the CloudBlobDictionaryFactory class.</summary>
        /// <param name="connectionString">Connection string for the Azure storage account.</param>
        public CloudBlobDictionaryFactory(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>Gets the connection string for the Azure storage account.</summary>
        public string ConnectionString { get; private set; }

        /// <summary>Gets the type of dictionaries created</summary>
        public PersistentDictionaryType DictionaryType
        {
            get { return PersistentDictionaryType.Cloud; }
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        public IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, bool raw)
        {
            return new CloudBlobDictionary<TValue>(this.ConnectionString, storeName, raw);
        }

        /// <summary>Gets an index of all the stores</summary>
        /// <returns>List of store names</returns>
        public string[] GetStoreIndex()
        {
            var storageAccount = CloudStorageAccount.Parse(this.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient.ListContainers()
                .Select(c => c.Name)
                .ToArray();
        }
    }
}
