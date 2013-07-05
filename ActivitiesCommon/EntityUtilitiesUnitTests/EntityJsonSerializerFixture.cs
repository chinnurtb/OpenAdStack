// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityJsonSerializerFixture.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using DataAccessLayerUnitTests;
using Diagnostics;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino.Mocks;

namespace EntityUtilitiesUnitTests
{
    /// <summary>Test fixture for EntityJsonSerializer.</summary>
    [TestClass]
    public class EntityJsonSerializerFixture
    {
        /// <summary>Per test initialization</summary>
        [TestInitialize]
        public void InitializeTests()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateStub<ILogger>() });
        }

        /// <summary>We should throw if a schematized property is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithBlankSchematizedProperty()
        {
            // Create Json input with schematized property names whose type will be coerced
            var sourceJson =
                  @"{
                        ""LocalVersion"":
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw if a non-schematized property is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithBlankNonSchematizedProperty()
        {
            // Create Json input with schematized property names whose type will be coerced
            var sourceJson =
                  @"{
                        ""Properties"":
                        {
                            ""SomeProperty"":
                        }
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw on a schematized null.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithSchematizedNullProperty()
        {
            // Create Json input with schematized property names whose type will be coerced
            var sourceJson =
                  @"{
                        ""LocalVersion"":null
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw on a non-schematized null.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithNonSchematizedNullProperty()
        {
            // Create Json input with non-schematized property with null value
            var sourceJson =
                  @"{
                        ""Properties"":
                        {
                            ""SomeProperty"":null
                        }
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw if a non-schematized property has a value of NaN.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithNonSchematizedNaNProperty()
        {
            // Create Json input with non-schematized property with NaN value
            var sourceJson =
                  @"{
                        ""Properties"":
                        {
                            ""NotANumber"":NaN
                        }
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw if a schematized double has a value of NaN.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithSchematizedNaNProperty()
        {
            // Create Json input with non-schematized property with NaN value
            var sourceJson =
                  @"{
                        ""Properties"":
                        {
                            ""Budget"":NaN
                        }
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>We should throw if a property has a value of "NaN".</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeserializeJsonWithNaNStringProperty()
        {
            // Create Json input with non-schematized property with NaN value
            var sourceJson =
                  @"{
                        ""Properties"":
                        {
                            ""SomeProperty"":""NaN""
                        }
                    }";

            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>Empty and null string properties should be treated as empty string.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithEmptyStringProperties()
        {
            // Create Json input with schematized property names whose type will be coerced
            var jsonFormat =
                  @"{{
                        ""EntityCategory"": ""{0}"",
                        ""ExternalEntityId"":""{1}"",
                        ""ExternalName"":"""",
                        ""Properties"":
                        {{
                            ""UserId"":"""",
                            ""SomeOtherString"":"""",
                        }}
                    }}";

            var entityCategory = TestEntity.CategoryName;
            var sourceJson = jsonFormat.FormatInvariant(entityCategory, new EntityId().ToString());

            // Deserialize to an entity
            var testEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(sourceJson));
            Assert.AreEqual(string.Empty, (string)testEntity.ExternalName);
            Assert.AreEqual(string.Empty, (string)testEntity.TryGetPropertyValueByName("UserId"));
            Assert.AreEqual(string.Empty, (string)testEntity.TryGetPropertyValueByName("SomeOtherString"));

            // Do a complete round-trip of the deserialized entity and compare the entities
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(true, false, true));
            var roundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(roundTripJson));
            AssertEntitiesEqual(testEntity, roundTripEntity, null);
        }

        /// <summary>Test we can construct an entity from a json object with properties of known types.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithProperties()
        {
            // Create Json input with schematized property names whose type will be coerced
            // as well as non-schematized properties for each type the json deserializer will produce.
            // ExternalName - schematized string
            // ExternalEntityId - schematized guid
            // LocalVersion - schematized Int32
            // Budget - schematized Double
            // Int32Name - non-schematized Int32 -> double
            // Int64Name - non-schematized Int64 -> double
            var jsonFormat =
                  @"{{
                        ""ExternalName"": ""{0}"",
                        ""EntityCategory"": ""{1}"",
                        ""ExternalEntityId"": ""{2}"",
                        ""LocalVersion"":{3},
                        ""LastModifiedDate"":""{4}"",
                        ""Properties"":
                        {{
                            ""Budget"":{5},
                            ""Int32Name"":{6},
                            ""Int64Name"":{7},
                            ""DoubleName"":{8},
                            ""BoolTrueName"":{9},
                            ""BoolFalseName"":{10},
                            ""GuidName"":""{11}"",
                            ""DateName"":""{12}"",
                            ""StringName"":""{13}"",
                            ""NumberAsStringName"":""{14}""
                        }}
                    }}";

            var externalNameValue = "fooname";
            var entityCategory = TestEntity.CategoryName;
            var externalEntityId = new EntityId().ToString();
            var localVersion = int.MinValue;
            var lastModifiedDate = "2012-10-02T19:36:25.0220653Z";
            var budget = 10000.00;
            var int32Value = int.MaxValue;
            var int64Value = (long)int.MinValue - 1;
            var doubleValue = 1.1;
            var boolTrueValue = "true"; // json serializes lower-case
            var boolFalseValue = "false"; // json serializes lower-case
            var guidValue = new EntityId().ToString();
            var dateValue = "2012-10-02T19:36:25.0220653Z";
            var stringValue = "a string, a palpable string";
            var numberAsStringValue = "12345";
            var sourceJson = jsonFormat.FormatInvariant(
                externalNameValue,
                entityCategory,
                externalEntityId,
                localVersion,
                lastModifiedDate,
                budget,
                int32Value,
                int64Value,
                doubleValue,
                boolTrueValue,
                boolFalseValue,
                guidValue,
                dateValue,
                stringValue,
                numberAsStringValue);

            // Deserialize to an entity
            var testEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(sourceJson));

            // Schematized values should have type preserved
            Assert.AreEqual(externalNameValue, (string)testEntity.ExternalName);
            Assert.AreEqual(PropertyType.String, testEntity.ExternalName.Value.DynamicType);
            Assert.AreEqual(externalEntityId, (string)(EntityId)testEntity.ExternalEntityId);
            Assert.AreEqual(PropertyType.Guid, testEntity.ExternalEntityId.Value.DynamicType);
            Assert.AreEqual(localVersion, (int)testEntity.LocalVersion);
            Assert.AreEqual(PropertyType.Int32, testEntity.LocalVersion.Value.DynamicType);
            Assert.AreEqual(budget, (double)testEntity.GetPropertyValueByName("Budget"));
            Assert.AreEqual(PropertyType.Double, testEntity.GetPropertyValueByName("Budget").DynamicType);
            
            // All non-schematized numbers should be treated as doubles
            Assert.AreEqual(int32Value, (double)testEntity.GetPropertyValueByName("Int32Name"));
            Assert.AreEqual(PropertyType.Double, testEntity.GetPropertyValueByName("Int32Name").DynamicType);
            Assert.AreEqual(int64Value, (double)testEntity.GetPropertyValueByName("Int64Name"));
            Assert.AreEqual(PropertyType.Double, testEntity.GetPropertyValueByName("Int64Name").DynamicType);
            Assert.AreEqual(doubleValue, (double)testEntity.GetPropertyValueByName("DoubleName"));
            Assert.AreEqual(PropertyType.Double, testEntity.GetPropertyValueByName("DoubleName").DynamicType);

            // Bool should be explicit
            Assert.AreEqual(true, (bool)testEntity.GetPropertyValueByName("BoolTrueName"));
            Assert.AreEqual(PropertyType.Bool, testEntity.GetPropertyValueByName("BoolTrueName").DynamicType);
            Assert.AreEqual(false, (bool)testEntity.GetPropertyValueByName("BoolFalseName"));
            Assert.AreEqual(PropertyType.Bool, testEntity.GetPropertyValueByName("BoolFalseName").DynamicType);

            // Values that come in as strings need to be inferred
            Assert.AreEqual(guidValue, testEntity.GetPropertyValueByName("GuidName").SerializationValue);
            Assert.AreEqual(PropertyType.Guid, testEntity.GetPropertyValueByName("GuidName").DynamicType);
            Assert.AreEqual(dateValue, testEntity.GetPropertyValueByName("DateName").SerializationValue);
            Assert.AreEqual(PropertyType.Date, testEntity.GetPropertyValueByName("DateName").DynamicType);
            Assert.AreEqual(stringValue, (string)testEntity.GetPropertyValueByName("StringName"));
            Assert.AreEqual(PropertyType.String, testEntity.GetPropertyValueByName("StringName").DynamicType);

            // Number as string should be a string
            Assert.AreEqual(numberAsStringValue, (string)testEntity.GetPropertyValueByName("NumberAsStringName"));
            Assert.AreEqual(PropertyType.String, testEntity.GetPropertyValueByName("NumberAsStringName").DynamicType);

            // Do a complete round-trip of the deserialized entity and compare the entities
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(true, false, true));
            var roundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(roundTripJson));
            AssertEntitiesEqual(testEntity, roundTripEntity, null);

            // Assert that numeric and boolean types are not treated as strings in serialized json and the rest are
            var rawProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(roundTripJson)["Properties"];
            var deserializedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                ((JContainer)rawProperties).ToString(Formatting.None));
            Assert.IsNotInstanceOfType(deserializedProperties["Int32Name"], typeof(string));
            Assert.IsNotInstanceOfType(deserializedProperties["Int64Name"], typeof(string));
            Assert.IsNotInstanceOfType(deserializedProperties["DoubleName"], typeof(string));
            Assert.IsNotInstanceOfType(deserializedProperties["BoolTrueName"], typeof(string));
            Assert.IsNotInstanceOfType(deserializedProperties["BoolFalseName"], typeof(string));
            Assert.IsInstanceOfType(deserializedProperties["GuidName"], typeof(string));
            Assert.IsInstanceOfType(deserializedProperties["DateName"], typeof(string));
            Assert.IsInstanceOfType(deserializedProperties["StringName"], typeof(string));
            Assert.IsInstanceOfType(deserializedProperties["NumberAsStringName"], typeof(string));
        }

        /// <summary>Test we can construct an entity from a json object with properties that are complex json objects.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithComplexObjectProperty()
        {
            var somePropertyName = "SomeProperty";
            var jsonObjectName1Value = 1.1;
            var jsonObjectName2Value = "value2";
            var externalName = "AssocFoo";

            // Create an IEntity with a Json object property
            // Include Association just because it's a convenient complex reference type for this test
            var objectJson = JsonConvert.SerializeObject(
                new
                    {
                        Name1 = jsonObjectName1Value, 
                        Name2 = jsonObjectName2Value,
                        Name3 = new Association { AssociationType = AssociationType.Child, ExternalName = externalName }
                    });

            var rawEntity = new Entity { ExternalEntityId = new EntityId(), EntityCategory = TestEntity.CategoryName };
            var testEntity = new TestEntity(rawEntity);
            testEntity.SetPropertyValueByName(somePropertyName, objectJson);

            // Round-trip the entity and verify the result entity has the same values as the source entity.
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(true, false, true));
            var roundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(roundTripJson));

            // Assert the entity made the round-trip correctly
            AssertEntitiesEqual(testEntity, roundTripEntity, null);

            // Assert that a json parser recognizes it as json instead of treating it as a string.
            var rawProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(roundTripJson)["Properties"];
            var propertiesJson = ((JContainer)rawProperties).ToString(Formatting.None);
            var rawJsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesJson)[somePropertyName];
            Assert.IsNotInstanceOfType(rawJsonObject, typeof(string));

            // Assert the json object made the round-trip correctly
            var roundTripObjectJson = roundTripEntity.TryGetPropertyValueByName(somePropertyName);
            var roundTripObject = JsonConvert.DeserializeAnonymousType(
                roundTripObjectJson, new { Name1 = 0.0, Name2 = string.Empty, Name3 = new Association() });
            Assert.AreEqual(jsonObjectName1Value, roundTripObject.Name1);
            Assert.AreEqual(jsonObjectName2Value, roundTripObject.Name2);
            Assert.IsInstanceOfType(roundTripObject.Name3, typeof(Association));
            Assert.AreEqual(externalName, roundTripObject.Name3.ExternalName);
        }

        /// <summary>
        /// Test we can construct an entity from a json object with properties that are json arrays.
        /// They should be left as json strings in the entity.
        /// </summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithArrayProperty()
        {
            // Create Json input with non-schematized property with NaN value
            var sourceJson =
                @"{
                    ""ExternalEntityId"":""170d217896d14bb7930cd382d3255781"",
                    ""EntityCategory"":""Entity"",
                    ""CreateDate"":""2013-03-20T22:27:28.1330000Z"",
                    ""Properties"":{""measures"":[{""measureId"":""108000000001011688""}]}
                }";

            // Round-trip the entity and verify the result entity has the same values as the source entity.
            var entity = new TestEntity(EntityJsonSerializer.DeserializeEntity(sourceJson));
            Assert.AreEqual(@"[{""measureId"":""108000000001011688""}]", entity.GetPropertyByName<string>("measures"));

            var roundTripJson = EntityJsonSerializer.SerializeToJson(entity, BuildEntitySerializationFilter(true, false, true));
            Assert.IsTrue(roundTripJson.Contains(@"""measures"":[{""measureId"":""108000000001011688""}]"));
        }

        /// <summary>Test we can construct an entity from a json object with properties and property collections, match result to a regex.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithPropertiesMatchRegex()
        {
            var externalNameValue = "fooname";
            var externalEntityId = new EntityId();

            var rawEntity = new Entity
                {
                    ExternalEntityId = externalEntityId, 
                    ExternalName = externalNameValue,
                    EntityCategory = TestEntity.CategoryName
                };
            var testEntity = new TestEntity(rawEntity);
            testEntity.SetPropertyValueByName("SomeProperty", "somevalue");
            Assert.AreEqual(externalNameValue, (string)testEntity.ExternalName);

            // Round-trip to verify we passed regex condition
            var queryValues = new Dictionary<string, string> { { "externalname", "^f" } }; // match to an external name starting with f
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, false, queryValues));
            var roundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(roundTripJson));
            AssertEntitiesEqual(testEntity, roundTripEntity, null);
        }

        /// <summary>Test we can construct an entity from a json object with properties and property collections, match result to a regex fails on ExternalName.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithPropertiesMismatchRegex()
        {
            var externalNameValue = "fooname";
            var externalEntityId = new EntityId();

            var rawEntity = new Entity
            {
                ExternalEntityId = externalEntityId,
                ExternalName = externalNameValue,
                EntityCategory = TestEntity.CategoryName
            };

            var testEntity = new TestEntity(rawEntity);
            testEntity.SetPropertyValueByName("SomeProperty", "somevalue");
            Assert.AreEqual(externalNameValue, (string)testEntity.ExternalName);

            // Serialize and check result. Expect the result to fail on regex match so this should return an empty string
            var queryValues = new Dictionary<string, string> { { "externalname", "^b" } }; // match to an external name starting with f
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, false, queryValues));
            Assert.AreEqual(roundTripJson, string.Empty);
        }

        /// <summary>Test handle default serialization and deserialization filtering.</summary>
        [TestMethod]
        public void DefaultSerializeDeserializeFiltering()
        {
            // Json with an IEntity property, a property bag property, a simple association and a collection of associations.
            // The ExternalName member of each association will be taken from the name of the Json member. So in this example
            // There will be a simple association with ExternalName 'ParentCompany' and a collection where each association
            // will have an ExternalName of 'Foos'
            const string JsonFormat =
@"{{
    ""ExternalName"":""{0}"",
    ""EntityCategory"":""{1}"",
    ""Properties"":{{""SomeProperty"":""{2}""}},
    ""SystemProperties"":{{""SomeSysProperty"":""{3}""}},
    ""ExtendedProperties"":{{""SomeExtProperty"":""{4}""}},
    ""Associations"":{{""ParentCompany"":{5}}},
    ""ExternalEntityId"":""{6}""
}}";
            var assocJson1 = @"{""TargetEntityId"":""00000000000000000000000000000001"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";

            var externalNameValue = "fooname";
            var propertyValue = "someValue";
            var sysPropertyValue = "someSysValue";
            var extPropertyValue = "someExtValue";

            var sourceJson = JsonFormat.FormatInvariant(
                externalNameValue, TestEntity.CategoryName, propertyValue, sysPropertyValue, extPropertyValue, assocJson1, new EntityId().ToString());

            // Deserialize entity from Json (with default filtering)
            // There should be no external properties or associations.
            // There should be system and default properties
            var testEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(sourceJson));
            Assert.AreEqual(1, testEntity.Properties.Count(p => p.IsDefaultProperty));
            Assert.AreEqual(1, testEntity.Properties.Count(p => p.IsSystemProperty));
            Assert.AreEqual(0, testEntity.Properties.Count(p => p.IsExtendedProperty));
            Assert.AreEqual(0, testEntity.Associations.Count);

            // Deserialize the full entity
            testEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(sourceJson, BuildEntitySerializationFilter(true, true, true)));
            Assert.AreEqual(1, testEntity.Properties.Count(p => p.IsDefaultProperty));
            Assert.AreEqual(1, testEntity.Properties.Count(p => p.IsSystemProperty));
            Assert.AreEqual(1, testEntity.Properties.Count(p => p.IsExtendedProperty));
            Assert.AreEqual(1, testEntity.Associations.Count);

            // Serialize with default filtering
            // Roundtrip it with full deserialization to verify that values were filtered in serialization
            // There should be only default properties
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity);
            var roundTripEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(roundTripJson, BuildEntitySerializationFilter(true, true, true)));
            Assert.AreEqual(1, roundTripEntity.Properties.Count(p => p.IsDefaultProperty));
            Assert.AreEqual(0, roundTripEntity.Properties.Count(p => p.IsSystemProperty));
            Assert.AreEqual(0, roundTripEntity.Properties.Count(p => p.IsExtendedProperty));
            Assert.AreEqual(0, roundTripEntity.Associations.Count);
        }

        /// <summary>Test we can construct minimum user entity from a json object.</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithAssociations()
        {
            // Json with an IEntity property, a property bag property, a simple association and a collection of associations.
            // The ExternalName member of each association will be taken from the name of the Json member. So in this example
            // There will be a simple association with ExternalName 'ParentCompany' and a collection where each association
            // will have an ExternalName of 'Foos'
            const string JsonFormat =
@"{{
    ""ExternalName"":""{0}"",
    ""EntityCategory"":""{1}"",
    ""Properties"":{{""SomeProperty"":""{2}""}},
    ""Associations"":{{""ParentCompany"":{3},""Foos"":[{4},{5}]}},
    ""ExternalEntityId"":""{6}""
}}";
            var assocJson1 = @"{""TargetEntityId"":""00000000000000000000000000000001"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";
            var assocJson2 = @"{""TargetEntityId"":""00000000000000000000000000000002"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";
            var assocJson3 = @"{""TargetEntityId"":""00000000000000000000000000000003"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";

            var externalNameValue = "fooname";
            var somePropertyValue = "somevalue";
            var sourceJson = JsonFormat.FormatInvariant(
                externalNameValue, TestEntity.CategoryName, somePropertyValue, assocJson1, assocJson2, assocJson3, new EntityId().ToString());

            // Deserialize entity from Json (allow associations too be deserialized)
            var testEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(sourceJson, BuildEntitySerializationFilter(false, false, true)));
            Assert.AreEqual(1, testEntity.Properties.Count);
            Assert.AreEqual(externalNameValue, (string)testEntity.ExternalName);
            Assert.AreEqual(3, testEntity.Associations.Count);
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.ExternalName == "ParentCompany"));
            Assert.AreEqual(2, testEntity.Associations.Count(a => a.ExternalName == "Foos"));
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.TargetEntityId == new EntityId(1)));
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.TargetEntityId == new EntityId(2)));
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.TargetEntityId == new EntityId(3)));

            // Round-trip (and a half) back to Json and then create a new entity from the result
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, true));
            var roundTripEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(roundTripJson, BuildEntitySerializationFilter(false, false, true)));
            AssertEntitiesEqual(testEntity, roundTripEntity, null);
        }

        /// <summary>Test we can construct minimum user entity from a json object and match against an association starting with P</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithAssociationsRegexMatch()
        {
            const string AssocJson1 =
                @"{""TargetEntityId"":""00000000000000000000000000000001"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";

            // Json with an IEntity property, a property bag property, a simple association and a collection of associations.
            // The ExternalName member of each association will be taken from the name of the Json member. So in this example
            // There will be a simple association with ExternalName 'ParentCompany' and a collection where each association
            // will have an ExternalName of 'Foos'
            const string JsonFormat =
                @"{{
                        ""ExternalName"":""{0}"",
                        ""EntityCategory"":""{1}"",
                        ""Properties"":{{""SomeProperty"":""{2}""}},
                        ""Associations"":{{""ParentCompany"":{3}}},
                        ""ExternalEntityId"":""{4}""
                    }}";

            var queryValues = new Dictionary<string, string>
            {
                { "associations", "^P" }, { "flags", "withassociations" } 
            };

            var externalNameValue = "fooname";
            var somePropertyValue = "somevalue";
            var sourceJson = JsonFormat.FormatInvariant(
                externalNameValue, TestEntity.CategoryName, somePropertyValue, AssocJson1, new EntityId().ToString());

            // Deserialize entity from Json
            var testEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(sourceJson, BuildEntitySerializationFilter(false, false, true)));
            Assert.AreEqual(1, testEntity.Properties.Count);
            Assert.AreEqual(externalNameValue, (string)testEntity.ExternalName);
            Assert.AreEqual(1, testEntity.Associations.Count);
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.ExternalName == "ParentCompany"));
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.TargetEntityId == new EntityId(1)));

            // Round-trip (and a half) back to Json and then create a new entity from the result
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, true, queryValues));
            var roundTripEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(roundTripJson, BuildEntitySerializationFilter(false, false, true)));
            AssertEntitiesEqual(testEntity, roundTripEntity, null);
        }

        /// <summary>Test we can construct minimum user entity from a json object and not match againast an association starting with P</summary>
        [TestMethod]
        public void SerializeDeserializeJsonWithAssociationsRegexMismatch()
        {
            const string AssocJson1 = @"{""TargetEntityId"":""00000000000000000000000000000001"",""TargetEntityCategory"":""Company"",""TargetExternalType"":""Foo"",""AssociationType"":""Relationship""}";

            // Json with an IEntity property, a property bag property, a simple association and a collection of associations.
            // The ExternalName member of each association will be taken from the name of the Json member. So in this example
            // There will be a simple association with ExternalName 'ParentCompany' and a collection where each association
            // will have an ExternalName of 'Foos'
            const string JsonFormat =
                  @"{{
                        ""ExternalName"":""{0}"",
                        ""EntityCategory"":""{1}"",
                        ""Properties"":{{""SomeProperty"":""{2}""}},
                        ""Associations"":{{""ParentCompany"":{3},""KCompany"":{3}}},
                        ""ExternalEntityId"":""{4}""
                    }}";

            var queryValues = new Dictionary<string, string> { { "associations", "^K" } };

            var externalNameValue = "fooname";
            var somePropertyValue = "somevalue";
            var sourceJson = JsonFormat.FormatInvariant(
                externalNameValue, TestEntity.CategoryName, somePropertyValue, AssocJson1, new EntityId().ToString());

            // Deserialize entity from Json
            var testEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(sourceJson, BuildEntitySerializationFilter(false, false, true)));
            Assert.AreEqual(2, testEntity.Associations.Count);

            // Round-trip (and a half) back to Json and then create a new entity from the result
            var roundTripJson = EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, true, queryValues));
            var roundTripEntity = new TestEntity(
                EntityJsonSerializer.DeserializeEntity(roundTripJson, BuildEntitySerializationFilter(false, false, true)));
            Assert.AreEqual(1, roundTripEntity.Associations.Count);
            Assert.AreEqual(1, testEntity.Associations.Count(a => a.ExternalName == "KCompany"));
        }

        /// <summary>Test that filtered properties (e.g. system, extended) are only included in serialization when requested</summary>
        [TestMethod]
        public void SerializeJsonWithFilteredProperties()
        {
            // Note: The deserializer treats integers as doubles because json doesn't differentiate.
            AssertSerializeFilteredProperty<double>("SomeSystemName", 1, PropertyFilter.System);
            AssertSerializeFilteredProperty("SomeSystemName", "SomeValue", PropertyFilter.System);
            AssertSerializeFilteredProperty<double>("SomeExtendedName", 1, PropertyFilter.Extended);
            AssertSerializeFilteredProperty("SomeExtendedName", "SomeValue", PropertyFilter.Extended);
        }

        /// <summary>Test that duplicate properties are not serialized</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SerializeEntityWithDuplicatePropertiesFails()
        {
            var visiblePropertyValue = 1;
            var externalEntityId = new EntityId();
            var entityCategory = TestEntity.CategoryName;

            // Create the entity
            var testEntity = new TestEntity(new Entity
            {
                ExternalEntityId = externalEntityId,
                EntityCategory = entityCategory,
                Properties =
                {
                    new EntityProperty("PublicProperty", visiblePropertyValue),
                    new EntityProperty("PublicProperty", new EntityId())
                }
            });

            // Round-trip serialize without system properties
            EntityJsonSerializer.SerializeToJson(testEntity, BuildEntitySerializationFilter(false, false, false));
        }

        /// <summary>Deserialization throws on null input json</summary>
        [TestMethod]
        public void JsonDeserializationReturnsEmptyEntityIfJsonNullOrEmpty()
        {
            var entity = EntityJsonSerializer.DeserializeEntity(null);
            Assert.IsNotNull(entity);
            entity = EntityJsonSerializer.DeserializeEntity(string.Empty);
            Assert.IsNotNull(entity);
        }

        /// <summary>Test that association deserialization an association property we don't recognize throws ArgumentException.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssociationJsonDeserializationThrowsIfJsonHasInvalidProperty()
        {
            var sourceJson =
            @"{
                ""ExternalName"":""foo"",
                ""Associations"":{""ParentCompany"":""NotAnAssociation""}}
            }";
            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>Test that association deserialization of a duplicate association name throws ArgumentException.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssociationJsonDeserializationThrowsIfDuplicateAssociation()
        {
            var sourceJson =
            @"{
                ""ExternalName"":""foo"",
                ""Associations"":{""ParentCompany"":{""TargetExternalType"":""Type""}, ""ParentCompany"":{""TargetExternalType"":""Type""}}
            }";
            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>Test that deserialization of a duplicate interface property throws ArgumentException.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JsonDeserializationThrowsIfDuplicateInterfaceProperty()
        {
            var sourceJson =
            @"{
                ""ExternalName"":""foo"",
                ""ExternalName"":""bar""
            }";
            EntityJsonSerializer.DeserializeEntity(sourceJson);
        }

        /// <summary>Test that deserialization of a duplicate non-interface property throws ArgumentException.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void JsonDeserializationThrowsIfDuplicateNonInterfaceProperty()
        {
            var sourceJson =
            @"{
                ""Properties"":
                {{
                    ""SomeProperty"":""foo"",
                    ""SomeProperty"":""bar"",
                }}
            }";
            new TestEntity(EntityJsonSerializer.DeserializeEntity(sourceJson));
        }

        /// <summary>Determine if two entities have the same elements.</summary>
        /// <param name="expected">The expected entity.</param>
        /// <param name="actual">The actual entity.</param>
        /// <param name="expectedAssociationCount">Nullable count of expectd associations</param>
        private static void AssertEntitiesEqual(TestEntity expected, TestEntity actual, int? expectedAssociationCount)
        {
            // Assert the collections
            Assert.AreEqual(expected.InterfaceProperties.Count, actual.InterfaceProperties.Count);
            Assert.AreEqual(expected.Properties.Count, actual.Properties.Count);
            Assert.AreEqual(
                expectedAssociationCount.HasValue ? expectedAssociationCount.Value : expected.Associations.Count,
                actual.Associations.Count);

            // Order of IEntity properties is not significant but confirm they are there
            foreach (var property in expected.InterfaceProperties)
            {
                Assert.IsTrue(actual.InterfaceProperties.Contains(property));
            }

            // Properties and Associations are lists and sequence should be preserved
            for (var i = 0; i < expected.Properties.Count; i++)
            {
                Assert.AreEqual(expected.Properties[i], actual.Properties[i]);
            }

            for (var i = 0; i < (expectedAssociationCount.HasValue ? expectedAssociationCount.Value : expected.Associations.Count); i++)
            {
                Assert.AreEqual(expected.Associations[i], actual.Associations[i]);
            }
        }

        /// <summary>Assert that a filtered property is only included when requested.</summary>
        /// <typeparam name="T">The expected type of the value</typeparam>
        /// <param name="filteredPropertyName">property name</param>
        /// <param name="filteredPropertyValue">property value</param>
        /// <param name="propertyFilter">property filter type</param>
        private static void AssertSerializeFilteredProperty<T>(
            string filteredPropertyName, T filteredPropertyValue, PropertyFilter propertyFilter)
        {
            // Create an entity with extended properties
            var visiblePropertyName = "SomeName";
            var visiblePropertyValue = 1;

            var testEntity = new TestEntity(new Entity
            {
                ExternalEntityId = new EntityId(),
                EntityCategory = TestEntity.CategoryName,
            });

            testEntity.TrySetPropertyByName(visiblePropertyName, visiblePropertyValue);
            testEntity.TrySetPropertyByName(filteredPropertyName, filteredPropertyValue, propertyFilter);

            // Allow all elements to be deserialized
            var deserFilter = BuildEntitySerializationFilter(true, true, true);

            // Round-trip serialize without filtered properties
            var serFilter = BuildEntitySerializationFilter(false, false, false);
            var publicJson = EntityJsonSerializer.SerializeToJson(testEntity, serFilter);
            var publicRoundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(publicJson, deserFilter));
            Assert.AreEqual(visiblePropertyValue, (double)publicRoundTripEntity.TryGetPropertyValueByName(visiblePropertyName).DynamicValue);
            Assert.IsNull(publicRoundTripEntity.TryGetPropertyValueByName(filteredPropertyName));

            // Round-trip serialize WITH extended properties
            serFilter = BuildEntitySerializationFilter(
                propertyFilter == PropertyFilter.System, propertyFilter == PropertyFilter.Extended, false);
            var extendedJson = EntityJsonSerializer.SerializeToJson(testEntity, serFilter);
            var extendedRoundTripEntity = new TestEntity(EntityJsonSerializer.DeserializeEntity(extendedJson, deserFilter));
            Assert.AreEqual(visiblePropertyValue, (double)publicRoundTripEntity.TryGetPropertyValueByName(visiblePropertyName).DynamicValue);
            extendedRoundTripEntity.TryGetPropertyValueByName(filteredPropertyName);
            Assert.AreEqual(filteredPropertyValue, (T)extendedRoundTripEntity.TryGetPropertyValueByName(filteredPropertyName).DynamicValue);
        }

        /// <summary>
        /// Build an IEntityFilter stub
        /// </summary>
        /// <param name="includeSystemProperties">True to include system properties.</param>
        /// <param name="includeExtendedProperties">True to include extended properties.</param>
        /// <param name="includeAssociations">True to include associations.</param>
        /// <param name="queryParams">Query param dictionary.</param>
        /// <returns>IEntityFilter stub.</returns>
        private static IEntityFilter BuildEntitySerializationFilter(bool includeSystemProperties, bool includeExtendedProperties, bool includeAssociations, Dictionary<string, string> queryParams = null)
        {
            var entityFilter = MockRepository.GenerateStub<IEntityFilter>();
            entityFilter.Stub(f => f.IncludeSystemProperties).Return(includeSystemProperties);
            entityFilter.Stub(f => f.IncludeExtendedProperties).Return(includeExtendedProperties);
            entityFilter.Stub(f => f.IncludeAssociations).Return(includeAssociations);
            entityFilter.Stub(f => f.EntityQueries).Return(new EntityActivityQuery(queryParams));
            return entityFilter;
        }
    }
}
