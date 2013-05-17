// -----------------------------------------------------------------------
// <copyright file="AzureBlobStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
