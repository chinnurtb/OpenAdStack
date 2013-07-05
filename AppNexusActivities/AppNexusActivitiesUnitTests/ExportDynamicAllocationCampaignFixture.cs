//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaignFixture.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Activities;
using AppNexusActivities;
using AppNexusActivities.Measures;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Rhino.Mocks;
using ScheduledActivities;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>
    /// Tests for the ExportDynamicAllocationCampaign activity
    /// </summary>
    [TestClass]
    public class ExportDynamicAllocationCampaignFixture
    {
        /// <summary>Test 3rd party ad tag</summary>
        private const string TestAdTag = @"
<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank"">
<img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>Test setting for report request frequency</summary>
        /// <remarks>Must be at least 2 hours for test purposes</remarks>
        private const string ReportRequestFrequency = "01:00:00";

        /// <summary>Random number generator</summary>
        private readonly Random R = new Random();

        /// <summary>Test logger for testing</summary>
        private TestLogger testLogger;

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>Mock AppNexus client for testing</summary>
        private IAppNexusApiClient mockAppNexusClient;

        /// <summary>
        /// List of AppNexus campaigns under the line item
        /// </summary>
        private IList<object> mockAppNexusLineItemCampaigns;

        /// <summary>The measure map</summary>
        private MeasureMap measureMap;

        /// <summary>
        /// The last request submitted via the test SubmitActivityRequestHandler
        /// </summary>
        private ActivityRequest submittedRequest;

        /// <summary>Company for testing</summary>
        private CompanyEntity testCompany;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity testCampaign;

        /// <summary>Campaign owner user entity for testing</summary>
        private UserEntity testCampaignOwner;

        /// <summary>Creative entity for testing</summary>
        private CreativeEntity[] testCreatives;

        /// <summary>Allocations blob entity for testing</summary>
        private BlobEntity testAllocationsBlob;

        /// <summary>EntityId for the test allocations blob</summary>
        private EntityId testAllocationsBlobEntityId;

        /// <summary>Ids of the test segmentsIds created in AppNexus</summary>
        private IDictionary<long, int> testMeasureSegmentIds;

        /// <summary>Test initial budget allocations</summary>
        private BudgetAllocation testInitialAllocations;

        /// <summary>Test updated budget allocations</summary>
        private BudgetAllocation testUpdatedAllocations;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testInitialExportAllocationIds;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testUpdatedExportAllocationIds;

        /// <summary>Gets the TimeSlottedRegistry of reports to request</summary>
        private static TimeSlottedRegistry<Tuple<string, DeliveryNetworkDesignation>> ReportsToRequest
        {
            get
            {
                return Scheduler.GetRegistry<Tuple<string, DeliveryNetworkDesignation>>(
                    DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            }
        }

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["Delivery.ReportFrequency"] = ReportRequestFrequency;
            ConfigurationManager.AppSettings["AppNexus.Sandbox"] = "true";

            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });
            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();
            MeasureSourceFactory.Initialize(
                new IMeasureSourceProvider[]
                {
                    new AppNexusMeasureSourceProvider(),
                    new AppNexusLegacyMeasureSourceProvider()
                });

            this.CreateTestSegmentsAndMeasures();
            this.CreateTestAllocations();
            this.CreateTestEntities();
            this.CreateMockAppNexusClient();
            this.CreateMockEntityRepository();
        }

        /// <summary>Basic activity create test</summary>
        [TestMethod]
        public void Create()
        {
            var activity = this.CreateActivity() as ExportDynamicAllocationCampaignActivity;
            Assert.IsNotNull(activity);
        }

        /// <summary>Test exporting a campaign</summary>
        [TestMethod]
        public void ExportCampaign()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.ExportDACampaign,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.testCampaignOwner.UserId },
                    { EntityActivityValues.CompanyEntityId, this.testCompany.ExternalEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.testCampaign.ExternalEntityId.ToString() },
                    { AppNexusActivityValues.CampaignStartDate, DateTime.UtcNow.ToString("o") },
                    { DynamicAllocationActivityValues.ExportAllocationsEntityId, this.testAllocationsBlobEntityId }
                }
            };

            // Initial export
            var activity = this.CreateActivity();

            // Check activity result
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);
            Assert.IsTrue(result.Values.ContainsKey(AppNexusActivityValues.LineItemId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values[AppNexusActivityValues.LineItemId]));

            // Check that advertiser was created in AppNexus
            var advertiserId = this.testCompany.GetAppNexusAdvertiserId();
            Assert.IsNotNull(advertiserId);

            // Check that report request was scheduled
            var reportRequestFrequency = TimeSpan.Parse(ReportRequestFrequency);
            var nextReportRequestTime = DateTime.UtcNow + reportRequestFrequency;
            var beforeReportRequestTime = DateTime.UtcNow + reportRequestFrequency - TimeSpan.FromHours(1);
            Assert.AreEqual(0, ReportsToRequest[beforeReportRequestTime].Count);
            Assert.AreEqual(1, ReportsToRequest[nextReportRequestTime].Count);

            // Check log entries
            Assert.IsFalse(this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error));
            Assert.IsTrue(this.testLogger.HasMessagesMatching("One or more creatives for campaign .* have not passed audit.*"));
            foreach (var allocation in this.testInitialAllocations.PerNodeResults)
            {
                if (this.testInitialExportAllocationIds.Contains(allocation.Value.AllocationId))
                {
                    var escapedMeasureSetString = allocation.Key.ToString().Replace("[", @"\[").Replace("]", @"\]");
                    var profileCreatedPattern = "Created AppNexus Targeting Profile '[0-9]+' for Measures '{0}'.*".FormatInvariant(escapedMeasureSetString);
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(profileCreatedPattern));
                    var campaignCreatedPattern = @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*".FormatInvariant(allocation.Value.AllocationId);
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(campaignCreatedPattern));
                }
                else
                {
                    // Assert allocation node without export budget not exported
                    var exportingNodeSubstring = "Exporting allocation node '{0}' of campaign".FormatInvariant(allocation.Value.AllocationId);
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(exportingNodeSubstring));
                }
            }

            // Update allocations and exportAllocationIds and re-export
            this.UpdateTestAllocationsBlob();
            result = activity.Run(request);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);
            Assert.IsTrue(result.Values.ContainsKey(AppNexusActivityValues.LineItemId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values[AppNexusActivityValues.LineItemId]));

            // Check that no additional report requests were scheduled
            // Export should only schedule the initial request when the line-item is created
            Assert.AreEqual(1, ReportsToRequest[DateTime.UtcNow.AddYears(100)].Count);

            // Check log entries
            Assert.IsFalse(this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error));
            var initialAllocationsWithBudget = this.testInitialAllocations.PerNodeResults.Values
                .Where(allocation => allocation.ExportBudget > 0)
                .Select(allocation => allocation.AllocationId);
            foreach (var allocation in this.testUpdatedAllocations.PerNodeResults.Values)
            {
                var campaignUpdatedSubstring = @"Updated AppNexus Campaign for Allocation '{0}'".FormatInvariant(allocation.AllocationId);
                var campaignCreatedPattern = @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*".FormatInvariant(allocation.AllocationId);
                var deletedCampaignSubstring = "Deleted AppNexus campaign with code '{0}'".FormatInvariant(allocation.AllocationId);
                var noCampaignFoundToDeactivateSubstring = "No AppNexus campaign exists with code '{0}' to deactivate.".FormatInvariant(allocation.AllocationId);

                if (this.testUpdatedExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was created
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(campaignCreatedPattern));
                }
                else if (this.testInitialExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was deleted
                    Assert.IsTrue(this.testLogger.HasMessagesContaining(deletedCampaignSubstring));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(noCampaignFoundToDeactivateSubstring));
                }
                else
                {
                    // if neither initialExportedAllocationIds nor initialExportedAllocationIds contains allocation.AllocationId
                    // then no log activity of that campaign should have taken place
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(campaignUpdatedSubstring));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(campaignCreatedPattern));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(deletedCampaignSubstring));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(noCampaignFoundToDeactivateSubstring));
                }
            }
        }

        /// <summary>
        /// Creates an instance of the ExportDynamicAllocationCampaign activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private ExportDynamicAllocationCampaignActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository }
            };

            return Activity.CreateActivity(typeof(ExportDynamicAllocationCampaignActivity), context, this.SubmitActivityRequest) as ExportDynamicAllocationCampaignActivity;
        }

        /// <summary>Test submit activity request handler</summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if successful; otherwise, false.</returns>
        private bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            this.submittedRequest = request;
            return true;
        }

        /// <summary>Creates segmentsIds in AppNexus for the test campaign</summary>
        private void CreateTestSegmentsAndMeasures()
        {
            // Create new test segments pairs
            this.testMeasureSegmentIds = new Dictionary<long, int>();
            var segmentIds = new[] { 98902, 98907, 98909, 98911, 98918, 98921, 98920, 99361, 99365, 99370 };
            foreach (var segmentId in segmentIds)
            {
                // Create a new, unique measure id
                int measureId;
                do
                {
                    measureId = new Random().Next(100000);
                }
                while (this.testMeasureSegmentIds.ContainsKey(measureId));

                this.testMeasureSegmentIds.Add(measureId, segmentId);
            }

            // Stuff the MeasureMap with the test segments
            this.measureMap = new MeasureMap(
                this.testMeasureSegmentIds
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IDictionary<string, object>)new Dictionary<string, object>
                        {
                            { "displayName", Guid.NewGuid().ToString() },
                            { "leafName", Guid.NewGuid().ToString() },
                            { "APNXId", kvp.Value },
                            { "APNXId_Sandbox", kvp.Value },
                            { "DataProvider", null },
                            { "DataCost", 0.25 },
                            { "type", "Segment" }
                    }));
        }

        /// <summary>Create test allocations</summary>
        private void CreateTestAllocations()
        {
            // Initial allocations with some arbitrary values for exporting.
            // The last allocation is budget-less and should not be created.
            this.testInitialAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(new[] { 0, 1, 2, 3, 4 }, 5500, 55, 70, 50, 3200, 4.25m),
                    this.CreateTestAllocation(new[] { 5, 6, 7, 8, 9 }, 1250, 23, 30, 20, 1650, 1.50m),
                    this.CreateTestAllocation(new[] { 2, 6, 3, 4 }, 1350, 18, 22, 16, 1400, 5.25m),
                    this.CreateTestAllocation(new[] { 3, 2, 9 }, 1175, 10, 13, 8, 1234, 3.75m),
                    this.CreateTestAllocation(new[] { 8, 3, 4 }, 0, 0, 0, 0, 600, 3.25m),
                }
                .ToDictionary()
            };

            // Updated allocations have the same measure sets with value changes.
            // Value changes include no export budget for the first allocation to
            // test deactivation.
            var initialMeasureSets = this.testInitialAllocations
                .PerNodeResults
                .Keys
                .ToArray();
            var initialAllocationIds = this.testInitialAllocations
                .PerNodeResults
                .Values
                .Select(allocation => allocation.AllocationId)
                .ToArray();
            
            // create an intial string list of allocation IDs to export
            // the nodes to export should not be defined as those with export budget 
            // - it will be a subset of those in general
            this.testInitialExportAllocationIds =
                this.testInitialAllocations.PerNodeResults
                .Where(allocation => allocation.Value.ExportBudget > 0)
                .Select(allocation => allocation.Value.AllocationId)
                .ToArray();

            this.testUpdatedAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(initialMeasureSets[0], initialAllocationIds[0], 0, 0, 0, 0, 3500, 3.75m),
                    this.CreateTestAllocation(initialMeasureSets[1], initialAllocationIds[1], 2250, 33, 36, 30, 1825, 4.25m),
                    this.CreateTestAllocation(initialMeasureSets[2], initialAllocationIds[2], 1250, 20, 24, 18, 1500, 5.25m),
                    this.CreateTestAllocation(initialMeasureSets[3], initialAllocationIds[3], 1350, 18, 21, 16, 2345, 3.75m),
                    this.CreateTestAllocation(initialMeasureSets[4], initialAllocationIds[4], 2500, 26, 29, 23, 600, 4.25m),
                }
                .ToDictionary()
            };

            // create an updated string list of allocation IDs to export
            // the nodes to export should not be defined as those with export budget 
            // - it will be a subset of those in general
            this.testUpdatedExportAllocationIds =
                this.testUpdatedAllocations.PerNodeResults
                .Where(allocation => allocation.Value.ExportBudget > 0)
                .Select(allocation => allocation.Value.AllocationId)
                .ToArray();
        }

        /// <summary>
        /// Creates a test allocation from the provided values
        /// </summary>
        /// <param name="testMeasureSegmentIdIndexes">
        /// Indexes of test measure segments to use in order
        /// </param>
        /// <param name="periodImpressionCap">The period impression cap</param>
        /// <param name="periodMediaBudget">The period media budget</param>
        /// <param name="periodTotalBudget">The period total budget</param>
        /// <param name="exportBudget">The export budget</param>
        /// <param name="lifetimeMediaSpend">The lifetime media spend</param>
        /// <param name="maxBid">The max bid</param>
        /// <returns>The allocation</returns>
        private KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> CreateTestAllocation(
            int[] testMeasureSegmentIdIndexes,
            long periodImpressionCap,
            decimal periodMediaBudget,
            decimal periodTotalBudget,
            decimal exportBudget,
            decimal lifetimeMediaSpend,
            decimal maxBid)
        {
            var measureSet = new MeasureSet();
            foreach (var testMeasureSegmentIdIndex in testMeasureSegmentIdIndexes)
            {
                measureSet.Add(this.testMeasureSegmentIds.Keys.ElementAt(
                    testMeasureSegmentIdIndex));
            }

            return this.CreateTestAllocation(
                measureSet,
                Guid.NewGuid().ToString("N"),
                periodImpressionCap,
                periodMediaBudget,
                periodTotalBudget,
                exportBudget,
                lifetimeMediaSpend,
                maxBid);
        }

        /// <summary>
        /// Creates a test allocation from the provided values
        /// </summary>
        /// <param name="measureSet">The measure set</param>
        /// <param name="allocationId">The allocation id</param>
        /// <param name="periodImpressionCap">The period impression cap</param>
        /// <param name="periodMediaBudget">The period media budget</param>
        /// <param name="periodTotalBudget">The period total budget</param>
        /// <param name="exportBudget">The export budget</param>
        /// <param name="lifetimeMediaSpend">The lifetime media spend</param>
        /// <param name="maxBid">The max bid</param>
        /// <returns>The allocation</returns>
        private KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> CreateTestAllocation(
            MeasureSet measureSet,
            string allocationId,
            long periodImpressionCap,
            decimal periodMediaBudget,
            decimal periodTotalBudget,
            decimal exportBudget,
            decimal lifetimeMediaSpend,
            decimal maxBid)
        {
            return new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(
                measureSet,
                new PerNodeBudgetAllocationResult
                {
                    AllocationId = allocationId,
                    PeriodImpressionCap = periodImpressionCap,
                    PeriodMediaBudget = periodMediaBudget,
                    PeriodTotalBudget = periodTotalBudget,
                    ExportBudget = exportBudget,
                    LifetimeMediaSpend = lifetimeMediaSpend,
                    MaxBid = maxBid
                });
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestEntities()
        {
            this.testCompany = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(), "Test Company");

            this.testCampaignOwner = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"))),
                "nobody@rc.dev");

            this.testCreatives = new[] { "audited", "no_audit", "pending" }
                .Select(auditStatus =>
                    {
                        var creative = EntityTestHelpers.CreateTestCreativeEntity(
                            new EntityId(),
                            "Test Creative",
                            @"<a href=\""http://example.com\""><img src=\""http://cdn.example.com/ad/12345678.gif\"" /></a>");
                        creative.SetCreativeType(CreativeType.ThirdPartyAd);
                        creative.SetAppNexusCreativeId(R.Next());
                        creative.SetAppNexusAuditStatus(auditStatus);
                        return creative;
                    })
                .ToArray();

            this.testCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(),
                "Test Campaign - " + Guid.NewGuid().ToString(),
                10000,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(7, 0, 0, 0),
                "Test Persona - " + Guid.NewGuid().ToString());
            this.testCampaign.SetOwnerId(this.testCampaignOwner.UserId);

            this.testAllocationsBlob = BlobEntity.BuildBlobEntity<string>(
                this.testAllocationsBlobEntityId = new EntityId(),
                JsonConvert.SerializeObject(this.testInitialAllocations));

            this.testCampaign.AssociateEntities(
                DynamicAllocationEntityProperties.AllocationSetActive,
                "description",
                new HashSet<IEntity>(new[] { this.testAllocationsBlob }),
                AssociationType.Relationship,
                true);

            this.testCampaign.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                new PropertyValue(PropertyType.String, JsonConvert.SerializeObject(this.measureMap.Map)));

            this.testCampaign.AssociateEntities(
                "Creative",
                "description",
                new HashSet<IEntity>(this.testCreatives),
                AssociationType.Relationship,
                true);
        }

        /// <summary>Update the test allocation blob entity</summary>
        private void UpdateTestAllocationsBlob()
        {
            this.testAllocationsBlob = BlobEntity.BuildBlobEntity<string>(
                this.testAllocationsBlobEntityId,
                JsonConvert.SerializeObject(this.testUpdatedAllocations));
        }

        /// <summary>Create the IAppNexusApiClient mock</summary>
        private void CreateMockAppNexusClient()
        {
            var rand = new Random();
            var testLineItemId = rand.Next();

            this.mockAppNexusLineItemCampaigns = new List<object>();
            this.mockAppNexusClient = MockRepository.GenerateMock<IAppNexusApiClient>();

            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType).Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything)).Return(this.mockAppNexusClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });

            // TODO: Add some validations to these stubs
            this.mockAppNexusClient.Stub(f =>
                f.CreateAdvertiser(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .Return(rand.Next());

            this.mockAppNexusClient.Stub(f =>
                f.CreateLineItem(
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<bool>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<decimal>.Is.Anything))
                .Return(testLineItemId);

            this.mockAppNexusClient.Stub(f =>
                f.CreateCampaignProfile(
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<bool>.Is.Anything,
                    Arg<Tuple<int, int>>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<IDictionary<int, string>>.Is.Anything,
                    Arg<IDictionary<int, string>>.Is.Anything,
                    Arg<IEnumerable<string>>.Is.Anything,
                    Arg<PageLocation>.Is.Anything,
                    Arg<IEnumerable<int>>.Is.Anything,
                    Arg<IDictionary<int, bool>>.Is.Anything,
                    Arg<IDictionary<int, bool>>.Is.Anything))
                .Return(rand.Next());

            this.mockAppNexusClient.Stub(f =>
                f.CreateCampaign(
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int[]>.Is.Anything,
                    Arg<bool>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<decimal>.Is.Anything,
                    Arg<long>.Is.Anything,
                    Arg<decimal>.Is.Anything))
                .Return(-1)
                .WhenCalled(call =>
                {
                    var id = rand.Next();
                    var name = (string)call.Arguments[1];
                    var code = (string)call.Arguments[2];
                    var profileId = (int)call.Arguments[4];
                    var active = (bool)call.Arguments[6];
                    if (this.MockCampaignCreated(code))
                    {
                        Assert.Fail("A campaign with code '{0}' has already been created.".FormatInvariant(code));
                    }

                    this.mockAppNexusLineItemCampaigns.Add(
                        new Dictionary<string, object>
                        {
                            { AppNexusValues.Id, id },
                            { AppNexusValues.Name, name },
                            {
                                AppNexusValues.State,
                                (bool)call.Arguments[6] ?
                                    AppNexusValues.StateActive :
                                    AppNexusValues.StateInactive
                            },
                            { AppNexusValues.Code, code },
                            { AppNexusValues.ProfileId, profileId },
                        });

                    call.ReturnValue = id;
                });

            this.mockAppNexusClient.Stub(f =>
                f.UpdateCampaign(
                    Arg<string>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<int[]>.Is.Anything,
                    Arg<bool>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<DateTime>.Is.Anything,
                    Arg<decimal>.Is.Anything,
                    Arg<long>.Is.Anything,
                    Arg<decimal>.Is.Anything))
                .WhenCalled(call =>
                {
                    Assert.Fail("Campaigns should be deleted and recreated, not updated.");

                    /*
                    var code = call.Arguments[0] as string;
                    if (!this.MockCampaignCreated(code))
                    {
                        // Campaign does not exist (yet)
                        throw new AppNexusClientException(string.Empty, string.Empty);
                    }
                    */
                });

            this.mockAppNexusClient.Stub(f =>
                f.DeleteCampaign(
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything))
                .WhenCalled(call =>
                {
                    var campaignId = (int)call.Arguments[1];
                    var campaign = this.GetMockCreatedCampaign(campaignId);
                    this.mockAppNexusLineItemCampaigns.Remove(campaign);
                });

            this.mockAppNexusClient.Stub(f =>
                f.GetCampaignByCode(
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything))
                .WhenCalled(call =>
                {
                    var code = call.Arguments[1] as string;
                    var campaign = this.GetMockCreatedCampaign(code);
                    if (campaign == null)
                    {
                        // Campaign does not exist (yet)
                        throw new AppNexusClientException(string.Empty, string.Empty);
                    }

                    call.ReturnValue = campaign;
                });

            this.mockAppNexusClient.Stub(f =>
                f.GetLineItemById(Arg<int>.Is.Anything, Arg<int>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    call.ReturnValue = new Dictionary<string, object>
                    {
                        { AppNexusValues.Campaigns, this.mockAppNexusLineItemCampaigns.Cast<object>().ToArray() }
                    };
                });

            this.mockAppNexusClient.Stub(f =>
                f.GetCreative(Arg<int>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    call.ReturnValue = new Dictionary<string, object>
                    {
                        {
                            AppNexusValues.AuditStatus,
                            this.testCreatives.Single(c =>
                                c.GetAppNexusCreativeId() == (int)call.Arguments[0])
                                .GetAppNexusAuditStatus()
                        }
                    };
                });
        }

        /// <summary>
        /// Gets a campaign created using the mock
        /// </summary>
        /// <param name="code">Code of the campaign</param>
        /// <returns>The campaign, if created; otherwise, null.</returns>
        private IDictionary<string, object> GetMockCreatedCampaign(string code)
        {
            return this.mockAppNexusLineItemCampaigns
                .Cast<IDictionary<string, object>>()
                .FirstOrDefault(c => (string)c[AppNexusValues.Code] == code);
        }

        /// <summary>
        /// Gets a campaign created using the mock
        /// </summary>
        /// <param name="id">Id of the campaign</param>
        /// <returns>The campaign, if created; otherwise, null.</returns>
        private IDictionary<string, object> GetMockCreatedCampaign(int id)
        {
            return this.mockAppNexusLineItemCampaigns
                .Cast<IDictionary<string, object>>()
                .FirstOrDefault(c => (int)c[AppNexusValues.Id] == id);
        }

        /// <summary>
        /// Checks whether the campaign has been created
        /// </summary>
        /// <param name="code">Code of the campaign</param>
        /// <returns>The campaign</returns>
        private bool MockCampaignCreated(string code)
        {
            return this.GetMockCreatedCampaign(code) != null;
        }

        /// <summary>Create the IEntityRepository mock</summary>
        private void CreateMockEntityRepository()
        {
            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();

            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCompany.ExternalEntityId, this.testCompany, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCampaign.ExternalEntityId, this.testCampaign, false);
            foreach (var creative in this.testCreatives)
            {
                RepositoryStubUtilities.SetupGetEntityStub(
                    this.mockRepository, creative.ExternalEntityId, creative, false);
            }

            RepositoryStubUtilities.SetupGetUserStub(
                this.mockRepository, this.testCampaignOwner.UserId, this.testCampaignOwner, false);

            this.mockRepository.Stub(f =>
                f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.testAllocationsBlobEntityId)))
                .Return(null)
                .WhenCalled(call =>
                {
                    call.ReturnValue = this.testAllocationsBlob;
                });

            this.mockRepository.Stub(f =>
                f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var entityIds = call.Arguments[1] as EntityId[];
                    var entities =
                        new IEntity[] { this.testCompany, this.testCampaign, this.testAllocationsBlob }
                            .Concat(this.testCreatives)
                            .Where(e => entityIds.Contains((EntityId)e.ExternalEntityId))
                            .ToArray();
                    call.ReturnValue = new HashSet<IEntity>(entities);
                });
        }
    }
}
