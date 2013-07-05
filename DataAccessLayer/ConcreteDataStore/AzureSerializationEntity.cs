// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureSerializationEntity.cs" company="Rare Crowds Inc">
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

using System.Data.Services.Common;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Do not try to reference this class - it is for internal use only.
    /// Azure needs a "conforming" class for the client interface to work. The class must
    /// include PartitionKey and RowKey properties and must not contain anything that
    /// the storage client might try to serialize. This is a somewhat unfortunate
    /// limitation caused by the fact that the WritingEntity event is a last-chance
    /// rather than a first-chance handler and there does not currently seem to be
    /// a way to override this.
    /// </summary>
    [DataServiceKey(new[] { "PartitionKey", "RowKey" })]
    internal class AzureSerializationEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSerializationEntity"/> class.
        /// </summary>
        public AzureSerializationEntity()
        {
            this.WrappedEntity = new Entity();
        }

        /// <summary>Initializes a new instance of the <see cref="AzureSerializationEntity"/> class.</summary>
        /// <param name="entity">The entity.</param>
        internal AzureSerializationEntity(IEntity entity)
        {
            this.WrappedEntity = entity;
        }

        /// <summary>Gets or sets PartitionKey.</summary>
        public string PartitionKey { get; set; }

        /// <summary>Gets or sets RowKey.</summary>
        public string RowKey { get; set; }

        /// <summary>Gets wrapped entity.</summary>
        internal IEntity WrappedEntity { get; private set; }
    }
}
