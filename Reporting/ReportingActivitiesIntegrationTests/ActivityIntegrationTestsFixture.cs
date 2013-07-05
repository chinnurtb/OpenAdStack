// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivityIntegrationTestsFixture.cs" company="Rare Crowds Inc">
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
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportingActivities;
using ReportingUtilities;
using Rhino.Mocks;
using SimulatedDataStore;
using Utilities.Serialization;

namespace ReportingActivitiesIntegrationTests
{
    /// <summary>Integration test fixture for Reporting activities</summary>
    [TestClass]
    public class ActivityIntegrationTestsFixture
    {
        /// <summary>DA campaign stub for testing.</summary>
        private static readonly DynamicAllocationCampaignTestStub campaignStub = new DynamicAllocationCampaignTestStub();

        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>company entity id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>campaign entity id for testing.</summary>
        private EntityId campaignEntityId;

        /// <summary>campaign owner id for testing</summary>
        private string campaignOwnerId;

        /// <summary>activity request for testing.</summary>
        private ActivityRequest request;

        /// <summary>One time class initialization</summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
            {
                new AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider(),
                new AppNexusActivities.Measures.AppNexusMeasureSourceProvider(),
                new GoogleDfpActivities.Measures.DfpMeasureSourceProvider()
            });
        }

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Set up an in-memory simulated repository. There are too many interior saves
            // for Rhino to be effective.
            this.repository = new SimulatedEntityRepository();

            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.campaignOwnerId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));

            this.request = new ActivityRequest
            {
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.campaignOwnerId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntityId },
                    { ReportingActivityValues.ReportType, ReportTypes.ClientCampaignBilling }
                }
            };
        }

        /// <summary>Happy path process request</summary>
        [TestMethod]
        public void ProcessRequestSuccess()
        {
            // Set up this test with data to support the whole call chain of the activity
            campaignStub.SetupCampaign(this.repository, this.companyEntityId, this.campaignEntityId, this.campaignOwnerId);

            // Set up our activity
            // Use the real handler factory
            var activity = Activity.CreateActivity(
                    typeof(CreateCampaignReportActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                    ActivityTestHelpers.SubmitActivityRequest) as CreateCampaignReportActivity;

            // No SaveLegacyConversion flag on request
            this.request.Values[ReportingActivityValues.VerboseReport] = string.Empty;
            var result = activity.Run(this.request);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Assert all the data in the chain to the generated report is present
            DateTime reportDate;
            var report = GetReportFromCampaign(
                this.repository, null, this.campaignEntityId, ReportTypes.ClientCampaignBilling, out reportDate);
            Assert.IsFalse(string.IsNullOrEmpty(report));
        }

        /// <summary>Happy path process request</summary>
        [TestMethod]
        [Ignore]
        public void ProcessRequestLegacyConversionSuccess()
        {
            // Set up the test data for a legacy campaign
            SetupLegacyTestCampaign(this.repository, this.companyEntityId, this.campaignEntityId);

            // Set up our activity
            // Use the real handler factory
            var activity = Activity.CreateActivity(
                    typeof(CreateCampaignReportActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                    ActivityTestHelpers.SubmitActivityRequest) as CreateCampaignReportActivity;

            this.request.Values[ReportingActivityValues.VerboseReport] = string.Empty;
            this.request.Values[ReportingActivityValues.SaveLegacyConversion] = string.Empty;
            var result = activity.Run(this.request);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Assert all the data in the chain to the generated report is present
            DateTime reportDate;
            var report = GetReportFromCampaign(
                this.repository, null, this.campaignEntityId, ReportTypes.ClientCampaignBilling, out reportDate);
            Assert.IsFalse(string.IsNullOrEmpty(report));
        }

        /// <summary>Get a report from a campaign</summary>
        /// <param name="repository">the repository</param>
        /// <param name="context">the repository request context</param>
        /// <param name="campaignEntityId">the campaign id</param>
        /// <param name="reportType">The report type.</param>
        /// <param name="reportDate">The report date.</param>
        /// <returns>The report string.</returns>
        internal static string GetReportFromCampaign(
            IEntityRepository repository, 
            RequestContext context, 
            EntityId campaignEntityId, 
            string reportType, 
            out DateTime reportDate)
        {
            var campaignEntity = repository.GetEntity<CampaignEntity>(context, campaignEntityId);
            var currentReportsJson = campaignEntity.TryGetPropertyByName<string>(ReportingPropertyNames.CurrentReports, null);
            var currentReports = AppsJsonSerializer.DeserializeObject<Dictionary<string, CurrentReportItem>>(currentReportsJson);
            reportDate = currentReports[reportType].ReportDate;
            var reportBlobId = currentReports[reportType].ReportEntityId;
            var reportBlob = repository.GetEntity<BlobEntity>(context, reportBlobId);
            return reportBlob.DeserializeBlob<string>();
        }

        /// <summary>Stubbed wrapper for legacy campaign setup.</summary>
        /// <param name="repository">The entity Repository.</param>
        /// <param name="companyEntityId">The company entity id.</param>
        /// <param name="campaignEntityId">The campaign entity id.</param>
        private static void SetupLegacyTestCampaign(
             IEntityRepository repository, EntityId companyEntityId, EntityId campaignEntityId)
        {
            // TODO: This is just stubbed for now until we determine if we need to preserve this scenario.
            Assert.IsNotNull(campaignEntityId);
            Assert.IsNotNull(repository);
            Assert.IsNotNull(companyEntityId);
            ////LegacyCampaignHelpers.SetupLegacyCampaign(localRepository, companyEntityId, campaignEntityId, campaignOwnerId);
        }
    }
}
