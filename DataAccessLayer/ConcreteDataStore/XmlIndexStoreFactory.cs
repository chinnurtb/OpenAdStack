// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlIndexStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Singleton implementation for default index store. The type of the store
    /// is determined by the connection string.
    /// </summary>
    internal class XmlIndexStoreFactory : IIndexStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="XmlIndexStoreFactory"/> class.</summary>
        /// <param name="indexStoreConnectionString">
        /// The index store connection string may be a file, database, or other storage connection string).
        /// </param>
        public XmlIndexStoreFactory(string indexStoreConnectionString)
        {
            this.IndexStoreConnectionString = indexStoreConnectionString;
        }

        /// <summary>
        /// Gets or sets IndexStoreConnectionString.
        /// </summary>
        private string IndexStoreConnectionString { get; set; }

        /// <summary>Get the one and only index store object.</summary>
        /// <returns>The index store object.</returns>
        public IIndexStore GetIndexStore()
        {
            return XmlIndexDataStore.GetInstance(this.IndexStoreConnectionString);
        }
    }
}
