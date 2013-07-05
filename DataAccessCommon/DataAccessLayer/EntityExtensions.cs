// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DataAccessLayer
{
    /// <summary>Extension methods for IEntity and closely related classes.</summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static T GetPropertyByName<T>(this IEntity entity, string propertyName)
        {
            var propertyValue = entity.GetPropertyValueByName(propertyName);
            return propertyValue.GetValueAs<T>();
        }

        /// <summary>
        /// Tries to get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value to return on fail.</param>
        /// <returns>The value of the property, or default value if not found or cannot be cast.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static T TryGetPropertyByName<T>(this IEntity entity, string propertyName, T defaultValue)
        {
            try
            {
                return entity.GetPropertyByName<T>(propertyName);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static void SetPropertyByName<T>(this IEntity entity, string propertyName, T value)
        {
            entity.SetPropertyByName(propertyName, value, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static void SetPropertyByName<T>(this IEntity entity, string propertyName, T value, PropertyFilter filter)
        {
            entity.SetEntityProperty(new EntityProperty(propertyName, PropertyValue.BuildPropertyValue(value), filter));
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if setting the property succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static bool TrySetPropertyByName<T>(this IEntity entity, string propertyName, T value)
        {
            return entity.TrySetPropertyByName(propertyName, value, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the property succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static bool TrySetPropertyByName<T>(this IEntity entity, string propertyName, T value, PropertyFilter filter)
        {
            try
            {
                return entity.TrySetPropertyValueByName(propertyName, PropertyValue.BuildPropertyValue(value), filter);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Remove a property from the Properties collection by name.</summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name to remove.</param>
        public static void RemovePropertyByName(this IEntity entity, string propertyName)
        {
            var existingProperty = entity.TryGetEntityPropertyByName(propertyName);
            if (existingProperty != null)
            {
                entity.Properties.Remove(existingProperty);
            }
        }

        /// <summary>
        /// Get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static PropertyValue GetPropertyValueByName(this IEntity entity, string propertyName)
        {
            var propertyValue = entity.GetEntityPropertyByName(propertyName).Value;

            if (propertyValue == null)
            {
                throw new InvalidOperationException("PropertyValue is null: {0}"
                    .FormatInvariant(propertyName));
            }

            return propertyValue;
        }

        /// <summary>
        /// Try to get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The PropertyValue, or null if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static PropertyValue TryGetPropertyValueByName(this IEntity entity, string propertyName)
        {
            try
            {
                return entity.GetPropertyValueByName(propertyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Set a single property on the Properties collection given a name and PropertyValue
        /// (or something cast'able to a PropertyValue).
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static void SetPropertyValueByName(this IEntity entity, string propertyName, PropertyValue value)
        {
            entity.SetEntityProperty(new EntityProperty(propertyName, value));
        }

        /// <summary>
        /// Set a single property on the Properties collection given a name and PropertyValue
        /// (or something cast'able to a PropertyValue).
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static void SetPropertyValueByName(
            this IEntity entity, string propertyName, PropertyValue value, PropertyFilter filter)
        {
            entity.SetEntityProperty(new EntityProperty(propertyName, value, filter));
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        public static bool TrySetPropertyValueByName(this IEntity entity, string propertyName, PropertyValue propertyValue)
        {
            return entity.TrySetPropertyValueByName(propertyName, propertyValue, PropertyFilter.Default);
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static bool TrySetPropertyValueByName(this IEntity entity, string propertyName, PropertyValue propertyValue, PropertyFilter filter)
        {
            try
            {
                entity.SetPropertyValueByName(propertyName, propertyValue, filter);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Don't want to expose IRawEntity here.")]
        public static EntityProperty GetEntityPropertyByName(this IEntity entity, string propertyName)
        {
            var properties = entity.Properties.Where(p => p.Name == propertyName).ToList();

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
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default property value to use if not found.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static EntityProperty TryGetEntityPropertyByName(this IEntity entity, string propertyName, PropertyValue defaultValue)
        {
            try
            {
                return entity.GetEntityPropertyByName(propertyName);
            }
            catch (Exception)
            {
                return new EntityProperty { Name = propertyName, Value = defaultValue };
            }
        }

        /// <summary>
        /// Try to get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The EntityProperty, or null if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static EntityProperty TryGetEntityPropertyByName(this IEntity entity, string propertyName)
        {
            try
            {
                return entity.GetEntityPropertyByName(propertyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Set a value from the Properties collection by name or add it if not already present.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="entityProperty">The property to set.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        public static void SetEntityProperty(this IEntity entity, EntityProperty entityProperty)
        {
            var existingProperty = TryGetEntityPropertyByName(entity, entityProperty.Name);

            if (existingProperty == null)
            {
                // Add the property if not found
                entity.Properties.Add(entityProperty);
                return;
            }

            // Otherwise update the property
            entity.Properties.Remove(existingProperty);
            entity.Properties.Add(entityProperty);
        }

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// Filter value of EntityProperty must match if updating.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="entityProperty">The property to set.</param>
        /// <returns>True if setting the EntityProperty succeeded.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        public static bool TrySetEntityProperty(this IEntity entity, EntityProperty entityProperty)
        {
            try
            {
                entity.SetEntityProperty(entityProperty);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name, and not a collection.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Don't want to expose IRawEntity here.")]
        public static Association GetAssociationByName(this IEntity entity, string associationName)
        {
            return entity.Associations.Single(a => a.ExternalName == associationName);
        }

        /// <summary>
        /// Tries to get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name or none, and not a collection.
        /// </summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern - should not throw.")]
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Don't want to expose IRawEntity here.")]
        public static Association TryGetAssociationByName(this IEntity entity, string associationName)
        {
            try
            {
                return entity.Associations.SingleOrDefault(a => a.ExternalName == associationName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <exception cref="ArgumentException">If the target entities are not all the same category and external type.</exception>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Don't want to expose IRawEntity here.")]
        public static void AssociateEntities(
            this IEntity entity,
            string associationName,
            string associationDetails,
            HashSet<IEntity> targetEntities,
            AssociationType associationType,
            bool replaceIfPresent)
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
                var matchList = entity.Associations.Where(a => a.ExternalName == associationName).ToList();
                foreach (var match in matchList)
                {
                    entity.Associations.Remove(match);
                }
            }

            foreach (var targetEntity in targetEntities)
            {
                // Duplicate associations not allowed
                // Determine if an entity with the same ExternalEntityId with the same Association ExternalName is already associated.
                var idMatch = entity.Associations.SingleOrDefault(a => a.TargetEntityId == (EntityId)targetEntity.ExternalEntityId
                    && a.ExternalName == associationName);

                if (idMatch != null)
                {
                    // Remove the existing target
                    entity.Associations.Remove(idMatch);
                }

                entity.Associations.Add(
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

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <returns>true if association was allowed.</returns>
        public static bool TryAssociateEntities(
            this IEntity entity, 
            string associationName, 
            string associationDetails, 
            HashSet<IEntity> targetEntities, 
            AssociationType associationType, 
            bool replaceIfPresent)
        {
            try
            {
                entity.AssociateEntities(associationName, associationDetails, targetEntities, associationType, replaceIfPresent);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>Get a associations from the Associations collection by name.</summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">The association name get.</param>
        /// <returns>Get the association or associations by name.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Don't want to expose IRawEntity here.")]
        public static IList<Association> GetAssociationsByName(this IEntity entity, string associationName)
        {
            return entity.Associations.Where(a => a.ExternalName == associationName).ToList();
        }

        /// <summary>Remove an association from the Associations collection by name.</summary>
        /// <param name="entity">The IEntity.</param>
        /// <param name="associationName">The association name to remove.</param>
        public static void RemoveAssociationsByName(this IEntity entity, string associationName)
        {
            var associations = entity.GetAssociationsByName(associationName);
            foreach (var association in associations)
            {
                entity.Associations.Remove(association);
            }
        }

        /// <summary>Build a wrapped entity based on the entity category.</summary>
        /// <typeparam name="T">The IEntity wrapper type to return.</typeparam>
        /// <param name="possibleUnwrappedEntity">A possibly unwrapped entity.</param>
        /// <returns>An IEntity object.</returns>
        public static T BuildWrappedEntity<T>(this IEntity possibleUnwrappedEntity)
            where T : EntityWrapperBase, new()
        {
            var wrapperEntity = new T();
            wrapperEntity.Initialize(possibleUnwrappedEntity);
            return wrapperEntity;
        }

        /// <summary>Build a wrapped entity based on the entity category.</summary>
        /// <param name="possibleUnwrappedEntity">A possibly unwrapped entity.</param>
        /// <returns>An IEntity object.</returns>
        internal static IEntity BuildWrappedEntity(this IEntity possibleUnwrappedEntity)
        {
            if (possibleUnwrappedEntity == null)
            {
                throw new DataAccessException("Entity required to build wrapper.");
            }

            if (possibleUnwrappedEntity.EntityCategory == null)
            {
                throw new DataAccessException("EntityCategory required to build wrapper");
            }

            var typeMap = new Dictionary<string, Func<IEntity, IEntity>>
                {
                    { CompanyEntity.CategoryName, e => e.BuildWrappedEntity<CompanyEntity>() },
                    { UserEntity.CategoryName, e => e.BuildWrappedEntity<UserEntity>() },
                    { CampaignEntity.CategoryName, e => e.BuildWrappedEntity<CampaignEntity>() },
                    { CreativeEntity.CategoryName, e => e.BuildWrappedEntity<CreativeEntity>() },
                    { PartnerEntity.CategoryName, e => e.BuildWrappedEntity<PartnerEntity>() },
                    { BlobEntity.CategoryName, e => e.BuildWrappedEntity<BlobEntity>() },
                    { ReportEntity.CategoryName, e => e.BuildWrappedEntity<ReportEntity>() },
                };

            var category = (string)possibleUnwrappedEntity.EntityCategory;
            if (!typeMap.ContainsKey(category))
            {
                var id = possibleUnwrappedEntity.ExternalEntityId != null
                             ? possibleUnwrappedEntity.ExternalEntityId.ToString()
                             : string.Empty;
                var msg = "Entity: {0} does not match requested type: {1}"
                    .FormatInvariant(id, category);
                throw new DataAccessTypeMismatchException(msg);
            }

            return typeMap[category](possibleUnwrappedEntity);
        }

        /// <summary>Determine if an IRawEntity is really an IEntity and unwrap if so.</summary>
        /// <param name="possibleWrappedEntity">The possible wrapped entity.</param>
        /// <returns>A bare IRawEntity that is not an IEntity</returns>
        internal static IEntity SafeUnwrapEntity(this IEntity possibleWrappedEntity)
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
    }
}
