// -----------------------------------------------------------------------
// <copyright file="GetCampaignDeliveryDataFixture.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using Activities;
using ActivityTestUtilities;
using AppNexusUtilities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using GoogleDfpUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Unit test fixture for GetCampaignDeliveryDataActivity</summary>
    [TestClass]
    public class GetCampaignDeliveryDataFixture
    {
        /// <summary>Default history lookback for testing.</summary>
        private static readonly TimeSpan HistoryLookBack = new TimeSpan(3, 0, 0, 0);

        /// <summary>Lifetime lookback.</summary>
        private static readonly TimeSpan LifetimeLookBack = GetCampaignDeliveryDataActivity.LifetimeLookBack;

        /// <summary>Time constant - one hour.</summary>
        private static readonly TimeSpan OneHour = new TimeSpan(0, 1, 0, 0);

        /// <summary>Time constant - one day.</summary>
        private static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);

        /// <summary>Time constant - zero hours.</summary>
        private static readonly TimeSpan ZeroHours = new TimeSpan(0);

        /// <summary>Stubbed entity repository for testing</summary>
        private IEntityRepository entityRepository;

        /// <summary>10 pm EST</summary>
        private DateTime time10pmESTasUTC;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet0;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet1;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet2;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet3;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet4;

        /// <summary>buget allocation history test data</summary>
        private string allocationId0;

        /// <summary>buget allocation history test data</summary>
        private string allocationId1;

        /// <summary>buget allocation history test data</summary>
        private string allocationId2;

        /// <summary>buget allocation history test data</summary>
        private string allocationId3;

        /// <summary>buget allocation history test data</summary>
        private string allocationId4;

        /// <summary>Map of allocationId to measureSet</summary>
        private Dictionary<string, MeasureSet> nodeMap;

        /// <summary>parsed delivery data for testing</summary>
        private CanonicalDeliveryData testCanonicalDeliveryData;

        /// <summary>Company entity for testing</summary>
        private CompanyEntity companyEntity;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Margin AllocationParameter for testing</summary>
        private decimal testMargin;

        /// <summary>PerMilleFees AllocationParameter for testing</summary>
        private decimal testPerMilleFees;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();

            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            this.entityRepository = MockRepository.GenerateMock<IEntityRepository>();

            this.time10pmESTasUTC = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.Parse("2012-03-16 22:00:00"), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            // Setup allocation parameter test values
            this.testMargin = 1 / 0.85m;
            this.testPerMilleFees = 0m;

            // Set up node map
            this.measureSet0 = new MeasureSet { 1106005 };
            this.measureSet1 = new MeasureSet { 1106005, 1106006 }; // .25 data cost
            this.measureSet2 = new MeasureSet { 1106007, 1155941 }; // .75 data cost
            this.measureSet3 = new MeasureSet { 1106006, 1106004 }; // .25 data cost
            this.measureSet4 = new MeasureSet { 1106006, 1106007 }; // .25 data cost

            this.allocationId0 = "00000000000000000000000000000000";
            this.allocationId1 = "00000000000000000000000000000001";
            this.allocationId2 = "00000000000000000000000000000002";
            this.allocationId3 = "00000000000000000000000000000003";
            this.allocationId4 = "00000000000000000000000000000004";

            // Build a node map of allocation Id's to measureSet
            this.nodeMap = new Dictionary<string, MeasureSet>
                {
                    { this.allocationId0, this.measureSet0 },
                    { this.allocationId1, this.measureSet1 },
                    { this.allocationId2, this.measureSet2 },
                    { this.allocationId3, this.measureSet3 },
                    { this.allocationId4, this.measureSet4 },
                };
            
            // Set up raw delivery data
            var rawDeliveryData = EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(GetCampaignDeliveryDataFixture), "Resources.ApnxDeliveryData.csv");
            this.testCanonicalDeliveryData = new CanonicalDeliveryData(DeliveryNetworkDesignation.AppNexus);
            this.testCanonicalDeliveryData.AddRawData(rawDeliveryData, this.time10pmESTasUTC, new ApnxReportCsvParser());
            
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(), "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");

            this.SetupAllocationParameters(this.campaignEntity);

            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(), "Bar");
        }

        /// <summary>Test simplest happy path scenario for processing delivery data.</summary>
        [TestMethod]
        public void ProcessDeliveryData()
        {
            string userId = Guid.NewGuid().ToString();
            var companyEntityId = (EntityId)this.companyEntity.ExternalEntityId;
            var campaignEntityId = (EntityId)this.campaignEntity.ExternalEntityId;

            var request = new ActivityRequest 
            {   
                Values =
                {
                    { "AuthUserId", userId },
                    { "CompanyEntityId", companyEntityId.ToString() },
                    { "CampaignEntityId", campaignEntityId.ToString() }
                }
            };

            // Set up the stub for getting the campaign
            var dynAllocationSetBlobEntityId = new EntityId();
            var nodeMapBlobId = new EntityId();
            var activeAllocationOutputsBlobId = new EntityId();
            this.campaignEntity.Associations.Add(new Association { ExternalName = DynamicAllocationEntityProperties.AllocationsHistory, TargetEntityId = dynAllocationSetBlobEntityId });
            this.campaignEntity.Associations.Add(new Association { ExternalName = DynamicAllocationEntityProperties.AllocationNodeMap, TargetEntityId = nodeMapBlobId });
            this.campaignEntity.Associations.Add(new Association { ExternalName = DynamicAllocationEntityProperties.AllocationSetActive, TargetEntityId = activeAllocationOutputsBlobId });

            // Setup the raw delivery data
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            // Setup eligibility history
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - OneDay);

            // Set up the stub for getting the DAAllocationSetHistory blob
            var allocationsHistoryJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(GetCampaignDeliveryDataFixture), "Resources.AllocationsHistory.js");
            var dynAllocationSetBlob = BlobEntity.BuildBlobEntity(dynAllocationSetBlobEntityId, allocationsHistoryJson);
            this.entityRepository.Stub(f => f.GetEntity(
                Arg<RequestContext>.Is.Anything, 
                Arg<EntityId>.Is.Equal(dynAllocationSetBlobEntityId))).Return(dynAllocationSetBlob);

            // Set up the stub for getting the AllocationNodeMap blob
            var nodeMapBlob = BlobEntity.BuildBlobEntity(nodeMapBlobId, this.nodeMap);
            this.entityRepository.Stub(f => f.GetEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<EntityId>.Is.Equal(nodeMapBlobId))).Return(nodeMapBlob);

            // Set up the stub for updating.
            IList<EntityProperty> savedProperties = null;
            this.SetupUpdateCampaignStub(c => savedProperties = c.ToList());

            var activity = this.SetupActivity();
            var result = activity.Run(request);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Assert allocation parameters properly retrieved
            Assert.AreEqual(this.testMargin, activity.Dac.AllocationParameters.Margin);
            Assert.AreEqual(this.testPerMilleFees, activity.Dac.AllocationParameters.PerMilleFees);

            // Assert campaign updated
            Assert.IsNotNull(savedProperties);
            Assert.IsNotNull(savedProperties.Single(p => p.Name == daName.AllocationNodeMetrics));
            Assert.IsNotNull(savedProperties.Single(p => p.Name == daName.RemainingBudget));
            Assert.IsNotNull(savedProperties.Single(p => p.Name == daName.LifetimeMediaBudgetCap));
        }

        /// <summary>Test empty raw delivery data collection results in empty canonical record collection.</summary>
        [TestMethod]
        public void BuildCanonicalDeliveryDataEmptyCollection()
        {
            // Should get an empty collection back
            var activity = this.SetupActivity();
            var canonicalDeliveryDataCollection = activity.BuildCanonicalDeliveryData(
                new List<RawDeliveryDataIndexItem>(), 
                GetCampaignDeliveryDataActivity.HistoryLookBack);
            Assert.AreEqual(0, canonicalDeliveryDataCollection.Count);
        }

        /// <summary>Test we can parse raw delivery data into canonical delivery data from multiple networks.</summary>
        [TestMethod]
        public void BuildCanonicalDeliveryDataMultipleNetwork()
        {
            // Setup the campaign with a APNX raw delivery data index
            var apnxIndex = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            // Setup the campaign with a DFP raw delivery data index
            var dfpIndex = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.DfpDeliveryData.csv" },
                DateTime.UtcNow,
                GoogleDfpEntityProperties.DfpRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var indexes = new List<RawDeliveryDataIndexItem>
                {
                    new RawDeliveryDataIndexItem(DeliveryNetworkDesignation.AppNexus, apnxIndex),
                    new RawDeliveryDataIndexItem(DeliveryNetworkDesignation.GoogleDfp, dfpIndex)
                };

            var activity = this.SetupActivity();
            var canonicalDeliveryDataCollection = activity.BuildCanonicalDeliveryData(
                indexes,
                GetCampaignDeliveryDataActivity.HistoryLookBack);

            // There should be two networks
            Assert.AreEqual(2, canonicalDeliveryDataCollection.Count);

            // There should be 18 unique records
            Assert.AreEqual(18, canonicalDeliveryDataCollection.Sum(d => d.DeliveryDataForNetwork.Count));
        }

        /// <summary>Test we fail correctly when an entity id in index cannot be resolved.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetCanonicalDeliveryDataFromIndexMissingRawDataEntity()
        {
            var activity = this.SetupActivity();
            var rawDataBlobId = new EntityId();
            RepositoryStubUtilities.SetupGetEntityStub(this.entityRepository, rawDataBlobId, null, true);
            activity.GetCanonicalDeliveryDataFromIndex(
                new[] { rawDataBlobId }, LifetimeLookBack, DeliveryNetworkDesignation.AppNexus);
        }

        /// <summary>Test we fail correctly when CanonicalDeliveryData fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetCanonicalDeliveryDataFromIndexFailParse()
        {
            // Setup raw delivery data with a rogue header
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\nNOTANID,2012-03-16 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            // Setup the campaign with a raw delivery data
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaignCsv(
                this.entityRepository,
                this.campaignEntity,
                rawDeliveryDataList,
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            try
            {
                var activity = this.SetupActivity();
                activity.GetCanonicalDeliveryDataFromIndex(
                    index, HistoryLookBack, DeliveryNetworkDesignation.AppNexus);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Unable to add raw data"));
                throw;
            }
        }

        /// <summary>Test lookback.</summary>
        [TestMethod]
        public void TryGetCanonicalDeliveryDataFromIndexLookBack()
        {
            // Setup raw delivery data
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-16 15:00,00000000000000000000000000000001,1,1,1,1",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-15 15:00,00000000000000000000000000000001,1,1,1,1",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-14 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            // Setup the campaign with a raw delivery data
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaignCsv(
                this.entityRepository,
                this.campaignEntity,
                rawDeliveryDataList,
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var activity = this.SetupActivity();
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(
                DateTime.Parse("2012-03-16T15:00:00.0000000Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            var canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, OneDay, DeliveryNetworkDesignation.AppNexus);

            // The report for the second 24 hr period should get included, but not the third
            Assert.AreEqual(2, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));

            canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, OneDay + OneHour, DeliveryNetworkDesignation.AppNexus);

            // The report for the third 24 hr period should get included
            Assert.AreEqual(3, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));
        }

        /// <summary>Test lookback with no delivery in latest report.</summary>
        [TestMethod]
        public void TryGetCanonicalDeliveryDataFromIndexNoDeliveryInLatest()
        {
            // Setup raw delivery data
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-14 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            // Setup the campaign with empty delivery reports
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaignCsv(
                this.entityRepository,
                this.campaignEntity,
                rawDeliveryDataList,
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var activity = this.SetupActivity();

            // Setup latest delivery seen
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(
                DateTime.Parse("2012-03-14T15:00:00.0000000Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            var canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, OneDay, DeliveryNetworkDesignation.AppNexus);

            // The zero delivery report should not hide the one before it
            Assert.AreEqual(1, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual("2012-03-14T15:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));
        }

        /// <summary>Test lookback with no delivery for more than the lookback period.</summary>
        [TestMethod]
        public void TryGetCanonicalDeliveryDataFromIndexNoDeliveryForMoreThanLookBack()
        {
            // Setup raw delivery data
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n"
            };

            // Setup the campaign with empty delivery reports
            var latestReportDate = DateTime.UtcNow;
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaignCsv(
                this.entityRepository,
                this.campaignEntity,
                rawDeliveryDataList,
                latestReportDate,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var activity = this.SetupActivity();

            // No delivery (this will force us to use the span of report dates to exit processing delivery data
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(DateTime.MinValue);

            var canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, OneDay, DeliveryNetworkDesignation.AppNexus);

            // All reports processed
            var earliestIncludedReportDate = latestReportDate.AddDays(-2);
            Assert.AreEqual(0, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(earliestIncludedReportDate, canonicalDeliveryData.EarliestDeliveryReportDate);
        }

        /// <summary>Test lookback with interrupted delivery for more than the lookback period.</summary>
        [TestMethod]
        public void TryGetCanonicalDeliveryDataFromIndexStoppedDeliveryForMoreThanLookBack()
        {
            // Setup raw delivery data
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-14 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            // Setup the campaign with empty delivery reports
            var latestReportDate = DateTime.UtcNow;
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaignCsv(
                this.entityRepository,
                this.campaignEntity,
                rawDeliveryDataList,
                latestReportDate,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var activity = this.SetupActivity();

            // Delivery has occured before the lookback period
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(
                DateTime.Parse("2012-03-14T15:00:00.0000000Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            var canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, OneDay, DeliveryNetworkDesignation.AppNexus);

            // The presence of delivery will cause it to keep processing to that point.
            var earliestIncludedReportDate = latestReportDate.AddDays(-3);
            Assert.AreEqual(1, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(earliestIncludedReportDate, canonicalDeliveryData.EarliestDeliveryReportDate);
            Assert.AreEqual("2012-03-14T15:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));
        }

        /// <summary>Test lifetime lookback.</summary>
        [TestMethod]
        public void TryGetCanonicalDeliveryDataFromIndexAllHistory()
        {
            // Setup the campaign with a raw delivery data
            var index = RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false).Select(i => new EntityId(i)).ToArray();

            var activity = this.SetupActivity();
            var canonicalDeliveryData = activity.GetCanonicalDeliveryDataFromIndex(
                index, LifetimeLookBack, DeliveryNetworkDesignation.AppNexus);

            // Should get all the history (9 unique records)
            Assert.AreEqual(9, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual("2012-03-16T23:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));
        }
        
        /// <summary>Happy path test of TryGetDeliveryData</summary>
        [TestMethod]
        public void TryGetDeliveryDataSuccess()
        {
            // Setup the campaign with a APNX raw delivery data index
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            var activity = this.SetupActivity();
            var canonicalDeliveryData = activity.GetDeliveryData(ZeroHours);

            Assert.IsNotNull(canonicalDeliveryData);
            Assert.IsNotNull(canonicalDeliveryData.DeliveryDataForNetwork);
            Assert.AreNotEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryReportDate);
            Assert.AreNotEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryDataDate);
            Assert.AreNotEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryReportDate);
            Assert.AreNotEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryDataDate);
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, canonicalDeliveryData.Network);
        }

        /// <summary>Happy path test of TryGetDeliveryData when only the report header is present.</summary>
        [TestMethod]
        public void TryGetDeliveryDataSuccessHeaderOnly()
        {
            var expectedReportDate = DateTime.UtcNow;

            // Setup the campaign with a APNX raw delivery data index
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataEmpty.csv" },
                expectedReportDate,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            var activity = this.SetupActivity();
            var canonicalDeliveryData = activity.GetDeliveryData(ZeroHours);

            Assert.IsNotNull(canonicalDeliveryData);
            Assert.AreEqual(0, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(expectedReportDate, canonicalDeliveryData.LatestDeliveryReportDate);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryDataDate);
        }

        /// <summary>Happy path test of TryGetDeliveryData when no delivery data is present.</summary>
        [TestMethod]
        public void TryGetDeliveryDataSuccessNoDeliveryDataPresent()
        {
            // Setup with bare campaign (no delivery data added)
            var activity = this.SetupActivity();
            var canonicalDeliveryData = activity.GetDeliveryData(ZeroHours);

            Assert.IsNotNull(canonicalDeliveryData);
            Assert.AreEqual(0, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryReportDate);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryReportDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryDataDate);
        }

        /// <summary>TryGetDeliveryData fail entity get returns activity error</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetDeliveryDataFailEntityRetrieve()
        {
            // Setup fail on raw delivery data entity get
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                true);

            try
            {
                var activity = this.SetupActivity();
                activity.GetDeliveryData(ZeroHours);
            }
            catch (ActivityException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(DataAccessEntityNotFoundException));
                throw;
            }
        }

        /// <summary>TryGetDeliveryData fail parse returns activity error</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void TryGetDeliveryDataFailParse()
        {
            // Setup failure to parse
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataUnparseable.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            var activity = this.SetupActivity();
            activity.GetDeliveryData(ZeroHours);
        }

        /// <summary>TryGetDeliveryData with multiple networks returns activity error</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void TryGetDeliveryDataFailMultipleNetworks()
        {
            // Setup the campaign with a APNX raw delivery data index
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            // Setup the campaign with a DFP raw delivery data index
            RawDeliveryDataFixture.SetupRawDeliveryDataOnCampaign(
                this.entityRepository,
                this.campaignEntity,
                new[] { "Resources.DfpDeliveryData.csv" },
                DateTime.UtcNow,
                GoogleDfpEntityProperties.DfpRawDeliveryDataIndex,
                false,
                false);

            var activity = this.SetupActivity();
            activity.GetDeliveryData(ZeroHours);
        }

        /// <summary>Set budget properties on campaign when saving.</summary>
        [TestMethod]
        public void UpdateCampaignSetsBudgetProperties()
        {
            var activity = this.SetupActivity();
            activity.DeliveryMetrics.Stub(f => f.NodeMetricsCollection).Return(
                new Dictionary<MeasureSet, NodeDeliveryMetrics>());
            activity.DeliveryMetrics.Stub(f => f.RemainingBudget).Return(8888m);
            activity.DeliveryMetrics.Stub(f => f.LifetimeMediaBudgetCap).Return(4444m);

            // Set up the stub for updating.
            IList<EntityProperty> savedProperties = null;
            this.SetupUpdateCampaignStub(c => savedProperties = c.ToList());

            // save the campaign with the node metrics
            activity.UpdateCampaign();

            // Assert repository was called
            Assert.AreEqual(8888m, (decimal)savedProperties.Single(p => p.Name == daName.RemainingBudget));
            Assert.AreEqual(4444m, (decimal)savedProperties.Single(p => p.Name == daName.LifetimeMediaBudgetCap));
        }

        /// <summary>Round trip serialize Node Metrics</summary>
        [TestMethod]
        public void RoundtripSerializeNodeMetrics()
        {
            // Json serialization truncates to milliseconds on dates
            var lastProcessedEligibilityHour = new DateTime(2012, 1, 1, 1, 1, 1, 111, DateTimeKind.Utc);

            var hourMetrics1 = new NodeHourMetrics
                { AverageImpressions = 1, AverageMediaSpend = 1, EligibilityCount = 1, };
            hourMetrics1.LastNImpressions.Add(new[] { 1L });
            hourMetrics1.LastNMediaSpend.Add(new[] { 1m });
            var hourMetrics2 = new NodeHourMetrics 
                { AverageImpressions = 1, AverageMediaSpend = 1, EligibilityCount = 1, };
            hourMetrics2.LastNImpressions.Add(new[] { 1L });
            hourMetrics2.LastNMediaSpend.Add(new[] { 1m });

            var nodeMetrics = new NodeDeliveryMetrics
                {
                    TotalEligibleHours = 1,
                    TotalSpend = 1,
                    TotalImpressions = 1,
                    TotalMediaSpend = 1,
                    LastProcessedEligibilityHour = lastProcessedEligibilityHour,
                };
            nodeMetrics.DeliveryProfile[1] = hourMetrics1;
            nodeMetrics.DeliveryProfile[2] = hourMetrics2;

            var activity = this.SetupActivity();
            activity.DeliveryMetrics.Stub(f => f.NodeMetricsCollection).Return(
                    new Dictionary<MeasureSet, NodeDeliveryMetrics>
                        {
                            { this.measureSet0, nodeMetrics },
                            { this.measureSet1, nodeMetrics }
                        });

            // Set up the stub for updating.
            IList<EntityProperty> savedProperties = null;
            this.SetupUpdateCampaignStub(c => savedProperties = c.ToList());

            // save the campaign with the node metrics using the values captured by the stub
            activity.UpdateCampaign();
            this.campaignEntity.SetPropertyByName(
                daName.AllocationNodeMetrics,
                (string)savedProperties.Single(p => p.Name == daName.AllocationNodeMetrics));

            // Reconstitute them
            activity.InitDeliveryMetrics();

            // Assert that the retrieved data is the same as the saved
            var actualNodeMetrics = activity.DeliveryMetrics.NodeMetricsCollection;
            var actualNodeMetrics1 = actualNodeMetrics[this.measureSet0];
            Assert.AreEqual(2, actualNodeMetrics.Count);
            Assert.AreEqual(nodeMetrics.TotalEligibleHours, actualNodeMetrics1.TotalEligibleHours);
            Assert.AreEqual(nodeMetrics.TotalSpend, actualNodeMetrics1.TotalSpend);
            Assert.AreEqual(nodeMetrics.TotalImpressions, actualNodeMetrics1.TotalImpressions);
            Assert.AreEqual(nodeMetrics.TotalMediaSpend, actualNodeMetrics1.TotalMediaSpend);
            Assert.AreEqual(nodeMetrics.LastProcessedEligibilityHour, actualNodeMetrics1.LastProcessedEligibilityHour);
            Assert.AreEqual(2, actualNodeMetrics1.DeliveryProfile.Count);
            Assert.AreEqual(nodeMetrics.DeliveryProfile[2].AverageImpressions, actualNodeMetrics1.DeliveryProfile[2].AverageImpressions);
            Assert.AreEqual(nodeMetrics.DeliveryProfile[2].AverageMediaSpend, actualNodeMetrics1.DeliveryProfile[2].AverageMediaSpend);
            Assert.AreEqual(nodeMetrics.DeliveryProfile[2].EligibilityCount, actualNodeMetrics1.DeliveryProfile[2].EligibilityCount);
            Assert.AreEqual(
                0, 
                nodeMetrics.DeliveryProfile[2].LastNImpressions.Except(actualNodeMetrics1.DeliveryProfile[2].LastNImpressions).Count());
            Assert.AreEqual(
                0, 
                nodeMetrics.DeliveryProfile[2].LastNMediaSpend.Except(actualNodeMetrics1.DeliveryProfile[2].LastNMediaSpend).Count());
        }

        /// <summary>Succeed if there are no node metrics on the campaign.</summary>
        [TestMethod]
        public void TryGetNodeMetricsNotPresent()
        {
            var activity = this.SetupActivity();
            activity.InitDeliveryMetrics();

            // Make sure we don't have node results
            Assert.AreEqual(
                0,
                this.campaignEntity.Properties.Count(p => p.Name == DynamicAllocationEntityProperties.AllocationNodeMetrics));
        }

        /// <summary>Fail if we could not deserialize the node metrics on the campaign.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void TryGetNodeMetricsFailDeserialize()
        {
            this.campaignEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.AllocationNodeMetrics, 
                "*&*BogusJson");

            var activity = this.SetupActivity();
            activity.InitDeliveryMetrics();
        }

        /// <summary>Dont' fail if we don't have a populated collection.</summary>
        [TestMethod]
        public void RoundtripEmptyNodeMetricsEmptyOk()
        {
            // Setup empty collection
            var activity = this.SetupActivity();
            activity.DeliveryMetrics.Stub(f => f.NodeMetricsCollection).Return(
                new Dictionary<MeasureSet, NodeDeliveryMetrics>());

            // Set up the stub for updating.
            IList<EntityProperty> savedProperties = null;
            this.SetupUpdateCampaignStub(c => savedProperties = c.ToList());

            // save the campaign with the node metrics using the values captured by the stub
            activity.UpdateCampaign();
            this.campaignEntity.SetPropertyByName(
                daName.AllocationNodeMetrics, 
                (string)savedProperties.Single(p => p.Name == daName.AllocationNodeMetrics));

            // Assert we retrieve the empty collection
            activity.InitDeliveryMetrics();
            Assert.IsNotNull(activity.DeliveryMetrics.NodeMetricsCollection);

            // Setup empty delivery profile
            activity.DeliveryMetrics = MockRepository.GenerateStub<IDeliveryMetrics>();
            activity.DeliveryMetrics.Stub(f => f.NodeMetricsCollection).Return(
                new Dictionary<MeasureSet, NodeDeliveryMetrics>
                    {
                        { new MeasureSet { 1 }, new NodeDeliveryMetrics() }
                    });

            activity.UpdateCampaign();
            this.campaignEntity.SetPropertyByName(
                daName.AllocationNodeMetrics,
                (string)savedProperties.Single(p => p.Name == daName.AllocationNodeMetrics));

            // Assert we retrieve the empty profile
            activity.InitDeliveryMetrics();
            Assert.IsNotNull(activity.DeliveryMetrics.NodeMetricsCollection.First().Value.DeliveryProfile);
        }

        /// <summary>Fail if we have a null collection.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void UpdateNodeMetricsFailNullCollection()
        {
            try
            {
                // Null node metrics
                var activity = this.SetupActivity();
                activity.UpdateCampaign();
            }
            catch (ActivityException e)
            {
                Assert.AreEqual(ActivityErrorId.GenericError, e.ActivityErrorId);
                Assert.IsNull(e.InnerException);
                throw;
            }
        }

        /// <summary>Happy path test of constructing eligibility history.</summary>
        [TestMethod]
        public void GetEligibilityHistory()
        {
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - OneDay);
            var activity = this.SetupActivity();

            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC);
            var eligibilityHistoryBuilder = activity.GetEligibilityHistory(HistoryLookBack);
            Assert.IsNotNull(eligibilityHistoryBuilder);

            var eligibilityHistory = eligibilityHistoryBuilder.EligibilityHistory;

            // There should only be eligibility history for nodes with export budget (2, 3, and 4)
            Assert.AreEqual(3, eligibilityHistory.Count);
            Assert.IsFalse(eligibilityHistory.ContainsKey(this.measureSet0));
            Assert.IsFalse(eligibilityHistory.ContainsKey(this.measureSet1));
            Assert.AreEqual(2, eligibilityHistory[this.measureSet2].Count());
            Assert.AreEqual(2, eligibilityHistory[this.measureSet3].Count());
            Assert.AreEqual(2, eligibilityHistory[this.measureSet4].Count());
        }

        /// <summary>Test of constructing eligibility history after extended delay in processing report.</summary>
        [TestMethod]
        public void GetEligibilityHistoryStale()
        {
            // Set up two eligibility period several days apart (more than lookback)
            // with last reported delivery occuring in the first period
            var delay = HistoryLookBack + OneDay + OneDay;
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - delay);
            var activity = this.SetupActivity();

            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC - delay);
            var eligibilityHistoryBuilder = activity.GetEligibilityHistory(HistoryLookBack);
            Assert.IsNotNull(eligibilityHistoryBuilder);

            var eligibilityHistory = eligibilityHistoryBuilder.EligibilityHistory;

            // Even though the two are separated by more than a lookback period, we should still
            // pick them both up because lookback starts from the last known delivery.
            // There should only be eligibility history for nodes with export budget (2, 3, and 4)
            Assert.AreEqual(3, eligibilityHistory.Count);
            Assert.IsFalse(eligibilityHistory.ContainsKey(this.measureSet0));
            Assert.IsFalse(eligibilityHistory.ContainsKey(this.measureSet1));
            Assert.AreEqual(2, eligibilityHistory[this.measureSet2].Count());
            Assert.AreEqual(2, eligibilityHistory[this.measureSet3].Count());
            Assert.AreEqual(2, eligibilityHistory[this.measureSet4].Count());
        }

        /// <summary>Lookback must be greater than zero.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetEligibilityHistoryInvalidLookBack()
        {
            var zeroLookBack = ZeroHours;
            var activity = this.SetupActivity();
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC);
            try
            {
                activity.GetEligibilityHistory(zeroLookBack);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("lookback duration"));
                throw;
            }
        }

        /// <summary>History association must be present.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetEligibilityHistoryNoHistoryAssociation()
        {
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - OneDay, true, false, false);
            var activity = this.SetupActivity();
            
            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC);
            try
            {
                activity.GetEligibilityHistory(HistoryLookBack);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Index Association"));
                throw;
            }
        }

        /// <summary>History index blob must be present.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetEligibilityHistoryNoHistoryIndexBlob()
        {
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - OneDay, false, true, false);
            var activity = this.SetupActivity();

            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC);
            try
            {
                activity.GetEligibilityHistory(HistoryLookBack);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Index Blob"));
                throw;
            }
        }

        /// <summary>Allocation blob must be present.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void GetEligibilityHistoryNoAllocationBlob()
        {
            this.SetupEligibilityHistory(this.time10pmESTasUTC, this.time10pmESTasUTC - OneDay, false, false, true);
            var activity = this.SetupActivity();

            activity.DeliveryMetrics.Stub(f => f.PreviousLatestCampaignDeliveryHour).Return(this.time10pmESTasUTC);

            activity.GetEligibilityHistory(HistoryLookBack);
        }

        /// <summary>Setup a GetCampaignDeliveryDataActivity with data and stubs.</summary>
        /// <param name="repository">The repository.</param>
        /// <param name="companyEntity">The company Entity.</param>
        /// <param name="campaignEntity">The campaign Entity.</param>
        /// <returns>An instance of the activity.</returns>
        private static GetCampaignDeliveryDataActivity SetupActivity(
            IEntityRepository repository, CompanyEntity companyEntity, CampaignEntity campaignEntity)
        {
            // Setup company entity stub
            RepositoryStubUtilities.SetupGetEntityStub(
                repository, companyEntity.ExternalEntityId, companyEntity, false);

            // Setup campaign entity stub
            RepositoryStubUtilities.SetupGetEntityStub(
                repository, campaignEntity.ExternalEntityId, campaignEntity, false);

            // Set up our activity
            var activity = Activity.CreateActivity(
                    typeof(GetCampaignDeliveryDataActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), repository } },
                    ActivityTestHelpers.SubmitActivityRequest) as GetCampaignDeliveryDataActivity;

            activity.DeliveryMetrics = MockRepository.GenerateStub<IDeliveryMetrics>();

            // Make sure there is an IDynamicAllocationCampaign on the activity
            var dac = new DynamicAllocationCampaign(
                repository, companyEntity.ExternalEntityId, campaignEntity.ExternalEntityId);
            activity.Dac = dac;

            return activity;
        }

        /// <summary>Setup a GetCampaignDeliveryDataActivity with data and stubs.</summary>
        /// <returns>An instance of the activity.</returns>
        private GetCampaignDeliveryDataActivity SetupActivity()
        {
            return SetupActivity(this.entityRepository, this.companyEntity, this.campaignEntity);
        }

        /// <summary>Set up allocation history index for eligibility history.</summary>
        /// <param name="activeAllocationStart">Time of active allocation start</param>
        /// <param name="activeAllocationMinus1Start">Time of previous allocation start</param>
        /// <param name="failIndexAssociation">True to fail on index association</param>
        /// <param name="failIndexBlob">True to fail on index blob</param>
        /// <param name="failAllocationBlob">True to fail on allocation blobs</param>
        private void SetupEligibilityHistory(
            DateTime activeAllocationStart, 
            DateTime activeAllocationMinus1Start, 
            bool failIndexAssociation = false, 
            bool failIndexBlob = false, 
            bool failAllocationBlob = false)
        {
            // Build the node results
            var budgetAllocation0 = new PerNodeBudgetAllocationResult
            {
                AllocationId = this.allocationId0
            };

            var budgetAllocation1 = new PerNodeBudgetAllocationResult
            {
                AllocationId = this.allocationId1,
                PeriodTotalBudget = 100m,
                PeriodMediaBudget = 1m,
                ExportBudget = 0m,
                PeriodImpressionCap = 1000,
                MaxBid = 1.2m,
                ExportCount = 1,
                NodeIsIneligible = true
            };

            var budgetAllocation2 = new PerNodeBudgetAllocationResult
            {
                AllocationId = this.allocationId3,
                PeriodTotalBudget = 201m,
                PeriodMediaBudget = 2.1m,
                ExportBudget = 2.1m,
                PeriodImpressionCap = 2001,
                MaxBid = 2.1m,
                ExportCount = 1,
                NodeIsIneligible = false
            };

            var budgetAllocation3 = new PerNodeBudgetAllocationResult
            {
                AllocationId = this.allocationId4,
                PeriodTotalBudget = 200m,
                PeriodMediaBudget = 2m,
                ExportBudget = 2m,
                PeriodImpressionCap = 2000,
                MaxBid = 2m,
                ExportCount = 1,
                NodeIsIneligible = false
            };

            var budgetAllocation4 = new PerNodeBudgetAllocationResult
            {
                AllocationId = this.allocationId4,
                PeriodTotalBudget = 200m,
                PeriodMediaBudget = 2m,
                ExportBudget = 2m,
                PeriodImpressionCap = 2000,
                MaxBid = 2m,
                ExportCount = 0,
                NodeIsIneligible = false
            };

            // build perNodeResult collection
            var perNodeResult = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                {
                    { this.measureSet0, budgetAllocation0 }, 
                    { this.measureSet1, budgetAllocation1 }, 
                    { this.measureSet2, budgetAllocation2 }, 
                    { this.measureSet3, budgetAllocation3 }, 
                    { this.measureSet4, budgetAllocation4 } 
                };

            var activeAllocationStartTime = activeAllocationStart;
            var activeAlloctionMinus1StartTime = activeAllocationMinus1Start;
            var allocationOutputsActive = new BudgetAllocation
            {
                PerNodeResults = perNodeResult,
                PeriodStart = activeAllocationStartTime
            };

            var allocationOutputsActiveMinus1 = new BudgetAllocation
            {
                PerNodeResults = perNodeResult,
                PeriodStart = activeAlloctionMinus1StartTime
            };

            // Set up the stub for getting eligibility off the campaign
            var allocationHistoryIndexBlobEntityId = new EntityId();

            var budgetAllocationActiveOutputsJson = AppsJsonSerializer.SerializeObject(allocationOutputsActive);
            var budgetAllocationActiveMinus1OutputsJson = AppsJsonSerializer.SerializeObject(allocationOutputsActiveMinus1);
            var allocationOutputsIdActive = new EntityId();
            var allocationOutputsIdActiveMinus1 = new EntityId();
            var allocationsBlobActive = BlobEntity.BuildBlobEntity(allocationOutputsIdActive, budgetAllocationActiveOutputsJson) as IEntity;
            var allocationsBlobActiveMinus1 = BlobEntity.BuildBlobEntity(allocationOutputsIdActiveMinus1, budgetAllocationActiveMinus1OutputsJson) as IEntity;
            allocationsBlobActive.ExternalName = DynamicAllocationEntityProperties.AllocationSetActive;
            allocationsBlobActiveMinus1.ExternalName = DynamicAllocationEntityProperties.AllocationSetActive;

            var index = new List<HistoryElement>
                {
                    new HistoryElement
                        {
                            AllocationStartTime = activeAllocationStartTime.ToString("o"),
                            AllocationOutputsId = allocationOutputsIdActive.ToString()
                        },
                    new HistoryElement
                        {
                            AllocationStartTime = activeAlloctionMinus1StartTime.ToString("o"),
                            AllocationOutputsId = allocationOutputsIdActiveMinus1.ToString()
                        }
                };

            var indexJson = AppsJsonSerializer.SerializeObject(index);
            var allocationHistoryIndexBlob = BlobEntity.BuildBlobEntity(allocationHistoryIndexBlobEntityId, indexJson);

            var indexAssociation = new Association
                {
                    ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                    TargetEntityId = allocationHistoryIndexBlobEntityId
                };

            this.campaignEntity.Associations.Add(indexAssociation);

            if (failIndexAssociation)
            {
                this.campaignEntity.Associations.Remove(indexAssociation);
            }

            RepositoryStubUtilities.SetupGetEntityStub(
                this.entityRepository, allocationHistoryIndexBlobEntityId, allocationHistoryIndexBlob, failIndexBlob);

            RepositoryStubUtilities.SetupGetEntityStub(
                this.entityRepository, allocationOutputsIdActive, allocationsBlobActive, failAllocationBlob);

            RepositoryStubUtilities.SetupGetEntityStub(
                this.entityRepository, allocationOutputsIdActiveMinus1, allocationsBlobActiveMinus1, failAllocationBlob);
        }

        /// <summary>Setup the allocation parameters on the campaign.</summary>
        /// <param name="sourceCampaignEntity">The campaign entity.</param>
        private void SetupAllocationParameters(IEntity sourceCampaignEntity)
        {
            // Initialize allocation parameters
            AllocationParametersTestHelpers.Initialize(sourceCampaignEntity);
            var config = sourceCampaignEntity.GetConfigSettings();
            config["DynamicAllocation.Margin"] = "{0}".FormatInvariant(this.testMargin);
            config["DynamicAllocation.PerMilleFees"] = "{0}".FormatInvariant(this.testPerMilleFees);
            sourceCampaignEntity.SetConfigSettings(config);
        }

        /// <summary>Setup a save campaign stub that captures the saved campaign.</summary>
        /// <param name="captureCampaign">delegate to capture campaign being saved.</param>
        /// <param name="saveSucceeds">True if the save should return a success result.</param>
        private void SetupUpdateCampaignStub(
            Action<IEnumerable<EntityProperty>> captureCampaign,
            bool saveSucceeds = true)
        {
            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.entityRepository, this.campaignEntity.ExternalEntityId, captureCampaign, !saveSucceeds);
        }
    }
}
