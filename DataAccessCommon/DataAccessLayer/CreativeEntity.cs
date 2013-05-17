// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreativeEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Wrapped Entity with validating members for Creative.
    /// No Default construction - use Enity or JSON string
    /// to initialize.
    /// </summary>
    public class CreativeEntity : EntityWrapperBase
    {
        /// <summary>Category Name for Creative Entities.</summary>
        public const string CreativeEntityCategory = "Creative";

        /// <summary>Initializes a new instance of the <see cref="CreativeEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public CreativeEntity(EntityId externalEntityId, IRawEntity rawEntity)
        {
            this.Initialize(externalEntityId, CreativeEntityCategory, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="CreativeEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public CreativeEntity(IRawEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void ValidateEntityType(IRawEntity entity)
        {
            // TODO: Determine appropriate type validation for creative
            ThrowIfCategoryMismatch(entity, CreativeEntityCategory);
        }
    }
}
