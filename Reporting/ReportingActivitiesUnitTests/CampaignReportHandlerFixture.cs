// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignReportHandlerFixture.cs" company="Rare Crowds Inc">
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
using DynamicAllocation;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportingActivities;
using ReportingTools;
using ReportingUtilities;
using Rhino.Mocks;
using Utilities;
using Utilities.Serialization;

namespace ReportingActivitiesUnitTests
{
    /// <summary>
    /// Unit test fixture for CampaignReportBuilder
    /// </summary>
    [TestClass]
    public class CampaignReportHandlerFixture
    {
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>campaign id for testing</summary>
        private EntityId campaignEntityId;

        /// <summary>company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>report generators for testing.</summary>
        private IDictionary<DeliveryNetworkDesignation, IReportGenerator> generators;

        /// <summary>fake report for testing.</summary>
        private StringBuilder expectedReport;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.expectedReport = new StringBuilder("the report");
            var generator = MockRepository.GenerateStub<IReportGenerator>();
            generator.Stub(f => f.BuildReport(Arg<string>.Is.Anything, Arg<bool>.Is.Anything))
                .Return(this.expectedReport);
            this.generators = new Dictionary<DeliveryNetworkDesignation, IReportGenerator> { { DeliveryNetworkDesignation.AppNexus, generator } };
        }

        /// <summary>Happy path construction success.</summary>
        [TestMethod]
        public void ConstructorSuccess()
        {
            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            Assert.AreSame(this.repository, handler.Repository);
            Assert.AreSame(this.generators, handler.ReportGenerators);
            Assert.AreEqual(this.campaignEntityId, handler.CampaignEntityId);
            Assert.AreEqual(true, handler.BuildVerbose);
            Assert.AreEqual("SomeReport", handler.ReportType);
        }

        /// <summary>Null generators should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullRepositoryFail()
        {
            new CampaignReportHandler(null, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
        }

        /// <summary>Null generators should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullGeneratorsFail()
        {
            new CampaignReportHandler(this.repository, null, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
        }

        /// <summary>Null company entity id should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullCompanyIdFail()
        {
            new CampaignReportHandler(this.repository, this.generators, null, this.campaignEntityId, true, "SomeReport");
        }

        /// <summary>Null campaign entity id should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullCampaignIdFail()
        {
            new CampaignReportHandler(this.repository, this.generators, this.companyEntityId, null, true, "SomeReport");
        }

        /// <summary>Null report type should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullReportTypeFail()
        {
            new CampaignReportHandler(this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, null);
        }

        /// <summary>Null report type should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorEmptyReportTypeFail()
        {
            new CampaignReportHandler(this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, string.Empty);
        }

        /// <summary>No generators should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ExecuteNoGeneratorsFail()
        {
            this.generators = new Dictionary<DeliveryNetworkDesignation, IReportGenerator>();
            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Multiple generators should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void MultipleGeneratorsNotSupportedYet()
        {
            var extraGenerator = MockRepository.GenerateStub<IReportGenerator>();
            this.generators.Add(DeliveryNetworkDesignation.GoogleDfp, extraGenerator);
            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Anything but AppNexus generator should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void OnlyAppNexusSupported()
        {
            var extraGenerator = MockRepository.GenerateStub<IReportGenerator>();
            this.generators = new Dictionary<DeliveryNetworkDesignation, IReportGenerator>();
            this.generators.Add(DeliveryNetworkDesignation.GoogleDfp, extraGenerator);
            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Happy path Execute.</summary>
        [TestMethod]
        public void ExecuteSuccess()
        {
            BlobEntity savedBlob = null;
            IEnumerable<EntityProperty> savedProperties = null;
            this.SetupRepositoryStubs(e => savedBlob = e, c => savedProperties = c, false, false, false);

            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            var actualReport = handler.Execute();
            
            Assert.AreEqual(0, actualReport.Count);
            Assert.AreEqual(this.expectedReport.ToString(), savedBlob.DeserializeBlob<string>());
            var currentReportsJson = savedProperties.Single().Value;
            var currentReports =
                AppsJsonSerializer.DeserializeObject<Dictionary<string, CurrentReportItem>>(currentReportsJson);
            var currentItem = currentReports["SomeReport"];
            Assert.AreEqual(savedBlob.ExternalEntityId, (EntityId)currentItem.ReportEntityId);
            Assert.AreEqual((DateTime)savedBlob.LastModifiedDate, currentItem.ReportDate);
        }

        /// <summary>New report reference merged with existing references on campaign.</summary>
        [TestMethod]
        public void ExecuteSuccessExistingReports()
        {
            // Set up a campaign with existing report references
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                    this.campaignEntityId, "foo", 0, DateTime.UtcNow, DateTime.UtcNow, "foo");

            var existingReports = new Dictionary<string, CurrentReportItem>
                {
                    {
                        "SomeReport", 
                        new CurrentReportItem { ReportDate = DateTime.UtcNow, ReportEntityId = new EntityId() }
                    },
                    {
                        "SomeOtherReport", 
                        new CurrentReportItem { ReportDate = DateTime.UtcNow, ReportEntityId = new EntityId() }
                    },
                };
            var existingReportsJson = AppsJsonSerializer.SerializeObject(existingReports);

            campaignEntity.TrySetPropertyByName(ReportingPropertyNames.CurrentReports, existingReportsJson);

            // Setup the repository
            BlobEntity savedBlob = null;
            IEnumerable<EntityProperty> savedProperties = null;
            this.SetupRepositoryStubs(e => savedBlob = e, c => savedProperties = c, false, false, false, campaignEntity);

            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();

            // Assert the that the report reference on the entity is the new one
            var newReportsJson = savedProperties.Single().Value;
            var newReports =
                AppsJsonSerializer.DeserializeObject<Dictionary<string, CurrentReportItem>>(newReportsJson);
            Assert.AreEqual(2, newReports.Count);
            Assert.AreEqual(savedBlob.ExternalEntityId, (EntityId)newReports["SomeReport"].ReportEntityId);
        }

        /// <summary>Fail to retrieve campaign during execute.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void ExecuteFailCampaignNotFound()
        {
            this.SetupRepositoryStubs(e => { }, c => { }, false, false, true);

            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Fail to save report blob during execute.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ExecuteFailSaveBlob()
        {
            this.SetupRepositoryStubs(e => { }, c => { }, true, false, false);

            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Fail to save updated campaign during execute.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ExecuteFailSaveCampaign()
        {
            this.SetupRepositoryStubs(e => { }, c => { }, false, true, false);

            var handler = new CampaignReportHandler(
                this.repository, this.generators, this.companyEntityId, this.campaignEntityId, true, "SomeReport");
            handler.Execute();
        }

        /// <summary>Setup repository stubs</summary>
        /// <param name="captureBlob">Lambda to capture saved blob entity.</param>
        /// <param name="captureProperties">Lambda to capture saved campaign properties</param>
        /// <param name="blobSaveFail">True if blob save should fail.</param>
        /// <param name="campaignSaveFail">True if campaign save should fail.</param>
        /// <param name="campaignGetFail">True if campaign get should fail.</param>
        /// <param name="campaignToGet">Campaign to return from get or null for default.</param>
        private void SetupRepositoryStubs(
            Action<BlobEntity> captureBlob, 
            Action<IEnumerable<EntityProperty>> captureProperties, 
            bool blobSaveFail, 
            bool campaignSaveFail, 
            bool campaignGetFail, 
            CampaignEntity campaignToGet = null)
        {
            var campaignEntity = campaignToGet ?? EntityTestHelpers.CreateTestCampaignEntity(
                    this.campaignEntityId, "foo", 0, DateTime.UtcNow, DateTime.UtcNow, "foo");

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignEntityId, campaignEntity, campaignGetFail);
            RepositoryStubUtilities.SetupSaveEntityStub(this.repository, captureBlob, blobSaveFail);
            RepositoryStubUtilities.SetupTryUpdateEntityStub(this.repository, this.campaignEntityId, captureProperties, campaignSaveFail);
        }
    }
}
