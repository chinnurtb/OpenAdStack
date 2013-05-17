// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompanyEntityFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test CompanyEntity class.</summary>
    [TestClass]
    public class CompanyEntityFixture
    {
        /// <summary>Entity object with company properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", CompanyEntity.CompanyEntityCategory),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.AgencyExternalType),
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
            var companyEntity = new CompanyEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, companyEntity.WrappedEntity);

            var blobEntityBase = EntityWrapperBase.BuildWrappedEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, EntityWrapperBase.SafeUnwrapEntity(blobEntityBase));
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var companyEntity = new CompanyEntity(this.wrappedEntity);
            var companyEntityWrap = new CompanyEntity(companyEntity);
            Assert.AreSame(this.wrappedEntity, companyEntityWrap.WrappedEntity);
        }

        /// <summary>Test we can construct from a json object.</summary>
        [TestMethod]
        public void ConstructFromJson()
        {
            var externalEntityId = new EntityId();
            var companyEntity = TestEntityBuilder.BuildCompanyEntity(externalEntityId);

            // We currently don't use any properties beyond IEntity but assert those that are relevant
            Assert.AreEqual(externalEntityId, (EntityId)companyEntity.ExternalEntityId);
            Assert.AreEqual(TestEntityBuilder.ExternalName, (string)companyEntity.ExternalName);
            Assert.AreEqual(TestEntityBuilder.AgencyExternalType, (string)companyEntity.ExternalType);
            Assert.AreEqual(CompanyEntity.CompanyEntityCategory, (string)companyEntity.EntityCategory);
        }

        /// <summary>Validate that entity construction fails if category is not Company.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FailValidationIfCategoryPropertyNotCompany()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            var companyEntity = new CompanyEntity(this.wrappedEntity);
        }
    }
}
