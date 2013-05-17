//-----------------------------------------------------------------------
// <copyright file="SqlUserAccessStoreFactory.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Index store object backed by Sql
    /// </summary>
    internal class SqlUserAccessStoreFactory : IUserAccessStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="SqlUserAccessStoreFactory"/> class.</summary>
        /// <param name="connectionString">Sql connection string.</param>
        public SqlUserAccessStoreFactory(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>Gets the ConnectionString.</summary>
        internal string ConnectionString { get; private set; }

        /// <summary>Get an IUserAccessStore implementation based on Sql.</summary>
        /// <returns>The user access store object.</returns>
        public IUserAccessStore GetUserAccessStore()
        {
            return new SqlUserAccessDataStore(new ConcreteSqlStore(this.ConnectionString));
        }
    }
}
