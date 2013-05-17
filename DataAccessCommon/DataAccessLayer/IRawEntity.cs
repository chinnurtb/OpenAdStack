// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRawEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Definition of interface for elements common to all entities.</summary>
    public interface IRawEntity
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
