// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityFilter.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Interface defining filters for an IEntity object.</summary>
    public interface IEntityFilter
    {
        /// <summary>Gets a value indicating whether default properties are filtered.</summary>
        bool IncludeDefaultProperties { get; }

        /// <summary>Gets a value indicating whether system properties are filtered.</summary>
        bool IncludeSystemProperties { get; }

        /// <summary>Gets a value indicating whether extended properties are filtered.</summary>
        bool IncludeExtendedProperties { get; }

        /// <summary>Gets a value indicating whether associations are filtered.</summary>
        bool IncludeAssociations { get; }

        /// <summary>Gets the property filters to include.</summary>
        IList<PropertyFilter> Filters { get; }

        /// <summary>Gets ad-hoc query values used when filtering an entity.</summary>
        IEntityQuery EntityQueries { get; }

        /// <summary>Check if EntityFilter contains a given property filter.</summary>
        /// <param name="propertyFilter">The PropertyFilter to check.</param>
        /// <returns>True if the property filter is contained in the Filters collection.</returns>
        bool ContainsFilter(PropertyFilter propertyFilter);

        /// <summary>Clone this instance of IEntityFilter.</summary>
        /// <returns>A cloned instance of this IEntityFilter.</returns>
        IEntityFilter Clone();
    }
}