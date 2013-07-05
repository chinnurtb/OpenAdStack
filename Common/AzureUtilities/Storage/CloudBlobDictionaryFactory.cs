//-----------------------------------------------------------------------
// <copyright file="CloudBlobDictionaryFactory.cs" company="Rare Crowds Inc">
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
