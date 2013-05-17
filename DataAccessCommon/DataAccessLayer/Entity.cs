// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Entity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer
{
    /// <summary>
    /// A generalized Entity for a late-bound schema. No validation at this level.
    /// </summary>
    internal class Entity : IRawEntity
    {
        /// <summary>Backing field for first-class interface properties.</summary>
        private readonly List<EntityProperty> interfaceProperties = new List<EntityProperty>();

        /// <summary>Backing field for Properties.</summary>
        private readonly List<EntityProperty> properties = new List<EntityProperty>();

        /// <summary>Backing field for Associations.</summary>
        private readonly List<Association> associations = new List<Association>();

        /// <summary>Gets or sets storage key of the entity.</summary>
        public IStorageKey Key { get; set; }

        /// <summary>Gets or sets external Entity Id.</summary>
        public EntityProperty ExternalEntityId
        {
            get { return this.GetInterfaceProperty("ExternalEntityId"); }
            set { this.SetInterfaceProperty("ExternalEntityId", value.Value); }
        }

        /// <summary>Gets or sets category of the entity (Pre-defined categories such as Company and Campaign).</summary>
        public EntityProperty EntityCategory
        {
            get { return this.GetInterfaceProperty("EntityCategory"); }
            set { this.SetInterfaceProperty("EntityCategory", value.Value); }
        }

        /// <summary>Gets or sets creation date of entity.</summary>
        public EntityProperty CreateDate
        {
            get { return this.GetInterfaceProperty("CreateDate"); }
            set { this.SetInterfaceProperty("CreateDate", value.Value); }
        }

        /// <summary>Gets or sets last modified date of entity.</summary>
        public EntityProperty LastModifiedDate
        {
            get { return this.GetInterfaceProperty("LastModifiedDate"); }
            set { this.SetInterfaceProperty("LastModifiedDate", value.Value); }
        }

        /// <summary>Gets or sets user who last modified entity.</summary>
        public EntityProperty LastModifiedUser
        {
            get { return this.GetInterfaceProperty("LastModifiedUser"); }
            set { this.SetInterfaceProperty("LastModifiedUser", value.Value); }
        }

        /// <summary>Gets or sets entity schema version.</summary>
        public EntityProperty SchemaVersion
        {
            get { return this.GetInterfaceProperty("SchemaVersion"); }
            set { this.SetInterfaceProperty("SchemaVersion", value.Value); }
        }

        /// <summary>Gets or sets current writable version.</summary>
        public EntityProperty LocalVersion
        {
            get { return this.GetInterfaceProperty("LocalVersion"); }
            set { this.SetInterfaceProperty("LocalVersion", value.Value); }
        }

        /// <summary>Gets or sets partner-defined name of entity.</summary>
        public EntityProperty ExternalName
        {
            get { return this.GetInterfaceProperty("ExternalName"); }
            set { this.SetInterfaceProperty("ExternalName", value.Value); }
        }

        /// <summary>Gets or sets partner-defined type of entity.</summary>
        public EntityProperty ExternalType
        {
            get { return this.GetInterfaceProperty("ExternalType"); }
            set { this.SetInterfaceProperty("ExternalType", value.Value); }
        }

        /// <summary>Gets the properties of the entity.</summary>
        public IList<EntityProperty> Properties
        {
            get { return this.properties; }
        }

        /// <summary>Gets the associations of the entity.</summary>
        public IList<Association> Associations
        {
            get { return this.associations; }
        }

        /// <summary>Gets the interface property bag of the entity.</summary>
        public IList<EntityProperty> InterfaceProperties
        {
            get { return this.interfaceProperties; }
        }

        /// <summary>Get an interface property (not a collection) on the property bag.</summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The EntityProperty from the property bag.</returns>
        private EntityProperty GetInterfaceProperty(string propertyName)
        {
            var existingProperty = this.InterfaceProperties.Where(p => p.Name == propertyName).ToList();
            if (existingProperty.Count() > 1)
            {
                throw new ArgumentException("Attempting to get a property collection as a simple property: {0}"
                    .FormatInvariant(propertyName));
            }

            return existingProperty.SingleOrDefault();
        }

        /// <summary>Add or update an interface property (not a collection) on the property bag.</summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The property value.</param>
        private void SetInterfaceProperty(string propertyName, PropertyValue propertyValue)
        {
            var existingProperty = this.InterfaceProperties.Where(p => p.Name == propertyName).ToList();
            if (existingProperty.Count() > 1)
            {
                throw new ArgumentException("Attempting to set a property collection as a simple property: {0}"
                    .FormatInvariant(propertyName));
            }

            if (!existingProperty.Any())
            {
                // Add the property if not found
                this.InterfaceProperties.Add(new EntityProperty(propertyName, propertyValue));
                return;
            }

            // Otherwise update the property value
            existingProperty.Single().Value = propertyValue;
        }
    }
}
