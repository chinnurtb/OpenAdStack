//-----------------------------------------------------------------------
// <copyright file="AzureEntityDataStore.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DataAccessLayer;
using Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace ConcreteDataStore
{
    /// <summary>Entity data store for Azure Storage Table.</summary>
    internal class AzureEntityDataStore : IEntityStore
    {
        /// <summary>Azure partition key name</summary>
        private const string PartitionKeyName = "PartitionKey";

        /// <summary>Azure row key name</summary>
        private const string RowKeyName = "RowKey";

        /// <summary>Azure timestamp name</summary>
        private const string TimestampName = "Timestamp";

        /// <summary>IRawEntity schema version name</summary>
        private const string SchemaVersionName = "SchemaVersion";

        /// <summary>Initializes a new instance of the <see cref="AzureEntityDataStore"/> class.</summary>
        /// <param name="azureEntityTableConnectionString">The azure entity table connection string.</param>
        public AzureEntityDataStore(string azureEntityTableConnectionString)
        {
            var account = CloudStorageAccount.Parse(azureEntityTableConnectionString);
            this.TableClient = account.CreateCloudTableClient();

            // Specify a retry backoff of 10 seconds max instead of using default values.
            this.TableClient.RetryPolicy = RetryPolicies.RetryExponential(
                3, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 3));

            this.CurrentEntitySchema = new ConcreteEntitySchema();
        }

        /// <summary>Delegate definition for a query expression against a table.</summary>
        /// <param name="tableServiceContext">Context to execute the query against.</param>
        /// <returns>Returns a collection of AzureSerializationEntity</returns>
        internal delegate IEnumerable<AzureSerializationEntity> TableQuery(TableServiceContext tableServiceContext);

        /// <summary>Gets Azure table client.</summary>
        internal CloudTableClient TableClient { get; private set; }

        /// <summary>Gets the current entity schema object.</summary>
        internal ConcreteEntitySchema CurrentEntitySchema { get; private set; }

        /// <summary>Do the setup work in a datastore needed to add a new company (does not save a company entity).</summary>
        /// <param name="externalName">External name of company.</param>
        /// <returns>A partial storage key with any key fields bound to the new company populated.</returns>
        public IStorageKey SetupNewCompany(string externalName)
        {
            var tableName = BuildTableName(externalName);
            if (!this.CreateTable(tableName))
            {
                return null;
            }

            return new AzureStorageKey(null, tableName, null, null);
        }

        /// <summary>Save an entity in the entity store.</summary>
        /// <param name="requestContext">Context information for the request.</param>
        /// <param name="entity">The generic entity.</param>
        /// <param name="isUpdate">True if this is an update of an existing entity.</param>
        /// <returns>True if successful.</returns>
        public bool SaveEntity(RequestContext requestContext, IRawEntity entity, bool isUpdate = false)
        {
            var key = (AzureStorageKey)entity.Key;
            var table = key.TableName;

            TableQuery query = context =>
            {
                context.AddObject(table, CreateWrappedEntity(entity));

                // The storage level entities are immutable, which means logical updates are always writing
                // the entire entity. Any desired merge behavior, or moderated access to the properties and
                // associations must be implemented in the layers above the physical storage layer.
                context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);

                // return an empty list to conform to the lambda expression
                return new List<AzureSerializationEntity>();
            };

            var result = this.ExecuteTableSave(query);

            return result != null;
        }

        /// <summary>Get the user entities with a given UserId.</summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyKey">The key for the company holding the user.</param>
        /// <returns>The user entities.</returns>
        public HashSet<IRawEntity> GetUserEntitiesByUserId(string userId, IStorageKey companyKey)
        {
            var storageKey = (AzureStorageKey)companyKey;

            var entitiesResult = this.ExecuteTableQuery(
                context => context.CreateQuery<UserSerializationEntity>(storageKey.TableName)
                .Where(e => e.UserId == userId));

            var entities = entitiesResult == null 
                ? new List<AzureSerializationEntity>()
                : entitiesResult.ToList();

            foreach (var entity in entities)
            {
                entity.WrappedEntity.Key = BuildKeyFromSerializationEntity(entity, storageKey);
            }

            return new HashSet<IRawEntity>(entities.Select(e => e.WrappedEntity));
        }

        /// <summary>Remove and entity from entity store.</summary>
        /// <param name="storageKey">The storage key of the entity to remove.</param>
        public void RemoveEntity(IStorageKey storageKey)
        {
            var key = (AzureStorageKey)storageKey;

            TableQuery query = context =>
            {
                var entities = context.CreateQuery<AzureSerializationEntity>(key.TableName)
                    .Where(e => e.PartitionKey == key.Partition && e.RowKey == (string)key.RowId).ToList();

                var entity = entities.FirstOrDefault();

                if (entity != null)
                {
                    context.DeleteObject(entity);
                    context.SaveChanges();
                }

                // return an empty list to conform to the lambda expression
                return new List<AzureSerializationEntity>();
            };

            this.ExecuteTableSave(query);
        }

        /// <summary>Get a raw entity given a storage key.</summary>
        /// <param name="requestContext">
        /// Context information for the request.</param><param name="key">An IStorageKey key.</param>
        /// <returns>An entity that is normalized but not serialized.</returns>
        public IRawEntity GetEntityByKey(RequestContext requestContext, IStorageKey key)
        {
            var storageKey = (AzureStorageKey)key;

            var result = this.ExecuteTableQuery(
                context => context.CreateQuery<AzureSerializationEntity>(storageKey.TableName)
                .Where(e => e.PartitionKey == storageKey.Partition && e.RowKey == (string)storageKey.RowId));

            if (result == null)
            {
                return null;
            }

            var entity = result.SingleOrDefault();

            if (entity == null)
            {
                return null;
            }

            // Make sure the Key property is populated.
            entity.WrappedEntity.Key = storageKey;
            return entity.WrappedEntity;
        }

        /// <summary>Get the schema version of the entity from the collection of property elements.</summary>
        /// <param name="propertyElements">Collection of property elements from the OData.</param>
        /// <returns>The entity schema version.</returns>
        internal static int ExtractSchemaVersion(IEnumerable<ODataElement> propertyElements)
        {
            var schemaVersion = 0;
            var schemaVersionElement = propertyElements.SingleOrDefault(p => p.ODataName == SchemaVersionName);
            if (schemaVersionElement != null && schemaVersionElement.HasValue)
            {
                schemaVersion = new PropertyValue(
                    ODataSerializer.GetPropertyType(schemaVersionElement), 
                    schemaVersionElement.ODataValue)
                    .DynamicValue;
            }

            return schemaVersion;
        }

        /// <summary>Build a valid Azure table name.</summary>
        /// <param name="externalName">External name of the company.</param>
        /// <returns>The table name.</returns>
        internal static string BuildTableName(string externalName)
        {
            if (externalName == null)
            {
                externalName = string.Empty;
            }

            var allowedFirstCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var allowedCharacters = allowedFirstCharacters + "0123456789";
            Func<char, bool> isAllowed = charToTest => allowedCharacters.Contains(charToTest);

            // Table names cannot begin with a number (probably an xml element name requirement)
            // Must be at least 3 characters long and must be alphanumeric otherwise
            var sb = new StringBuilder("TAB");
            foreach (var c in externalName)
            {
                sb.Append(isAllowed(c) 
                    ? "{0}".FormatInvariant(c)
                    : "{0:X4}".FormatInvariant(Convert.ToInt32(c)));
            }

            // Add a randomizing component. Truncate to make sure some space is reserved for it.
            // Limit the final name to 63 characters in length
            var tableName = sb.ToString().Left(50);
            return (tableName + new EntityId()).Left(63);
        }

        /// <summary>Create an Azure table.</summary>
        /// <param name="tableName">The table name.</param>
        /// <returns>True if successful.</returns>
        internal bool CreateTable(string tableName)
        {
            try
            {
                this.TableClient.CreateTable(tableName);
                return true;
            }
            catch (StorageClientException ex)
            {
                LogManager.Log(LogLevels.Error, string.Format(CultureInfo.InvariantCulture, "Status Code {0}: {1}", ex.StatusCode, ex.Message));
            }

            return false;
        }

        /// <summary>Serialize the AzureRawEntity object to the Atom/OData content element</summary>
        /// <param name="serializationEntity">The AzureRawEntity source object.</param>
        /// <param name="data">The entity OData xml to populate.</param>
        internal void SerializeEntity(AzureSerializationEntity serializationEntity, XElement data)
        {
            // Set the current entity schema version
            serializationEntity.WrappedEntity.SchemaVersion = this.CurrentEntitySchema.CurrentSchemaVersion;

            var odataContent = ODataSerializer.GetODataContent(data);

            // Serialize the top-level IEntity elements that are of type EntityProperty
            foreach (var entityProperty in serializationEntity.WrappedEntity.InterfaceProperties)
            {
                ODataSerializer.SerializeEntityProperty(odataContent, entityProperty);
            }

            foreach (var entityProperty in serializationEntity.WrappedEntity.Properties)
            {
                // Encode the property names for Azure before serializing to odata
                var encodedProperty = new EntityProperty(entityProperty);
                encodedProperty.Name = AzureNameEncoder.EncodeAzureName(encodedProperty.Name);

                // Serialize to odata xml
                ODataSerializer.SerializeEntityProperty(odataContent, encodedProperty);
            }

            // Encode the external association fields for Azure before serializing to odata
            var associationGroups = AssociationComparer
                .BuildEntityAssociationGroups(serializationEntity.WrappedEntity.Associations);
            foreach (var associationGroup in associationGroups)
            {
                ODataSerializer.SerializeAssociationGroup(odataContent, associationGroup.Key, associationGroup.Value);
            }
        }

        /// <summary>Deserialize the Atom/OData content into our AzureRawEntity object.</summary>
        /// <param name="serializationEntity">The AzureRawEntity object to populate.</param>
        /// <param name="odataXml">The entity OData xml from the storage response.</param>
        internal void DeserializeEntity(AzureSerializationEntity serializationEntity, XElement odataXml)
        {
            // First parse the properties into raw oData elements.
            var odataPropertyElements = ODataSerializer.GetODataElements(odataXml).Where(p => p.HasValue).ToList();

            // Schema version of the entity is needed to inform deserialization
            var schemaVersion = ExtractSchemaVersion(odataPropertyElements);

            // Create a precedence map of deserializer methods.
            var deserializerMap = new List<DeserializerMapItem>
                {
                    new DeserializerMapItem(IsStorageMetadata, odataElement => { }),
                    new DeserializerMapItem(IsIEntityProperty, odataElement => DeserializeEntityInterfaceProperty(serializationEntity, odataElement)),
                    new DeserializerMapItem(ODataSerializer.IsEntityProperty, odataElement => this.DeserializeEntityProperty(serializationEntity, odataElement, schemaVersion)),
                    new DeserializerMapItem(ODataSerializer.IsAssociation, odataElement => this.DeserializeAssociation(serializationEntity, odataElement, schemaVersion)),
                };
            
            foreach (var odataPropertyElement in odataPropertyElements)
            {
                if (!DeserializeOdataElement(deserializerMap, odataPropertyElement))
                {
                    // Logged but not currently treated as an error
                    var msg =
                        "Odata element {0} could not be deserialized.".FormatInvariant(odataPropertyElement.ODataName);
                    LogManager.Log(LogLevels.Warning, msg);
                }
            }
        }

        /// <summary>Test if the odata element name is storage metadata (not deserialized).</summary>
        /// <param name="odataName">The oData name.</param>
        /// <returns>True if this is storage metadata.</returns>
        private static bool IsStorageMetadata(string odataName)
        {
            // Partition, Row, & Timestamp do not get deserialized.
            return odataName == PartitionKeyName || odataName == RowKeyName || odataName == TimestampName;
        }

        /// <summary>Determine if the property is a first-class IEntity member of type EntityProperty</summary>
        /// <param name="odataName">The oData name.</param>
        /// <returns>True if it should be included as a first-class IEntity member</returns>
        private static bool IsIEntityProperty(string odataName)
        {
            var matches =
                typeof(IRawEntity).GetProperties().Where(p => p.PropertyType == typeof(EntityProperty) && p.Name == odataName).ToList();

            // If this happens something is bad wrong.
            if (matches.Count() > 1)
            {
                throw new DataAccessException("Duplicate IRawEntity interface level property.");
            }

            return matches.Count() == 1;
        }

        /// <summary>Create an object compatible with Azure storage client.</summary>
        /// <param name="entity">The entity to wrap.</param>
        /// <returns>The the wrapper object.</returns>
        private static AzureSerializationEntity CreateWrappedEntity(IRawEntity entity)
        {
            var key = (AzureStorageKey)entity.Key;
            var entityWrapper = new AzureSerializationEntity(entity);
            entityWrapper.PartitionKey = key.Partition;
            entityWrapper.RowKey = key.RowId;
            return entityWrapper;
        }

        /// <summary>Build an entity key from the serialization entity and the filter key.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="filterKey">The filter key.</param>
        /// <returns>The entity key.</returns>
        private static AzureStorageKey BuildKeyFromSerializationEntity(AzureSerializationEntity entity, AzureStorageKey filterKey)
        {
            return new AzureStorageKey(
                filterKey.StorageAccountName,
                filterKey.TableName,
                entity.PartitionKey,
                entity.RowKey,
                entity.WrappedEntity.LocalVersion,
                null);
        }

        /// <summary>Deserialize an odata element according to the type of data it contains.</summary>
        /// <param name="deserializerMap">The precedence map of deserializers.</param>
        /// <param name="odataPropertyElement">The odata element to deserialize.</param>
        /// <returns>True if a deserializer was found and executed.</returns>
        private static bool DeserializeOdataElement(IEnumerable<DeserializerMapItem> deserializerMap, ODataElement odataPropertyElement)
        {
            foreach (var deserializer in deserializerMap)
            {
                if (deserializer.CheckExecute(odataPropertyElement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Deserialize the odata element as an IEntity interface property.</summary>
        /// <param name="serializationEntity">The entity to build out with deserialized data.</param>
        /// <param name="odataPropertyElement">The odata element to deserialize.</param>
        private static void DeserializeEntityInterfaceProperty(
            AzureSerializationEntity serializationEntity, ODataElement odataPropertyElement)
        {
            serializationEntity.WrappedEntity.InterfaceProperties.Add(
                new EntityProperty
                {
                    Name = odataPropertyElement.ODataName,
                    Value = new PropertyValue(ODataSerializer.GetPropertyType(odataPropertyElement), odataPropertyElement.ODataValue)
                });
        }

        /// <summary>Deserialize the odata element as an entity property.</summary>
        /// <param name="serializationEntity">The entity to build out with deserialized data.</param>
        /// <param name="odataPropertyElement">The odata element to deserialize.</param>
        /// <param name="schemaVersion">The schema version of the entity being deserialized.</param>
        private void DeserializeEntityProperty(AzureSerializationEntity serializationEntity, ODataElement odataPropertyElement, int schemaVersion)
        {
            var odataName = new ODataPropertyName(odataPropertyElement.ODataName);

            // Unencode the name if supported in this version
            // as the last step before populating the entity
            var isNameEncoded = this.CurrentEntitySchema.CheckSchemaFeature(
                    schemaVersion, EntitySchemaFeatureId.NameEncoding);

            var entityProperty = new EntityProperty
            {
                Name = isNameEncoded ? AzureNameEncoder.UnencodeAzureName(odataName.PropertyName) : odataName.PropertyName,
                Value = new PropertyValue(ODataSerializer.GetPropertyType(odataPropertyElement), odataPropertyElement.ODataValue),
                IsBlobRef = odataName.IsBlobRef(),
                Filter = odataName.Filter
            };

            serializationEntity.WrappedEntity.Properties.Add(entityProperty);
        }

        /// <summary>Deserialize the odata element as an association.</summary>
        /// <param name="serializationEntity">The entity to build out with deserialized data.</param>
        /// <param name="odataPropertyElement">The odata element to deserialize.</param>
        /// <param name="schemaVersion">The schema version of the entity being deserialized.</param>
        private void DeserializeAssociation(
            AzureSerializationEntity serializationEntity, ODataElement odataPropertyElement, int schemaVersion)
        {
            // If supported for the schema version deserialize as an association group. Otherwise use legacy
            // deserialization
            if (this.CurrentEntitySchema.CheckSchemaFeature(schemaVersion, EntitySchemaFeatureId.AssociationGroups))
            {
                var associationGroup = ODataSerializer.DeserializeAssociationGroup(odataPropertyElement).ToList();

                // Unencode the external names as the last step before populating the entity
                foreach (var association in associationGroup)
                {
                    association.TargetExternalType = AzureNameEncoder.UnencodeAzureName(association.TargetExternalType);
                    association.ExternalName = AzureNameEncoder.UnencodeAzureName(association.ExternalName);
                }

                serializationEntity.WrappedEntity.Associations.Add(associationGroup);
            }
            else
            {
                // Add simple association to the associations collection
                if (ODataSerializer.IsSimpleAssociation(odataPropertyElement))
                {
                    serializationEntity.WrappedEntity.Associations.Add(ODataSerializer.DeserializeAssociation(odataPropertyElement));
                    return;
                }

                // This is a collection of properties or associations
                ODataSerializer.DeserializeODataToCollection(serializationEntity.WrappedEntity, odataPropertyElement);
            }
        }

        /// <summary>Execute a query expression against table storage.</summary>
        /// <param name="query">The query expression.</param>
        /// <param name="write">True to write.</param>
        /// <returns>A collection of IRawEntity.</returns>
        private IEnumerable<AzureSerializationEntity> ExecuteTableQuery(
            TableQuery query, bool write = false)
        {
            try
            {
                // Get the data context.
                // Tell it to ignore anything in the entity that it doesn't know how to map to our raw entity object.
                // Add our own deserialization handler so we can map the entity to our raw entity object.
                var tableServiceContext = this.TableClient.GetDataServiceContext();
                tableServiceContext.IgnoreMissingProperties = true;
                if (write)
                {
                    tableServiceContext.WritingEntity += (sender, args) => this.SerializeEntity(
                        args.Entity as AzureSerializationEntity, 
                        args.Data);
                }
                else
                {
                    tableServiceContext.ReadingEntity += (sender, args) => this.DeserializeEntity(
                        args.Entity as AzureSerializationEntity, 
                        args.Data);
                }

                // Realize the query result.
                return query(tableServiceContext).ToList();
            }
            catch (DataServiceRequestException ex)
            {
                LogManager.Log(LogLevels.Error, "AzureEntityStore Table Service Error: {0}".FormatInvariant(ex.ToString()));
                return null;
            }
            catch (DataServiceQueryException ex)
            {
                LogManager.Log(LogLevels.Error, "Status Code {0}: {1}".FormatInvariant(ex.Response.StatusCode, ex.ToString()));
                return null;
            }
            catch (StorageClientException ex)
            {
                LogManager.Log(LogLevels.Error, "Status Code {0}: {1}".FormatInvariant(ex.StatusCode, ex.ToString()));
                return null;
            }
        }

        /// <summary>Execute a query expression against table storage.</summary>
        /// <param name="query">The query expression.</param>
        /// <returns>A collection of IRawEntity.</returns>
        private IEnumerable<AzureSerializationEntity> ExecuteTableSave(TableQuery query)
        {
            return this.ExecuteTableQuery(query, true);
        }

        /// <summary>Temporary class to use to with table storage client until we have a user index.</summary>
        private class UserSerializationEntity : AzureSerializationEntity
        {
            /// <summary>Gets or sets UserId.</summary>
            public string UserId { get; set; }
        }

        /// <summary>Class to encapsulate a deserializer map item.</summary>
        private class DeserializerMapItem
        {
            /// <summary>Initializes a new instance of the <see cref="DeserializerMapItem"/> class.</summary>
            /// <param name="elementMatchTest">A function that tests whether to use mapped deserializer.</param>
            /// <param name="deserializer">A deserializer method.</param>
            public DeserializerMapItem(Func<string, bool> elementMatchTest, Action<ODataElement> deserializer)
            {
                this.CheckElementMatch = elementMatchTest;
                this.DeserializerFunction = deserializer;
            }

            /// <summary>Gets or sets a method to check if a deserializer matches the type of odata element.</summary>
            private Func<string, bool> CheckElementMatch { get; set; }

            /// <summary>Gets or sets a method to deserialize an odata element.</summary>
            private Action<ODataElement> DeserializerFunction { get; set; }

            /// <summary>Execute the deserializer if it matches the odata element.</summary>
            /// <param name="odataPropertyElement">The odata element to deserialize.</param>
            /// <returns>True if deserializer matched and was executed.</returns>
            public bool CheckExecute(ODataElement odataPropertyElement)
            {
                // If this deserializer doesn't match the element type do nothing
                if (!this.CheckElementMatch(odataPropertyElement.ODataName))
                {
                    return false;
                }

                // Otherwise execute the deserializer
                this.DeserializerFunction(odataPropertyElement);
                return true;
            }
        }
    }
}
