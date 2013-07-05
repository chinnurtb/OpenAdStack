// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityWrapperBaseFixture.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test EntityWrapperBase class.</summary>
    [TestClass]
    public class EntityWrapperBaseFixture
    {
        /// <summary>Test we can construct from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var wrappedEntity = new Entity { EntityCategory = TestEntity.CategoryName };
            var testEntity = new TestEntity(wrappedEntity);

            Assert.AreSame(wrappedEntity, testEntity.WrappedEntity);
        }

        /// <summary>Verify that the derived class validation method is called during construction from IEntity.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void ValidationFailsOnConstructFromIEntityIfIncomplete()
        {
            var wrappedEntity = new Entity();
            wrappedEntity.EntityCategory = string.Empty;
            new TestEntity(wrappedEntity);
        }

        /// <summary>Verify that the derived class validation method is called during construction from IRawEntity.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void ValidationFailsOnConstructFromRawEntityIfNull()
        {
            new TestEntity(new EntityId(), string.Empty, null);
        }

        /// <summary>Test we can get the members of the wrapped entity.</summary>
        [TestMethod]
        public void GetWrappedMembers()
        {
            var testEntity = TestEntityBuilder.BuildTestEntityPopulated();

            Assert.AreSame(testEntity.WrappedEntity.ExternalEntityId, testEntity.ExternalEntityId);
            Assert.AreEqual(testEntity.WrappedEntity.ExternalName, testEntity.ExternalName);
            Assert.AreEqual(testEntity.WrappedEntity.EntityCategory, testEntity.EntityCategory);
            Assert.AreEqual(testEntity.WrappedEntity.ExternalType, testEntity.ExternalType);
            Assert.AreEqual(testEntity.WrappedEntity.CreateDate, testEntity.CreateDate);
            Assert.AreEqual(testEntity.WrappedEntity.LastModifiedDate, testEntity.LastModifiedDate);
            Assert.AreEqual(testEntity.WrappedEntity.LocalVersion, testEntity.LocalVersion);
            Assert.AreEqual(testEntity.WrappedEntity.LastModifiedUser, testEntity.LastModifiedUser);
            Assert.AreEqual(testEntity.WrappedEntity.SchemaVersion, testEntity.SchemaVersion);
            Assert.AreSame(testEntity.WrappedEntity.Key, testEntity.Key);
            Assert.AreSame(testEntity.WrappedEntity.Properties, testEntity.Properties);
            Assert.AreSame(testEntity.WrappedEntity.Associations, testEntity.Associations);
        }

        /// <summary>Test we can set the members of the wrapped entity.</summary>
        [TestMethod]
        public void SetWrappedMembers()
        {
            var testEntity = TestEntityBuilder.BuildTestEntity();

            // TODO: sometime when you're board change this to iterate over IEntity properties
            EntityProperty someId = new EntityId();
            EntityProperty someStringValue = "12345";
            EntityProperty someDateValue = DateTime.Now;
            EntityProperty someIntValue = 123;
            var someKey = MockRepository.GenerateStub<IStorageKey>();

            AssertEntityPropertySet(someId, "ExternalEntityId", testEntity);
            AssertEntityPropertySet(someStringValue, "ExternalName", testEntity);
            AssertEntityPropertySet(someStringValue, "EntityCategory", testEntity);
            AssertEntityPropertySet(someStringValue, "ExternalType", testEntity);
            AssertEntityPropertySet(someDateValue, "CreateDate", testEntity);
            AssertEntityPropertySet(someDateValue, "LastModifiedDate", testEntity);
            AssertEntityPropertySet(someIntValue, "LastModifiedUser", testEntity);
            AssertEntityPropertySet(someIntValue, "SchemaVersion", testEntity);
            AssertEntityPropertySet(someIntValue, "LocalVersion", testEntity);
            AssertPropertySet(someKey, "Key", testEntity);

            // Assert Property set
            var someProperty = new EntityProperty();
            testEntity.Properties.Add(someProperty);
            Assert.IsTrue(testEntity.Properties.Contains(someProperty));

            // Assert Association set
            var someAssociation = new Association();
            testEntity.Associations.Add(someAssociation);
            Assert.IsTrue(testEntity.Associations.Contains(someAssociation));
        }

        /// <summary>Category mismatch base class handler throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void CategoryMismatchFails()
        {
            var entity = new Entity
                { ExternalEntityId = new EntityId(), EntityCategory = PartnerEntity.CategoryName };
            new TestEntity(entity);
        }

        /// <summary>Category mismatch base class handler throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void RequiredPropertyMissingFails()
        {
            var entity = new Entity { EntityCategory = TestEntity.CategoryName };
            new TestEntity(entity, "ExternalEntityId");
        }

        /// <summary>Helper method to round-trip assert an EntityProperty Set</summary>
        /// <param name="value">The property value to set.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="targetObject">The object with the property.</param>
        private static void AssertEntityPropertySet(EntityProperty value, string propertyName, EntityWrapperBase targetObject)
        {
            targetObject.GetType().GetProperty(propertyName).SetValue(targetObject, value, null);
            var entityProperty = (EntityProperty)targetObject.GetType().GetProperty(propertyName).GetValue(targetObject, null);
            Assert.AreEqual(value.Value, entityProperty.Value);
            Assert.AreEqual(propertyName, entityProperty.Name);
        }

        /// <summary>Helper method to round-trip assert a non-EntityProperty Set</summary>
        /// <param name="value">The property value to set.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="targetObject">The object with the property.</param>
        private static void AssertPropertySet(object value, string propertyName, EntityWrapperBase targetObject)
        {
            targetObject.GetType().GetProperty(propertyName).SetValue(targetObject, value, null);
            var property = targetObject.GetType().GetProperty(propertyName).GetValue(targetObject, null);
            Assert.AreEqual(value, property);
        }
    }
}