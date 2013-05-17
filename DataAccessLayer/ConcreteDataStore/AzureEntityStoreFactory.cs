// -----------------------------------------------------------------------
// <copyright file="AzureEntityStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Factory class for AzureEntityStore object.</summary>
    internal class AzureEntityStoreFactory : IEntityStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="AzureEntityStoreFactory"/> class.</summary>
        /// <param name="entityConnectionString">The Azure table connection string.</param>
        public AzureEntityStoreFactory(string entityConnectionString)
        {
            this.AzureEntityTableConnectionString = entityConnectionString;
        }

        /// <summary>Gets or sets AzureEntityTableConnectionString.</summary>
        private string AzureEntityTableConnectionString { get; set; }

        /// <summary>Get the one and only entity store object.</summary>
        /// <returns>The entity store object.</returns>
        public IEntityStore GetEntityStore()
        {
            var entityStore = new AzureEntityDataStore(this.AzureEntityTableConnectionString);
            return entityStore;
        }
    }
}
