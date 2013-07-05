// -----------------------------------------------------------------------
// <copyright file="BudgetAllocationHistoryFixture.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using DataAccessLayer;
using DynamicAllocationActivities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Serialization;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Unit-test fixture for BudgetAllocationHistory
    /// </summary>
    [TestClass]
    public class BudgetAllocationHistoryFixture
    {
        /// <summary>Stubbed entity repository for testing</summary>
        private IEntityRepository entityRepository;

        /// <summary>Company entity for testing</summary>
        private CompanyEntity companyEntity;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.entityRepository = MockRepository.GenerateMock<IEntityRepository>();

            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(), "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");

            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(), "Bar");
        }

        /// <summary>
        /// Happy-path test we can get allocation history index.
        /// </summary>
        [TestMethod]
        public void GetAllocationHistoryIndexSuccess()
        {
            var historyIndexBlobEntityId = new EntityId();
            var expectedIndex = new List<HistoryElement>
                {
                    new HistoryElement
                        {
                            AllocationStartTime = (new PropertyValue(PropertyType.Date, DateTime.UtcNow)).ToString(),
                            AllocationOutputsId = new EntityId()
                        },
                    new HistoryElement
                        {
                            AllocationStartTime = (new PropertyValue(PropertyType.Date, DateTime.UtcNow)).ToString(),
                            AllocationOutputsId = new EntityId()
                        }
                };

            // This is technically an unnecessary step but it matches what the activity does
            var indexJson = AppsJsonSerializer.SerializeObject(expectedIndex);
            
            var historyIndexBlob = BlobEntity.BuildBlobEntity(historyIndexBlobEntityId, indexJson);
            RepositoryStubUtilities.SetupGetEntityStub(this.entityRepository, historyIndexBlobEntityId, historyIndexBlob, false);

            var indexAssociation = new Association
            {
                ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                TargetEntityId = historyIndexBlobEntityId
            };

            this.campaignEntity.Associations.Add(indexAssociation);

            var history = new BudgetAllocationHistory(this.entityRepository, this.companyEntity, this.campaignEntity);
            var actualIndex = history.RetrieveAllocationHistoryIndex().ToList();

            Assert.IsNotNull(actualIndex);
            Assert.AreEqual(expectedIndex.Count, actualIndex.Count);
            Assert.AreEqual(expectedIndex[0].AllocationStartTime, actualIndex[0].AllocationStartTime);
            Assert.AreEqual(expectedIndex[0].AllocationOutputsId, actualIndex[0].AllocationOutputsId);
        }

        /// <summary>
        /// Test we fail correctly when the history index cannot be deserialized.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetAllocationHistoryIndexBlobDeserializeFails()
        {
            var historyIndexBlobEntityId = new EntityId();
            var indexJson = "This is not the json you're looking for.";
            var historyIndexBlob = BlobEntity.BuildBlobEntity(historyIndexBlobEntityId, indexJson);
            RepositoryStubUtilities.SetupGetEntityStub(this.entityRepository, historyIndexBlobEntityId, historyIndexBlob, false);

            var indexAssociation = new Association
            {
                ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                TargetEntityId = historyIndexBlobEntityId
            };

            this.campaignEntity.Associations.Add(indexAssociation);

            var history = new BudgetAllocationHistory(this.entityRepository, this.companyEntity, this.campaignEntity);
            history.RetrieveAllocationHistoryIndex();
        }

        /// <summary>
        /// Test we fail correctly when the history index is not found.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetAllocationHistoryIndexBlobNotFound()
        {
            var historyIndexBlobEntityId = new EntityId();
            var indexAssociation = new Association
            {
                ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                TargetEntityId = historyIndexBlobEntityId
            };

            this.campaignEntity.Associations.Add(indexAssociation);

            var history = new BudgetAllocationHistory(this.entityRepository, this.companyEntity, this.campaignEntity);

            RepositoryStubUtilities.SetupGetEntityStub(this.entityRepository, historyIndexBlobEntityId, null, true);
            history.RetrieveAllocationHistoryIndex();
        }

        /// <summary>
        /// Test we fail correctly when the history index association is not present on the campaign entity.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetAllocationHistoryAssociationNotPresent()
        {
            var history = new BudgetAllocationHistory(this.entityRepository, this.companyEntity, this.campaignEntity);
            history.RetrieveAllocationHistoryIndex();
        }
    }
}
