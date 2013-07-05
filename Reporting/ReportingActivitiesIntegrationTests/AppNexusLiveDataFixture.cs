// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusLiveDataFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Globalization;
using System.IO;
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
using TestUtilities;

namespace ReportingActivitiesIntegrationTests
{
    /// <summary>Test fixture for Reallocation reporting</summary>
    [TestClass]
    public class AppNexusLiveDataFixture
    {
        /// <summary>DA campaign stub for testing.</summary>
        private static readonly DynamicAllocationCampaignTestStub campaignStub = new DynamicAllocationCampaignTestStub();

        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>activity request for testing.</summary>
        private ActivityRequest request;

        /// <summary>One time class initialization</summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            // Force Azure emulated storage to start. DSService can still be running
            // but the emulated storage not available. The most reliable way to make sure
            // it's running and available is to stop it then start again.
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);

            AllocationParametersDefaults.Initialize();
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
            this.repository = new SimulatedEntityRepository(
                ConfigurationManager.AppSettings["IndexLocal.ConnectionString"],
                ConfigurationManager.AppSettings["EntityLocal.ConnectionString"],
                ConfigurationManager.AppSettings["IndexReadOnly.ConnectionString"],
                ConfigurationManager.AppSettings["EntityReadOnly.ConnectionString"]);

            this.request = new ActivityRequest
            {
                Values =
                {
                    { EntityActivityValues.AuthUserId, "user123" },
                }
            };
        }

        /// <summary>Generate a report from a test campaign.</summary>
        [TestMethod]
        [Ignore]
        public void CreateReportFromTestLegacyCampaign()
        {
            // For this test want a local emulated store
            var localRepository = ((SimulatedEntityRepository)this.repository).LocalRepository;

            // Set up the test data for a legacy campaign
            var companyEntityId = new EntityId();
            var campaignEntityId = new EntityId();
            SetupLegacyTestCampaign(localRepository, companyEntityId, campaignEntityId);

            // Set up our activity with a local store
            var activity = Activity.CreateActivity(
                    typeof(CreateCampaignReportActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), localRepository } },
                    ActivityTestHelpers.SubmitActivityRequest) as CreateCampaignReportActivity;

            this.request.Values[EntityActivityValues.CompanyEntityId] = companyEntityId;
            this.request.Values[EntityActivityValues.CampaignEntityId] = campaignEntityId;
            this.request.Values[ReportingActivityValues.VerboseReport] = string.Empty;
            this.request.Values[ReportingActivityValues.SaveLegacyConversion] = string.Empty;
            this.request.Values[ReportingActivityValues.ReportType] = ReportTypes.ClientCampaignBilling;
            var result = activity.Run(this.request);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Assert all the data in the chain to the generated report is present
            DateTime reportDate;
            var context = new RequestContext { ExternalCompanyId = companyEntityId };
            var report = ActivityIntegrationTestsFixture.GetReportFromCampaign(
                this.repository, context, campaignEntityId, ReportTypes.ClientCampaignBilling, out reportDate);
            Assert.IsFalse(string.IsNullOrEmpty(report));
        }

        /// <summary>Generate a report from an existing legacy campaign.</summary>
        [TestMethod]
        [Ignore]
        public void CreateReportFromExistingLegacyCampaign()
        {
            ////new EntityId("f771702d-9aaa-41c7-8488-41fc73972512"), 
            ////new EntityId("70a15d8d-f7f4-4bbd-982f-cf8e3b481c74"), 
            ////new EntityId("eebac3d2-5f33-4d82-816f-baaa6afc4353"), 
            ////new EntityId("f7f75529-3cc7-4df1-baa3-e850fde22669"), 
            ////new EntityId("59027117-6b6d-495a-97c7-b9d487f714a8"), 
            ////new EntityId("61b57beb-883f-4d49-915e-5a5d324b644a")
            ////new EntityId("f6102ce1-4880-4366-970f-ccfc2fcba093"),
            ////new EntityId("83cd1ba3-a2e0-4bcc-b0cc-8aae42077c87"),
            ////new EntityId("8b9a3f92-cb1d-4f5d-bd5e-24ac951d0538"),
            ////new EntityId("11720b77-abdb-4bc3-936e-e6d84dee93a6"),
            var companyId = new EntityId("360b1a09-6a66-4dc2-898e-4a370e4079d0");
            var campaignId = new EntityId("11720b77-abdb-4bc3-936e-e6d84dee93a6");

            // Touch the company to pull it down
            var context = new RequestContext { ExternalCompanyId = companyId };
            this.repository.GetEntity(context, companyId);

            // Set up our activity the SimulatedEntityRepository.
            // This will read from whatever source is configured for the read repository.
            var activity = Activity.CreateActivity(
                    typeof(CreateCampaignReportActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                    ActivityTestHelpers.SubmitActivityRequest) as CreateCampaignReportActivity;

            this.request.Values[EntityActivityValues.CompanyEntityId] = companyId;
            this.request.Values[EntityActivityValues.CampaignEntityId] = campaignId;
            this.request.Values[ReportingActivityValues.VerboseReport] = string.Empty;
            this.request.Values[ReportingActivityValues.SaveLegacyConversion] = string.Empty;

            // Generate campaign billing report
            this.request.Values[ReportingActivityValues.ReportType] = ReportTypes.ClientCampaignBilling;
            var result = activity.Run(this.request);
            ActivityTestHelpers.AssertValidSuccessResult(result);

            DateTime reportDate;
            var report = ActivityIntegrationTestsFixture.GetReportFromCampaign(
                this.repository, context, campaignId, ReportTypes.ClientCampaignBilling, out reportDate);
            Assert.IsFalse(string.IsNullOrEmpty(report));

            WriteReport(@"C:\ReportFiles", ReportTypes.ClientCampaignBilling, campaignId, report, reportDate);

            // Generate data provider billing report
            this.request.Values[ReportingActivityValues.ReportType] = ReportTypes.ClientCampaignBilling;
            result = activity.Run(this.request);

            report = ActivityIntegrationTestsFixture.GetReportFromCampaign(
                this.repository, context, campaignId, ReportTypes.ClientCampaignBilling, out reportDate);
            Assert.IsFalse(string.IsNullOrEmpty(report));

            WriteReport(@"C:\ReportFiles", ReportTypes.DataProviderBilling, campaignId, report, reportDate);
        }

        /// <summary>Write a report to a csv file.</summary>
        /// <param name="path">Directory to write report.</param>
        /// <param name="reportType">The report type.</param>
        /// <param name="campaignId">The campaign entity id.</param>
        /// <param name="report">The report string.</param>
        /// <param name="reportDate">The report date.</param>
        private static void WriteReport(string path, string reportType, EntityId campaignId, string report, DateTime reportDate)
        {
            var reportDay = reportDate.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            var reportTime = reportDate.ToString("HH_mm", CultureInfo.InvariantCulture);

            var reportFile = @"{0}_{1}_{2}_{3}.csv".FormatInvariant(
                reportType, campaignId, reportDay, reportTime);

            var fullPath = Path.Combine(Path.GetFullPath(path), reportFile);

            File.WriteAllText(fullPath, report);
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