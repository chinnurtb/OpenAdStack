//-----------------------------------------------------------------------
// <copyright file="SqlDictionary.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Diagnostics;
using Utilities.Data;
using Utilities.Storage;

namespace SqlUtilities.Storage
{
    /// <summary>Generic persistent collection of <typeparamref name="TValue"/> using SQL</summary>
    /// <typeparam name="TValue">Value type. Must be a valid data contract</typeparam>
    /// <remarks>
    /// <para>This IDictionary implementation exclusively stores its data in an Azure CloudBlobContainer.</para>
    /// <para>
    /// Users of this class must be mindful that in Azure SQL is billed by storage size. This should not
    /// be used for storing data sets that will grow unbounded.
    /// </para>
    /// <para>
    /// This should only be used when dealing with values that must be persisted at all times in case of a system
    /// failure or situations where multiple role instances/threads need to access the same collection of values.
    /// </para>
    /// </remarks>
    public sealed class SqlDictionary<TValue> :
        AbstractPersistentDictionary<TValue>,
        IPersistentDictionary<TValue>
    {
        /// <summary>SqlException.Number for timeout expirations</summary>
        private const int SqlErrorNumberTimeoutExpired = -2;

        /// <summary>Maximum timeout for command execution</summary>
        /// <remarks>This is the longest time to attempt during retries</remarks>
        private const int MaximumCommandTimout = 300;

        /// <summary>
        /// How many times to retry executing stored procedures
        /// </summary>
        private const int SqlErrorRetries = 5;

        /// <summary>
        /// How long to wait between retries executing stored procedures
        /// </summary>
        private const int SqlErrorRetryWait = 3000;

        /// <summary>Empty ETag value</summary>
        private static readonly string EmptyETag = Guid.Empty.ToString("D");

        /// <summary>SQL client</summary>
        private readonly ISqlClient sqlClient;

        /// <summary>
        /// Name under which entries to this dictionary are stored
        /// </summary>
        private readonly string storeName;

        /// <summary>Initializes a new instance of the SqlDictionary class.</summary>
        /// <param name="connectionString">SQL connection string</param>
        /// <param name="storeName">Name to store the dictionary as</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        internal SqlDictionary(string connectionString, string storeName, bool raw)
            : this(new SqlWrapper(connectionString), storeName, raw)
        {
        }

        /// <summary>Initializes a new instance of the SqlDictionary class.</summary>
        /// <param name="sqlClient">SQL client</param>
        /// <param name="storeName">Name to store the dictionary as</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        internal SqlDictionary(ISqlClient sqlClient, string storeName, bool raw)
            : base(raw)
        {
            this.sqlClient = sqlClient;
            this.storeName = storeName;
        }

        /// <summary>Gets the name that entries are stored as</summary>
        public override string StoreName
        {
            get { return this.storeName; }
        }

        /// <summary>Gets the number of elements</summary>
        public override int Count
        {
            get
            {
                // TODO: Replace with an SP that gets the number of 
                // entries for this store, or is this good enough?
                return this.Keys.Count;
            }
        }

        /// <summary>Gets the names of the value entries</summary>
        public override ICollection<string> Keys
        {
            get
            {
                var resultRows = this.ExecuteStoredProcedure(
                    Constants.StoredProcedures.GetEntryNames,
                    this.StoreNameSqlParameter);
                return new List<string>(
                    resultRows.Select(row =>
                        (string)row[Constants.SqlResultValues.EntryName]));
            }
        }

        /// <summary>Gets a SQL parameter containing the store name</summary>
        private SqlParameter StoreNameSqlParameter
        {
            get
            {
                return new SqlParameter(Constants.SqlParameterNames.StoreName, SqlDbType.NVarChar)
                {
                    Value = this.StoreName
                };
            }
        }

        /// <summary>Determines whether the dictionary contains an entry with the sepecified key</summary>
        /// <param name="key">The key of the value entry to locate</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null</exception>
        /// <returns>True if an entry with the key is found; otherwise, false.</returns>
        public override bool ContainsKey(string key)
        {
            // TODO: Replace with a specialized SP that checks if there is a matching entry?
            return this.Keys.Contains(key);
        }

        /// <summary>Returns an enumerator that iterates through the entry value.</summary>
        /// <returns>An IEnumerator&lt;WorkItemScheduleEntry&gt; that can be used to iterate through the entry value.</returns>
        public override IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return new SqlDictionaryEnumerator<TValue>(this);
        }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <remarks>
        /// Allows explict control over sql types.
        /// Retries failed requests (SqlDictionary stored procedures are idempotent
        /// so no filtering for specific "retriable" errors is required).
        /// </remarks>
        /// <param name="sqlClient">The ISqlClient used to execute the stored procedure.</param>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        internal static IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            ISqlClient sqlClient,
            string commandName,
            params SqlParameter[] parameters)
        {
            var commandTimeout = sqlClient.CommandTimeout;
            var retries = 0;
            while (true)
            {
                try
                {
                    // Copy the Sql Parameters
                    var sqlparams = parameters.Select(
                        p => new SqlParameter
                        {
                            ParameterName = p.ParameterName,
                            SqlDbType = p.SqlDbType,
                            Value = p.Value
                        })
                        .ToArray();

                    // Execute the stored procedure
                    return sqlClient.ExecuteStoredProcedure(
                        commandTimeout,
                        commandName,
                        sqlparams);
                }
                catch (SqlException sqle)
                {
                    // Do not retry 50000 errors
                    if (sqle.Errors.Count > 0 &&
                        sqle.Errors[0].Number == 50000)
                    {
                        throw;
                    }

                    if (++retries > SqlErrorRetries)
                    {
                        throw;
                    }

                    var parameterStrings = parameters.Select(p =>
                        "{0}: ({1}) {2}".FormatInvariant(p.ParameterName, p.TypeName, p.Value));
                    LogManager.Log(
                        LogLevels.Trace,
                        "Error executing stored procedure {0}({1}) attempt {2}/{3}\n{4}",
                        commandName,
                        string.Join(", ", parameterStrings),
                        retries,
                        SqlErrorRetries,
                        sqle);

                    // If error was a timeout, increase up to maximum
                    if (sqle.Number == SqlErrorNumberTimeoutExpired &&
                        commandTimeout < MaximumCommandTimout)
                    {
                        commandTimeout *= 2;
                    }

                    Thread.Sleep(SqlErrorRetryWait);
                }
            }
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
            var exists = this.ContainsKey(key);

            if (mustExist && !exists)
            {
                throw new KeyNotFoundException(key);
            }

            var entry = new SqlDictionaryEntry(this.sqlClient, this.StoreName, key);

            if (entry.ETag == EmptyETag)
            {
                this.ETags.Remove(key);
            }

            if (checkETag &&
                this.ETags.ContainsKey(key) &&
                entry.ETag != this.ETags[key])
            {
                throw new InvalidETagException(this.StoreName, key, this.ETags[key]);
            }

            if (updateETag)
            {
                this.ETags[key] = entry.ETag;
            }

            return entry;
        }

        /// <summary>Removes the entry with the specified key</summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>
        /// True if the entry is successfully removed; otherwise, false.
        /// Also returns false if the entry was not found.
        /// </returns>
        protected override bool RemoveEntry(string key)
        {
            var entryNameParameter = new SqlParameter(
                Constants.SqlParameterNames.EntryName,
                SqlDbType.NVarChar)
            {
                Value = key
            };
            var resultRows = this.ExecuteStoredProcedure(
                "DeleteEntry",
                this.StoreNameSqlParameter,
                entryNameParameter);
            var result = resultRows.SingleOrDefault();

            // Result is the count of rows deleted
            // TODO: What will the result really be?
            return result != null;
        }

        /// <summary>Removes all value entries</summary>
        /// <remarks>Calls SqlDictionary.Delete</remarks>
        protected override void ClearEntries()
        {
            this.DeleteStore();
        }

        /// <summary>Deletes all entries for the dictionary</summary>
        protected override void DeleteStore()
        {
            this.ExecuteStoredProcedure(
                Constants.StoredProcedures.DeleteEntries,
                this.StoreNameSqlParameter);
        }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <remarks>
        /// Allows explict control over sql types.
        /// Retries failed requests (SqlDictionary stored procedures are idempotent
        /// so no filtering for specific "retriable" errors is required).
        /// </remarks>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        private IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            string commandName,
            params SqlParameter[] parameters)
        {
            return ExecuteStoredProcedure(
                this.sqlClient,
                commandName,
                parameters);
        }
    }
}
