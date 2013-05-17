// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityDeserializationFilter.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;

namespace EntityUtilities
{
    /// <summary>Implementation of IEntityFilter for entity deserialization.</summary>
    public class EntityDeserializationFilter : IEntityFilter
    {
        /// <summary>Initializes a new instance of the <see cref="EntityDeserializationFilter"/> class.</summary>
        /// <remarks>By default we include system properties but not extended properties or associations.</remarks>
        public EntityDeserializationFilter()
            : this(true, false, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="EntityDeserializationFilter"/> class.</summary>
        /// <param name="includeSystemProperties">True to deserialize system properties.</param>
        /// <param name="includeExtendedProperties">True to deserialize extended properties.</param>
        /// <param name="includeAssociations">True to deserialize associations.</param>
        public EntityDeserializationFilter(bool includeSystemProperties, bool includeExtendedProperties, bool includeAssociations)
        {
            // Default query string
            this.EntityQueries = new EntityActivityQuery();
            
            // Always include default properties
            this.Filters = new List<PropertyFilter> { PropertyFilter.Default };

            if (includeSystemProperties)
            {
                this.Filters.Add(PropertyFilter.System);
            }

            if (includeExtendedProperties)
            {
                this.Filters.Add(PropertyFilter.Extended);
            }

            this.IncludeAssociations = includeAssociations;
        }

        /// <summary>Gets a value indicating whether default properties are filtered.</summary>
        public bool IncludeDefaultProperties
        {
            get { return this.ContainsFilter(PropertyFilter.Default); }
        }

        /// <summary>Gets a value indicating whether system properties are filtered.</summary>
        public bool IncludeSystemProperties
        {
            get { return this.ContainsFilter(PropertyFilter.System); }
        }

        /// <summary>Gets a value indicating whether extended properties are filtered.</summary>
        public bool IncludeExtendedProperties
        {
            get { return this.ContainsFilter(PropertyFilter.Extended); }
        }

        /// <summary>Gets a value indicating whether associations are filtered.</summary>
        public bool IncludeAssociations { get; private set; }

        /// <summary>Gets the property filters to include.</summary>
        public IList<PropertyFilter> Filters { get; private set; }

        /// <summary>Gets ad-hoc query values used when filtering an entity.</summary>
        public IEntityQuery EntityQueries { get; private set; }

        /// <summary>Check if EntityFilter contains a given property filter.</summary>
        /// <param name="propertyFilter">The PropertyFilter to check.</param>
        /// <returns>True if the property filter is contained in the Filters collection.</returns>
        public bool ContainsFilter(PropertyFilter propertyFilter)
        {
            return this.Filters.Contains(propertyFilter);
        }

        /// <summary>Clone this instance of IEntityFilter.</summary>
        /// <returns>A cloned instance of this IEntityFilter.</returns>
        public IEntityFilter Clone()
        {
            var clonedFilter = new EntityDeserializationFilter(
                this.IncludeSystemProperties,
                this.IncludeExtendedProperties,
                this.IncludeAssociations);
            clonedFilter.EntityQueries = this.EntityQueries.Clone();

            return clonedFilter;
        }
    }
}
