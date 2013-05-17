// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlobPropertyEntity.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// An entity wrapper for blob property references.
    /// </summary>
    internal class BlobPropertyEntity : EntityWrapperBase
    {
        /// <summary>Property Name for BlobPropertyType property.</summary>
        internal const string BlobPropertyTypeName = "BlobPropertyType";

        /// <summary>Category Name for Blob Property Entities.</summary>
        internal const string BlobPropertyEntityCategory = "BlobReference";

        /// <summary>Property Name for BlobBytes property.</summary>
        internal const string BlobBytesPropertyName = "BlobBytes";

        /// <summary>Initializes a new instance of the <see cref="BlobPropertyEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        internal BlobPropertyEntity(IRawEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Initializes a new instance of the <see cref="BlobPropertyEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        internal BlobPropertyEntity(EntityId externalEntityId)
            : this(externalEntityId, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BlobPropertyEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="serializedObjToBlob">The serialized object to persist to a blob.</param>
        internal BlobPropertyEntity(EntityId externalEntityId, byte[] serializedObjToBlob) 
            : this(externalEntityId, serializedObjToBlob, string.Empty)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BlobPropertyEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="serializedObjToBlob">The serialized object to persist to a blob.</param>
        /// <param name="propertyType">A PropertyType for the bytes.</param>
        internal BlobPropertyEntity(EntityId externalEntityId, byte[] serializedObjToBlob, PropertyType propertyType)
            : this(externalEntityId, serializedObjToBlob, Enum.GetName(typeof(PropertyType), propertyType))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BlobPropertyEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="serializedObjToBlob">The serialized object to persist to a blob.</param>
        /// <param name="propertyType">A type string for the bytes.</param>
        internal BlobPropertyEntity(EntityId externalEntityId, byte[] serializedObjToBlob, string propertyType)
        {
            // Construct the underlying wrapped entity
            var wrappedEntity = new Entity
            {
                ExternalEntityId = externalEntityId,
                EntityCategory = BlobPropertyEntityCategory,
            };

            this.Initialize(wrappedEntity);

            this.BlobPropertyType = propertyType;

            // Set the blob bytes on the entity if provided
            if (serializedObjToBlob != null)
            {
                this.BlobBytes = serializedObjToBlob;
            }
        }

        /// <summary>Gets or sets BlobBytes. Property passed on set can be un-named and name will be set.</summary>
        internal EntityProperty BlobBytes
        {
            get { return TryGetEntityPropertyByName(BlobBytesPropertyName, null); }
            set { SetEntityProperty(new EntityProperty(BlobBytesPropertyName, value.Value)); }
        }

        /// <summary>Gets or sets Blob property type.</summary>
        internal EntityProperty BlobPropertyType
        {
            get { return TryGetEntityPropertyByName(BlobPropertyTypeName, null); }
            set { SetEntityProperty(new EntityProperty(BlobPropertyTypeName, value.Value)); }
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void ValidateEntityType(IRawEntity entity)
        {
            ThrowIfCategoryMismatch(entity, BlobPropertyEntityCategory);
        }
    }
}
