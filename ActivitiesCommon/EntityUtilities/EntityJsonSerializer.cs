//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DataAccessLayer;
using Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EntityUtilities
{
    /// <summary>Json Serialization methods for IRawEnity.</summary>
    public static class EntityJsonSerializer
    {
        /// TODO: Need to encapuslate and scope the names on this enforcement (e.g. - schema) in each type
        /// TODO: that has well-known properties, e.g. - IEntity, UserEnity, CompanyEntity, CampaignEntity - but for now...
        /// TODO: We have to reserve these names in the interface contract.
        /// <summary>Well-known property name to PropertyType map.</summary>
        private static readonly Dictionary<string, PropertyType> NameToTypeMap = new Dictionary<string, PropertyType>
        {
            { "ExternalEntityId", PropertyType.Guid },
            { "EntityCategory", PropertyType.String },
            { "CreateDate", PropertyType.Date },
            { "LastModifiedDate", PropertyType.Date },
            { "LocalVersion", PropertyType.Int32 },
            { "ExternalName", PropertyType.String },
            { "ExternalType", PropertyType.String },
            { "LastModifiedUser", PropertyType.String },
            { "SchemaVersion", PropertyType.Int32 },
            { "UserId", PropertyType.String },
            { "FullName", PropertyType.String },
            { "ContactEmail", PropertyType.String },
            { "FirstName", PropertyType.String },
            { "LastName", PropertyType.String },
            { "ContactPhone", PropertyType.String },
            { "Budget", PropertyType.Double },
            { "StartDate", PropertyType.Date },
            { "EndDate", PropertyType.Date },
            { "PersonaName", PropertyType.String },
        };

        /// <summary>
        /// Precedence for evaluating type coercion.
        /// Numeric types are all deserialized as double.
        /// </summary>
        private static readonly PropertyType[] TypePrecedence = new[]
        {
            PropertyType.Bool, 
            PropertyType.Double,
            PropertyType.Date,
            PropertyType.Guid, 
            PropertyType.String
        };

        /// <summary>Serialize a raw entity to Json.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A JSON string.</returns>
        public static string SerializeToJson(IRawEntity entity)
        {
            return SerializeToJson(entity, new EntitySerializationFilter());
        }

        /// <summary>Serialize a raw entity to Json.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entityFilter">An entity filter.</param>
        /// <returns>A JSON string.</returns>
        public static string SerializeToJson(IRawEntity entity, IEntityFilter entityFilter)
        {
            IDictionary<string, object> serializationDictionary = new Dictionary<string, object>();
            var entityId = string.Empty;
            var entityCategory = string.Empty;

            if (entityFilter == null)
            {
                entityFilter = new EntitySerializationFilter();
            }

            try
            {
                entityId = entity.ExternalEntityId.Value.SerializationValue;
                entityCategory = entity.EntityCategory;

                // Convert the entity into a Dictionary<string, object> containing the members to be json serialized.
                if (entityFilter.EntityQueries.CheckPropertyRegexMatch(entity))
                {
                    // First-class IEntity properties
                    AddInterfacePropertiesToSerializationDictionary(entity, ref serializationDictionary);

                    // Serialize the property bags
                    AddPropertyBagsToSerializationDictionary(entity, entityFilter, ref serializationDictionary);

                    // Serialize the associations
                    AddAssociationsToSerializationDictionary(entity, ref serializationDictionary, entityFilter);

                    // Now json serialize the filtered Dictionary<string, object>
                    var jsonString = SerializeObjectToJson(serializationDictionary);
                    return jsonString;
                }
            }
            catch (Exception e)
            {
                string msg = "Unable to serialize entity {0}, {1}: {2}: {3}".FormatInvariant(entityCategory, entityId, e.Message, e.StackTrace);

                LogManager.Log(LogLevels.Error, msg);
                throw new ArgumentException(msg, "entity");
            }

            return string.Empty;
        }

        /// <summary>Realize the wrapped entity from JSON. Used for construction, shouldn't be needed publically.</summary>
        /// <param name="jsonEntity">The JSON object from which to deserialize.</param>
        /// <param name="entityFilter">
        /// IEntityFilter indicating what elements of the json entity should be included in the
        /// deserialized entity and which should be ignored.
        /// </param>
        /// <returns>An IEntity object.</returns>
        public static IRawEntity DeserializeEntity(string jsonEntity, IEntityFilter entityFilter)
        {
            try
            {
                IRawEntity entity = new Entity();

                if (string.IsNullOrEmpty(jsonEntity))
                {
                    return entity;
                }

                if (entityFilter == null)
                {
                    entityFilter = new EntityDeserializationFilter();
                }

                // Deserialize to a collection of JProperty elements
                var jsonPropertyList = ((JContainer)DeserializeObjectFromJson<object>(jsonEntity)).Select(p => (JProperty)p).ToList();

                // First handle IEntity properties
                DeserializeInterfaceProperties(ref entity, jsonPropertyList);

                // Deserialize the property bags
                DeserializePropertiesFromJson(ref entity, jsonPropertyList, entityFilter);

                // Deserialize Associations collection.
                DeserializeAssociationsFromJson(ref entity, jsonPropertyList, entityFilter);

                return entity;
            }
            catch (Exception e)
            {
                string msg = "Unable to deserialize entity json {0}, {1}: {2}".FormatInvariant(jsonEntity, e.Message, e.StackTrace);

                LogManager.Log(LogLevels.Error, msg);
                throw new ArgumentException(msg, "jsonEntity");
            }
        }

        /// <summary>Realize the wrapped entity from JSON. Used for construction, shouldn't be needed publically.</summary>
        /// <param name="jsonEntity">The JSON object from which to deserialize.</param>
        /// <returns>An IEntity object.</returns>
        public static IRawEntity DeserializeEntity(string jsonEntity)
        {
            return DeserializeEntity(jsonEntity, new EntityDeserializationFilter());
        }

        /// <summary>Initializes a new CampaignEntity instance from the provided JSON.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        /// <returns>The deserialized CampaignEntity.</returns>
        public static CampaignEntity DeserializeCampaignEntity(EntityId externalEntityId, string jsonEntity)
        {
            var rawEntity = DeserializeEntity(jsonEntity);
            return new CampaignEntity(externalEntityId, rawEntity);
        }

        /// <summary>Initializes a new CompanyEntity instance from the provided JSON.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        /// <returns>The deserialized CompanyEntity.</returns>
        public static CompanyEntity DeserializeCompanyEntity(EntityId externalEntityId, string jsonEntity)
        {
            var rawEntity = DeserializeEntity(jsonEntity);
            return new CompanyEntity(externalEntityId, rawEntity);
        }

        /// <summary>Initializes a new CreativeEntity instance from the provided JSON.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        /// <returns>The deserialized CreativeEntity.</returns>
        public static CreativeEntity DeserializeCreativeEntity(EntityId externalEntityId, string jsonEntity)
        {
            var rawEntity = DeserializeEntity(jsonEntity);
            return new CreativeEntity(externalEntityId, rawEntity);
        }

        /// <summary>Initializes a new PartnerEntity instance from the provided JSON.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        /// <returns>The deserialized PartnerEntity.</returns>
        public static PartnerEntity DeserializePartnerEntity(EntityId externalEntityId, string jsonEntity)
        {
            var rawEntity = DeserializeEntity(jsonEntity);
            return new PartnerEntity(externalEntityId, rawEntity);
        }

        /// <summary>Initializes a new UserEntity instance from the provided JSON.</summary>
        /// <param name="externalEntityId">The external entity id to assign the entity.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        /// <returns>The deserialized UserEntity.</returns>
        public static UserEntity DeserializeUserEntity(EntityId externalEntityId, string jsonEntity)
        {
            var rawEntity = DeserializeEntity(jsonEntity);
            return new UserEntity(externalEntityId, rawEntity);
        }

        /// <summary>Deserialize the IEntity interface properties from a Json Dictionary.</summary>
        /// <param name="entity">The entity to which properties are added.</param>
        /// <param name="jsonPropertyList">The top level entity properties.</param>
        private static void DeserializeInterfaceProperties(ref IRawEntity entity, IList<JProperty> jsonPropertyList)
        {
            // Get the IEntity property names
            var interfacePropertyNames =
                typeof(IRawEntity).GetProperties().Where(p => p.PropertyType == typeof(EntityProperty)).Select(p => p.Name);

            // Get the IEntity properties from the container
            var interfaceProperties = jsonPropertyList.Where(p => interfacePropertyNames.Contains(p.Name)).ToList();
            
            // There should not be more than one element with any IEntity property name
            if (interfaceProperties.Select(p => p.Name).Distinct().Count() != interfaceProperties.Count())
            {
                throw new ArgumentException("Duplicate name in collection.", "jsonPropertyList");
            }

            foreach (var property in interfaceProperties)
            {
                var entityProperty = DeserializeEntityPropertyFromJson(property, PropertyFilter.Default);
                typeof(IRawEntity).GetProperty(property.Name).SetValue(entity, entityProperty, null);
            }
        }

        /// <summary>Deserialize the property collections from a Json Dictionary.</summary>
        /// <param name="entity">The entity to which the properties are added.</param>
        /// <param name="jsonPropertyList">The top level entity properties.</param>
        /// <param name="entityFilter">The entity filter.</param>
        private static void DeserializePropertiesFromJson(ref IRawEntity entity, IList<JProperty> jsonPropertyList, IEntityFilter entityFilter)
        {
            // By default we ignore extended properties from the client when deserializing, but we allow it internally.
            var propertyBagMap = BuildPropertyBagMap(entityFilter);

            foreach (var bag in propertyBagMap)
            {
                var propertyBagName = bag.Key;
                var propertyFilter = bag.Value;

                // There should be one property bag with this mapping name and it should reference a collection
                // of properties (which will be a JTokenType.Object)
                var propertyBag = jsonPropertyList.SingleOrDefault(p => p.Name == propertyBagName);
                if (propertyBag == null || propertyBag.Value.Type != JTokenType.Object)
                {
                    continue;
                }

                // This should be a collection of JProperty objects.
                var properties = propertyBag.Value.Select(p => (JProperty)p);

                // Deserialize each property to an entity property and add it to the
                // entities properties
                foreach (var property in properties)
                {
                    entity.Properties.Add(DeserializeEntityPropertyFromJson(property, propertyFilter));
                }
            }
        }

        /// <summary>Deserialize the associations from a Json Dictionary.</summary>
        /// <param name="entity">The entity to which the associations are added.</param>
        /// <param name="jsonPropertyList">The json dictionary.</param>
        /// <param name="entityFilter">entity filter object.</param>
        private static void DeserializeAssociationsFromJson(ref IRawEntity entity, IList<JProperty> jsonPropertyList, IEntityFilter entityFilter)
        {
            // By default we ignore associations from the client but we allow it internally
            if (!entityFilter.IncludeAssociations)
            {
                return;
            }

            // There should be one property bag with this mapping name and it should reference a collection
            // of properties (which will be a JTokenType.Object)
            var associationBag = jsonPropertyList.SingleOrDefault(p => p.Name == "Associations");
            if (associationBag == null || associationBag.Value.Type != JTokenType.Object)
            {
                return;
            }

            // This should be a collection of JProperty objects.
            var associations = associationBag.Value.Select(p => (JProperty)p).ToList();

            // Deserialize each association
            foreach (var associationItem in associations)
            {
                var simpleAssociation = TryDeserializeAssociation(associationItem.Name, associationItem);

                // If this is a simple association just add it do the entity
                if (simpleAssociation != null)
                {
                    entity.Associations.Add(simpleAssociation);
                    continue;
                }

                // Otherwise it is a collection of associations to the same external types of entities
                var associationCollection = TryDeserializeAssociationCollection(associationItem.Name, associationItem);
                if (associationCollection == null)
                {
                    var msg = "Attempt to deserialize associations failed: {0}".FormatInvariant(
                            string.Join(",", associations.Select(p => p.Name)));
                    LogManager.Log(LogLevels.Error, msg);
                    throw new ArgumentException(msg, "jsonPropertyList");
                }

                entity.Associations.Add(associationCollection);
            }
        }

        /// <summary>
        /// Initialize an instance of EntityProperty from a Json name/value. It determines the property type 
        /// from the propertyName if possible. If that doesn't work it optimistically attempts 
        /// to coerce to the allowed types.
        /// </summary>
        /// <param name="propertyJson">A property value of unknown type.</param>
        /// <param name="propertyFilter">The PropertyFilter value to set on the property.</param>
        /// <returns>The deserialized EntityProperty</returns>
        private static EntityProperty DeserializeEntityPropertyFromJson(JProperty propertyJson, PropertyFilter propertyFilter)
        {
            var propertyTypes = TypePrecedence;

            // If it's a schematized property make sure we enforce type
            if (NameToTypeMap.ContainsKey(propertyJson.Name))
            {
                // TODO: Introduce a name to serialized name map as well...change ExternalEntityId to Id?
                propertyTypes = new[] { NameToTypeMap[propertyJson.Name] };
            }

            var value = TryBuildPropertyValueFromDeserializedValue(propertyTypes, propertyJson);
            if (value == null)
            {
                var errorMessage =
                    "Could not coerce json to property value. {0}".FormatInvariant(propertyJson.Name);
                throw new ArgumentException(errorMessage, "propertyJson");
            }

            return new EntityProperty(propertyJson.Name, value, propertyFilter);
        }

        /// <summary>Try to build a PropertyValue from a json value.</summary>
        /// <param name="propertyTypes">The property types to attempt in order of precedence.</param>
        /// <param name="propertyJson">The property json.</param>
        /// <returns>A PropertyValue or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static PropertyValue TryBuildPropertyValueFromDeserializedValue(PropertyType[] propertyTypes, JProperty propertyJson)
        {
            try
            {
                // If the property value is a complex object deserialize it as a json string.
                if ((propertyJson.Value.Type == JTokenType.Object || propertyJson.Value.Type == JTokenType.Array)
                    && propertyTypes.Contains(PropertyType.String))
                {
                    return new PropertyValue(PropertyType.String, propertyJson.Value.ToString(Formatting.None));
                }

                // Otherwise it's a native type value
                var valueJson = (JValue)propertyJson.Value;

                // Get the string serialized form of the json value
                var serializedStringValue = valueJson.Value.ToString();
                
                // NaN should fail
                if ((valueJson.Value is double && double.IsNaN((double)valueJson))
                    || string.Compare(serializedStringValue, "NaN", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return null;
                }

                // null should fail
                if (valueJson.Type == JTokenType.Null)
                {
                    return null;
                }

                var allowedTypePrecendence = propertyTypes;

                // If it's a quoted string and that's one of our options, evaluate precedence
                // just among types that come across as quoted strings.
                if (valueJson.Type == JTokenType.String && propertyTypes.Contains(PropertyType.String))
                {
                    var stringTypePrecedence = new[]
                    {
                        PropertyType.Date,
                        PropertyType.Guid, 
                        PropertyType.String
                    };

                    allowedTypePrecendence = propertyTypes.Intersect(stringTypePrecedence).ToArray();
                }

                // Interpret it according to our precedence
                foreach (var propertyType in allowedTypePrecendence)
                {
                    try
                    {
                        return new PropertyValue(propertyType, serializedStringValue);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>Try to deserialize an object as an Association.</summary>
        /// <param name="externalName">This is the ExternalName of the associations in the collection.</param>
        /// <param name="candidateAssociation">The candidate association.</param>
        /// <returns>The association or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static Association TryDeserializeAssociation(string externalName, JProperty candidateAssociation)
        {
            try
            {
                // If this isn't an object at the outer json level it's not a simple association
                if (candidateAssociation.Value.Type != JTokenType.Object)
                {
                    return null;
                }

                var associationJson = candidateAssociation.Value.ToString(Formatting.None);
                var association = DeserializeObjectFromJson<Association>(associationJson);

                if (association == null)
                {
                    return null;
                }

                association.ExternalName = externalName;
                return association;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Try to deserialize an object as an array of Associations.</summary>
        /// <param name="externalName">This is the ExternalName of the associations in the collection.</param>
        /// <param name="candidateAssociationCollection">The candidate association collection.</param>
        /// <returns>The Association[] or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static IEnumerable<Association> TryDeserializeAssociationCollection(string externalName, JProperty candidateAssociationCollection)
        {
            try
            {
                // If this isn't an array at the outer json level it's not an association collection
                if (candidateAssociationCollection.Value.Type != JTokenType.Array)
                {
                    return null;
                }

                var associationCollectionJson = candidateAssociationCollection.Value.ToString(Formatting.None);
                var associationCollection = DeserializeObjectFromJson<Association[]>(associationCollectionJson);

                if (associationCollection == null)
                {
                    return null;
                }

                // The associations will not have ExternalName since that is provided
                // at the collection level, so add it.
                foreach (var association in associationCollection)
                {
                    association.ExternalName = externalName;
                }

                return associationCollection;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// CheckPropertyRegexMatch walks the list of properties and checks against any value the user may have entered as a regex evaluator.
        /// If a property name match is found, a regex compare checks to see if the property value matches the regex. 
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="queryValues">Dictionary where the key is a property name and the value is a regex expression</param>
        /// <returns>true if regex matches, false if no match </returns>
        private static bool CheckPropertyRegexMatch(IRawEntity entity, Dictionary<string, string> queryValues)
        {
            if (queryValues == null)
            {
                return true;
            }

            IEnumerable<string> propertyNames =
                typeof(IRawEntity).GetProperties().Where(p => p.PropertyType == typeof(EntityProperty)).Select(p => p.Name);

            foreach (var name in propertyNames)
            {
                var entityProperty = (EntityProperty)typeof(IRawEntity).GetProperty(name).GetValue(entity, null);
                if (entityProperty != null)
                {
                    if (queryValues.Count > 0)
                    {
                        // perform regex checking on this property
                        foreach (string key in queryValues.Keys)
                        {
                            if (key == entityProperty.Name.ToLower(CultureInfo.InvariantCulture))
                            {
                                if (Regex.IsMatch(entityProperty.Value.SerializationValue, queryValues[key]) == false)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // we are here if there is no match for the queryValue key to property name, retrun default of true
            return true;
        }

        /// <summary>Add the IEntity interface properties of an entity to the serialization dictionary.</summary>
        /// <param name="entity">The entity being serialized.</param>
        /// <param name="serializationDictionary">The target output dictionary.</param>
        private static void AddInterfacePropertiesToSerializationDictionary(IRawEntity entity, ref IDictionary<string, object> serializationDictionary)
        {
            IEnumerable<string> propertyNames =
                typeof(IRawEntity).GetProperties().Where(p => p.PropertyType == typeof(EntityProperty)).Select(p => p.Name);

            foreach (var name in propertyNames)
            {
                var entityProperty = (EntityProperty)typeof(IRawEntity).GetProperty(name).GetValue(entity, null);
                if (entityProperty != null)
                {
                    serializationDictionary.Add(entityProperty.Name, ConvertPropertyValueToDictionaryValue(entityProperty.Name, entityProperty.Value));
                }
            }
        }

        /// <summary>Add a property bag to the serialization dictionary.</summary>
        /// <param name="entity">The entity to serialize.</param>
        /// <param name="entityFilter">The entity filter.</param>
        /// <param name="serializationDictionary">The target output dictionary.</param>
        private static void AddPropertyBagsToSerializationDictionary(IRawEntity entity, IEntityFilter entityFilter, ref IDictionary<string, object> serializationDictionary)
        {
            var entityProperties = entity.Properties;

            // We do not allow multiple properties with the same name
            if (entityProperties.Select(p => p.Name).Distinct().Count() != entityProperties.Count)
            {
                var msg = "Attempt to Json serialize multiple properties of same name: {0}".FormatInvariant(
                        string.Join(",", entityProperties.Select(p => p.Name)));
                throw new ArgumentException(msg, "entity");
            }

            // By default we do not send system and extended properties to the client,
            // but we allow it internally.
            var propertyBagMap = BuildPropertyBagMap(entityFilter);
            foreach (var bag in propertyBagMap)
            {
                var propertyFilter = bag.Value;
                var propertyBagName = bag.Key;

                // Create a dictionary containing properties filtered by 'filter'
                var filteredProperties = entityProperties.Where(p => p.Filter == propertyFilter);
                var propertiesDictionary = filteredProperties.ToDictionary(
                    property => property.Name, property => ConvertPropertyValueToDictionaryValue(property.Name, property.Value));

                // Add the properties to the serialization dictionary as a single value
                // with the propertyBagName as key.
                serializationDictionary.Add(propertyBagName, propertiesDictionary);
            }
        }

        /// <summary>Add the Associations of an entity to the serialization dictionary.</summary>
        /// <param name="entity">The entity being serialized.</param>
        /// <param name="serializationDictionary">The target serialization dictionary.</param>
        /// <param name="entityFilter">Entity filter object</param>
        private static void AddAssociationsToSerializationDictionary(IRawEntity entity, ref IDictionary<string, object> serializationDictionary, IEntityFilter entityFilter)
        {
            // Don't include associations if there aren't any or they are filtered
            if (entity.Associations.Count == 0 || !entityFilter.IncludeAssociations)
            {
                return;
            }

            var accumulatedAssociations = new Dictionary<string, object>();

            // Group associations by external name
            var assocationGroups = entity.Associations.GroupBy(a => a.ExternalName);
            foreach (var group in assocationGroups)
            {
                object dictionaryValue;

                if (group.Count() == 1)
                {
                    // This is a simple association
                    dictionaryValue = group.First().SerializeToJsonCollectionFragmentDictionary();
                }
                else
                {
                    // This is an array of associations to entities of the same external type
                    // that will share a collection name
                    dictionaryValue = group.Select(a => a.SerializeToJsonCollectionFragmentDictionary()).ToArray();
                }

                FilterAndAccumulateAssociations(ref accumulatedAssociations, group.Key, dictionaryValue, entityFilter);
            }

            // Add the accumulated associations and association collections to the serialization dictionary
            // as a single value with the "Associations" key.
            serializationDictionary.Add("Associations", accumulatedAssociations);
        }

        /// <summary>
        /// Filter and accumulate associations. If the queryValue has an entry for Association, it must match the regex
        /// from the queryValue.
        /// </summary>
        /// <param name="accumulationDictionary">The target output serialization dictionary.</param>
        /// <param name="associationExternalName">The association external name to use as the dictionary key.</param>
        /// <param name="dictionaryObject">The association(s) object to add to serialization dictionary.</param>
        /// <param name="filter">Entity filter object</param>
        private static void FilterAndAccumulateAssociations(ref Dictionary<string, object> accumulationDictionary, string associationExternalName, object dictionaryObject, IEntityFilter filter)
        {
            var queryValues = filter.EntityQueries.QueryStringParams;

            // TODO: reconcile this with the approach for properties and move to EntityActivityQuery
            // perform regex checking if entityQueries exist
            if (queryValues != null && queryValues.ContainsKey("associations"))
            {
                if (Regex.IsMatch(associationExternalName, queryValues["associations"]))
                {
                    accumulationDictionary.Add(associationExternalName, dictionaryObject);
                }
            }
            else
            {
                accumulationDictionary.Add(associationExternalName, dictionaryObject);
            }
        }

        /// <summary>Convert a named PropertyValue object to an object for the output serialization dictionary.</summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The PropertyValue object.</param>
        /// <returns>The serialization object.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1801", Justification = "Temporary")]
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Optimistic coerce pattern.")]
        private static object ConvertPropertyValueToDictionaryValue(string propertyName, PropertyValue value)
        {
            // These types will be json serialized as strings using the serialization value
            if (value.DynamicType == PropertyType.Date 
                || value.DynamicType == PropertyType.Guid 
                || value.DynamicType == PropertyType.Binary)
            {
                return value.SerializationValue;
            }

            // Numeric and bool types should be left as native values and handled
            // according to the json serializer's default behavior (unquoted values)
            if (value.DynamicType == PropertyType.Bool
                || value.DynamicType == PropertyType.Double
                || value.DynamicType == PropertyType.Int32
                || value.DynamicType == PropertyType.Int64)
            {
                return value.DynamicValue;
            }

            // Attempt a rudimentary check for types of json we expect to see.
            // If it's not json return as quoted string.
            if (string.IsNullOrEmpty(value.SerializationValue))
            {
                return value.SerializationValue;
            }

            // Early exit. If it doesn't look like a json object or array it will
            // be treated as a quoted string.
            var trimmedStringValue = value.SerializationValue.Trim();
            if (!trimmedStringValue.StartsWith("{", StringComparison.Ordinal)
                && !trimmedStringValue.StartsWith("[", StringComparison.Ordinal))
            {
                return value.SerializationValue;
            }

            // If the property value can be deserialized as a json object return it as a Dictionary<string, object>
            try
            {
                var jsonObject = DeserializeObjectFromJson<Dictionary<string, object>>((string)value.DynamicValue);
                if (jsonObject != null)
                {
                    return jsonObject;
                }
            }
            catch (Exception)
            {
            }

            // If the property value can be deserialized as a json array return it as a object[]
            try
            {
                // If the property value can be deserialized as a json array return it as a object[]
                var jsonObject = DeserializeObjectFromJson<object[]>((string)value.DynamicValue);
                if (jsonObject != null)
                {
                    return jsonObject;
                }
            }
            catch (Exception)
            {
            }

            // Otherwise return the serialization value which will be treated as a quoted string
            // by the json serializer
            return value.SerializationValue;
        }

        /// <summary>
        /// Build a map of json property bag names to entity PropertyFilter values
        /// used to determine what properties to include in serilaization/deserializization
        /// </summary>
        /// <param name="entityFilter">The IEntityFilter</param>
        /// <returns>The map.</returns>
        private static Dictionary<string, PropertyFilter> BuildPropertyBagMap(IEntityFilter entityFilter)
        {
            var propertyBagMap = new Dictionary<string, PropertyFilter> { { "Properties", PropertyFilter.Default }, };

            if (entityFilter.IncludeSystemProperties)
            {
                propertyBagMap.Add("SystemProperties", PropertyFilter.System);
            }

            if (entityFilter.IncludeExtendedProperties)
            {
                propertyBagMap.Add("ExtendedProperties", PropertyFilter.Extended);
            }

            return propertyBagMap;
        }

        /// <summary>Perform the actual json deserialization.</summary>
        /// <typeparam name="T">The deserialization target type.</typeparam>
        /// <param name="jsonSource">The source json string.</param>
        /// <returns>The deserialized object.</returns>
        private static T DeserializeObjectFromJson<T>(string jsonSource)
        {
            var settings = new JsonSerializerSettings();
            ////settings.NullValueHandling = NullValueHandling.Ignore;
            return JsonConvert.DeserializeObject<T>(jsonSource, settings);
        }

        /// <summary>Perform the actual json serialization.</summary>
        /// <param name="serializationObject">The object to serialize.</param>
        /// <returns>The json string.</returns>
        private static string SerializeObjectToJson(object serializationObject)
        {
            var settings = new JsonSerializerSettings();
            ////settings.NullValueHandling = NullValueHandling.Ignore;
            var jsonString = JsonConvert.SerializeObject(serializationObject, Formatting.None, settings);
            return jsonString;
        }
    }
}
