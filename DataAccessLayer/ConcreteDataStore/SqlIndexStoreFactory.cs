// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlIndexStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Index store object backed by Sql
    /// </summary>
    internal class SqlIndexStoreFactory : IIndexStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="SqlIndexStoreFactory"/> class.</summary>
        /// <param name="indexStoreConnectionString">Sql connection string.</param>
        public SqlIndexStoreFactory(string indexStoreConnectionString)
        {
            this.IndexStoreConnectionString = indexStoreConnectionString;
        }

        /// <summary>
        /// Gets or sets IndexStoreConnectionString.
        /// </summary>
        private string IndexStoreConnectionString { get; set; }

        /// <summary>Get an IIndexStore implementation based on Sql.</summary>
        /// <returns>The index store object.</returns>
        public IIndexStore GetIndexStore()
        {
            return new SqlIndexDataStore(new ConcreteSqlStore(this.IndexStoreConnectionString));
        }
    }
}
