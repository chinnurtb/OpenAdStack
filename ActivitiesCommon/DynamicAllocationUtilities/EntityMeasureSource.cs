//-----------------------------------------------------------------------
// <copyright file="EntityMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationUtilities
{
    /// <summary>Source for measures loaded from an entity</summary>
    public class EntityMeasureSource : JsonMeasureSource, IMeasureSource
    {
        /// <summary>Backing field for JsonMeasureSource.MeasureJson</summary>
        private string measureJson;

        /// <summary>Initializes a new instance of the EntityMeasureSource class.</summary>
        /// <remarks>Measures are read from the "MeasureMap" entity property</remarks>
        /// <param name="entity">Entity from which to read measures</param>
        /// <seealso cref="DynamicAllocationEntityProperties"/>
        /// <exception cref="ArgumentException">
        /// Thrown if the entity does not have a value for the MeasureMap property.
        /// </exception>
        public EntityMeasureSource(IEntity entity)
            : base("ENTITY:{0}".FormatInvariant(entity.ExternalEntityId))
        {
            var measureMapProperty = entity.TryGetPropertyValueByName(DynamicAllocationEntityProperties.MeasureMap);
            if (measureMapProperty == null)
            {
                var message =
                    "Unable to create measures from entity '{0}' ({1}). The property {2} is not set."
                    .FormatInvariant(
                        entity.ExternalName,
                        entity.ExternalEntityId,
                        DynamicAllocationEntityProperties.MeasureMap);
                throw new ArgumentException(message, "entity");
            }

            this.measureJson = (string)measureMapProperty;
        }

        /// <summary>Gets the measures JSON read from the entity.</summary>
        protected override string MeasureJson
        {
            get { return this.measureJson; }
        }
    }
}
