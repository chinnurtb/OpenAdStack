// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureSerializationEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
        internal AzureSerializationEntity(IRawEntity entity)
        {
            this.WrappedEntity = entity;
        }

        /// <summary>Gets or sets PartitionKey.</summary>
        public string PartitionKey { get; set; }

        /// <summary>Gets or sets RowKey.</summary>
        public string RowKey { get; set; }

        /// <summary>Gets wrapped entity.</summary>
        internal IRawEntity WrappedEntity { get; private set; }
    }
}
