// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityFilterNames.cs" company="Rare Crowds Inc">
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

namespace DataAccessLayer
{
    /// <summary>Class to define names used with IEntityFilter</summary>
    public static class EntityFilterNames
    {
        /// <summary>Filter name for Version filter.</summary>
        public const string VersionFilter = "VersionFilter";

        /// <summary>Filter name for EntityCategory filter.</summary>
        public const string EntityCategoryFilter = "EntityCategoryFilter";

        /// <summary>Filter name for EntityCategory filter.</summary>
        public const string ExternalTypeFilter = "ExternalTypeFilter";

        /// <summary>Filter name for PropertyNameFilter filter.</summary>
        public const string PropertyNameFilter = "PropertyNameFilter";

        /// <summary>Filter name for AssociationNameFilter filter.</summary>
        public const string AssociationNameFilter = "AssociationNameFilter";
    }
}