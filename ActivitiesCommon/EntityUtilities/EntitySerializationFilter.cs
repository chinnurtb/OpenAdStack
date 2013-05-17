// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntitySerializationFilter.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;

namespace EntityUtilities
{
    /// <summary>Implementation of IEntityFilter for entity serialization.</summary>
    public class EntitySerializationFilter : IEntityFilter
    {
        /// <summary>Initializes a new instance of the <see cref="EntitySerializationFilter"/> class.</summary>
        /// <param name="entityQueries">The entity queries.</param>
        public EntitySerializationFilter(IEntityQuery entityQueries)
        {
            this.EntityQueries = entityQueries ?? new EntityActivityQuery(null);

            // Set the filter properties based on the EntityQueries
            // Always include default properties
            this.Filters = new List<PropertyFilter> { PropertyFilter.Default };

            if (this.EntityQueries.ContainsFlag("WithSystemProperties"))
            {
                this.Filters.Add(PropertyFilter.System);
            }

            if (this.EntityQueries.ContainsFlag("WithExtendedProperties"))
            {
                this.Filters.Add(PropertyFilter.Extended);
            }

            this.IncludeAssociations = this.EntityQueries.ContainsFlag("WithAssociations");
        }

        /// <summary>Initializes a new instance of the <see cref="EntitySerializationFilter"/> class.</summary>
        /// <param name="queryStringParams">The entity query string params.</param>
        public EntitySerializationFilter(Dictionary<string, string> queryStringParams) 
            : this(new EntityActivityQuery(queryStringParams))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="EntitySerializationFilter"/> class.</summary>
        public EntitySerializationFilter() 
            : this(new EntityActivityQuery(null))
        {
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
            return new EntitySerializationFilter(this.EntityQueries.Clone());
        }
    }
}