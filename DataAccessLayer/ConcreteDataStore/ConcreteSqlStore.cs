//-----------------------------------------------------------------------
// <copyright file="ConcreteSqlStore.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
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
                switch (e.Number)
                {
                    case 2627:
                        throw new DataAccessStaleEntityException("Could not save stale entity.", e);
                    case 50000:
                        throw new DataAccessException("Likely attempt to save non-sequential entity version.", e);
                    default:
                        throw new DataAccessException("Sql error", e);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new DataAccessException("Sql Connection Error.", e);
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