// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityFilter.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
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