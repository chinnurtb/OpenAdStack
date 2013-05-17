//-----------------------------------------------------------------------
// <copyright file="SqlDictionaryFactory.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Linq;
using Utilities.Data;
using Utilities.Storage;

namespace SqlUtilities.Storage
{
    /// <summary>Factory for SqlDictionary</summary>
    public class SqlDictionaryFactory : IPersistentDictionaryFactory
    {
        /// <summary>Initializes a new instance of the SqlDictionaryFactory class.</summary>
        /// <param name="connectionString">Connection string for the Azure storage account.</param>
        public SqlDictionaryFactory(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>Gets the connection string for the Azure storage account.</summary>
        public string ConnectionString { get; private set; }

        /// <summary>Gets the type of dictionaries created</summary>
        public PersistentDictionaryType DictionaryType
        {
            get { return PersistentDictionaryType.Sql; }
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        public IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, bool raw)
        {
            return new SqlDictionary<TValue>(this.ConnectionString, storeName, raw);
        }

        /// <summary>Gets the names of the stores</summary>
        /// <returns>The store names</returns>
        public string[] GetStoreIndex()
        {
            var resultRows = SqlDictionary<object>.ExecuteStoredProcedure(
                new SqlWrapper(this.ConnectionString),
                Constants.StoredProcedures.GetStoreNames);
            return resultRows.Select(row =>
                row[Constants.SqlResultValues.StoreName] as string)
                .ToArray();
        }
    }
}
