// -----------------------------------------------------------------------
// <copyright file="AzureBlobStoreFactory.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Factory class for AzureBlobStore object.</summary>
    internal class AzureBlobStoreFactory : IBlobStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="AzureBlobStoreFactory"/> class.</summary>
        /// <param name="entityBlobConnectionString">The Azure blob connection string.</param>
        public AzureBlobStoreFactory(string entityBlobConnectionString)
        {
            this.AzureEntityBlobConnectionString = entityBlobConnectionString;
        }

        /// <summary>Gets or sets AzureEntityBlobConnectionString.</summary>
        private string AzureEntityBlobConnectionString { get; set; }

        /// <summary>Get a blob store object.</summary>
        /// <returns>The a blob store object.</returns>
        public IBlobStore GetBlobStore()
        {
            return new AzureBlobStore(this.AzureEntityBlobConnectionString);
        }
    }
}
