//-----------------------------------------------------------------------
// <copyright file="ISqlStore.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlClient;

namespace ConcreteDataStore
{
    /// <summary>Interface for low-level sql accessors.</summary>
    public interface ISqlStore
    {
        /// <summary>Gets ConnectionString.</summary>
        string ConnectionString { get; }

        /// <summary>Execute a stored procedure and return the result set as a collection.</summary>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>A list of rows with a dictionary of name/value pairs for each row.</returns>
        QueryResult ExecuteStoredProcedure(string commandName, IList<SqlParameter> parameters);

        /// <summary>Execute a stored procedure with exception handling.</summary>
        /// <param name="commandName">The stored procedure name.</param>
        /// <param name="parameters">The sql parameters.</param>
        /// <returns>Returns null if exception thrown.</returns>
        QueryResult TryExecuteStoredProcedure(string commandName, IList<SqlParameter> parameters);
    }
}