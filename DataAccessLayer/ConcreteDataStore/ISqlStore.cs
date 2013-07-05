//-----------------------------------------------------------------------
// <copyright file="ISqlStore.cs" company="Rare Crowds Inc.">
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