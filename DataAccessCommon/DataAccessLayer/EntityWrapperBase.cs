// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityWrapperBase.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer
{
    /// <summary>
    /// This is a base class for a series of wrapper classes that wrap an Entity object to provide validation
    /// and additional schema support based on the EntityCategory values.
    /// </summary>
    public abstract class EntityWrapperBase : IEntity
    {
        /// <summary>Backing field for WrappedEntity property.</summary>
        private IEntity wrappedEntityBackingField;
        
        /// <summary>Gets or sets storage key of the entity.</summary>
        public virtual IStorageKey Key
        {
            get { return this.WrappedEntity.Key; }
            set { this.WrappedEntity.Key = value; }
        }
        
        ////
        // On First-Class property setters make sure we enforce the name of the EntityProperty
        // In case we are being assigned from a bare value (un-named EntityProperty)
        ////

        /// <summary>Gets or sets external Entity Id.</summary>
        public virtual EntityProperty ExternalEntityId
        {
            get { return this.WrappedEntity.ExternalEntityId; }
            set { this.WrappedEntity.ExternalEntityId = value; }
        }

        /// <summary>Gets or sets category of the entity (Pre-defined categories such as Company and Campaign).</summary>
        public virtual EntityProperty EntityCategory
        {
            get { return this.WrappedEntity.EntityCategory; }
            set { this.WrappedEntity.EntityCategory = value; }
        }

        /// <summary>Gets or sets creation date of entity.</summary>
        public virtual EntityProperty CreateDate
        {
            get { return this.WrappedEntity.CreateDate; }
            set { this.WrappedEntity.CreateDate = value; }
        }

        /// <summary>Gets or sets last modified date of entity.</summary>
        public virtual EntityProperty LastModifiedDate
        {
            get { return this.WrappedEntity.LastModifiedDate; }
            set { this.WrappedEntity.LastModifiedDate = value; }
        }

        /// <summary>Gets or sets user who last modified entity.</summary>
        public EntityProperty LastModifiedUser
        {
            get { return this.WrappedEntity.LastModifiedUser; }
            set { this.WrappedEntity.LastModifiedUser = value; }
        }

        /// <summary>Gets or sets entity schema version.</summary>
        public EntityProperty SchemaVersion
        {
            get { return this.WrappedEntity.SchemaVersion; }
            set { this.WrappedEntity.SchemaVersion = value; }
        }

        /// <summary>Gets or sets current writable version.</summary>
        public virtual EntityProperty LocalVersion
        {
            get { return this.WrappedEntity.LocalVersion; }
            set { this.WrappedEntity.LocalVersion = value; }
        }

        /// <summary>Gets or sets partner-defined name of entity.</summary>
        public virtual EntityProperty ExternalName
        {
            get { return this.WrappedEntity.ExternalName; }
            set { this.WrappedEntity.ExternalName = value; }
        }

        /// <summary>Gets or sets partner-defined type of entity.</summary>
        public virtual EntityProperty ExternalType
        {
            get { return this.WrappedEntity.ExternalType; }
            set { this.WrappedEntity.ExternalType = value; }
        }

        /// <summary>Gets the properties of the entity.</summary>
        public virtual IList<EntityProperty> Properties
        {
            get { return this.WrappedEntity.Properties; }
        }

        /// <summary>Gets the associations of the entity.</summary>
        public virtual IList<Association> Associations
        {
            get { return this.WrappedEntity.Associations; }
        }

        /// <summary>Gets a collection with the Interface-level EntityProperty members of the entity (e.g. - ExternalEntityId).</summary>
        public IList<EntityProperty> InterfaceProperties
        {
            get { return this.WrappedEntity.InterfaceProperties; }
        }

        /// <summary>
        /// Gets WrappedEntity.
        /// Not expecting derived classes to override this.
        /// </summary>
        public IEntity WrappedEntity
        {
            get { return this.wrappedEntityBackingField; }
            private set { this.wrappedEntityBackingField = value; }
        }

        /// <summary>Initialize helper to do late-bound construction by derived classes.</summary>
        /// <param name="entity">The entity (which may already be wrapped) from which to construct.</param>
        internal void Initialize(IEntity entity)
        {
            if (entity == null)
            {
                throw new DataAccessException("Entity required to initialize entity wrapper.");
            }

            this.ValidateEntityType(entity);
            this.WrappedEntity = entity.SafeUnwrapEntity();
        }

        /// <summary>Check if a property is defined and throw ArgumentException if not.</summary>
        /// <param name="wrappedEntity">The wrapped entity.</param>
        /// <param name="targetCategory">The target category of the wrapped entity.</param>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <exception cref="DataAccessTypeMismatchException">If property not found.</exception>
        protected static void ThrowIfPropertyNotDefined(IEntity wrappedEntity, string targetCategory, string propertyName)
        {
            ThrowIfPropertyNotDefined(wrappedEntity, targetCategory, propertyName, PropertyFilter.Default);
        }

        /// <summary>Check if a property is defined and throw ArgumentException if not.</summary>
        /// <param name="wrappedEntity">The wrapped entity.</param>
        /// <param name="targetCategory">The target category of the wrapped entity.</param>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <param name="filter">The property filter that must be used.</param>
        /// <exception cref="ArgumentException">If property not found.</exception>
        protected static void ThrowIfPropertyNotDefined(IEntity wrappedEntity, string targetCategory, string propertyName, PropertyFilter filter)
        {
            var id = wrappedEntity.ExternalEntityId != null
                         ? wrappedEntity.ExternalEntityId.ToString()
                         : string.Empty;

            if (wrappedEntity.Properties.Count(p => p.Name == propertyName) != 1)
            {
                var msg = "Entity: {0} does not have required property: {1} for requested type: {2}"
                    .FormatInvariant(id, propertyName, targetCategory);
                throw new DataAccessException(msg);
            }

            if (wrappedEntity.GetEntityPropertyByName(propertyName).Filter != filter)
            {
                var msg = "Entity: {0} does not have required filter: {1} for property: {2} for requested type: {3}"
                    .FormatInvariant(id, filter.ToString(), propertyName, targetCategory);
                throw new DataAccessException(msg);
            }
        }

        /// <summary>Check if a property is defined and throw ArgumentException if not.</summary>
        /// <param name="wrappedEntity">The wrapped entity.</param>
        /// <param name="targetCategory">The target category of the wrapped entity.</param>
        /// <exception cref="DataAccessTypeMismatchException">If property not found.</exception>
        protected static void ThrowIfCategoryMismatch(IEntity wrappedEntity, string targetCategory)
        {
            var actualCategory = (string)wrappedEntity.EntityCategory;
            if (actualCategory != targetCategory)
            {
                var id = wrappedEntity.ExternalEntityId != null
                             ? wrappedEntity.ExternalEntityId.ToString()
                             : string.Empty;
                var msg = "Entity: {0} does not match requested type: {1}"
                    .FormatInvariant(id, targetCategory);
                throw new DataAccessTypeMismatchException(msg);
            }
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected abstract void ValidateEntityType(IEntity entity);

        /// <summary>Initialize helper to do late-bound construction by derived classes.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="entityCategory">The Entity Category.</param>
        /// <param name="entity">The entity from which to construct.</param>
        protected void Initialize(EntityId externalEntityId, string entityCategory, IEntity entity)
        {
            if (entity == null)
            {
                throw new DataAccessException("Entity required to initialize entity wrapper.");
            }

            if (externalEntityId == null)
            {
                throw new DataAccessException("ExternalEntityId required to initialize entity wrapper.");
            }

            if (string.IsNullOrEmpty(entityCategory))
            {
                throw new DataAccessException("EntityCategory required to initialize entity wrapper.");
            }

            entity.EntityCategory = new EntityProperty { Name = "EntityCategory", Value = entityCategory };
            entity.ExternalEntityId = new EntityProperty { Name = "ExternalEntityId", Value = externalEntityId };
            this.Initialize(entity);
        }
    }
}
