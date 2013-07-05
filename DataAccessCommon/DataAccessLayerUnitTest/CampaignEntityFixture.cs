// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignEntityFixture.cs" company="Rare Crowds Inc">
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
    /// <summary>Test fixture for CampaignEntity class.</summary>
    [TestClass]
    public class CampaignEntityFixture
    {
        /// <summary>Entity object with campaign properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", CampaignEntity.CategoryName),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.ExternalType),
                CreateDate = new EntityProperty("CreateDate", DateTime.Now),
                LastModifiedDate = new EntityProperty("LastModifiedDate", DateTime.Now),
                LocalVersion = new EntityProperty("LocalVersion", 1),
                Key = MockRepository.GenerateStub<IStorageKey>(),
                Properties =
                {
                    new EntityProperty(CampaignEntity.BudgetPropertyName, TestEntityBuilder.Budget),
                    new EntityProperty(CampaignEntity.StartDatePropertyName, TestEntityBuilder.StartDate),
                    new EntityProperty(CampaignEntity.EndDatePropertyName, TestEntityBuilder.EndDate),
                    new EntityProperty(CampaignEntity.PersonaNamePropertyName, TestEntityBuilder.PersonaName)
                }
            };
        }
        
        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, campaignEntity.WrappedEntity);
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            var campaignEntityWrap = new CampaignEntity(campaignEntity);
            Assert.AreSame(this.wrappedEntity, campaignEntityWrap.WrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Campaign.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void FailValidationIfCategoryPropertyNotCampaign()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new CampaignEntity(this.wrappedEntity);
        }

        /// <summary>Test we can construct from a raw entity.</summary>
        [TestMethod]
        public void ConstructFromRawEntity()
        {
            var externalEntityId = new EntityId();
            var campaignEntity = TestEntityBuilder.BuildCampaignEntity(externalEntityId);

            Assert.AreEqual(externalEntityId, (EntityId)campaignEntity.ExternalEntityId);
            Assert.AreEqual(CampaignEntity.CategoryName, (string)campaignEntity.EntityCategory);
            Assert.AreEqual(TestEntityBuilder.ExternalName, (string)campaignEntity.ExternalName);
            Assert.AreEqual(TestEntityBuilder.Budget, (long)campaignEntity.Budget);
            Assert.AreEqual(TestEntityBuilder.StartDate, (DateTime)campaignEntity.StartDate);
            Assert.AreEqual(TestEntityBuilder.EndDate, (DateTime)campaignEntity.EndDate);
            Assert.AreEqual(TestEntityBuilder.PersonaName, (string)campaignEntity.PersonaName);

            var entityBase = this.wrappedEntity.BuildWrappedEntity();
            Assert.AreSame(this.wrappedEntity, entityBase.SafeUnwrapEntity());
        }

        /// <summary>Verify we can correctly get and set Budget property.</summary>
        [TestMethod]
        public void BudgetProperty()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.Budget, "1001", CampaignEntity.BudgetPropertyName, campaignEntity);
        }

        /// <summary>Verify we can correctly get and set StartDate property.</summary>
        [TestMethod]
        public void StartDateProperty()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.StartDate, TestEntityBuilder.StartDate.AddDays(1), CampaignEntity.StartDatePropertyName, campaignEntity);
        }

        /// <summary>Verify we can correctly get and set EndDate property.</summary>
        [TestMethod]
        public void EndDateProperty()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.EndDate, TestEntityBuilder.EndDate.AddDays(-1), CampaignEntity.EndDatePropertyName, campaignEntity);
        }

        /// <summary>Verify we can correctly get and set PersonaName property.</summary>
        [TestMethod]
        public void PersonaNameProperty()
        {
            var campaignEntity = new CampaignEntity(this.wrappedEntity);
            EntityTestHelpers.AssertPropertyAccessors(TestEntityBuilder.PersonaName, "NotTheUltimate", CampaignEntity.PersonaNamePropertyName, campaignEntity);
        }
    }
}
