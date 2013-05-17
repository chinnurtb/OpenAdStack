// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserEntityFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test UserEntity class.</summary>
    [TestClass]
    public class UserEntityFixture
    {
        /// <summary>Entity object with user properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                // TODO: As if this comment wasn't somewhere already - really need to make it impossible to set
                // TODO: an IEntity property without the name being set
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", UserEntity.UserEntityCategory),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.ExternalType),
                CreateDate = new EntityProperty("CreateDate", DateTime.Now),
                LastModifiedDate = new EntityProperty("LastModifiedDate", DateTime.Now),
                LocalVersion = new EntityProperty("LocalVersion", 1),
                Key = MockRepository.GenerateStub<IStorageKey>(),
                Properties =
                {
                    new EntityProperty(UserEntity.UserIdPropertyName, TestEntityBuilder.UserId),
                    new EntityProperty(UserEntity.FullNamePropertyName, TestEntityBuilder.FullName),
                    new EntityProperty(UserEntity.ContactEmailPropertyName, TestEntityBuilder.ContactEmail),
                    new EntityProperty(UserEntity.FirstNamePropertyName, TestEntityBuilder.FirstName),
                    new EntityProperty(UserEntity.LastNamePropertyName, TestEntityBuilder.LastName),
                    new EntityProperty(UserEntity.ContactPhonePropertyName, TestEntityBuilder.ContactPhone)
                }
            };
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, userEntity.WrappedEntity);
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            var userEntityWrap = new UserEntity(userEntity);
            Assert.AreSame(this.wrappedEntity, userEntityWrap.WrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not User.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailConstructionIfCategoryPropertyNotUser()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            var userEntity = new UserEntity(this.wrappedEntity);
        }

        /// <summary>Test we can construct from a json object.</summary>
        [TestMethod]
        public void ConstructFromJson()
        {
            var externalId = new EntityId();
            var userEntity = TestEntityBuilder.BuildUserEntity(externalId);

            Assert.AreEqual(externalId, (EntityId)userEntity.ExternalEntityId);
            Assert.AreEqual(TestEntityBuilder.UserId, (string)userEntity.UserId);
            Assert.AreEqual(TestEntityBuilder.FullName, (string)userEntity.FullName);
            Assert.AreEqual(TestEntityBuilder.FirstName, (string)userEntity.FirstName);
            Assert.AreEqual(TestEntityBuilder.LastName, (string)userEntity.LastName);
            Assert.AreEqual(TestEntityBuilder.ContactEmail, (string)userEntity.ContactEmail);
            Assert.AreEqual(TestEntityBuilder.ContactPhone, (string)userEntity.ContactPhone);
            Assert.AreEqual(UserEntity.UserEntityCategory, (string)userEntity.EntityCategory);
        }

        // TODO: Handle associations in Json
        // TODO: validation asserts

        /// <summary>Verify we can correctly get and set UserId property.</summary>
        [TestMethod]
        public void UserIdProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.UserId, "newabc123", UserEntity.UserIdPropertyName, userEntity);
        }
        
        /// <summary>Verify we can correctly get and set FullName property.</summary>
        [TestMethod]
        public void FullNameProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.FullName, "New Full Name", UserEntity.FullNamePropertyName, userEntity);
        }

        /// <summary>Verify we can correctly get and set ContactEmail property.</summary>
        [TestMethod]
        public void ContactEmailProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.ContactEmail, "New Contact Email", UserEntity.ContactEmailPropertyName, userEntity);
        }

        /// <summary>Verify we can correctly get and set FirstName property.</summary>
        [TestMethod]
        public void FirstNameProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.FirstName, "New First Name", UserEntity.FirstNamePropertyName, userEntity);
        }

        /// <summary>Verify we can correctly get and set LastName property.</summary>
        [TestMethod]
        public void LastNameProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.LastName, "New Last Name", UserEntity.LastNamePropertyName, userEntity);
        }

        /// <summary>Verify we can correctly get and set ContactPhone property.</summary>
        [TestMethod]
        public void ContactPhoneProperty()
        {
            var userEntity = new UserEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.ContactPhone, "321-456-7890", UserEntity.ContactPhonePropertyName, userEntity);
        }
    }
}
