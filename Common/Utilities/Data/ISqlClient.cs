//-----------------------------------------------------------------------
// <copyright file="ISqlClient.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace Utilities.Data
{
    /// <summary>Interface for SQL interaction</summary>
    public interface ISqlClient
    {
        /// <summary>
        /// Gets or sets the wait time before terminating attempts to execute a command
        /// and generating an error.
        /// </summary>
        int CommandTimeout { get; set; }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <remarks>Allows explict control over sql types.</remarks>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting generic IDictionary within IEnumerable is appropriate here")]
        IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            string commandName,
            params SqlParameter[] parameters);

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <remarks>Allows explict control over sql types.</remarks>
        /// <param name="commandTimeout">The wait time before terminating execution and returning an error.</param>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting generic IDictionary within IEnumerable is appropriate here")]
        IEnumerable<IDictionary<string, object>> ExecuteStoredProcedure(
            int commandTimeout,
            string commandName,
            params SqlParameter[] parameters);
    }
}
