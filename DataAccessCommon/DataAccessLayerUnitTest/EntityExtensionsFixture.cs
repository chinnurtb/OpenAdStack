// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityExtensionsFixture.cs" company="Rare Crowds Inc">
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
using System.Text;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>
    /// Test fixture for DataAccessLayer EntityExtensions class
    /// </summary>
    [TestClass]
    public class EntityExtensionsFixture
    {
        /// <summary>Association to BlobEntity for testing.</summary>
        internal static readonly Association BlobAssociation = new Association
        {
            ExternalName = "BlobOnAStick",
            TargetEntityCategory = BlobEntity.CategoryName,
            TargetEntityId = new EntityId(),
            TargetExternalType = "MyFoo",
            Details = "OldBlobDetails",
            AssociationType = AssociationType.Relationship
        };

        /// <summary>EntityId for testing.</summary>
        private EntityId partnerEntityId;

        /// <summary>PartnerEntity for testing.</summary>
        private PartnerEntity partnerEntity;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.partnerEntityId = new EntityId();
            this.partnerEntity = TestEntityBuilder.BuildPartnerEntity(this.partnerEntityId);
        }

        /// <summary>RoundTrip property access through try methods with property filters.</summary>
        [TestMethod]
        public void RoundtripTryMethodsWithPropertyFilter()
        {
            var propertyName = "SomeProperty";
            var sysPropertyName = "SomeSysProperty";
            var extPropertyName = "SomeExtProperty";
            var propertyValue = "SomeValue";
            var testEntity = TestEntityBuilder.BuildTestEntity();

            // Verify with default property filter
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(propertyName, propertyValue));
            var actualValue = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.AreEqual(propertyValue, (string)actualValue);
            Assert.AreEqual(PropertyFilter.Default, testEntity.TryGetEntityPropertyByName(propertyName, string.Empty).Filter);

            // Verify with System property filter
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(sysPropertyName, propertyValue, PropertyFilter.System));
            actualValue = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.AreEqual(propertyValue, (string)actualValue);
            Assert.AreEqual(PropertyFilter.System, testEntity.TryGetEntityPropertyByName(sysPropertyName, string.Empty).Filter);

            // Verify with Extended property filter
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(extPropertyName, propertyValue, PropertyFilter.Extended));
            actualValue = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.AreEqual(propertyValue, (string)actualValue);
            Assert.AreEqual(PropertyFilter.Extended, testEntity.TryGetEntityPropertyByName(extPropertyName, string.Empty).Filter);

            // Cannot update a property of the same name with different filter ok
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(propertyName, propertyValue, PropertyFilter.Extended));
            Assert.AreEqual(PropertyFilter.Extended, testEntity.TryGetEntityPropertyByName(propertyName, string.Empty).Filter);
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(propertyName, propertyValue, PropertyFilter.System));
            Assert.AreEqual(PropertyFilter.System, testEntity.TryGetEntityPropertyByName(propertyName, string.Empty).Filter);
            Assert.IsTrue(testEntity.TrySetPropertyValueByName(sysPropertyName, propertyValue, PropertyFilter.Default));
            Assert.AreEqual(PropertyFilter.Default, testEntity.TryGetEntityPropertyByName(sysPropertyName, string.Empty).Filter);
        }

        /// <summary>TrySetPropertyByName maps type param to PropertyType correctly.</summary>
        [TestMethod]
        public void TrySetPropertyMapsTypeParamCorrectly()
        {
            this.AssertTrySetPropertyMapsTypeParam("Name", "SomeString", PropertyType.String);
            this.AssertTrySetPropertyMapsTypeParam("Name", 1, PropertyType.Int32);
            this.AssertTrySetPropertyMapsTypeParam("Name", (long)1, PropertyType.Int64);
            this.AssertTrySetPropertyMapsTypeParam("Name", 1.0, PropertyType.Double);
            this.AssertTrySetPropertyMapsTypeParam("Name", 1.0m, PropertyType.Double);
            this.AssertTrySetPropertyMapsTypeParam("Name", true, PropertyType.Bool);
            this.AssertTrySetPropertyMapsTypeParam("Name", DateTime.Now, PropertyType.Date);
            this.AssertTrySetPropertyMapsTypeParam("Name", Guid.NewGuid(), PropertyType.Guid);
            this.AssertTrySetPropertyMapsTypeParam("Name", new EntityId(), PropertyType.Guid);
            this.AssertTrySetPropertyMapsTypeParam("Name", new byte[] { 0x1, 0x2, 0x3, 0x4 }, PropertyType.Binary);
        }

        /// <summary>Remove property by name.</summary>
        [TestMethod]
        public void RemovePropertyByName()
        {
            this.partnerEntity.SetPropertyByName("newProp", "value");
            Assert.AreEqual("value", this.partnerEntity.GetPropertyByName<string>("newProp"));
            this.partnerEntity.RemovePropertyByName("newProp");
            Assert.IsNull(this.partnerEntity.TryGetPropertyByName<string>("newProp", null));
        }

        /// <summary>Remove property by name when it's missing succeeds.</summary>
        [TestMethod]
        public void RemovePropertyByNameMissingOk()
        {
            Assert.IsNull(this.partnerEntity.TryGetPropertyByName<string>("newProp", null));
            this.partnerEntity.RemovePropertyByName("newProp");
            Assert.IsNull(this.partnerEntity.TryGetPropertyByName<string>("newProp", null));
        }

        /// <summary>Remove association by name.</summary>
        [TestMethod]
        public void RemoveAssociationByName()
        {
            var targetEntity = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            this.partnerEntity.AssociateEntities("newAssoc", "none", new HashSet<IEntity> { targetEntity }, AssociationType.Relationship, true);
            Assert.AreEqual((EntityId)targetEntity.ExternalEntityId, this.partnerEntity.GetAssociationByName("newAssoc").TargetEntityId);
            this.partnerEntity.RemoveAssociationsByName("newAssoc");
            Assert.IsNull(this.partnerEntity.TryGetAssociationByName("newAssoc"));
        }

        /// <summary>Remove association collection by name.</summary>
        [TestMethod]
        public void RemoveAssociationCollectionByName()
        {
            var targetEntity1 = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            var targetEntity2 = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            this.partnerEntity.AssociateEntities("newAssoc", "none", new HashSet<IEntity> { targetEntity1, targetEntity2 }, AssociationType.Relationship, true);
            Assert.AreEqual(2, this.partnerEntity.GetAssociationsByName("newAssoc").Count());
            this.partnerEntity.RemoveAssociationsByName("newAssoc");
            Assert.IsNull(this.partnerEntity.TryGetAssociationByName("newAssoc"));
        }

        /// <summary>Remove association by name when it's missing succeeds.</summary>
        [TestMethod]
        public void RemoveAssociationByNameMissingOk()
        {
            Assert.IsNull(this.partnerEntity.TryGetAssociationByName("newAssoc"));
            this.partnerEntity.RemoveAssociationsByName("newAssoc");
            Assert.IsNull(this.partnerEntity.TryGetAssociationByName("newAssoc"));
        }

        /// <summary>Test EntityProperty accessor</summary>
        [TestMethod]
        public void TestEntityPropertyAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripEntityPropertyAccessor(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyType.String, PropertyFilter.Extended);
            AssertRoundtripEntityPropertyAccessor(
                testEntity, "someothername", 1, 2, PropertyType.Int32, PropertyFilter.System);
        }

        /// <summary>Test EntityProperty accessor throws on duplicate property</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEntityPropertyAccessorDuplicateProperty()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            testEntity.WrappedEntity.Properties.Add(new EntityProperty("somename", "somevalue"));
            testEntity.WrappedEntity.Properties.Add(new EntityProperty("somename", "somevalue"));
            testEntity.GetEntityPropertyByName("somename");
        }

        /// <summary>Test PropertyValue accessor</summary>
        [TestMethod]
        public void TestPropertyValueAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripPropertyValueAccessor(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyType.String, PropertyFilter.Extended);
            AssertRoundtripPropertyValueAccessor(
                testEntity, "someothername", 1, 2, PropertyType.Int32, PropertyFilter.System);
        }

        /// <summary>Test PropertyValue accessor with null property value</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestPropertyValueAccessorNull()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            testEntity.SetEntityProperty(new EntityProperty("somename", null));
            testEntity.GetPropertyValueByName("somename");
        }

        /// <summary>Test Property accessor</summary>
        [TestMethod]
        public void TestPropertyAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripPropertyAccessor<string, int>(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyFilter.Extended);
            AssertRoundtripPropertyAccessor<int, string>(
                testEntity, "someothername", 1, 2, PropertyFilter.System);
        }

        /// <summary>SetEntityProperty should be destructive.</summary>
        [TestMethod]
        public void SetEntityPropertyDestructive()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            var someProperty = new EntityProperty { Name = "SomeName", Filter = PropertyFilter.Default, IsBlobRef = true, Value = "SomeValue" };
            testEntity.SetEntityProperty(someProperty);
            var modifiedProperty = new EntityProperty(someProperty);
            modifiedProperty.IsBlobRef = false;
            modifiedProperty.Value = "SomeNewValue";
            testEntity.SetEntityProperty(modifiedProperty);
            var roundtripProperty = testEntity.GetEntityPropertyByName("SomeName");
            Assert.AreEqual(false, roundtripProperty.IsBlobRef);
            Assert.AreEqual("SomeNewValue", (string)roundtripProperty.Value);
        }

        /// <summary>Test EntityProperty Try accessor</summary>
        [TestMethod]
        public void TestEntityPropertyTryAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripEntityPropertyTryAccessor(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyType.String, PropertyFilter.Extended);
            AssertRoundtripEntityPropertyTryAccessor(
                testEntity, "someothername", 1, 2, PropertyType.Int32, PropertyFilter.System);
        }

        /// <summary>Test PropertyValue Try accessor</summary>
        [TestMethod]
        public void TestPropertyValueTryAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripPropertyValueTryAccessor(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyType.String, PropertyFilter.Extended);
            AssertRoundtripPropertyValueTryAccessor(
                testEntity, "someothername", 1, 2, PropertyType.Int32, PropertyFilter.System);
        }

        /// <summary>Test Property Try accessor</summary>
        [TestMethod]
        public void TestPropertyTryAccessor()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            AssertRoundtripPropertyTryAccessor<string, int>(
                testEntity, "somename", "somevalue", "somenewvalue", PropertyFilter.Extended);
            AssertRoundtripPropertyTryAccessor<int, string>(
                testEntity, "someothername", 1, 2, PropertyFilter.System);
        }

        /// <summary>TrySetPropertyByName should fail on unsupported type.</summary>
        [TestMethod]
        public void GenericTrySetPropertyFailsOnUnsupportedTypes()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            Assert.IsFalse(testEntity.TrySetPropertyByName("SomeName", new StringBuilder()));
        }

        /// <summary>Get a property from the property collection by name.</summary>
        [TestMethod]
        public void GetPropertyByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            Assert.AreEqual(
                TestEntityBuilder.PropertyValue,
                (string)testEntity.TryGetEntityPropertyByName(TestEntityBuilder.PropertyName, string.Empty));
            Assert.AreEqual(
                TestEntityBuilder.PropertyValue,
                (string)testEntity.TryGetEntityPropertyByName(TestEntityBuilder.PropertyName, string.Empty));
        }

        /// <summary>If property is not found the supplied default value should be returned.</summary>
        [TestMethod]
        public void GetPropertyByNameDefaultIfNotFound()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            Assert.AreEqual(string.Empty, (string)testEntity.TryGetEntityPropertyByName("NonExistent", string.Empty));
        }

        /// <summary>Get a property value from the property collection by name.</summary>
        [TestMethod]
        public void GetPropertyValueByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            Assert.AreEqual(
                TestEntityBuilder.PropertyValue,
                (string)testEntity.GetPropertyValueByName(TestEntityBuilder.PropertyName));
        }

        /// <summary>Get a nonexistent property value from the property collection by name.</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetPropertyValueByNameExceptionIfNotFound()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            testEntity.GetPropertyValueByName("NonExistent");
        }

        /// <summary>Try to get a property value from the property collection by name.</summary>
        [TestMethod]
        public void TryGetPropertyValueByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            Assert.AreEqual(
                TestEntityBuilder.PropertyValue,
                (string)testEntity.TryGetPropertyValueByName(TestEntityBuilder.PropertyName));
        }

        /// <summary>Try to get a nonexistent property value from the property collection by name.</summary>
        [TestMethod]
        public void TryGetPropertyValueByNameNullIfNotFound()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            Assert.AreEqual(null, testEntity.TryGetPropertyValueByName("NonExistent"));
        }

        /// <summary>Set a property from the property collection by name.</summary>
        [TestMethod]
        public void SetPropertyByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            var newName = "newName";
            var newValue = "newValue";

            testEntity.SetEntityProperty(new EntityProperty { Name = newName, Value = newValue });
            Assert.AreEqual(newValue, (string)testEntity.TryGetEntityPropertyByName(newName, string.Empty));

            var newName2 = "newName2";
            testEntity.SetPropertyValueByName(newName2, newValue);
            Assert.AreEqual(newValue, (string)testEntity.TryGetEntityPropertyByName(newName2, string.Empty));
        }

        /// <summary>Overwrite a property from the property collection by name.</summary>
        [TestMethod]
        public void SetPropertyByNameOverwrites()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            var newValue = "newValue";
            testEntity.SetEntityProperty(
                new EntityProperty { Name = TestEntityBuilder.PropertyName, Value = newValue });

            Assert.AreEqual(
                newValue,
                (string)testEntity.TryGetEntityPropertyByName(TestEntityBuilder.PropertyName, string.Empty));
            Assert.AreEqual(1, testEntity.WrappedEntity.Properties.Count);
        }

        /// <summary>Get an association from the association collection by name.</summary>
        [TestMethod]
        public void GetAssociationByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            Assert.AreEqual(
                TestEntityBuilder.TestAssociation,
                testEntity.GetAssociationByName(TestEntityBuilder.AssociationName));
        }

        /// <summary>Try to get an association from the association collection by name.</summary>
        [TestMethod]
        public void TryGetAssociationByName()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();
            Assert.AreEqual(
                TestEntityBuilder.TestAssociation,
                testEntity.TryGetAssociationByName(TestEntityBuilder.AssociationName));
        }

        /// <summary>Try to get an association from the association collection by name.</summary>
        [TestMethod]
        public void TryGetAssociationByNameNullIfNotFound()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            Assert.AreEqual(null, testEntity.TryGetAssociationByName("NonExistent"));
        }

        /// <summary>Test that associating entities with the replace flag works.</summary>
        [TestMethod]
        public void AssociateEntityWithReplace()
        {
            // Set up a new Blob Entity to replace the old association
            var blobEntity = new BlobEntity(new EntityId());
            this.AssertAssociate(true, new HashSet<IEntity> { blobEntity });
        }

        /// <summary>
        /// Test that associating entities replaces duplicate entity id's. 
        /// This will occur even if 'replaceIfPresent' is false - which is based on the association name.
        /// </summary>
        [TestMethod]
        public void AssociateEntityDuplicates()
        {
            this.partnerEntity.Associations.Add(BlobAssociation);

            // Set up a new Blob Entity with the same entity Id
            var blobEntity = new BlobEntity(BlobAssociation.TargetEntityId);
            var entities = new HashSet<IEntity> { blobEntity };

            // Associate new blob entity to partner entity with same association name
            this.partnerEntity.AssociateEntities(
                BlobAssociation.ExternalName,
                "NewBlobDetails",
                entities,
                AssociationType.Relationship,
                false);

            // Assert there is only one (forced replace occured)
            var match = this.partnerEntity.Associations.Single(a => a.TargetEntityId == (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual("NewBlobDetails", match.Details);

            // Associate new blob entity to partner entity with different association name
            this.partnerEntity.AssociateEntities(
                "BlobOnAStick2",
                "NewBlobDetails",
                entities,
                AssociationType.Relationship,
                false);

            // Assert there is two associations
            this.partnerEntity.Associations.Where(a => a.TargetEntityId == (EntityId)blobEntity.ExternalEntityId);
            Assert.AreEqual(2, this.partnerEntity.Associations.Count(a => a.TargetEntityId == (EntityId)blobEntity.ExternalEntityId));
            Assert.AreEqual(1, this.partnerEntity.Associations.Count(a => a.ExternalName == "BlobOnAStick"));
            Assert.AreEqual(
                1, 
                this.partnerEntity.Associations.Count(a => a.ExternalName == BlobAssociation.ExternalName));
        }

        /// <summary>Test all target entities must have same category and external type.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssociateEntitiesMismatchedCategoriesFails()
        {
            // Set up a new entities of different category
            var blobEntity = new BlobEntity(new EntityId());
            blobEntity.ExternalType = "foo";
            this.partnerEntity.ExternalType = "foo";
            this.AssertAssociate(false, new HashSet<IEntity> { blobEntity, this.partnerEntity });
        }

        /// <summary>Test TryAssociateEntities does not throw.</summary>
        [TestMethod]
        public void TryAssociateEntitiesMismatchedCategoriesFails()
        {
            // Set up a new entities of different category
            var blobEntity = new BlobEntity(new EntityId());
            blobEntity.ExternalType = "foo";
            var otherEntity = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            otherEntity.ExternalType = "foo";

            var success = this.partnerEntity.TryAssociateEntities(
                "BlobOnAStick",
                "NewBlobDetails",
                new HashSet<IEntity> { blobEntity, otherEntity },
                AssociationType.Relationship,
                false);

            Assert.IsFalse(success);
        }

        /// <summary>Test all target entities must have same category and external type.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssociateEntitiesMismatchedTypeFails()
        {
            // Set up a new entities of different ExternalType
            var blobEntity1 = new BlobEntity(new EntityId());
            blobEntity1.ExternalType = "foo";
            var blobEntity2 = new BlobEntity(new EntityId());
            blobEntity2.ExternalType = "bar";
            this.AssertAssociate(false, new HashSet<IEntity> { blobEntity1, blobEntity2 });
        }

        /// <summary>Test all target entities must have same category and external type (empty type ok).</summary>
        [TestMethod]
        public void AssociateEntitiesEmptyTypeOk()
        {
            // Set up a new entities of different ExternalType
            var blobEntity1 = new BlobEntity(new EntityId());
            blobEntity1.ExternalType = string.Empty;
            var blobEntity2 = new BlobEntity(new EntityId());
            blobEntity2.ExternalType = string.Empty;
            this.AssertAssociate(false, new HashSet<IEntity> { blobEntity1, blobEntity2 });
        }

        /// <summary>
        /// If a new association of same name (different targetid) is added and 'replaceIfPresent' is false
        /// it becomes a collection.
        /// </summary>
        [TestMethod]
        public void SingleAssociationBecomesCollection()
        {
            // Set up a new Blob Entity with the same entity Id
            var blobEntity = new BlobEntity(new EntityId());
            var updatedEntity = this.AssertAssociate(false, new HashSet<IEntity> { blobEntity });

            Assert.AreEqual(
                2, 
                updatedEntity.Associations.Count(a => a.ExternalName == BlobAssociation.ExternalName));
        }

        /// <summary>Test we can wrap and unwrap IEntity safely.</summary>
        [TestMethod]
        public void RoundtripWrap()
        {
            var entityWrapper = TestEntityBuilder.BuildPartnerEntity();
            var wrappedEntity = entityWrapper.WrappedEntity;

            // Should be able to get raw entity out of testEntity
            Assert.AreSame(wrappedEntity, entityWrapper.SafeUnwrapEntity());

            // Should raw entity should return itself
            Assert.AreSame(wrappedEntity, wrappedEntity.SafeUnwrapEntity());
            
            // Building a wrapped entity from a wrapped entity should result in a new wrapper
            Assert.AreNotSame(entityWrapper, entityWrapper.BuildWrappedEntity());

            // But it should have the same underlying raw entity
            Assert.AreSame(wrappedEntity, entityWrapper.BuildWrappedEntity().SafeUnwrapEntity());
        }

        /// <summary>Fail generic build wrapped entity if null.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void BuildWrappedEntityGenericNull()
        {
            IEntity entity = null;
            entity.BuildWrappedEntity<PartnerEntity>();
        }

        /// <summary>Fail build wrapped entity if null.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void BuildWrappedEntityNull()
        {
            IEntity entity = null;
            entity.BuildWrappedEntity();
        }

        /// <summary>Fail build wrapped entity if category is not present.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void BuildWrappedEntityNoCategory()
        {
            IEntity entity = new Entity();
            entity.BuildWrappedEntity();
        }

        /// <summary>Fail build wrapped entity if category does not match supported types.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void BuildWrappedEntityCategoryNotSupported()
        {
            IEntity entity = new Entity { EntityCategory = string.Empty };
            entity.BuildWrappedEntity();
        }

        /// <summary>Assert two property values are equal (handle byte arrays as sequences)</summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="expectedValue">expected value</param>
        /// <param name="actualValue">actual value</param>
        private static void AssertPropertyValuesAreEqual<T>(T expectedValue, T actualValue)
        {
            if (!typeof(T).IsArray)
            {
                Assert.AreEqual(expectedValue, actualValue);
                return;
            }

            var arrExpected = expectedValue as byte[];
            var arrActual = actualValue as byte[];
            Assert.IsNotNull(arrExpected);
            Assert.IsNotNull(arrActual);
            Assert.IsTrue(arrExpected.SequenceEqual(arrActual));
        }

        /// <summary>Assert roundtripping through EntityProperty accessor</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyType">property type to use</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripEntityPropertyAccessor<T1>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyType propertyType,
            PropertyFilter propertyFilter)
        {
            // Set up expected results
            var propertyValue = new PropertyValue(propertyType, value);
            var updatedPropertyValue = new PropertyValue(propertyType, updateValue);
            var entityProperty = new EntityProperty(propertyName, propertyValue, propertyFilter);
            var updatedEntityProperty = new EntityProperty(propertyName, updatedPropertyValue, propertyFilter);

            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);
            var mismatchedEntityProperty = new EntityProperty(propertyName, propertyValue, mismatchedPropertyFilter);

            // Not found
            AssertThrow(() => testEntity.GetEntityPropertyByName(propertyName));

            // Roundtrip
            testEntity.SetEntityProperty(entityProperty);
            var roundtripEntityProperty = testEntity.GetEntityPropertyByName(propertyName);
            Assert.AreEqual(entityProperty, roundtripEntityProperty);
            Assert.AreEqual(propertyType, roundtripEntityProperty.Value.DynamicType);
            Assert.AreEqual(propertyFilter, roundtripEntityProperty.Filter);

            // Update
            testEntity.SetEntityProperty(updatedEntityProperty);
            roundtripEntityProperty = testEntity.GetEntityPropertyByName(propertyName);
            Assert.AreEqual(updatedEntityProperty, roundtripEntityProperty);

            // Mismatch property filter will be updated
            testEntity.SetEntityProperty(mismatchedEntityProperty);
            Assert.AreEqual(mismatchedEntityProperty.Filter, testEntity.GetEntityPropertyByName(mismatchedEntityProperty.Name).Filter);
        }

        /// <summary>Assert roundtripping through PropertyValue accessor</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyType">property type to use</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripPropertyValueAccessor<T1>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyType propertyType,
            PropertyFilter propertyFilter)
        {
            // Set up expected results
            var propertyValue = new PropertyValue(propertyType, value);
            var updatedPropertyValue = new PropertyValue(propertyType, updateValue);

            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);

            // Not found
            testEntity.Properties.Clear();
            AssertThrow(() => testEntity.GetPropertyValueByName(propertyName));

            // Roundtrip
            testEntity.SetPropertyValueByName(propertyName, propertyValue);
            var roundtripPropertyValue = testEntity.GetPropertyValueByName(propertyName);
            Assert.AreEqual(propertyValue, roundtripPropertyValue);
            Assert.AreEqual(propertyType, roundtripPropertyValue.DynamicType);
            Assert.AreEqual(PropertyFilter.Default, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Update
            testEntity.SetPropertyValueByName(propertyName, updatedPropertyValue);
            roundtripPropertyValue = testEntity.GetPropertyValueByName(propertyName);
            Assert.AreEqual(updatedPropertyValue, roundtripPropertyValue);

            // Explicit property filter
            testEntity.Properties.Clear();
            testEntity.SetPropertyValueByName(propertyName, propertyValue, propertyFilter);
            Assert.AreEqual(propertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Mismatch property filter will be updated
            testEntity.SetPropertyValueByName(propertyName, propertyValue, mismatchedPropertyFilter);
            Assert.AreEqual(mismatchedPropertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);
        }

        /// <summary>Assert roundtripping through property accessor</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <typeparam name="T2">bad type for underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripPropertyAccessor<T1, T2>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyFilter propertyFilter)
        {
            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);

            // Not found
            testEntity.Properties.Clear();
            AssertThrow(() => testEntity.GetPropertyByName<T1>(propertyName));

            // Roundtrip
            testEntity.SetPropertyByName(propertyName, value);
            var roundtripProperty = testEntity.GetPropertyByName<T1>(propertyName);
            Assert.AreEqual(value, roundtripProperty);
            Assert.AreEqual(PropertyFilter.Default, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Update
            testEntity.SetPropertyByName(propertyName, updateValue);
            roundtripProperty = testEntity.GetPropertyByName<T1>(propertyName);
            Assert.AreEqual(updateValue, roundtripProperty);

            // Get as incompatible type
            AssertThrow(() => testEntity.GetPropertyByName<T2>(propertyName));

            // Explicit property filter
            testEntity.Properties.Clear();
            testEntity.SetPropertyByName(propertyName, value, propertyFilter);
            Assert.AreEqual(propertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Mismatch property filter will be updated
            testEntity.SetPropertyByName(propertyName, value, mismatchedPropertyFilter);
            Assert.AreEqual(mismatchedPropertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);
        }

        /// <summary>Assert roundtripping through EntityProperty Try accessors</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyType">property type to use</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripEntityPropertyTryAccessor<T1>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyType propertyType,
            PropertyFilter propertyFilter)
        {
            // Set up expected results
            var propertyValue = new PropertyValue(propertyType, value);
            var updatedPropertyValue = new PropertyValue(propertyType, updateValue);
            var entityProperty = new EntityProperty(propertyName, propertyValue, propertyFilter);
            var updatedEntityProperty = new EntityProperty(propertyName, updatedPropertyValue, propertyFilter);

            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);
            var mismatchedEntityProperty = new EntityProperty(propertyName, propertyValue, mismatchedPropertyFilter);

            // Not found
            testEntity.Properties.Clear();
            var entityPropertyNotFound = testEntity.TryGetEntityPropertyByName(propertyName);
            Assert.IsNull(entityPropertyNotFound);

            // Roundtrip
            var result = testEntity.TrySetEntityProperty(entityProperty);
            Assert.IsTrue(result);
            var roundtripEntityProperty = testEntity.TryGetEntityPropertyByName(propertyName);
            Assert.AreEqual(entityProperty, roundtripEntityProperty);
            Assert.AreEqual(propertyType, roundtripEntityProperty.Value.DynamicType);
            Assert.AreEqual(propertyFilter, roundtripEntityProperty.Filter);

            // Update
            testEntity.TrySetEntityProperty(updatedEntityProperty);
            roundtripEntityProperty = testEntity.TryGetEntityPropertyByName(propertyName);
            Assert.AreEqual(updatedEntityProperty, roundtripEntityProperty);

            // Mismatched property filter updated
            result = testEntity.TrySetEntityProperty(mismatchedEntityProperty);
            Assert.IsTrue(result);

            ////
            // TryGet/Set EntityProperty with default PropertyValue
            ////

            // Not found
            testEntity.Properties.Clear();
            entityPropertyNotFound = testEntity.TryGetEntityPropertyByName(propertyName, null);
            Assert.IsNull(entityPropertyNotFound.Value);

            // Roundtrip
            result = testEntity.TrySetEntityProperty(entityProperty);
            Assert.IsTrue(result);
            roundtripEntityProperty = testEntity.TryGetEntityPropertyByName(propertyName, null);
            Assert.AreEqual(entityProperty, roundtripEntityProperty);
            Assert.AreEqual(propertyType, roundtripEntityProperty.Value.DynamicType);
            Assert.AreEqual(propertyFilter, roundtripEntityProperty.Filter);

            // Update
            testEntity.TrySetEntityProperty(updatedEntityProperty);
            roundtripEntityProperty = testEntity.TryGetEntityPropertyByName(propertyName, null);
            Assert.AreEqual(updatedEntityProperty, roundtripEntityProperty);
        }

        /// <summary>Assert roundtripping through PropertyValue Try accessor</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyType">property type to use</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripPropertyValueTryAccessor<T1>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyType propertyType,
            PropertyFilter propertyFilter)
        {
            // Set up expected results
            var propertyValue = new PropertyValue(propertyType, value);
            var updatedPropertyValue = new PropertyValue(propertyType, updateValue);

            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);

            // Not found
            testEntity.Properties.Clear();
            var propertyValueNotFound = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.IsNull(propertyValueNotFound);

            // Roundtrip
            var result = testEntity.TrySetPropertyValueByName(propertyName, propertyValue);
            Assert.IsTrue(result);
            var roundtripPropertyValue = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.AreEqual(propertyValue, roundtripPropertyValue);
            Assert.AreEqual(propertyType, roundtripPropertyValue.DynamicType);
            Assert.AreEqual(PropertyFilter.Default, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Update
            testEntity.TrySetPropertyValueByName(propertyName, updatedPropertyValue);
            roundtripPropertyValue = testEntity.TryGetPropertyValueByName(propertyName);
            Assert.AreEqual(updatedPropertyValue, roundtripPropertyValue);

            // Explicit property filter
            testEntity.Properties.Clear();
            testEntity.TrySetPropertyValueByName(propertyName, propertyValue, propertyFilter);
            Assert.AreEqual(propertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Mismatched property filter updated
            result = testEntity.TrySetPropertyValueByName(propertyName, propertyValue, mismatchedPropertyFilter);
            Assert.IsTrue(result);
        }

        /// <summary>Assert roundtripping of a property through all get/set variants</summary>
        /// <typeparam name="T1">type of underlying value.</typeparam>
        /// <typeparam name="T2">bad type for underlying value.</typeparam>
        /// <param name="testEntity">IEntity test target</param>
        /// <param name="propertyName">name of property</param>
        /// <param name="value">underlying value</param>
        /// <param name="updateValue">a value to update with</param>
        /// <param name="propertyFilter">property filter to use</param>
        private static void AssertRoundtripPropertyTryAccessor<T1, T2>(
            IEntity testEntity,
            string propertyName,
            T1 value,
            T1 updateValue,
            PropertyFilter propertyFilter)
        {
            // Set up a property that has a mismatched property filter
            var mismatchedPropertyFilter = GetMismatchedPropertyFilter(propertyFilter);

            ////
            // TryGet/Set Property with default
            ////

            // Not found
            testEntity.Properties.Clear();
            var propertyNotFound = testEntity.TryGetPropertyByName(propertyName, default(T1));
            Assert.AreEqual(default(T1), propertyNotFound);

            // Roundtrip
            var result = testEntity.TrySetPropertyByName(propertyName, value);
            Assert.IsTrue(result);
            var roundtripProperty = testEntity.TryGetPropertyByName(propertyName, default(T1));
            Assert.AreEqual(value, roundtripProperty);

            // Update
            testEntity.TrySetPropertyByName(propertyName, updateValue);
            roundtripProperty = testEntity.TryGetPropertyByName(propertyName, default(T1));
            Assert.AreEqual(updateValue, roundtripProperty);

            // Get as incompatible type
            var badProperty = testEntity.TryGetPropertyByName(propertyName, default(T2));
            Assert.AreEqual(default(T2), badProperty);

            // Explicit property filter
            testEntity.Properties.Clear();
            testEntity.TrySetPropertyByName(propertyName, value, propertyFilter);
            Assert.AreEqual(propertyFilter, testEntity.GetEntityPropertyByName(propertyName).Filter);

            // Mismatched property filter
            result = testEntity.TrySetPropertyByName(propertyName, value, mismatchedPropertyFilter);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Get a property filter mismatched with the the one given.
        /// </summary>
        /// <param name="propertyFilter">The source property filter.</param>
        /// <returns>A mismatched property filter.</returns>
        private static PropertyFilter GetMismatchedPropertyFilter(PropertyFilter propertyFilter)
        {
            var mismatchedPropertyFilter = propertyFilter == PropertyFilter.Default
                                               ? PropertyFilter.System
                                               : PropertyFilter.Default;
            return mismatchedPropertyFilter;
        }

        /// <summary>
        /// Assert that an accessor expression throws an InvalidOperationException
        /// </summary>
        /// <param name="accessorToTest">accessor lambda expression</param>
        private static void AssertThrow(Action accessorToTest)
        {
            try
            {
                accessorToTest();
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }
        }

        /// <summary>Set up an entity with associations make assertions after adding new one.</summary>
        /// <param name="replaceNamedEntity">flag to pass to AssociateEntity.</param>
        /// <param name="entities">The new entities to associate.</param>
        /// <returns>The source entity.</returns>
        private IEntity AssertAssociate(bool replaceNamedEntity, HashSet<IEntity> entities)
        {
            // Set up an entity with an existing association
            var otherEntity = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            otherEntity.Associations.Add(BlobAssociation);

            // Associate new blob entity to partner entity
            otherEntity.AssociateEntities(
                "BlobOnAStick",
                "NewBlobDetails",
                entities,
                AssociationType.Relationship,
                replaceNamedEntity);

            // Assert all of the new entities are associated exactly once
            foreach (var entity in entities)
            {
                var match = otherEntity.Associations.Single(a => a.TargetEntityId == (EntityId)entity.ExternalEntityId);
                Assert.AreEqual("NewBlobDetails", match.Details);
            }

            return otherEntity;
        }

        /// <summary>Helper method to assert generic TryGet/Set methods map generic parameter to PropertyType.</summary>
        /// <typeparam name="T">The type to attempt to get the property as</typeparam>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The property value to set.</param>
        /// <param name="expectedType">The expected PropertyType.</param>
        private void AssertTrySetPropertyMapsTypeParam<T>(string propertyName, T value, PropertyType expectedType)
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();
            Assert.IsTrue(testEntity.TrySetPropertyByName(propertyName, value));
            Assert.AreEqual(expectedType, testEntity.TryGetPropertyValueByName(propertyName).DynamicType);
            var roundTripValue = testEntity.TryGetPropertyByName(propertyName, default(T));

            AssertPropertyValuesAreEqual(value, roundTripValue);
        }
    }
}
