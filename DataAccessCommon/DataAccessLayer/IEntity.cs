// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntity.cs" company="Rare Crowds Inc">
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
    /// <summary>Entity interface with additional out-ward facing helper methods defined.</summary>
    public interface IEntity
    {
        /// <summary>Gets or sets storage key of the entity.</summary>
        IStorageKey Key { get; set; }

        /// <summary>Gets or sets external Entity Id.</summary>
        EntityProperty ExternalEntityId { get; set; }

        /// <summary>Gets or sets category of the entity (Pre-defined categories such as Company and Campaign).</summary>
        EntityProperty EntityCategory { get; set; }

        /// <summary>Gets or sets creation date of entity.</summary>
        EntityProperty CreateDate { get; set; }

        /// <summary>Gets or sets last modified date of entity.</summary>
        EntityProperty LastModifiedDate { get; set; }

        /// <summary>Gets or sets user who last modified entity.</summary>
        EntityProperty LastModifiedUser { get; set; }

        /// <summary>Gets or sets entity schema version.</summary>
        EntityProperty SchemaVersion { get; set; }

        /// <summary>Gets or sets current writable version.</summary>
        EntityProperty LocalVersion { get; set; }

        /// <summary>Gets or sets partner-defined name of entity.</summary>
        EntityProperty ExternalName { get; set; }

        /// <summary>Gets or sets partner-defined type of entity.</summary>
        EntityProperty ExternalType { get; set; }

        /// <summary>Gets the properties of the entity.</summary>
        IList<EntityProperty> Properties { get; }

        /// <summary>Gets the associations of the entity.</summary>
        IList<Association> Associations { get; }

        /// <summary>Gets a collection with the Interface-level EntityProperty members of the entity (e.g. - ExternalEntityId).</summary>
        IList<EntityProperty> InterfaceProperties { get; }
    }
}
