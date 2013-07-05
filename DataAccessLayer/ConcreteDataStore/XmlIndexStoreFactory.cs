// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlIndexStoreFactory.cs" company="Rare Crowds Inc">
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
    /// Singleton implementation for default index store. The type of the store
    /// is determined by the connection string.
    /// </summary>
    internal class XmlIndexStoreFactory : IIndexStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="XmlIndexStoreFactory"/> class.</summary>
        /// <param name="indexStoreConnectionString">
        /// The index store connection string may be a file, database, or other storage connection string).
        /// </param>
        public XmlIndexStoreFactory(string indexStoreConnectionString)
        {
            this.IndexStoreConnectionString = indexStoreConnectionString;
        }

        /// <summary>
        /// Gets or sets IndexStoreConnectionString.
        /// </summary>
        private string IndexStoreConnectionString { get; set; }

        /// <summary>Get the one and only index store object.</summary>
        /// <returns>The index store object.</returns>
        public IIndexStore GetIndexStore()
        {
            return XmlIndexDataStore.GetInstance(this.IndexStoreConnectionString);
        }
    }
}
