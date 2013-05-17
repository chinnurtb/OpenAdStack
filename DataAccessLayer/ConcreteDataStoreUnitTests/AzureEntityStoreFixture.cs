//-----------------------------------------------------------------------
// <copyright file="AzureEntityStoreFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for Azure entity data store.</summary>
    [TestClass]
    public class AzureEntityStoreFixture
    {
        /// <summary>Azure entity data store for testing.</summary>
        private AzureEntityDataStore entityStore;

        /// <summary>A property name for testing</summary>
        private string propertyName = "TheStuff";

        /// <summary>ExternalType of an 'Agency' company.</summary>
        private string agencyExternalType = "Company.Agency";

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.entityStore = new AzureEntityDataStore("UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1:10002/");
        }

        /// <summary>Test injection constructor</summary>
        [TestMethod]
        public void InjectionConstructor()
        {
            Assert.IsNotNull(this.entityStore.TableClient);
            Assert.IsNotNull(this.entityStore.CurrentEntitySchema);
        }

        /// <summary>Serialize and round-trip deserialize a string property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataStringProperty()
        {
            this.AssertSinglePropertySerialization(PropertyType.String, this.propertyName, "theValue");
        }

        /// <summary>Serialize and round-trip deserialize an Int32 property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataInt32Property()
        {
            this.AssertSinglePropertySerialization(PropertyType.Int32, this.propertyName, int.MaxValue);
        }

        /// <summary>Serialize and round-trip deserialize an Int64 property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataInt64Property()
        {
            this.AssertSinglePropertySerialization(PropertyType.Int64, this.propertyName, long.MaxValue);
        }

        /// <summary>Serialize and round-trip deserialize an double property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataDoubleProperty()
        {
            this.AssertSinglePropertySerialization(PropertyType.Double, this.propertyName, double.MaxValue);
        }

        /// <summary>Serialize and round-trip deserialize an boolean property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataBoolProperty()
        {
            this.AssertSinglePropertySerialization(PropertyType.Bool, this.propertyName, false);
        }

        /// <summary>Serialize and round-trip deserialize an date-time property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataDateProperty()
        {
            this.AssertSinglePropertySerialization(PropertyType.Date, this.propertyName, DateTime.MaxValue);
        }

        /// <summary>Serialize and round-trip deserialize an date-time property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataGuidProperty()
        {
            this.AssertSinglePropertySerialization(PropertyType.Guid, this.propertyName, Guid.NewGuid());
        }

        /// <summary>Serialize and round-trip deserialize an date-time property to Azure OData xml.</summary>
        [TestMethod]
        public void SerializeODataBinaryProperty()
        {
            var propertyValue = new byte[] { 0x0, 0x1, 0xF, 0xFF };
            this.AssertSinglePropertySerialization(PropertyType.Binary, this.propertyName, propertyValue);
        }

        /// <summary>Handle property Serialization.</summary>
        [TestMethod]
        public void SerializeSimpleEntityPropertyMembers()
        {
            var entity = new Entity
                {
                    ExternalEntityId = new EntityId(),
                    EntityCategory = CompanyEntity.CompanyEntityCategory,
                    CreateDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    ExternalName = "CompanyFoo",
                    ExternalType = this.agencyExternalType,
                    LocalVersion = 1,
                    Key = new AzureStorageKey("acc", "tab", "par", new EntityId())
                };

            entity.Properties.Add(new EntityProperty { Name = "Foo", Value = "Bar" });
            entity.Properties.Add(new EntityProperty { Name = "FooId", Value = new EntityId() });

            var roundTripEntity = this.RoundTripSerialize(entity);

            // Schema version will be added by default
            Assert.AreEqual(8, roundTripEntity.InterfaceProperties.Count);
            
            Assert.AreEqual(2, roundTripEntity.Properties.Count);
            Assert.AreEqual(0, roundTripEntity.Associations.Count);

            // Assert interface members
            Assert.AreEqual(entity.ExternalEntityId, roundTripEntity.ExternalEntityId);
            Assert.AreEqual(entity.EntityCategory, roundTripEntity.EntityCategory);
            Assert.AreEqual(entity.CreateDate, roundTripEntity.CreateDate);
            Assert.AreEqual(entity.LastModifiedDate, roundTripEntity.LastModifiedDate);
            Assert.AreEqual(entity.ExternalName, roundTripEntity.ExternalName);
            Assert.AreEqual(entity.ExternalType, roundTripEntity.ExternalType);
            Assert.AreEqual(entity.LocalVersion, roundTripEntity.LocalVersion);

            // Assert property bag
            var getByName = new Func<string, IRawEntity, EntityProperty>((x, y) => y.Properties.Single(p => p.Name == x));
            Assert.AreEqual(getByName("Foo", entity), getByName("Foo", roundTripEntity));
            Assert.AreEqual(getByName("FooId", entity), getByName("FooId", roundTripEntity));

            // The key should not be serialized
            Assert.IsNull(roundTripEntity.Key);
        }

        /// <summary>Handle system property serialization.</summary>
        [TestMethod]
        public void SerializeSystemProperties()
        {
            var entity = new Entity { ExternalEntityId = new EntityId() };

            // Add property to system properties collection
            entity.Properties.Add(new EntityProperty
                {
                    Name = "SomeProperty",
                    Value = "SomeValue",
                    Filter = PropertyFilter.System
                });

            var roundTripEntity = this.RoundTripSerialize(entity);
            Assert.AreEqual(1, roundTripEntity.Properties.Count);

            // Assert system properties
            var sysProp = roundTripEntity.Properties.Single(p => p.Name == "SomeProperty");
            Assert.AreEqual("SomeValue", (string)sysProp.Value);
            Assert.AreEqual(PropertyFilter.System, sysProp.Filter);
        }

        /// <summary>Handle extended property serialization.</summary>
        [TestMethod]
        public void SerializeExtendedProperties()
        {
            var entity = new Entity { ExternalEntityId = new EntityId() };

            // Add extended property to properties collection
            entity.Properties.Add(new EntityProperty
                {
                    Name = "SomeProperty", Value = "SomeValue", Filter = PropertyFilter.Extended
                });

            var roundTripEntity = this.RoundTripSerialize(entity);
            Assert.AreEqual(1, roundTripEntity.Properties.Count);

            // Assert extended properties
            var extProp = roundTripEntity.Properties.Single(p => p.Name == "SomeProperty");
            Assert.AreEqual("SomeValue", (string)extProp.Value);
            Assert.AreEqual(PropertyFilter.Extended, extProp.Filter);
        }

        /// <summary>SchemaVersion is set.</summary>
        [TestMethod]
        public void SerializeEntityCurrentSchemaVersion()
        {
            var entity = new Entity { ExternalEntityId = new EntityId() };
            var roundtripEntity = this.RoundTripSerialize(entity);
            Assert.AreEqual(1, (int)roundtripEntity.SchemaVersion);
        }

        /// <summary>
        /// Non-interface property names and association names
        /// are encoded and decoded according to the latest schema version.
        /// </summary>
        [TestMethod]
        public void RoundtripEncodedNames()
        {
            var entity = new Entity { ExternalEntityId = new EntityId() };

            // Add extended property to properties collection
            entity.Properties.Add(new EntityProperty
            {
                Name = "\u0b83$ome_Property\u4e00\u0304",
                Value = "SomeValue",
                Filter = PropertyFilter.Default
            });

            entity.Properties.Add(new EntityProperty
            {
                Name = "$ome_SystemProperty",
                Value = "SomeValue",
                Filter = PropertyFilter.System
            });

            entity.Properties.Add(new EntityProperty
            {
                Name = "$ome_ExtendedProperty",
                Value = "SomeValue",
                Filter = PropertyFilter.Extended
            });

            entity.Associations.Add(new Association
                {
                    AssociationType = AssociationType.Child,
                    TargetEntityCategory = "Company",
                    TargetEntityId = new EntityId(),
                    TargetExternalType = "\u0b83ome_type",
                    ExternalName = "$ome_name"
                });

            var serializationEntity = new AzureSerializationEntity(entity);
            var odataOnWriteXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            this.SerializeEntity(serializationEntity, odataOnWriteXml);
            
            var deserializationEntity = new AzureSerializationEntity();
            this.DeserializeEntity(deserializationEntity, odataOnWriteXml);
            var roundtripEntity = deserializationEntity.WrappedEntity;

            Assert.IsTrue(roundtripEntity.Properties.Any(p => p.Name == "\u0b83$ome_Property\u4e00\u0304"));
            Assert.IsTrue(roundtripEntity.Properties.Any(p => p.Name == "$ome_SystemProperty"));
            Assert.IsTrue(roundtripEntity.Properties.Any(p => p.Name == "$ome_ExtendedProperty"));
            Assert.IsTrue(roundtripEntity.Associations.Any(a => a.TargetExternalType == "\u0b83ome_type"));
            Assert.IsTrue(roundtripEntity.Associations.Any(a => a.ExternalName == "$ome_name"));
        }

        /// <summary>Entity deserialization is handled differently depending on schema version.</summary>
        [TestMethod]
        public void DeserializeSchemaVersion()
        {
            // Get an OData shell
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);

            // Add schema version
            var entity = new Entity { SchemaVersion = 1 };
            ODataSerializer.SerializeEntityProperty(odataContent, entity.SchemaVersion);

            // Need a bare entity that conforms to the serialization interface
            var serializationEntity = new AzureSerializationEntity(new Entity());
            this.DeserializeEntity(serializationEntity, odataXml);
            var deserializedEntity = serializationEntity.WrappedEntity;

            Assert.AreEqual(entity.SchemaVersion, deserializedEntity.SchemaVersion);
        }

        /// <summary>Property names are not decoded for SchemaVersion=0 or not present.</summary>
        [TestMethod]
        public void DeserializeLegacySchemaVersion0()
        {
            // A property name that looks like an encoded name but should not get treated like
            // one because the schema is not version 1 or later;
            var legacyPropertyNameNotEncoded = "_005FSomeProperty";

            // Get an OData shell
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);

            // Add the property to the odata
            ODataSerializer.SerializeEntityProperty(odataContent, new EntityProperty(legacyPropertyNameNotEncoded, "somevalue"));

            // Need a bare entity that conforms to the serialization interface
            var serializationEntity = new AzureSerializationEntity(new Entity());
            this.DeserializeEntity(serializationEntity, odataXml);
            var deserializedEntity = serializationEntity.WrappedEntity;

            Assert.IsNull(deserializedEntity.SchemaVersion);
            Assert.IsTrue(deserializedEntity.Properties.Any(p => p.Name == legacyPropertyNameNotEncoded));
        }

        /// <summary>Missing schema version should return zero.</summary>
        [TestMethod]
        public void ExtractSchemaVersionMissing()
        {
            var propertyElements = new List<ODataElement>();
            var schemaVersion = AzureEntityDataStore.ExtractSchemaVersion(propertyElements);
            Assert.AreEqual(0, schemaVersion);
        }

        /// <summary>No value for schema version should return zero.</summary>
        [TestMethod]
        public void ExtractSchemaVersionNoValue()
        {
            var propertyElements = new List<ODataElement>
                {
                    new ODataElement { HasValue = false, ODataName = "SchemaVersion" },
                    new ODataElement { HasValue = true, ODataName = "SomeOther", ODataValue = "other" }
                };
            var schemaVersion = AzureEntityDataStore.ExtractSchemaVersion(propertyElements);
            Assert.AreEqual(0, schemaVersion);
        }

        /// <summary>Success path for schema version should.</summary>
        [TestMethod]
        public void ExtractSchemaVersionFound()
        {
            var propertyElements = new List<ODataElement>
                {
                    new ODataElement { HasValue = true, ODataName = "SchemaVersion", ODataValue = "1", ODataType = "Edm.Int32" },
                    new ODataElement { HasValue = true, ODataName = "SomeOther", ODataValue = "other" }
                };
            var schemaVersion = AzureEntityDataStore.ExtractSchemaVersion(propertyElements);
            Assert.AreEqual(1, schemaVersion);
        }

        /// <summary>
        /// Associations are grouped uniquely by 
        /// ExternalName, TargetEntityCategory, TargetExternalType, and AssociationType
        /// </summary>
        [TestMethod]
        public void BuildEntityAssociationGroups()
        {
            // These are contrived for uniqueness - not sensibility
            // There should be 17 groups (16 groups of 1, and 1 group of three)
            var associations = new List<Association>
                {
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Company", TargetExternalType = "Agency", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Company", TargetExternalType = "Agency", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Company", TargetExternalType = "Advertiser", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Company", TargetExternalType = "Advertiser", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Campaign", TargetExternalType = "Big", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Campaign", TargetExternalType = "Big", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Company", TargetExternalType = "Agency", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Company", TargetExternalType = "Agency", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Company", TargetExternalType = "Advertiser", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Company", TargetExternalType = "Advertiser", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Campaign", TargetExternalType = "Big", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Campaign", TargetExternalType = "Big", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Child, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Doo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Goo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Goo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Goo", TargetEntityCategory = "Campaign", TargetExternalType = "Small", AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                };

            var groups = AssociationComparer.BuildEntityAssociationGroups(associations);
            Assert.AreEqual(17, groups.Count());
            Assert.AreEqual(3, groups.Single(g => g.Key.ExternalName == "Goo").Value.Count());
        }

        /// <summary>
        /// GroupEntityAssociations returns an empty collection when the association collection
        /// is empty.
        /// </summary>
        [TestMethod]
        public void BuildEntityAssociationGroupsEmpty()
        {
            var groups = AssociationComparer.BuildEntityAssociationGroups(new List<Association>()).ToList();
            Assert.AreEqual(0, groups.Count());
        }

        /// <summary>
        /// GroupEntityAssociations supports partially specified associations.
        /// </summary>
        [TestMethod]
        public void BuildEntityAssociationGroupsPartiallySpecified()
        {
            // Each of these should be a unique groups
            var associations = new List<Association>
                {
                    new Association { ExternalName = "Foo", TargetEntityId = new EntityId() },
                    new Association { TargetEntityCategory = "Company", TargetEntityId = new EntityId() },
                    new Association { TargetExternalType = "Advertiser", TargetEntityId = new EntityId() },
                    new Association { AssociationType = AssociationType.Relationship, TargetEntityId = new EntityId() },
                    new Association { ExternalName = "Foo", TargetEntityCategory = "Campaign", TargetEntityId = new EntityId() },
                };

            var groups = AssociationComparer.BuildEntityAssociationGroups(associations);
            Assert.AreEqual(5, groups.Count());
        }

        /// <summary>
        /// External Association name elements are encoded.
        /// </summary>
        [TestMethod]
        public void BuildEntityAssociationGroupsExternalNamesEncoded()
        {
            // Each of these should be a unique groups
            var associations = new List<Association>
                {
                    new Association { ExternalName = "F oo", TargetExternalType = "F ooType", TargetEntityId = new EntityId() },
                };

            var associationKey = AssociationComparer.BuildEntityAssociationGroups(associations).Single().Key;
            Assert.AreEqual("F{0}0020oo".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter), associationKey.ExternalName);
            Assert.AreEqual("F{0}0020ooType".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter), associationKey.TargetExternalType);
        }

        /// <summary>Correctly deserialized association group with encoding.</summary>
        [TestMethod]
        public void DeserializeAssociationGroup()
        {
            // Get an OData shell
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);

            var associationGroupKey = new Association
                {
                    AssociationType = AssociationType.Relationship, 
                    TargetEntityCategory = "Company",
                    TargetExternalType = "{0}0024ome{0}005FAdvertiser".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter),
                    ExternalName = "{0}0024ome{0}005FAdvertisers".FormatInvariant(AzureNameEncoder.AzureEscapeCharacter), 
                };

            var ids = new[] { new EntityId(1), new EntityId(2) };

            // Add SchemaVersion
            var entity = new Entity { SchemaVersion = 1 };
            ODataSerializer.SerializeEntityProperty(odataContent, entity.SchemaVersion);

            // Add the associations to the odata
            ODataSerializer.SerializeAssociationGroup(odataContent, associationGroupKey, ids);

            // Need a bare entity that conforms to the serialization interface
            var serializationEntity = new AzureSerializationEntity(new Entity());
            this.DeserializeEntity(serializationEntity, odataXml);
            var deserializedEntity = serializationEntity.WrappedEntity;

            Assert.AreEqual(1, (int)deserializedEntity.SchemaVersion);
            Assert.AreEqual(2, deserializedEntity.Associations.Count);
            Assert.AreEqual(2, deserializedEntity.Associations.Count(a => a.ExternalName == "$ome_Advertisers"));
            Assert.AreEqual(2, deserializedEntity.Associations.Count(a => a.TargetExternalType == "$ome_Advertiser"));
            Assert.AreEqual(1, deserializedEntity.Associations.Count(a => a.TargetEntityId == new EntityId(1)));
            Assert.AreEqual(1, deserializedEntity.Associations.Count(a => a.TargetEntityId == new EntityId(2)));
        }

        /// <summary>Correctly deserialized legacy association</summary>
        [TestMethod]
        public void DeserializedLegacyAssociationSchemaVersion0()
        {
            // Get an OData shell
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);

            // Add the associations to the odata
            var odataName = "c_ptr_Relationship_Company_Agency_Agencies";
            var odataValue = "00000000000000000000000000000001||Agency||Agencies}{00000000000000000000000000000002||Agency||Agencies";
            var odataElement = new XElement(ODataSerializer.ODataNamespace + odataName, odataValue);
            odataElement.Add(new XAttribute(ODataSerializer.OdataTypeName, ODataSerializer.OdataDefaultTypeName));
            odataContent.Add(odataElement);

            odataName = "v_ptr_Relationship_Company_Agency_ParentCompany";
            odataValue = "00000000000000000000000000000003||Agency||ParentCompany";
            odataElement = new XElement(ODataSerializer.ODataNamespace + odataName, odataValue);
            odataElement.Add(new XAttribute(ODataSerializer.OdataTypeName, ODataSerializer.OdataDefaultTypeName));
            odataContent.Add(odataElement);

            // Need a bare entity that conforms to the serialization interface
            var serializationEntity = new AzureSerializationEntity(new Entity());
            this.DeserializeEntity(serializationEntity, odataXml);
            var deserializedEntity = serializationEntity.WrappedEntity;

            Assert.IsNull(deserializedEntity.SchemaVersion);
            Assert.AreEqual(3, deserializedEntity.Associations.Count);
            Assert.AreEqual(2, deserializedEntity.Associations.Count(a => a.ExternalName == "Agencies"));
            Assert.AreEqual(1, deserializedEntity.Associations.Count(a => a.ExternalName == "ParentCompany"));
            Assert.IsTrue(deserializedEntity.Associations.Any(a => a.TargetEntityId == new EntityId(1)));
            Assert.IsTrue(deserializedEntity.Associations.Any(a => a.TargetEntityId == new EntityId(2)));
            Assert.IsTrue(deserializedEntity.Associations.Any(a => a.TargetEntityId == new EntityId(3)));
        }

        /// <summary>Handle associations group serialization is default.</summary>
        [TestMethod]
        public void RoundtripSerializeAssociationGroupsByDefault()
        {
            var entity = new Entity { ExternalEntityId = new EntityId() };
            var association1 = new Association
            {
                TargetEntityId = new EntityId(1),
                TargetEntityCategory = CompanyEntity.CompanyEntityCategory,
                TargetExternalType = "Agency",
                ExternalName = "Agencies",
                AssociationType = AssociationType.Relationship
            };

            // Set this up with an association collection and a stand-alone association
            var association2 = new Association(association1) { TargetEntityId = new EntityId(2) };
 
            // Stand-alone association
            var association3 = new Association(association1)
                {
                    TargetEntityId = new EntityId(3),
                    ExternalName = "ParentCompany"
                };
            entity.Associations.Add(association1);
            entity.Associations.Add(association2);
            entity.Associations.Add(association3);

            var rawEntity = new AzureSerializationEntity(entity);

            // Get an OData shell like the one that will be passed to the serialize method
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            this.SerializeEntity(rawEntity, odataXml);

            var elements = ODataSerializer.GetODataElements(odataXml).ToList();
            var firstGroup =
                elements.Single(e => e.ODataName == "c_ptr_Relationship_Company_Agency_Agencies").ODataValue;
            var secondGroup =
                elements.Single(e => e.ODataName == "c_ptr_Relationship_Company_Agency_ParentCompany").ODataValue;
            var firstGroupIds = JsonConvert.DeserializeObject<string[]>(firstGroup);
            var secondGroupIds = JsonConvert.DeserializeObject<string[]>(secondGroup);
            Assert.AreEqual(2, firstGroupIds.Count());
            Assert.AreEqual(1, secondGroupIds.Count());
            Assert.IsTrue(firstGroupIds.Any(a => new EntityId(a) == new EntityId(1)));
            Assert.IsTrue(firstGroupIds.Any(a => new EntityId(a) == new EntityId(2)));
            Assert.IsTrue(secondGroupIds.Any(a => new EntityId(a) == new EntityId(3)));
        }

        /// <summary>Build a valid Azure table name based on the external company name.</summary>
        [TestMethod]
        public void BuildTableName()
        {
            var externalName = "somename";
            var tableName1 = AzureEntityDataStore.BuildTableName(externalName);
            var tableName2 = AzureEntityDataStore.BuildTableName(externalName);
            Assert.IsTrue(tableName1.Contains(externalName));
            Assert.IsTrue(tableName2.Contains(externalName));
            Assert.AreNotEqual(tableName1, tableName2);
        }

        /// <summary>Build a valid Azure table name when input name is null.</summary>
        [TestMethod]
        public void BuildTableNameNull()
        {
            var tableName = AzureEntityDataStore.BuildTableName(null);
            Assert.IsFalse(string.IsNullOrEmpty(tableName));
            Assert.IsTrue(tableName.Length > 3);
        }

        /// <summary>Build a valid Azure table name when input name is empty.</summary>
        [TestMethod]
        public void BuildTableNameEmpty()
        {
            var tableName = AzureEntityDataStore.BuildTableName(string.Empty);
            Assert.IsFalse(string.IsNullOrEmpty(tableName));
            Assert.IsTrue(tableName.Length > 3);
        }

        /// <summary>Build a valid Azure table name when input name would result in too long a name.</summary>
        [TestMethod]
        public void BuildTableNameTruncates()
        {
            var tableName1 = AzureEntityDataStore.BuildTableName(new string('a', 100));
            var tableName2 = AzureEntityDataStore.BuildTableName(new string('a', 100));
            Assert.AreEqual(63, tableName1.Length);
            Assert.AreEqual(63, tableName2.Length);
            Assert.AreNotEqual(tableName1, tableName2);
        }

        /// <summary>Build a valid Azure table name when company name is not valid azure.</summary>
        [TestMethod]
        public void BuildTableNameWithEncoding()
        {
            var externalName = "\u0b83some_name";
            var tableName = AzureEntityDataStore.BuildTableName(externalName);
            Assert.IsTrue(tableName.Contains("0B83some005Fname"));
        }

        /// <summary>Assert that a single property is correctly serialized/deserialized.</summary>
        /// <param name="propertyType">The expected PropertyType.</param>
        /// <param name="expectedPropertyName">The expected property name.</param>
        /// <param name="propertyValue">The expected property value.</param>
        /// <typeparam name="T">The native type of the property value.</typeparam>
        private void AssertSinglePropertySerialization<T>(PropertyType propertyType, string expectedPropertyName, T propertyValue)
        {
            var entity = new Entity();
            entity.Properties.Add(new EntityProperty { Name = expectedPropertyName, Value = new PropertyValue(propertyType, propertyValue) });

            // Round-trip serialize/deserialize the entity
            var roundTripEntity = this.RoundTripSerialize(entity);

            // There should only be one property
            Assert.AreEqual(1, roundTripEntity.Properties.Count);

            // Verify property value and type
            var property = roundTripEntity.Properties.Single(p => p.Name == expectedPropertyName);
            Assert.AreEqual(propertyType, property.Value.DynamicType);

            if (typeof(T) != typeof(byte[]))
            {
                Assert.AreEqual(propertyValue, (T)property.Value.DynamicValue);
                return;
            }

            // handle the array comparison
            var arrActual = property.Value.DynamicValue as byte[];
            var arrExpected = propertyValue as byte[];
            Assert.IsNotNull(arrActual);
            Assert.IsNotNull(arrExpected);
            Assert.IsTrue(arrActual.SequenceEqual(arrExpected));
        }

        /// <summary>Round-trip serialize/deserialize an entity through Azure OData xml.</summary>
        /// <param name="entity">The raw entity.</param>
        /// <returns>The round-trip raw entity.</returns>
        private IRawEntity RoundTripSerialize(IRawEntity entity)
        {
            var rawEntity = new AzureSerializationEntity(entity);

            // Get an OData shell like the one that will be passed to the serialize method
            var odataOnWriteXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));

            // Serialize the property
            this.SerializeEntity(rawEntity, odataOnWriteXml);

            // Verify by round-trip deserializing
            var roundTripWrapperEntity = new AzureSerializationEntity();
            this.DeserializeEntity(roundTripWrapperEntity, odataOnWriteXml);

            return roundTripWrapperEntity.WrappedEntity;
        }

        /// <summary>Call AzureEntityDataStore.SerializeEntity with no blob store.</summary>
        /// <param name="serializationEntity">The AzureRawEntity source object.</param>
        /// <param name="data">The entity OData xml to populate.</param>
        private void SerializeEntity(AzureSerializationEntity serializationEntity, XElement data)
        {
            this.entityStore.SerializeEntity(serializationEntity, data);
        }

        /// <summary>Call AzureEntityDataStore.DeserializeEntity with no blob store.</summary>
        /// <param name="serializationEntity">The AzureRawEntity object to populate.</param>
        /// <param name="odataXml">The entity OData xml from the storage response.</param>
        private void DeserializeEntity(AzureSerializationEntity serializationEntity, XElement odataXml)
        {
            this.entityStore.DeserializeEntity(serializationEntity, odataXml);
        }
    }
}
