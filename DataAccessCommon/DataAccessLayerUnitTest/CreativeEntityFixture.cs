// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreativeEntityFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test fixture for CreativeEntity class.</summary>
    [TestClass]
    public class CreativeEntityFixture
    {
        /// <summary>Entity object with creative properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", CreativeEntity.CreativeEntityCategory),
                ExternalType = new EntityProperty("ExternalType", "FooThingy"),
                CreateDate = new EntityProperty("CreateDate", DateTime.Now),
                LastModifiedDate = new EntityProperty("LastModifiedDate", DateTime.Now),
                LocalVersion = new EntityProperty("LocalVersion", 1),
                Key = MockRepository.GenerateStub<IStorageKey>()
            };
        }
        
        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var creativeEntity = new CreativeEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, creativeEntity.WrappedEntity);

            var blobEntityBase = EntityWrapperBase.BuildWrappedEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, EntityWrapperBase.SafeUnwrapEntity(blobEntityBase));
        }
        
        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var creativeEntity = new CreativeEntity(this.wrappedEntity);
            var creativeEntityWrap = new CreativeEntity(creativeEntity);
            Assert.AreSame(this.wrappedEntity, creativeEntityWrap.WrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Creative.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailValidationIfCategoryPropertyNotCampaign()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            var creativeEntity = new CreativeEntity(this.wrappedEntity);
        }

        /// <summary>Test we can construct from a json object.</summary>
        [TestMethod]
        public void ConstructFromJson()
        {
            var externalEntityId = new EntityId();
            var creativeEntity = TestEntityBuilder.BuildCreativeEntity(externalEntityId);

            Assert.AreEqual(externalEntityId, (EntityId)creativeEntity.ExternalEntityId);
            Assert.AreEqual(CreativeEntity.CreativeEntityCategory, (string)creativeEntity.EntityCategory);
            Assert.AreEqual(TestEntityBuilder.ExternalName, (string)creativeEntity.ExternalName);
        }
    }
}
