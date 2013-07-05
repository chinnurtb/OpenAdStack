//-----------------------------------------------------------------------
// <copyright file="ConcreteSqlStore.cs" company="Rare Crowds Inc.">
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
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DataAccessLayer;
using Diagnostics;

namespace ConcreteDataStore
{
    /// <summary>Helper methods for performing sql opertaions.</summary>
    internal sealed class ConcreteSqlStore : ISqlStore
    {
        /// <summary>Initializes a new instance of the <see cref="ConcreteSqlStore"/> class.</summary>
        /// <param name="connectionString">The connection string.</param>
        internal ConcreteSqlStore(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>Gets ConnectionString.</summary>
        public string ConnectionString { get; private set; }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        public QueryResult ExecuteStoredProcedure(string commandName, IList<SqlParameter> parameters)
        {
            var connection = new SqlConnection(this.ConnectionString);
            var command = new SqlCommand();
            command.Connection = connection;
            command.CommandText = commandName;
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters.ToArray());

            var results = new QueryResult();
            SqlDataReader reader = null;

            try
            {
                connection.Open();
                reader = command.ExecuteReader();

                // Realize the reader as a QueryResult (jagged table) that
                // the caller must know how to interpret. This will also 'realize'
                // any SqlExceptions that may have occured for the entire result set.
                do
                {
                    var recordSetIndex = results.AddRecordSet();
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                        }

                        results.AddRecord(new QueryRecord(row), recordSetIndex);
                    }
                }
                while (reader.NextResult());
            }
            catch (SqlException e)
            {
                Exception rethrowException;
                string msg;
                switch (e.Number)
                {
                    case 2627:
                        msg = "Could not save stale entity. {0}".FormatInvariant(e.ToString());
                        rethrowException = new DataAccessStaleEntityException(msg, e);
                        break;
                    case 50000:
                        msg = "Likely attempt to save non-sequential entity version. {0}".FormatInvariant(e.ToString());
                        rethrowException = new DataAccessException(msg, e);
                        break;
                    default:
                        msg = "Sql error, number {0} in DataAccessLayer. {1}".FormatInvariant(e.Number, e.ToString());
                        rethrowException = new DataAccessException(msg, e);
                        break;
                }

                LogManager.Log(LogLevels.Trace, msg);
                throw rethrowException;
            }
            catch (InvalidOperationException e)
            {
                var msg = "Sql Connection Error in DataAccessLayer. {0}".FormatInvariant(e.ToString());
                LogManager.Log(LogLevels.Trace, msg);
                throw new DataAccessException(msg, e);
            }
            finally
            {
                // Make sure we close the reader and connection
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }

                connection.Close();
            }

            return results;
        }

        /// <summary>Execute a stored procedure with exception handling.</summary>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>Returns null if exception thrown.</returns>
        public QueryResult TryExecuteStoredProcedure(string commandName, IList<SqlParameter> parameters)
        {
            QueryResult resultRows = null;

            try
            {
                resultRows = this.ExecuteStoredProcedure(commandName, parameters);
            }
            catch (DataAccessException e)
            {
                LogManager.Log(LogLevels.Error, e.ToString());
            }

            return resultRows;
        }
    }
}