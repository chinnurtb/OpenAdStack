// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlIndexStoreFactory.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Index store object backed by Sql
    /// </summary>
    internal class SqlIndexStoreFactory : IIndexStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="SqlIndexStoreFactory"/> class.</summary>
        /// <param name="indexStoreConnectionString">Sql connection string.</param>
        public SqlIndexStoreFactory(string indexStoreConnectionString)
        {
            this.IndexStoreConnectionString = indexStoreConnectionString;
        }

        /// <summary>
        /// Gets or sets IndexStoreConnectionString.
        /// </summary>
        private string IndexStoreConnectionString { get; set; }

        /// <summary>Get an IIndexStore implementation based on Sql.</summary>
        /// <returns>The index store object.</returns>
        public IIndexStore GetIndexStore()
        {
            return new SqlIndexDataStore(new ConcreteSqlStore(this.IndexStoreConnectionString));
        }
    }
}
