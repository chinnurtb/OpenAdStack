//-----------------------------------------------------------------------
// <copyright file="SqlWrapper.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Diagnostics;
using Microsoft.AppFabricCat.Samples.Azure.TransientFaultHandling.SqlAzure;

namespace Utilities.Data
{
    /// <summary>Wrapper for SQL interaction</summary>
    public class SqlWrapper : ISqlClient
    {
        /// <summary>
        /// Default CommandTimeout
        /// </summary>
        private const int DefaultCommandTimeout = 60;

        /// <summary>SQL Connection String</summary>
        private string connectionString;

        /// <summary>Initializes a new instance of the SqlWrapper class.</summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlWrapper(string connectionString)
        {
            this.CommandTimeout = DefaultCommandTimeout;
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Gets or sets the wait time before terminating attempts to execute a command
        /// and generating an error. Default is 60 seconds.
        /// </summary>
        public int CommandTimeout
        {
            get;
            set;
        }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <remarks>Allows explict control over sql types.</remarks>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generics are appropriate here.")]
        public IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            string commandName,
            params SqlParameter[] parameters)
        {
            return this.ExecuteStoredProcedure(this.CommandTimeout, commandName, parameters);
        }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <param name="commandTimeout">The wait time before terminating execution and returning an error.</param>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generics are appropriate here.")]
        public IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            int commandTimeout,
            string commandName,
            params SqlParameter[] parameters)
        {
            try
            {
                using (var connection = new SqlConnection(this.connectionString))
                {
                    var command = new SqlCommand
                    {
                        CommandType = CommandType.StoredProcedure,
                        Connection = connection,
                        CommandText = commandName,
                        CommandTimeout = commandTimeout
                    };
                    command.Parameters.AddRange(parameters);
                    
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        // Realize the reader as a List<Dictionary<string, object>>
                        var resultRows = new List<Dictionary<string, object>>();
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            resultRows.Add(row);
                        }

                        return resultRows as IEnumerable<IDictionary<string, object>>;
                    }
                }
            }
            catch (SqlException sqle)
            {
                var messageFormat =
                    "Error executing stored procedure '{0}':\n{1}" +
                    (sqle.Errors.Count > 0 ? "\nError(s):\n{2}" : string.Empty);
                LogManager.Log(
                    LogLevels.Trace,
                    messageFormat,
                    commandName,
                    sqle.Message,
                    string.Join("\n", sqle.Errors.OfType<SqlError>()));

                var throttling = ThrottlingCondition.FromException(sqle);
                if (throttling.ThrottlingMode != ThrottlingMode.NoThrottling)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "SQL Azure throttling encountered: {0}",
                        throttling);
                }

                throw;
            }
        }
    }
}
