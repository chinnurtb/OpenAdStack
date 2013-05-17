// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Entity interface with additional out-ward facing helper methods defined.</summary>
    public interface IEntity : IRawEntity
    {
        /// <summary>
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        EntityProperty GetEntityPropertyByName(string propertyName);

        /// <summary>
        /// Get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        PropertyValue GetPropertyValueByName(string propertyName);
        
        /// <summary>
        /// Get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        T GetPropertyByName<T>(string propertyName);

        /// <summary>
        /// Set a value from the Properties collection by name or add it if not already present.
        /// </summary>
        /// <param name="entityProperty">The property to set.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        void SetEntityProperty(EntityProperty entityProperty);

        /// <summary>
        /// Set a single property on the Properties collection given a name and PropertyValue
        /// (or something cast'able to a PropertyValue).
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        void SetPropertyValueByName(string propertyName, PropertyValue value);

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
        void SetPropertyValueByName(string propertyName, PropertyValue value, PropertyFilter filter);

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one of the property.
        /// </exception>
        void SetPropertyByName<T>(string propertyName, T value);

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
        void SetPropertyByName<T>(string propertyName, T value, PropertyFilter filter);

        /// <summary>
        /// Get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default property value to use if not found.</param>
        /// <returns>An EntityProperty with the value or with the default PropertyValue if not found.</returns>
        EntityProperty TryGetEntityPropertyByName(string propertyName, PropertyValue defaultValue);

        /// <summary>
        /// Try to get an EntityProperty from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The EntityProperty, or null if not found.</returns>
        EntityProperty TryGetEntityPropertyByName(string propertyName);

        /// <summary>
        /// Try to get a PropertyValue from the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The PropertyValue, or null if not found.</returns>
        PropertyValue TryGetPropertyValueByName(string propertyName);

        /// <summary>
        /// Tries to get a value from the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type to attempt getting the underlying value as.</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value to return on fail.</param>
        /// <returns>The value of the property, or default value if not found or cannot be cast.</returns>
        T TryGetPropertyByName<T>(string propertyName, T defaultValue);

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// Filter value of EntityProperty must match if updating.
        /// </summary>
        /// <param name="entityProperty">The property to set.</param>
        /// <returns>True if setting the EntityProperty succeeded.</returns>
        bool TrySetEntityProperty(EntityProperty entityProperty);

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        bool TrySetPropertyValueByName(string propertyName, PropertyValue propertyValue);

        /// <summary>
        /// Tries to set a PropertyValue on the Properties collection by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the PropertyValue succeeded.</returns>
        bool TrySetPropertyValueByName(string propertyName, PropertyValue propertyValue, PropertyFilter filter);

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if setting the property succeeded.</returns>
        bool TrySetPropertyByName<T>(string propertyName, T value);

        /// <summary>
        /// Tries to set a value on the Properties collection by name.
        /// </summary>
        /// <typeparam name="T">The type of the value (must be castable to PropertyValue).</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <param name="filter">The property filter type.</param>
        /// <returns>True if setting the property succeeded.</returns>
        bool TrySetPropertyByName<T>(string propertyName, T value, PropertyFilter filter);

        /// <summary>
        /// Get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name, and not a collection.
        /// </summary>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        Association GetAssociationByName(string associationName);

        /// <summary>
        /// Tries to get an Association from the Associations collection by name.
        /// This assumes you know there is a single association of this name or none, and not a collection.
        /// </summary>
        /// <param name="associationName">The association name.</param>
        /// <returns>The Association object.</returns>
        Association TryGetAssociationByName(string associationName);

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
        bool TryAssociateEntities(string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent);

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
        void AssociateEntities(string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent);

        //// TODO: Add additional methods for manipulating properties and associations (replace, add, remove)
    }
}
