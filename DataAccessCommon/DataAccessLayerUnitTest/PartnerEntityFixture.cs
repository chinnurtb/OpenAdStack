// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartnerEntityFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test PartnerEntity class.</summary>
    [TestClass]
    public class PartnerEntityFixture
    {
        /// <summary>Wrapped Entity object with for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", PartnerEntity.PartnerEntityCategory),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.ExternalType)
            };
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var partnerEntity = new PartnerEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, partnerEntity.WrappedEntity);

            var blobEntityBase = EntityWrapperBase.BuildWrappedEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, EntityWrapperBase.SafeUnwrapEntity(blobEntityBase));
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var partnerEntity = new PartnerEntity(this.wrappedEntity);
            var partnerEntityWrap = new PartnerEntity(partnerEntity);
            Assert.AreSame(this.wrappedEntity, partnerEntityWrap.WrappedEntity);
        }

        /// <summary>Test we can construct from a json object.</summary>
        [TestMethod]
        public void ConstructFromJson()
        {
            var externalEntityId = new EntityId();
            var partnerEntity = TestEntityBuilder.BuildPartnerEntity(externalEntityId);

            // We currently don't use any properties beyond IEntity but assert those that are relevant
            Assert.AreEqual(externalEntityId, (EntityId)partnerEntity.ExternalEntityId);
            Assert.AreEqual(TestEntityBuilder.ExternalName, (string)partnerEntity.ExternalName);
            Assert.AreEqual(TestEntityBuilder.ExternalType, (string)partnerEntity.ExternalType);
            Assert.AreEqual(PartnerEntity.PartnerEntityCategory, (string)partnerEntity.EntityCategory);
        }

        /// <summary>Validate that entity construction fails if category is not Partner.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailValidationIfCategoryPropertyNotPartner()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            var partnerEntity = new PartnerEntity(this.wrappedEntity);
        }
    }
}
