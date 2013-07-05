// -----------------------------------------------------------------------
// <copyright file="AzureEntityStoreFactory.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Factory class for AzureEntityStore object.</summary>
    internal class AzureEntityStoreFactory : IEntityStoreFactory
    {
        /// <summary>Initializes a new instance of the <see cref="AzureEntityStoreFactory"/> class.</summary>
        /// <param name="entityConnectionString">The Azure table connection string.</param>
        public AzureEntityStoreFactory(string entityConnectionString)
        {
            this.AzureEntityTableConnectionString = entityConnectionString;
        }

        /// <summary>Gets or sets AzureEntityTableConnectionString.</summary>
        private string AzureEntityTableConnectionString { get; set; }

        /// <summary>Get the one and only entity store object.</summary>
        /// <returns>The entity store object.</returns>
        public IEntityStore GetEntityStore()
        {
            var entityStore = new AzureEntityDataStore(this.AzureEntityTableConnectionString);
            return entityStore;
        }
    }
}
