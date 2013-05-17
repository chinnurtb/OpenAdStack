// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityWrapperBase.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DataAccessLayer
{
    /// <summary>An entity with validation methods for properties required of a user in the system.</summary>
    public abstract class EntityWrapperBase : IEntity
    {
        /// <summary>Type mapping dictionary for generic setters.</summary>
        private static readonly Dictionary<Type, PropertyType> typeMap = new Dictionary<Type, PropertyType>
                {
                    { typeof(string), PropertyType.String },
                    { typeof(int), PropertyType.Int32 },
                    { typeof(long), PropertyType.Int64 },
                    { typeof(double), PropertyType.Double },
                    { typeof(decimal), PropertyType.Double },
                    { typeof(bool), PropertyType.Bool },
                    { typeof(DateTime), PropertyType.Date },
                    { typeof(Guid), PropertyType.Guid },
                    { typeof(EntityId), PropertyType.Guid },
                    { typeof(byte[]), PropertyType.Binary },
                };

        /// <summary>Backing field for WrappedEntity property.</summary>
        private IRawEntity wrappedEntityBackingField;
        
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
        public IRawEntity WrappedEntity
        {
            get { return this.wrappedEntityBackingField; }
            private set { this.wrappedEntityBackingField = value; }
        }

        /// <summary>Build a wrapped entity based on the entity category.</summary>
        /// <param name="unwrappedEntity">The unwrapped entity.</param>
        /// <returns>An IEntity object.</returns>
        public static IEntity BuildWrappedEntity(IRawEntity unwrappedEntity)
        {
            if (unwrappedEntity == null)
            {
                return null;
            }

            if ((string)unwrappedEntity.EntityCategory == CompanyEntity.CompanyEntityCategory)
            {
                return new CompanyEntity(unwrappedEntity);
            }

            if ((string)unwrappedEntity.EntityCategory == UserEntity.UserEntityCategory)
            {
                return new UserEntity(unwrappedEntity);
            }

            if ((string)unwrappedEntity.EntityCategory == CampaignEntity.CampaignEntityCategory)
            {
                return new CampaignEntity(unwrappedEntity);
            }

            if ((string)unwrappedEntity.EntityCategory == CreativeEntity.CreativeEntityCategory)
            {
                return new CreativeEntity(unwrappedEntity);
            }

            if ((string)unwrappedEntity.EntityCategory == PartnerEntity.PartnerEntityCategory)
            {
                return new PartnerEntity(unwrappedEntity);
            }

            if ((string)unwrappedEntity.EntityCategory == BlobEntity.BlobEntityCategory)
            {
                return new BlobEntity(unwrappedEntity);
            }

            return null;
        }

        /// <summary>
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public EntityProperty GetEntityPropertyByName(string propertyName)
        {
            var properties = this.Properties.Where(p => p.Name == propertyName).ToList();
            
            if (!properties.Any())
            {
                throw new InvalidOperationException("Property not found: {0}"
                    .FormatInvariant(propertyName));
            }

            if (properties.Count > 1)
            {
                throw new InvalidOperationException("More than one property of that name: {0}"
                    .FormatInvariant(propertyName));
            }

            return properties.First();
        }

        /// <summary>
        /// Get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public PropertyValue GetPropertyValueByName(string propertyName)
        {
            var propertyValue = this.GetEntityPropertyByName(propertyName).Value;

            if (propertyValue == null)
            {
                throw new InvalidOperationException("PropertyValue is null: {0}"
                    .FormatInvariant(propertyName));
            }

            return propertyValue;
        }

        /// <summary>
        /// Get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public T GetPropertyByName<T>(string propertyName)
        {
            var propertyValue = this.GetPropertyValueByName(propertyName);
            return propertyValue.GetValueAs<T>();
        }
        
        /// <summary>
        /// Set a value from the Properties collection by name or add it if not already present.
        /// </summary>
        /// <param name="entityProperty">The property to set.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public void SetEntityProperty(EntityProperty entityProperty)
        {
            var existingProperty = TryGetEntityPropertyByName(entityProperty.Name);

            if (existingProperty == null)
            {
                // Add the property if not found
                this.Properties.Add(entityProperty);
                return;
            }

            // Otherwise update the property
            this.Properties.Remove(existingProperty);
            this.Properties.Add(entityProperty);
        }

        /// <summary>
        /// Set a single property on the Properties collection given a name and PropertyValue
        /// (or something cast'able to a PropertyValue).
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public void SetPropertyValueByName(string propertyName, PropertyValue value)
        {
            this.SetEntityProperty(new EntityProperty(propertyName, value));
        }

        /// <summary>
        /// Set a single property on the Properties collection given a name and PropertyValue
        /// (or something cast'able to a PropertyValue).
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public void SetPropertyValueByName(string propertyName, PropertyValue value, PropertyFilter filter)
        {
            this.SetEntityProperty(new EntityProperty(propertyName, value, filter));
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public void SetPropertyByName<T>(string propertyName, T value)
        {
            this.SetPropertyByName(propertyName, value, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public void SetPropertyByName<T>(string propertyName, T value, PropertyFilter filter)
        {
            this.SetEntityProperty(new EntityProperty(propertyName, BuildPropertyValue(value), filter));
        }

        /// <summary>
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default property value to use if not found.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public EntityProperty TryGetEntityPropertyByName(string propertyName, PropertyValue defaultValue)
        {
            try
            {
                return this.GetEntityPropertyByName(propertyName);
            }
            catch (Exception)
            {
                return new EntityProperty { Name = propertyName, Value = defaultValue };
            }
        }

        /// <summary>
        /// Try to get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The EntityProperty, or null if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public EntityProperty TryGetEntityPropertyByName(string propertyName)
        {
            try
            {
                return this.GetEntityPropertyByName(propertyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Try to get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The PropertyValue, or null if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public PropertyValue TryGetPropertyValueByName(string propertyName)
        {
            try
            {
                return this.GetPropertyValueByName(propertyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value to return on fail.</param>
        /// <returns>The value of the property, or default value if not found or cannot be cast.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public T TryGetPropertyByName<T>(string propertyName, T defaultValue)
        {
            try
            {
                return this.GetPropertyByName<T>(propertyName);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// Filter value of EntityProperty must match if updating.
        /// </summary>
        /// <param name="entityProperty">The property to set.</param>
        /// <returns>True if setting the EntityProperty succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public bool TrySetEntityProperty(EntityProperty entityProperty)
        {
            try
            {
                this.SetEntityProperty(entityProperty);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        public bool TrySetPropertyValueByName(string propertyName, PropertyValue propertyValue)
        {
            return this.TrySetPropertyValueByName(propertyName, propertyValue, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public bool TrySetPropertyValueByName(string propertyName, PropertyValue propertyValue, PropertyFilter filter)
        {
            try
            {
                this.SetPropertyValueByName(propertyName, propertyValue, filter);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if setting the property succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public bool TrySetPropertyByName<T>(string propertyName, T value)
        {
            return this.TrySetPropertyByName(propertyName, value, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the property succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public bool TrySetPropertyByName<T>(string propertyName, T value, PropertyFilter filter)
        {
            try
            {
                return this.TrySetPropertyValueByName(propertyName, BuildPropertyValue(value), filter);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name, and not a collection.
        /// </summary>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        public Association GetAssociationByName(string associationName)
        {
            return this.Associations.Single(a => a.ExternalName == associationName);
        }

        /// <summary>
        /// Tries to get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name or none, and not a collection.
        /// </summary>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public Association TryGetAssociationByName(string associationName)
        {
            try
            {
                return this.Associations.SingleOrDefault(a => a.ExternalName == associationName);
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <returns>true if association was allowed.</returns>
        public bool TryAssociateEntities(string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent)
        {
            try
            {
                this.AssociateEntities(associationName, associationDetails, targetEntities, associationType, replaceIfPresent);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <exception cref="ArgumentException">If the target entities are not all the same category and external type.</exception>
        public void AssociateEntities(string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent)
        {
            if (targetEntities.Count == 0)
            {
                // Nothing to do
                return;
            }

            // This will throw if all the EntityCategory and ExternalType values in the collection are not the same
            // ExternalType could be optional
            var targetEntityCategories = targetEntities.Select(e => e.EntityCategory).Distinct().ToList();
            var targetExternalTypes = targetEntities.Select(e => e.ExternalType).Distinct().ToList();
            if (targetEntityCategories.Count() != 1 || targetExternalTypes.Count() > 1)
            {
                throw new ArgumentException("Target Entities are not all of same category and external type.");
            }

            // If the named association is to be replaced remove the current association(s) of that name
            if (replaceIfPresent)
            {
                var matchList = this.Associations.Where(a => a.ExternalName == associationName).ToList();
                foreach (var match in matchList)
                {
                    this.Associations.Remove(match);
                }
            }

            foreach (var targetEntity in targetEntities)
            {
                // Duplicate associations not allowed
                // Determine if an entity with the same ExternalEntityId with the same Association ExternalName is already associated.
                var idMatch = this.Associations.SingleOrDefault(a => a.TargetEntityId == (EntityId)targetEntity.ExternalEntityId
                    && a.ExternalName == associationName);

                if (idMatch != null)
                {
                    // Remove the existing target
                    this.Associations.Remove(idMatch);
                }

                this.Associations.Add(
                    new Association
                    {
                        TargetEntityId = targetEntity.ExternalEntityId,
                        ExternalName = associationName,
                        Details = associationDetails,
                        AssociationType = associationType,
                        TargetEntityCategory = targetEntityCategories.Single(),
                        TargetExternalType = targetExternalTypes.Single()
                    });
            }
        }
        
        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        public abstract void ValidateEntityType(IRawEntity entity);
        
        /// <summary>Determine if an IRawEntity is really an IEntity and unwrap if so.</summary>
        /// <param name="possibleWrappedEntity">The possible wrapped entity.</param>
        /// <returns>A bare IRawEntity that is not an IEntity</returns>
        internal static IRawEntity SafeUnwrapEntity(IRawEntity possibleWrappedEntity)
        {
            var entityWrapper = possibleWrappedEntity as EntityWrapperBase;
            if (entityWrapper != null)
            {
                // To be absolutely bullet-proof this could be done recursively. But this check
                // should prevent it from ever going that far (since the the WrappedEntity setter is private)
                return entityWrapper.WrappedEntity;
            }

            return possibleWrappedEntity;
        }

        /// <summary>Check if a property is defined and throw ArgumentException if not.</summary>
        /// <param name="wrappedEntity">The wrapped entity.</param>
        /// <param name="targetCategory">The target category of the wrapped entity.</param>
        /// <param name="propertyName">The of the property to check.</param>
        /// <exception cref="ArgumentException">If property not found.</exception>
        protected static void ThrowIfPropertyNotDefined(IRawEntity wrappedEntity, string targetCategory, string propertyName)
        {
            if (wrappedEntity.Properties.Count(p => p.Name == propertyName) != 1)
            {
                throw new ArgumentException("Invalid {0} Entity: Missing property - {1}"
                    .FormatInvariant(targetCategory, propertyName));
            }
        }

        /// <summary>Check if a property is defined and throw ArgumentException if not.</summary>
        /// <param name="wrappedEntity">The wrapped entity.</param>
        /// <param name="targetCategory">The target category of the wrapped entity.</param>
        /// <exception cref="ArgumentException">If property not found.</exception>
        protected static void ThrowIfCategoryMismatch(IRawEntity wrappedEntity, string targetCategory)
        {
            var actualCategory = (string)wrappedEntity.EntityCategory;
            if (actualCategory != targetCategory)
            {
                throw new ArgumentException("Invalid {0} Entity: Incorrect category - {1}"
                    .FormatInvariant(targetCategory, actualCategory));
            }
        }

        /// <summary>Initialize helper to do late-bound construction by derived classes.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="entityCategory">The Entity Category.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        protected void Initialize(EntityId externalEntityId, string entityCategory, IRawEntity rawEntity)
        {
            if (externalEntityId == null)
            {
                throw new ArgumentException("ExternalEntityId required");
            }

            if (string.IsNullOrEmpty(entityCategory))
            {
                throw new ArgumentException("EntityCategory required.");
            }

            rawEntity.EntityCategory = new EntityProperty { Name = "EntityCategory", Value = entityCategory };
            rawEntity.ExternalEntityId = new EntityProperty { Name = "ExternalEntityId", Value = externalEntityId };
            this.Initialize(rawEntity);
        }

        /// <summary>Initialize helper to do late-bound construction by derived classes.</summary>
        /// <param name="entity">The entity (which may already be wrapped) from which to construct.</param>
        protected void Initialize(IRawEntity entity)
        {
            this.ValidateEntityType(entity);
            this.WrappedEntity = SafeUnwrapEntity(entity);
        }

        /// <summary>
        /// Factory method to build a PropertyValue of the appropriate type
        /// from a generically specified value.
        /// </summary>
        /// <param name="value">The value of the property.</param>
        /// <typeparam name="T">The generically specified type of the property.</typeparam>
        /// <returns>A PropertyValue if success, otherwise null.</returns>
        private static PropertyValue BuildPropertyValue<T>(T value)
        {
            if (!typeMap.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException("Type cannot be mapped to PropertyValue: {0}"
                    .FormatInvariant(typeof(T).FullName));
            }

            return new PropertyValue(typeMap[typeof(T)], value);
        }
    }
}
