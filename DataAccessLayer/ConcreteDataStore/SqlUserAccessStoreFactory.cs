//-----------------------------------------------------------------------
// <copyright file="SqlUserAccessStoreFactory.cs" company="Rare Crowds Inc.">
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
