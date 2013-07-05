// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusBillingReportFixture.cs" company="Rare Crowds Inc">
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
using System.IO;
using System.Linq;
using System.Text;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportingTools;
using ReportingUtilities;
using Rhino.Mocks;
using Utilities;

using parseName = DynamicAllocationActivities.RawDeliveryDataParserBase;

namespace ReportingToolsUnitTests
{
    /// <summary>
    /// Unit test fixture for AppNexusBillingReport
    /// </summary>
    [TestClass]
    public class AppNexusBillingReportFixture
    {
        /// <summary>DA campaign stub for testing.</summary>
        private static readonly DynamicAllocationCampaignTestStub campaignStub = new DynamicAllocationCampaignTestStub();

        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>company entity id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>campaign entity id for testing.</summary>
        private EntityId campaignEntityId;

        /// <summary>campaign owner id for testing.</summary>
        private string campaignOwnerId;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
            {
                new AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider(),
                new AppNexusActivities.Measures.AppNexusMeasureSourceProvider(),
                new GoogleDfpActivities.Measures.DfpMeasureSourceProvider()
            });

            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.campaignOwnerId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        }

        /// <summary>Throw if an unsupported report type is provided.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ReportTypeNotSupported()
        {
            this.SetupCampaign();
            var dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllCampaign);
            billingReport.BuildReport("notsupported", true);
        }

        /// <summary>
        /// Happy path campaign report run
        /// </summary>
        [TestMethod]
        public void CampaignReportSuccess()
        {
            this.SetupCampaign();

            var dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllCampaign);
            var report = billingReport.BuildReport(ReportTypes.ClientCampaignBilling, true);
            AssertReportSize(20, 79, report);
            report = billingReport.BuildReport(ReportTypes.ClientCampaignBilling, false);
            AssertReportSize(20, 31, report);
        }

        /// <summary>
        /// Happy path data provider report run
        /// </summary>
        [TestMethod]
        public void DataProviderReportSuccess()
        {
            this.SetupCampaign();
            var dynAllocCampaign = new DynamicAllocationCampaign(
                this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllocCampaign);
            var report = billingReport.BuildReport(ReportTypes.DataProviderBilling, true);
            AssertReportSize(20, 165, report);
            report = billingReport.BuildReport(ReportTypes.DataProviderBilling, false);
            AssertReportSize(20, 51, report);
        }

        /// <summary>
        /// Happy path generic report run
        /// </summary>
        [TestMethod]
        public void GenericReportSuccess()
        {
            this.SetupCampaign();
            var dynAllocCampaign = new DynamicAllocationCampaign(
                this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllocCampaign);
            var report = billingReport.BuildGenericReport(true, true, true, true, true, true);
            AssertReportSize(20, 230, report);
            report = billingReport.BuildGenericReport(true, false, true, false, true, false);
            AssertReportSize(20, 52, report);
            report = billingReport.BuildGenericReport(false, false, false, false, false, false);
            AssertReportSize(20, 21, report);
        }

        /// <summary>Build Row metrics test</summary>
        [TestMethod]
        public void BuildRowMetrics()
        {
            this.SetupCampaign();
            var dynAllocCampaign = new DynamicAllocationCampaign(
                this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllocCampaign);
            
            var measureSet = new MeasureSet(new[]
            {
                110000000000143315, 110000000000143422, 103000000000000036, 110000000000141937, 110000000000142695
            });
            var rawDeliveryRow = new Dictionary<string, PropertyValue>
                {
                    { parseName.AllocationIdFieldName, "1254eb09584443f88f65a129e62bfe3f" },
                    { parseName.CampaignIdFieldName, "404235" },
                    { parseName.ClicksFieldName, 2L },
                    { parseName.EcpmFieldName, 1.2 },
                    { parseName.HourFieldName, new DateTime(2012, 01, 01, 1, 0, 0, DateTimeKind.Utc) },
                    { parseName.ImpressionsFieldName, 1000L },
                    { parseName.MediaSpendFieldName, 1.1 },
                };

            var metrics = billingReport.BuildRowMetrics(DeliveryNetworkDesignation.AppNexus, measureSet, rawDeliveryRow);

            // Make sure the delivery metrics are present
            Assert.AreEqual("CampaignFoo", metrics["DA Campaign Name"]);
            Assert.AreEqual("CompanyFoo", metrics["Advertiser Name"]);
            Assert.AreEqual("AppNexus", metrics["Network"]);
            Assert.AreEqual("404235", metrics["Campaign Id"]);
            Assert.AreEqual("CampaignFoo--1254eb09584443f88f65a129e62bfe3f", metrics["Campaign Name"]);
            Assert.AreEqual("2012-01-01T01:00:00.0000000Z", metrics["Hour"]);
            Assert.AreEqual("1254eb09584443f88f65a129e62bfe3f", metrics["Allocation Id"]);
            Assert.AreEqual("Peer39:NoCost:Lotame:exelate", metrics["Data Provider Names"]);
            Assert.AreEqual("5.17", metrics["Valuation"]);
            Assert.AreEqual("1.1", metrics["Media Spend"]);
            Assert.AreEqual("1000", metrics["Impressions"]);
            Assert.AreEqual("1.2", metrics["Ecpm"]);
            Assert.AreEqual("2", metrics["Clicks"]);
            Assert.AreEqual("0.200", metrics["Ctr"]);
            Assert.AreEqual("1.915", metrics["Billable Data Cost"]);
            Assert.AreEqual("1.915", metrics["Effective Rate"]);
            Assert.AreEqual("0.06", metrics["Serving Cost Rate"]);
            Assert.AreEqual("0.06", metrics["Serving Cost"]);
            Assert.AreEqual("0.532059", metrics["Profit"]);
            Assert.AreEqual("3.607059", metrics["Total Spend"]);
            Assert.AreEqual("5", metrics["Number Of Segments"]);

            // Verify all of the data provider dimensions are correct for a cpm provider
            Assert.AreEqual("0.75", metrics["Lotame:Effective Data Cost"]);
            Assert.AreEqual("0.75", metrics["Lotame:Impression Based Rate"]);
            Assert.AreEqual("0.75", metrics["Lotame:Impression Based Data Cost"]);
            Assert.AreEqual("0", metrics["Lotame:Percent of Media Spend Rate"]);
            Assert.AreEqual("0", metrics["Lotame:Percent of Media Spend Data Cost"]);

            // Verify all of the data provider dimensions are correct for a percent spend provider
            Assert.AreEqual("0.165", metrics["Peer39:Effective Data Cost"]);
            Assert.AreEqual("0.05", metrics["Peer39:Impression Based Rate"]);
            Assert.AreEqual("0.05", metrics["Peer39:Impression Based Data Cost"]);
            Assert.AreEqual("0.15", metrics["Peer39:Percent of Media Spend Rate"]);
            Assert.AreEqual("0.165", metrics["Peer39:Percent of Media Spend Data Cost"]);
            
            // Verify the other data providers are present (sufficient to look at one dimension)
            Assert.AreEqual("0", metrics["NoCost:Effective Data Cost"]);
            Assert.AreEqual("1", metrics["exelate:Effective Data Cost"]);

            // Verify measure columns are blank for measures not in measureset (sufficient to look at one dimension)
            Assert.AreEqual(string.Empty, metrics["Id:110000000000142991:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:110000000000143425:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:110000000000143426:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:103000000000000016:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:103000000000000042:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:110000000000141417:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:110000000000142064:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:110000000000142518:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000512000:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000565000:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000516000:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000574000:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000577000:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Id:102000000000598000:Measure Name"]);

            // Verify all measure columns for at least one of the measures
            Assert.AreEqual("Lotame", metrics["Id:110000000000143315:Data Provider"]);
            Assert.AreEqual("AppNexus:Segments:Lotame:News:Government &amp; Politics", metrics["Id:110000000000143315:Measure Name"]);
            Assert.AreEqual("0.75", metrics["Id:110000000000143315:Data Cost"]);
            Assert.AreEqual("0", metrics["Id:110000000000143315:MinCpm"]);
            Assert.AreEqual("0", metrics["Id:110000000000143315:Percent of Spend"]);
            Assert.AreEqual("0.75", metrics["Id:110000000000143315:Effective Cost"]);
            Assert.AreEqual("110000000000143315", metrics["Id:110000000000143315:Group"]);

            // Verify all measures in measure set are present
            Assert.AreEqual("110000000000143422", metrics["Id:110000000000143422:Group"]);
            Assert.AreEqual("group2", metrics["Id:103000000000000036:Group"]);
            Assert.AreEqual("group1", metrics["Id:110000000000141937:Group"]);
            Assert.AreEqual("110000000000142695", metrics["Id:110000000000142695:Group"]);

            // Verify group columns are blank for groups not in measureset (sufficient to look at one dimension)
            Assert.AreEqual(string.Empty, metrics["Group:110000000000142991:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Group:110000000000143425:Measure Name"]);
            Assert.AreEqual(string.Empty, metrics["Group:110000000000143426:Measure Name"]);

            // Verify all group columns for at least one of the groups
            Assert.AreEqual("Lotame", metrics["Group:110000000000143315:Data Provider"]);
            Assert.AreEqual("AppNexus:Segments:Lotame:News:Government &amp; Politics", metrics["Group:110000000000143315:Measure Name"]);
            Assert.AreEqual("0.75", metrics["Group:110000000000143315:Data Cost"]);
            Assert.AreEqual("0", metrics["Group:110000000000143315:MinCpm"]);
            Assert.AreEqual("0", metrics["Group:110000000000143315:Percent of Spend"]);
            Assert.AreEqual("0.75", metrics["Group:110000000000143315:Effective Cost"]);
            Assert.AreEqual("110000000000143315", metrics["Group:110000000000143315:MeasureId"]);

            // Verify all groups in measure set are present
            Assert.AreEqual("110000000000143422", metrics["Group:110000000000143422:MeasureId"]);
            Assert.AreEqual("103000000000000036", metrics["Group:group2:MeasureId"]);
            Assert.AreEqual("110000000000141937", metrics["Group:group1:MeasureId"]);
            Assert.AreEqual("110000000000142695", metrics["Group:110000000000142695:MeasureId"]);
        }

        /// <summary>Make sure we dedupe raw delivery records.</summary>
        [TestMethod]
        public void ProcessRawDeliveryDataWithDuplicates()
        {
            this.SetupCampaign();

            var dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);
            var billingReport = new AppNexusBillingReport(this.repository, dynAllCampaign);

            var processedRows = new List<Dictionary<string, PropertyValue>>();
            Action<DeliveryNetworkDesignation, Dictionary<string, PropertyValue>>
                rowOutputHandler = (designation, values) => processedRows.Add(values);
            billingReport.ProcessRawDeliveryData(rowOutputHandler);

            Assert.AreEqual(19, processedRows.Count);
            var uniqueRows = processedRows.Select(
                r => r[parseName.HourFieldName].SerializationValue + r[parseName.CampaignIdFieldName].SerializationValue)
                .Distinct().ToList();
            Assert.AreEqual(processedRows.Count, uniqueRows.Count);

            // Assert we got the updated media spend from the campaign/hour record in the later report pull
            // 406173,2012-06-03 18:00,8d07c3af8f124babb383245bdee4016f,118,1.348296610169491525424000,.159099,0
            var updatedMediaSpend = (decimal)processedRows
                    .Where(r => r[parseName.AllocationIdFieldName] == "8d07c3af8f124babb383245bdee4016f")
                    .Select(r => r[parseName.MediaSpendFieldName])
                    .Single();
            Assert.AreEqual(.159099m, updatedMediaSpend);
        }

        /// <summary>Assert the number of rows and columns in the report.</summary>
        /// <param name="expectedRows">The expected rows.</param>
        /// <param name="expectedCols">The expected cols.</param>
        /// <param name="report">The report.</param>
        private static void AssertReportSize(int expectedRows, int expectedCols, StringBuilder report)
        {
            Assert.IsNotNull(report);
            var r = new StringReader(report.ToString());
            var lines = 0;
            string line;
            while ((line = r.ReadLine()) != null)
            {
                // Assert columns per row
                Assert.AreEqual(expectedCols, line.Split(new[] { ',' }).Length);
                lines++;
            }

            // Assert number of rows
            Assert.AreEqual(expectedRows, lines);
        }

        /// <summary>Setup a test campaign with delivery and measure data.</summary>
        private void SetupCampaign()
        {
            campaignStub.SetupCampaign(this.repository, this.companyEntityId, this.campaignEntityId, this.campaignOwnerId);

            // Change the margin override on the campaign
            var campaignEntity = this.repository.GetEntity<CampaignEntity>(null, this.campaignEntityId);
            var configs = campaignEntity.GetConfigSettings();
            configs["DynamicAllocation.Margin"] = "{0}".FormatInvariant(1 / 0.85);
            campaignEntity.SetConfigSettings(configs);
        }
    }
}
