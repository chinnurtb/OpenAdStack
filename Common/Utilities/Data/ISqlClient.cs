//-----------------------------------------------------------------------
// <copyright file="ISqlClient.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
