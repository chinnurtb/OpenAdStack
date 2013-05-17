// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using DataAccessLayer;
using Diagnostics;
using Newtonsoft.Json;

namespace EntityUtilities
{
    /// <summary>
    /// Useful extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>Serialize entity to JSON.</summary>
        /// <param name="entity">The entity</param>
        /// <returns>JSON string. </returns>
        public static string SerializeToJson(this IRawEntity entity)
        {
            return SerializeToJson(entity, new EntitySerializationFilter());
        }

        /// <summary>Serialize a raw entity to Json.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="filter">The entity filter.</param>
        /// <returns>A JSON string.</returns>
        public static string SerializeToJson(this IRawEntity entity, IEntityFilter filter)
        {
            return EntityJsonSerializer.SerializeToJson(entity, filter);
        }

        /// <summary>
        /// Serializes an enumerable of entities to a json list
        /// </summary>
        /// <param name="entities">The entities</param>
        /// <typeparam name="TEntity">The type of entity. Must inherit from EntityWrapperBase.</typeparam>
        /// <returns>The json list</returns>
        public static string SerializeToJson<TEntity>(this IEnumerable<TEntity> entities)
            where TEntity : IEntity
        {
            return SerializeToJson(entities, new EntitySerializationFilter());
        }

        /// <summary>
        /// Serializes an enumerable of entities to a json list
        /// </summary>
        /// <param name="entities">The entities</param>
        /// <param name="filter">The entity filter.</param>
        /// <typeparam name="TEntity">The type of entity. Must inherit from EntityWrapperBase.</typeparam>
        /// <returns>The json list</returns>
        public static string SerializeToJson<TEntity>(
            this IEnumerable<TEntity> entities,
            IEntityFilter filter)
            where TEntity : IEntity
        {
            var queryValues = filter == null ? null : filter.EntityQueries.QueryStringParams;

            // Certain queries may have user imposed limits. The user may specify any combination of the following query values:
            // 1. "NumObjects" to limit the number of return values
            // 2. "Top" to indicate return values from the top of the list
            // 3. "Skip" to indicate the return values skip this number from the top of the list.
            // 4. "OrderBy" to indicate which property is the key for the returned order
            // to account for these, use a combination of the Linq Skip and Take
            // ToDo: Implement OrderBy
            if (queryValues != null && queryValues.Keys.Contains("numobjects") && queryValues.Keys.Contains("skip"))
            {
                string[] entityJsons = entities
                    .Select(e => e.SerializeToJson(filter))
                    .Skip(Convert.ToInt32(queryValues["skip"], CultureInfo.InvariantCulture))
                    .Take(Convert.ToInt32(queryValues["numobjects"], CultureInfo.InvariantCulture))
                    .ToArray();
                return "[" + string.Join(", ", entityJsons) + "]";
            }

            // ToDo: this may be redundant with "Top". This will always have the same effect as using Top since NumObjects is 
            // ToDo: selecting a count to return from the top
            if (queryValues != null && queryValues.Keys.Contains("numobjects"))
            {
                string[] entityJsons = entities
                    .Select(e => e.SerializeToJson(filter))
                    .Take(Convert.ToInt32(queryValues["numobjects"], CultureInfo.InvariantCulture))
                    .ToArray();
                return "[" + string.Join(", ", entityJsons) + "]";
            }

            if (queryValues != null && queryValues.Keys.Contains("top") && queryValues.Keys.Contains("skip"))
            {
                string[] entityJsons = entities
                    .Select(e => e.SerializeToJson(filter))
                    .Skip(Convert.ToInt32(queryValues["skip"], CultureInfo.InvariantCulture))
                    .Take(Convert.ToInt32(queryValues["top"], CultureInfo.InvariantCulture))
                    .ToArray();
                return "[" + string.Join(", ", entityJsons) + "]";
            }

            if (queryValues != null && queryValues.Keys.Contains("top"))
            {
                string[] entityJsons = entities
                    .Select(e => e.SerializeToJson(filter))
                    .Take(Convert.ToInt32(queryValues["top"], CultureInfo.InvariantCulture))
                    .ToArray();
                return "[" + string.Join(", ", entityJsons) + "]";
            }

            string[] entityJsons2 = entities
                .Select(e => e.SerializeToJson(filter))
                .Where(e => !string.IsNullOrEmpty(e))
                .ToArray();
            return "[" + string.Join(", ", entityJsons2) + "]";
        }

        /// <summary>
        /// Serialize the Association to a dictionary representation of a Json object that will be part of a collection.
        /// ExternalName will be omitted because that will be the collection name.
        /// </summary>
        /// <param name="this">The association</param>
        /// <returns>A dictionary compatible with JavascriptSerializer.Serialize</returns>
        public static Dictionary<string, object> SerializeToJsonCollectionFragmentDictionary(this Association @this)
        {
            var jsonCollectionFragment = new Dictionary<string, object>
                {
                    { "TargetEntityId", (string)@this.TargetEntityId },
                    { "TargetEntityCategory", @this.TargetEntityCategory },
                    { "TargetExternalType", @this.TargetExternalType },
                    { "AssociationType", Association.StringFromAssociationType(@this.AssociationType) }
                };

            return jsonCollectionFragment;
        }

        /// <summary>Sets the custom configuration settings for the entity</summary>
        /// <param name="this">The entity</param>
        /// <param name="settings">The settings</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static void SetConfigSettings(this IEntity @this, IDictionary<string, string> settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            @this.SetSystemProperty("config", json);
        }
        
        /// <summary>Gets the custom configuration settings for the entity</summary>
        /// <param name="this">The entity</param>
        /// <returns>The settings</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static IDictionary<string, string> GetConfigSettings(this IEntity @this)
        {
            var configJson = @this.TryGetSystemProperty<string>("config");
            return configJson != null ?
                JsonConvert.DeserializeObject<IDictionary<string, string>>(configJson) :
                new Dictionary<string, string>();
        }

        /// <summary>Sets a system property</summary>
        /// <param name="this">The entity</param>
        /// <param name="propertyName">System property name</param>
        /// <param name="propertyValue">System property value</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static void SetSystemProperty(this IEntity @this, string propertyName, dynamic propertyValue)
        {
            var property = @this.Properties
                .Where(p => p.Name.ToUpperInvariant() == propertyName.ToUpperInvariant())
                .SingleOrDefault();
            if (property == null)
            {
                @this.Properties.Add(property = new EntityProperty(propertyName, propertyValue, PropertyFilter.System));
            }
            else
            {
                property.Value = propertyValue;
            }
        }

        /// <summary>
        /// Gets whether an entity has a value for the specified system property
        /// </summary>
        /// <param name="this">The entity</param>
        /// <param name="propertyName">System property name</param>
        /// <returns>True if the entity has a value for the system property; otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static bool HasSystemProperty(this IEntity @this, string propertyName)
        {
            return @this.Properties
                .Any(p =>
                    p.Name.ToUpperInvariant() ==
                    propertyName.ToUpperInvariant() && p.IsSystemProperty);
        }

        /// <summary>Tries to get a system property</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The entity</param>
        /// <param name="propertyName">System property name</param>
        /// <returns>System property value</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static TValue GetSystemProperty<TValue>(this IEntity @this, string propertyName)
        {
            if (!HasSystemProperty(@this, propertyName))
            {
                throw new ArgumentException("System property not found: {0}".FormatInvariant(propertyName), "propertyName");
            }

            return TryGetSystemProperty<TValue>(@this, propertyName);
        }

        /// <summary>Tries to get a system property</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="this">The entity</param>
        /// <param name="propertyName">System property name</param>
        /// <returns>System property value</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "IRawEntity should not be used outside the DAL")]
        public static TValue TryGetSystemProperty<TValue>(this IEntity @this, string propertyName)
        {
            if (!HasSystemProperty(@this, propertyName))
            {
                return default(TValue);
            }

            return (TValue)(dynamic)@this.Properties
                .Where(p => p.Name.ToUpperInvariant() == propertyName.ToUpperInvariant())
                .Select(p => p.Value)
                .Single();
        }

        /// <summary>Sets the type of user as the UserEntity.ExternalType</summary>
        /// <param name="this">The UserEntity</param>
        /// <param name="userType">The user type</param>
        /// <exception cref="ArgumentException">Attempted to set UserType.Unknown</exception>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for UserEntity")]
        public static void SetUserType(this UserEntity @this, UserType userType)
        {
            if (userType == UserType.Unknown)
            {
                throw new ArgumentException("Cannot set user type to {0}".FormatInvariant(userType), "userType");
            }

            @this.ExternalType = userType.ToString();
        }

        /// <summary>Gets the type of user from the UserEntity.ExternalType</summary>
        /// <param name="this">The UserEntity</param>
        /// <returns>The user type</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for UserEntity")]
        public static UserType GetUserType(this UserEntity @this)
        {
            UserType value;
            return Enum.TryParse<UserType>(@this.ExternalType, true, out value) ? value : UserType.Unknown;
        }

        /// <summary>Sets the entity's owner</summary>
        /// <param name="this">The entity</param>
        /// <param name="userId">The UserEntity.UserId of the owner</param>
        public static void SetOwnerId(this IEntity @this, string userId)
        {
            @this.SetPropertyByName<string>("OwnerId", userId);
        }

        /// <summary>Gets the entity's owner</summary>
        /// <param name="this">The entity</param>
        /// <returns>The UserEntity.UserId of the owner</returns>
        public static string GetOwnerId(this IEntity @this)
        {
            return @this.TryGetPropertyByName<string>("OwnerId", null);
        }

        /// <summary>Safely gets a numeric property for an entity</summary>
        /// <remarks>Any errors are caught and logged as warnings</remarks>
        /// <param name="this">The entity</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>The property value if available; otherwise, null</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern must not throw. Exception is logged.")]
        public static int? TryGetNumericPropertyValue(this IEntity @this, string propertyName)
        {
            try
            {
                try
                {
                    var value = @this.TryGetPropertyValueByName(propertyName);
                    return value != null ? (int?)(double)value : null;
                }
                catch (ArgumentException ae)
                {
                    if (!ae.Message.ToLowerInvariant().Contains("expected type"))
                    {
                        throw;
                    }

                    // Try to coerce a number from the serialization value
                    return Convert.ToInt32(
                        @this.TryGetPropertyValueByName(propertyName).SerializationValue,
                        CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Error getting property '{0}' from entity '{1}' ({2}): {3}",
                    propertyName,
                    @this.ExternalName,
                    @this.ExternalEntityId,
                    e);
                return null;
            }
        }
    }
}
