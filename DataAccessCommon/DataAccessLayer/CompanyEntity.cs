// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompanyEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Wrapped Entity with validating members for Company.
    /// No Default construction - use Enity or JSON string
    /// to initialize.
    /// </summary>
    public class CompanyEntity : EntityWrapperBase
    {
        /// <summary>Category Name for Company Entities.</summary>
        public const string CompanyEntityCategory = "Company";
        
        /// <summary>Initializes a new instance of the <see cref="CompanyEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public CompanyEntity(EntityId externalEntityId, IRawEntity rawEntity)
        {
            this.Initialize(externalEntityId, CompanyEntityCategory, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="CompanyEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public CompanyEntity(IRawEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void ValidateEntityType(IRawEntity entity)
        {
            // TODO: Determine appropriate type validation for campaign
            ThrowIfCategoryMismatch(entity, CompanyEntityCategory);
        }
    }
}
