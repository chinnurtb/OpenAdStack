// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartnerEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// An entity wrapper for partner-defined entities that do not well-known properties.
    /// </summary>
    public class PartnerEntity : EntityWrapperBase
    {
        /// <summary>Category Name for Partner Entities.</summary>
        public const string PartnerEntityCategory = "Partner";

        /// <summary>Initializes a new instance of the <see cref="PartnerEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public PartnerEntity(IRawEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Initializes a new instance of the <see cref="PartnerEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public PartnerEntity(EntityId externalEntityId, IRawEntity rawEntity)
        {
            this.Initialize(externalEntityId, PartnerEntityCategory, rawEntity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void ValidateEntityType(IRawEntity entity)
        {
            ThrowIfCategoryMismatch(entity, PartnerEntityCategory);
        }
    }
}
