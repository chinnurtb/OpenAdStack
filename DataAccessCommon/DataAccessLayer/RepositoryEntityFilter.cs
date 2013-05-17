// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RepositoryEntityFilter.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>IEntityFilter implementation for repository access.</summary>
    public class RepositoryEntityFilter : IEntityFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryEntityFilter"/> class. Default constructor.
        /// </summary>
        public RepositoryEntityFilter() : this(true, true, true, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="RepositoryEntityFilter"/> class.</summary>
        /// <param name="includeDefaultProperties">True to include Default Properties.</param>
        /// <param name="includeSystemProperties">True to include System Properties.</param>
        /// <param name="includeExtendedProperties">True to include Extended Properties.</param>
        /// <param name="includeAssociations">True to include Associations.</param>
        public RepositoryEntityFilter(
            bool includeDefaultProperties, 
            bool includeSystemProperties, 
            bool includeExtendedProperties, 
            bool includeAssociations)
        {
            this.EntityQueries = new RepositoryEntityQuery();
            this.Filters = new List<PropertyFilter>();

            if (includeDefaultProperties)
            {
                this.Filters.Add(PropertyFilter.Default);
            }

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
            var clonedFilter = new RepositoryEntityFilter(
                this.IncludeDefaultProperties,
                this.IncludeSystemProperties,
                this.IncludeExtendedProperties,
                this.IncludeAssociations);
            clonedFilter.EntityQueries = this.EntityQueries.Clone();

            return clonedFilter;
        }
    }
}
