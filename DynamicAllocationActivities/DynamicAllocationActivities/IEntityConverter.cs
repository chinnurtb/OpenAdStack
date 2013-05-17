// -----------------------------------------------------------------------
// <copyright file="IEntityConverter.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>An interface implemented by an entity conversion object.</summary>
    public interface IEntityConverter
    {
        /// <summary>Convert an entity to the current schema for that entity.</summary>
        /// <param name="entityToConvert">The entity id to convert.</param>
        /// <param name="companyEntityId">The entity id of the company the entity belongs to.</param>
        void ConvertEntity(EntityId entityToConvert, EntityId companyEntityId);
    }
}