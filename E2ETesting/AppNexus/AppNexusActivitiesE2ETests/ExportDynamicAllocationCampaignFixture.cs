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
using Activities;
using AppNexusActivities;
using AppNexusActivities.Measures;
using AppNexusClient;
using AppNexusTestUtilities;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Rhino.Mocks;
using ScheduledActivities;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesE2ETests
{
    /// <summary>
    /// Tests for the ExportDynamicAllocationCampaign activity
    /// </summary>
    [TestClass]
    public class ExportDynamicAllocationCampaignFixture
    {
        /// <summary>Test segment format</summary>
        private const string TestSegmentJsonFormat = @"
{{
    ""segment"":
    {{
        ""member_id"":{0},
        ""short_name"":""test segment {1}"",
        ""code"":""{2}"",
        ""price"":0.12
    }}
}}";

        /// <summary>Test 3rd party ad tag</summary>
        private const string TestAdTag = @"
<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank"">
<img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>Test setting for report request frequency</summary>
        /// <remarks>Must be at least 2 hours for test purposes</remarks>
        private const string ReportRequestFrequency = "01:00:00";

        /// <summary>Random number generator</summary>
        private static readonly Random R = new Random();

        /// <summary>Sandbox AppNexus client</summary>
        private AppNexusApiClient appNexusClient;

        /// <summary>Test logger for testing</summary>
        private TestLogger testLogger;

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>Test initial budget allocations</summary>
        private BudgetAllocation testInitialAllocations;

        /// <summary>Test updated budget allocations</summary>
        private BudgetAllocation testUpdatedAllocations;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testInitialExportAllocationIds;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testUpdatedExportAllocationIds;

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
        private CreativeEntity testCreative;

        /// <summary>Allocations blob entity for testing</summary>
        private BlobEntity testAllocationsBlob;

        /// <summary>EntityId for the test company</summary>
        private EntityId testCompanyEntityId;

        /// <summary>EntityId for the test campaign</summary>
        private EntityId testCampaignEntityId;

        /// <summary>EntityId for the test creative</summary>
        private EntityId testCreativeEntityId;

        /// <summary>EntityId for the test allocations blob</summary>
        private EntityId testAllocationsBlobEntityId;

        /// <summary>AppNexus advertiser id for the test company</summary>
        private int testAdvertiserId;

        /// <summary>Test measure ids</summary>
        private IList<long> testMeasureIds;

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
            ConfigurationManager.AppSettings["AppNexus.DataProviders"] = "Lotame|eXelate|Peer39|BlueKai";
            ConfigurationManager.AppSettings["AppNexus.SegmentCacheExpiry"] = "00:30:00";
            ConfigurationManager.AppSettings["AppNexus.SegmentDataCostsRequired"] = "false";
            ConfigurationManager.AppSettings["AppNexus.Sandbox"] = "true";

            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            CachedMeasureSource.CacheUpdateStartTimes = null;
            CachedMeasureSource.LocalMeasureCache = null;
            Scheduler.Registries = null;
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();

            this.testMeasureIds = new List<long>();
            this.appNexusClient = new AppNexusApiClient();

            this.CreateTestAdvertiserCompany();
            this.CreateTestCreativeEntity();
            this.CreateTestCampaignEntity();
            this.InitializeMeasureSourceProviders();
        }

        /// <summary>Cleanup any AppNexus objects created by the test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            AppNexusClientHelper.Cleanup();
        }

        /// <summary>Basic activity create test</summary>
        [TestMethod]
        public void Create()
        {
            var activity = this.CreateActivity();
            Assert.IsNotNull(activity);
        }

        /// <summary>Test exporting a campaign</summary>
        [TestMethod]
        public void ExportLegacyCampaign()
        {
            // Create legacy measure sources and add some measures from each to the test measure ids
            var sources = MeasureSourceFactory.CreateMeasureSources(DeliveryNetworkDesignation.AppNexus, 0, this.testCompany, this.testCampaign);
            this.testMeasureIds.Add(
                sources
                .AsParallel()
                .Select(source => MeasureSourceTestHelpers.LoadMeasures(source))
                .SelectMany(measures =>
                    Enumerable.Range(1, 1 + R.Next(Math.Min(64, measures.Count)) - 1)
                    .Select(i =>
                        measures.Keys.ElementAt(R.Next(measures.Count)))));

            // Initialize test allocations and campaign
            this.CreateTestAllocations();
            this.CreateTestCampaignEntity();
            this.InitializeEntityRepositoryMock();

            // Prepare the activity request
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.ExportDACampaign,
                Values =
                {
                    { EntityActivityValues.AuthUserId, "6Az3F8+9BA274Cf0/8gE/q98w13oB6u3==" },
                    { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId.ToString() },
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
            AppNexusClientHelper.AddAdvertiserForCleanup((int)advertiserId);

            // Check that report request was scheduled
            var reportRequestFrequency = TimeSpan.Parse(ReportRequestFrequency);
            var nextReportRequestTime = DateTime.UtcNow + reportRequestFrequency;
            var beforeReportRequestTime = DateTime.UtcNow + reportRequestFrequency - TimeSpan.FromHours(1);
            Assert.AreEqual(0, ReportsToRequest[beforeReportRequestTime].Count);
            Assert.AreEqual(1, ReportsToRequest[nextReportRequestTime].Count);

            // Check log entries
            Assert.IsFalse(
                this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error),
                "One or more errors were logged during export:\n{0}".FormatInvariant(string.Join("\n", this.testLogger.ErrorEntries)));

            foreach (var allocation in this.testInitialAllocations.PerNodeResults)
            {
                if (this.testInitialExportAllocationIds.Contains(allocation.Value.AllocationId))
                {
                    var escapedMeasureSetString = allocation.Key.ToString().Replace("[", @"\[").Replace("]", @"\]");
                    var profileCreatedPattern =
                        "Created AppNexus Targeting Profile '[0-9]+' for Measures '{0}'.*"
                        .FormatInvariant(escapedMeasureSetString);
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(profileCreatedPattern));

                    var campaignCreatedPattern =
                        @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*"
                        .FormatInvariant(allocation.Value.AllocationId);
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
            Assert.IsFalse(
                this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error),
                "One or more errors were logged during export:\n{0}".FormatInvariant(string.Join("\n", this.testLogger.ErrorEntries)));

            var initialAllocationsWithBudget = this.testInitialAllocations.PerNodeResults.Values
                .Where(allocation => allocation.ExportBudget > 0)
                .Select(allocation => allocation.AllocationId);
            foreach (var allocation in this.testUpdatedAllocations.PerNodeResults.Values)
            {
                var campaignUpdatedSubstring =
                    @"Updated AppNexus Campaign for Allocation '{0}'"
                    .FormatInvariant(allocation.AllocationId);
                var campaignCreatedPattern =
                    @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*"
                    .FormatInvariant(allocation.AllocationId);
                var deletedCampaignSubstring =
                    @"Deleted AppNexus campaign with code '{0}'"
                    .FormatInvariant(allocation.AllocationId);
                var noCampaignFoundToDeactivateSubstring =
                    @"No AppNexus campaign exists with code '{0}' to deactivate."
                    .FormatInvariant(allocation.AllocationId);

                if (this.testUpdatedExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was created
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(campaignCreatedPattern));
                }
                else if (this.testInitialExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was deactivated
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

        /// <summary>Test exporting a campaign</summary>
        [TestMethod]
        public void ExportCampaign()
        {
            // Create measure sources and add some measures from each
            var sources = MeasureSourceFactory.CreateMeasureSources(
                DeliveryNetworkDesignation.AppNexus,
                1,
                this.testCompany,
                this.testCampaign,
                this.testCampaignOwner);

            this.testMeasureIds.Add(
                sources
                .AsParallel()
                .Select(source => MeasureSourceTestHelpers.LoadMeasures(source))
                .SelectMany(measures =>
                    Enumerable.Range(1, 1 + R.Next(Math.Min(64, measures.Count)) - 1)
                    .Select(i =>
                        measures.Keys.ElementAt(R.Next(measures.Count)))));

            // Initialize test allocations and campaign
            this.CreateTestAllocations();
            this.CreateTestCampaignEntity();
            this.InitializeEntityRepositoryMock();

            // Set the campaign to use the new exporter
            this.testCampaign.SetExporterVersion(1);

            // Prepare the activity request
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.ExportDACampaign,
                Values =
                {
                    { EntityActivityValues.AuthUserId, "6Az3F8+9BA274Cf0/8gE/q98w13oB6u3==" },
                    { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId.ToString() },
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
            AppNexusClientHelper.AddAdvertiserForCleanup((int)advertiserId);

            // Check that report request was scheduled
            var reportRequestFrequency = TimeSpan.Parse(ReportRequestFrequency);
            var nextReportRequestTime = DateTime.UtcNow + reportRequestFrequency;
            var beforeReportRequestTime = DateTime.UtcNow + reportRequestFrequency - TimeSpan.FromHours(1);
            Assert.AreEqual(0, ReportsToRequest[beforeReportRequestTime].Count);
            Assert.AreEqual(1, ReportsToRequest[nextReportRequestTime].Count);

            // Check log entries
            Assert.IsFalse(
                this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error),
                "One or more errors were logged during export:\n{0}".FormatInvariant(string.Join("\n", this.testLogger.ErrorEntries)));

            foreach (var allocation in this.testInitialAllocations.PerNodeResults)
            {
                if (this.testInitialExportAllocationIds.Contains(allocation.Value.AllocationId))
                {
                    var escapedMeasureSetString = allocation.Key.ToString().Replace("[", @"\[").Replace("]", @"\]");
                    var profileCreatedPattern =
                        "Created AppNexus Targeting Profile '[0-9]+' for Measures '{0}'.*"
                        .FormatInvariant(escapedMeasureSetString);
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(profileCreatedPattern));

                    var campaignCreatedPattern =
                        @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*"
                        .FormatInvariant(allocation.Value.AllocationId);
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
            Assert.IsFalse(
                this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error),
                "One or more errors were logged during export:\n{0}".FormatInvariant(string.Join("\n", this.testLogger.ErrorEntries)));

            var initialAllocationsWithBudget = this.testInitialAllocations.PerNodeResults.Values
                .Where(allocation => allocation.ExportBudget > 0)
                .Select(allocation => allocation.AllocationId);
            foreach (var allocation in this.testUpdatedAllocations.PerNodeResults.Values)
            {
                var campaignCreatedPattern =
                    @"Created AppNexus Campaign '[0-9]+' for Allocation 'PerNodeBudgetAllocationResult: \[\n\tAllocationId={0}.*"
                    .FormatInvariant(allocation.AllocationId);
                var deletedCampaignSubstring =
                    @"Deleted AppNexus campaign with code '{0}'"
                    .FormatInvariant(allocation.AllocationId);
                var noCampaignFoundToDeactivateSubstring =
                    @"No AppNexus campaign exists with code '{0}' to deactivate."
                    .FormatInvariant(allocation.AllocationId);

                if (this.testUpdatedExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was created
                    Assert.IsTrue(this.testLogger.HasMessagesMatching(campaignCreatedPattern));
                }
                else if (this.testInitialExportAllocationIds.Contains(allocation.AllocationId))
                {
                    // Check the campaign was deactivated
                    Assert.IsTrue(this.testLogger.HasMessagesContaining(deletedCampaignSubstring));

                    Assert.IsFalse(this.testLogger.HasMessagesContaining(noCampaignFoundToDeactivateSubstring));
                }
                else
                {
                    // if neither initialExportedAllocationIds nor initialExportedAllocationIds contains allocation.AllocationId
                    // then no log activity of that campaign should have taken place
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(campaignCreatedPattern));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(deletedCampaignSubstring));
                    Assert.IsFalse(this.testLogger.HasMessagesContaining(noCampaignFoundToDeactivateSubstring));
                }
            }
        }

        /// <summary>Test exporting a page location campaign</summary>
        [TestMethod]
        public void ExportPageLocationMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                "Position");
        }

        /// <summary>Test exporting a gender campaign</summary>
        [TestMethod]
        public void ExportGenderMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                "demographic",
                "gender");
        }

        /// <summary>Test exporting an age campaign</summary>
        [TestMethod]
        public void ExportAgeRangeMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                AgeRangeMeasureSource.TargetingType,
                AgeRangeMeasureSource.TargetingSubType);
        }

        /// <summary>Test exporting a city campaign</summary>
        /// <remarks>TODO: Add city targeting support</remarks>
        [TestMethod]
        [Ignore]
        public void ExportCityMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                CityMeasureSource.TargetingType,
                CityMeasureSource.TargetingSubType);
        }

        /// <summary>Test exporting a metro code campaign</summary>
        [TestMethod]
        public void ExportMetroCodeMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                MetroCodeMeasureSource.TargetingType,
                MetroCodeMeasureSource.TargetingSubType);
        }

        /// <summary>Test exporting a metro code campaign</summary>
        [TestMethod]
        public void ExportRegionMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                RegionMeasureSource.TargetingType,
                RegionMeasureSource.TargetingSubType);
        }

        /// <summary>Test exporting a contenty category campaign</summary>
        [TestMethod]
        public void ExportCategoryMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                CategoryMeasureSource.TargetingType);
        }

        /// <summary>Test exporting an inventory attribute campaign</summary>
        [TestMethod]
        public void ExportInventoryMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                InventoryMeasureSource.TargetingType);
        }

        /// <summary>Test exporting a segment campaign</summary>
        [TestMethod]
        public void ExportSegmentMeasureCampaign()
        {
            this.TestExportingSingleMeasureCampaign(
                SegmentMeasureSource.TargetingType);
        }

        /// <summary>Test exporting a campaign with a lifetime frequency cap</summary>
        [TestMethod]
        public void ExportCampaignWithLifetimeFrequencyCap()
        {
            var lifetimeFrequencyCap = 100;
            this.CreateFrequencyCapCampaignAndAllocation(
                AppNexusFrequencyType.Lifetime,
                lifetimeFrequencyCap);
            this.TestExportingCampaignWithConfiguredAllocations(1);
            this.VerifyLineItemProfileValue("max_lifetime_imps", lifetimeFrequencyCap);
        }

        /// <summary>Test exporting a campaign with a session frequency cap</summary>
        [TestMethod]
        public void ExportCampaignWithSessionFrequencyCap()
        {
            var sessionFrequencyCap = 10;
            this.CreateFrequencyCapCampaignAndAllocation(
                AppNexusFrequencyType.Session,
                sessionFrequencyCap);
            this.TestExportingCampaignWithConfiguredAllocations(1);
            this.VerifyLineItemProfileValue("max_session_imps", sessionFrequencyCap);
        }

        /// <summary>Test exporting a campaign with a day frequency cap</summary>
        [TestMethod]
        public void ExportCampaignWithDayFrequencyCap()
        {
            var dayFrequencyCap = 40;
            this.CreateFrequencyCapCampaignAndAllocation(
                AppNexusFrequencyType.Day,
                dayFrequencyCap);
            this.TestExportingCampaignWithConfiguredAllocations(1);
            this.VerifyLineItemProfileValue("max_day_imps", dayFrequencyCap);
        }

        /// <summary>Test exporting a campaign with a minutes frequency cap</summary>
        [TestMethod]
        public void ExportCampaignWithMinutesFrequencyCap()
        {
            var minuteFrequencyCap = 30;
            this.CreateFrequencyCapCampaignAndAllocation(
                AppNexusFrequencyType.Minutes,
                minuteFrequencyCap);
            this.TestExportingCampaignWithConfiguredAllocations(1);
            this.VerifyLineItemProfileValue("min_minutes_per_imp", minuteFrequencyCap);
        }

        /// <summary>Test exporting a campaign with a site whitelist</summary>
        [TestMethod]
        public void ExportCampaignWithDomainTargets()
        {
            // TODO: Move all measure types to enum defined in AppNexusUtilities?
            // (currently defined as constants in each measure source)
            const string DomainTargetMeasureType = "domains";
            const string DomainMeasureValue = "value";

            var domains = new[] { "nytimes.com", "msnbc.com" };

            // Custom measure map with frequency cap measures
            var domainTargetsMeasureId = 1L;
            var campaignMeasureMap = new Dictionary<long, IDictionary<string, object>>
            {
                {
                    domainTargetsMeasureId,
                    new Dictionary<string, object>
                    {
                        { MeasureValues.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString() },
                        { MeasureValues.Type, DomainTargetMeasureType },
                        { DomainMeasureValue, string.Join(", ", domains) }
                    }
                }
            };
            this.CreateCampaignAndAllocationsWithCustomMeasureMap(campaignMeasureMap);
            this.TestExportingCampaignWithConfiguredAllocations(1);

            var exportedDomainTargets = this.GetLineItemProfileValue("domain_targets");
            Assert.IsNotNull(exportedDomainTargets);
            var exportedDomains = ((object[])exportedDomainTargets)
                .Cast<Dictionary<string, object>>()
                .Select(target => target["domain"]);
            var unexportedDomains = domains.Except(exportedDomains);
            Assert.AreEqual(
                0,
                unexportedDomains.Count(),
                "One or more domains were not exported: {0}",
                domains.Except(exportedDomains));
        }

        /// <summary>
        /// Test exporting a campaign with an include domain list
        /// </summary>
        [TestMethod]
        public void TestCampaignWithIncludeDomainList()
        {
            // Setup a campaign with the expected include domain list and export it
            var expectedIncludeDomainList = new[]
            {
                "campaign1.example.com",
                "campaign2.example.com"
            };

            // Setup a single measure allocation/campaign
            var measure = this.GetRandomMeasureByType(SegmentMeasureSource.TargetingType);
            this.CreateSingleMeasureTestAllocation(measure);
            this.CreateTestCampaignEntity();

            // Add an include domain list to the campaign and export it
            this.testCampaign.SetAppNexusIncludeDomainList(expectedIncludeDomainList);
            this.TestExportingCampaignWithConfiguredAllocations(1);

            // Verify expected include domain list exported correctly
            this.VerifyIncludeDomainListExported(expectedIncludeDomainList);
        }

        /// <summary>
        /// Test exporting a campaign from an advertiser with an include domain list
        /// </summary>
        [TestMethod]
        public void TestCompanyWithIncludeDomainList()
        {
            var expectedIncludeDomainList = new[]
            {
                "company1.example.com",
                "company2.example.com"
            };

            // Setup a single measure allocation/campaign
            var measure = this.GetRandomMeasureByType(SegmentMeasureSource.TargetingType);
            this.CreateSingleMeasureTestAllocation(measure);
            this.CreateTestCampaignEntity();

            // Add an include domain list to the advertiser company and export the campaign
            this.testCompany.SetAppNexusIncludeDomainList(expectedIncludeDomainList);
            this.TestExportingCampaignWithConfiguredAllocations(1);

            // Verify expected include domain list exported correctly
            this.VerifyIncludeDomainListExported(expectedIncludeDomainList);
        }

        /// <summary>
        /// Test exporting a campaign using the default include domain list
        /// </summary>
        /// <remarks>TODO: Enable once the embedded whitelist has been populated</remarks>
        [TestMethod]
        [Ignore]
        public void TestDefaultIncludeDomainList()
        {
            // TODO: Load from AppNexus activity assembly
            var expectedIncludeDomainList = new string[0];

            // Setup a single measure allocation/campaign
            var measure = this.GetRandomMeasureByType(SegmentMeasureSource.TargetingType);
            this.CreateSingleMeasureTestAllocation(measure);
            this.CreateTestCampaignEntity();

            // Export the campaign without setting an include domain list
            this.TestExportingCampaignWithConfiguredAllocations(1);

            // Verify expected include domain list exported correctly
            this.VerifyIncludeDomainListExported(expectedIncludeDomainList);
        }

        #region Test object/state initialization methods
        /// <summary>
        /// Creates a test Company entity with the specified values
        /// </summary>
        /// <param name="companyEntityId">Company Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>The Company company entity</returns>
        private static CompanyEntity CreateTestCompanyEntity(string companyEntityId, string externalName)
        {
            var companyJson =
            @"{{
                ""ExternalEntityId"":""{0}"",
                ""ExternalName"":""{1}""
            }}"
            .FormatInvariant(companyEntityId, externalName);
            return EntityUtilities.EntityJsonSerializer.DeserializeCompanyEntity(new EntityId(companyEntityId), companyJson);
        }

        /// <summary>
        /// Creates a test campaign entity with the specified values
        /// </summary>
        /// <param name="campaignEntityId">The campaign Id</param>
        /// <param name="externalName">The external name</param>
        /// <param name="budget">The budget</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="personaName">The persona name</param>
        /// <returns>Campaign json</returns>
        private static CampaignEntity CreateTestCampaignEntity(string campaignEntityId, string externalName, long budget, DateTime startDate, DateTime endDate, string personaName)
        {
            var campaignJson =
            @"{{
                ""ExternalEntityId"":""{0}"",
                ""ExternalName"":""{1}"",
                ""Properties"":
                {{
                    ""Budget"":{2},
                    ""EndDate"":""{3}"",
                    ""PersonaName"":""{4}"",
                    ""StartDate"":""{5}""
                }}
            }}"
            .FormatInvariant(campaignEntityId, externalName, budget, endDate.ToString("o"), personaName, startDate.ToString("o"));
            return EntityUtilities.EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(), campaignJson);
        }

        /// <summary>
        /// Creates a test creative entity
        /// </summary>
        /// <param name="creativeEntityId">Creative Id</param>
        /// <param name="externalName">Creative external name</param>
        /// <param name="creativeAdTag">Creative third party ad tag</param>
        /// <param name="width">Creative width</param>
        /// <param name="height">Creative height</param>
        /// <returns>Creative entity</returns>
        private static CreativeEntity CreateTestCreativeEntity(string creativeEntityId, string externalName, string creativeAdTag, int width, int height)
        {
            var creativeJson =
            @"{{
                ""ExternalEntityId"":""{0}"",
                ""ExternalName"":""{1}"",
                ""Properties"":
                {{
                    ""Tag"":""{2}"",
                    ""Width"":{3},
                    ""Height"":{4}
                }}
            }}"
            .FormatInvariant(creativeEntityId, externalName, creativeAdTag, width, height);
            return EntityUtilities.EntityJsonSerializer.DeserializeCreativeEntity(new EntityId(), creativeJson);
        }
        
        /// <summary>Creates a random measure set from the provided measures</summary>
        /// <param name="measures">Measures from which to create the MeasureSet</param>
        /// <returns>The measure set</returns>
        private static MeasureSet CreateRandomMeasureSet(long[] measures)
        {
            var count = Math.Min(1 + R.Next(1 + ((measures.Length / 2) % 16)), measures.Length / 2);
            var measureList = new List<long>();
            while (measureList.Count < count)
            {
                var measure = measures.Random();
                if (!measureList.Contains(measure))
                {
                    measureList.Add(measure);
                }
            }

            return new MeasureSet(measureList);
        }

        /// <summary>Get a random measure of the specified type/subtype</summary>
        /// <param name="type">The measure type</param>
        /// <param name="subtype">The measure subtype (optional)</param>
        /// <returns>The measure id</returns>
        private long GetRandomMeasureByType(string type, string subtype = null)
        {
            // Create measure sources and wait for them to load
            var sources = MeasureSourceFactory.CreateMeasureSources(
                DeliveryNetworkDesignation.AppNexus,
                1,
                this.testCompany,
                this.testCampaign,
                this.testCampaignOwner);

            return sources
                .AsParallel()
                .SelectMany(source =>
                    MeasureSourceTestHelpers.LoadMeasures(source)
                    .Where(measure =>
                        (string)measure.Value[MeasureValues.Type] == type &&
                        (subtype == null || (string)measure.Value[MeasureValues.SubType] == subtype)))
                .ToArray()
                .Random()
                .Key;
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

            return Activity.CreateActivity(
                typeof(ExportDynamicAllocationCampaignActivity),
                context,
                this.SubmitActivityRequest)
                as ExportDynamicAllocationCampaignActivity;
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

        /// <summary>Creates a test CompanyEntity and corresponding AppNexus advertiser</summary>
        private void CreateTestAdvertiserCompany()
        {
            this.testCompany = CreateTestCompanyEntity(
                (this.testCompanyEntityId = new EntityId()).ToString(),
                "Test Company - " + Guid.NewGuid().ToString());
            
            this.testAdvertiserId = this.appNexusClient.CreateAdvertiser(
                this.testCompany.ExternalName,
                this.testCompany.ExternalEntityId.ToString());
            AppNexusClientHelper.AddAdvertiserForCleanup(this.testAdvertiserId);

            this.testCompany.SetAppNexusAdvertiserId(this.testAdvertiserId);
        }

        /// <summary>Creates a test CreativeEntity and corresponding AppNexus creative</summary>
        private void CreateTestCreativeEntity()
        {
            this.testCreative = EntityTestHelpers.CreateTestCreativeEntity(
                (this.testCreativeEntityId = new EntityId()),
                Guid.NewGuid().ToString("N"),
                @"<a href=\""http://example.com/\""><img src=\""http://cdn.example.com/ad/12345.gif\""/></a>",
                300,
                250);
            this.testCreative.SetCreativeType(CreativeType.ThirdPartyAd);

            var creativeId = this.appNexusClient.CreateCreative(
                this.testAdvertiserId,
                this.testCreative.ExternalName.ToString(),
                this.testCreative.ExternalEntityId.ToString(),
                7,
                (int)this.testCreative.GetWidth(),
                (int)this.testCreative.GetHeight(),
                this.testCreative.GetThirdPartyAdTag());
            this.testCreative.SetAppNexusCreativeId(creativeId); 
        }

        /// <summary>
        /// Initializes a campaign and allocation using a custom measure map
        /// which has the frequency type and value.
        /// </summary>
        /// <param name="frequencyType">The frequency type</param>
        /// <param name="frequencyValue">The frequency value</param>
        private void CreateFrequencyCapCampaignAndAllocation(
            AppNexusFrequencyType frequencyType,
            int frequencyValue)
        {
            // TODO: Move all measure types to enum defined in AppNexusUtilities?
            // (currently defined as constants in each measure source)
            const string FrequencyCapMeasureType = "frequency";
            const string FrequencyMeasureValue = "value";

            // Custom measure map with frequency cap measures
            var frequencyCapMeasureId = 1L;
            var campaignMeasureMap = new Dictionary<long, IDictionary<string, object>>
            {
                {
                    frequencyCapMeasureId,
                    new Dictionary<string, object>
                    {
                        { MeasureValues.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString() },
                        { MeasureValues.Type, FrequencyCapMeasureType },
                        { MeasureValues.SubType, frequencyType.ToString() },
                        { FrequencyMeasureValue, frequencyValue }
                    }
                }
            };

            this.CreateCampaignAndAllocationsWithCustomMeasureMap(campaignMeasureMap);
        }

        /// <summary>
        /// Initializes a test campaign with custom measures and a test allocation using those measures.
        /// </summary>
        /// <param name="campaignMeasureMap">The campaign measure map</param>
        private void CreateCampaignAndAllocationsWithCustomMeasureMap(
            IDictionary<long, IDictionary<string, object>> campaignMeasureMap)
        {
            var allocationId = Guid.NewGuid().ToString("N");
            this.testInitialAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(new MeasureSet(campaignMeasureMap.Keys), allocationId, 5500, 55, 70, 50, 3200, 4.25m),
                }
                .ToDictionary()
            };

            var initialMeasureSets = this.testInitialAllocations
                .PerNodeResults
                .Keys
                .ToArray();
            var initialAllocationIds = this.testInitialAllocations
                .PerNodeResults
                .Values
                .Select(allocation => allocation.AllocationId)
                .ToArray();

            this.testInitialExportAllocationIds = new[] { allocationId.ToString() };

            // Create test campaign entity and add the measure map
            this.CreateTestCampaignEntity();
            this.testCampaign.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                JsonConvert.SerializeObject(campaignMeasureMap));
        }

        /// <summary>Creates test allocations with a single measure allocation</summary>
        /// <param name="measure">The measure to create an allocation for</param>
        private void CreateSingleMeasureTestAllocation(long measure)
        {
            // Allocations with a single measure allocation for exporting.
            var allocationId = Guid.NewGuid().ToString("N");
            this.testInitialAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(new MeasureSet(new[] { measure }), allocationId, 5500, 55, 70, 50, 3200, 4.25m),
                }
                .ToDictionary()
            };

            var initialMeasureSets = this.testInitialAllocations
                .PerNodeResults
                .Keys
                .ToArray();
            var initialAllocationIds = this.testInitialAllocations
                .PerNodeResults
                .Values
                .Select(allocation => allocation.AllocationId)
                .ToArray();

            // Create the string list of allocation IDs to export containing
            // the one created for the single measure allocation
            this.testInitialExportAllocationIds = new[] { allocationId.ToString() };
        }

        /// <summary>Create test allocations</summary>
        private void CreateTestAllocations()
        {
            // Initial allocations with some arbitrary values for exporting.
            // The last allocation is budget-less and should not be created.
            var measures = this.testMeasureIds.ToArray();
            var initialMeasureSets = Enumerable.Range(0, 20)
                .Select(i => CreateRandomMeasureSet(measures))
                .Distinct().Take(5).ToArray();
            this.testInitialAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(initialMeasureSets[0], Guid.NewGuid().ToString("N"), 5500, 55, 70, 50, 3200, 4.25m),
                    this.CreateTestAllocation(initialMeasureSets[1], Guid.NewGuid().ToString("N"), 1250, 23, 30, 20, 1650, 1.50m),
                    this.CreateTestAllocation(initialMeasureSets[2], Guid.NewGuid().ToString("N"), 1350, 18, 22, 16, 1400, 5.25m),
                    this.CreateTestAllocation(initialMeasureSets[3], Guid.NewGuid().ToString("N"), 1175, 10, 13, 8, 1234, 3.75m),
                    this.CreateTestAllocation(initialMeasureSets[4], Guid.NewGuid().ToString("N"), 0, 0, 0, 0, 600, 3.25m),
                }
                .Distinct(kvp => kvp.Key)
                .ToDictionary()
            };

            // Updated allocations have the same measure sets with value changes.
            // Value changes include no export budget for the first allocation to
            // test deactivation.
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
        /// <param name="measureSet">The measure set</param>
        /// <param name="allocationId">The allocation id</param>
        /// <param name="dailyImpressionCap">The daily impression cap</param>
        /// <param name="dailyMediaBudget">The daily media budget</param>
        /// <param name="dailyTotalBudget">The daily total budget</param>
        /// <param name="exportBudget">The export budget</param>
        /// <param name="lifetimeMediaSpend">The lifetime media spend</param>
        /// <param name="maxBid">The max bid</param>
        /// <returns>The allocation</returns>
        private KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> CreateTestAllocation(
            MeasureSet measureSet,
            string allocationId,
            long dailyImpressionCap,
            decimal dailyMediaBudget,
            decimal dailyTotalBudget,
            decimal exportBudget,
            decimal lifetimeMediaSpend,
            decimal maxBid)
        {
            return new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(
                measureSet,
                new PerNodeBudgetAllocationResult
                {
                    AllocationId = allocationId,
                    PeriodImpressionCap = dailyImpressionCap,
                    PeriodMediaBudget = dailyMediaBudget,
                    PeriodTotalBudget = dailyTotalBudget,
                    ExportBudget = exportBudget,
                    LifetimeMediaSpend = lifetimeMediaSpend,
                    MaxBid = maxBid
                });
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestCampaignEntity()
        {
            this.testCampaignOwner = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(), Guid.NewGuid().ToString("N"), "nobody@rc.dev");
            this.testCampaignOwner.SetUserType(UserType.StandAlone);

            this.testCampaign = CreateTestCampaignEntity(
                (this.testCampaignEntityId = new EntityId()).ToString(),
                "Test Campaign - " + Guid.NewGuid().ToString(),
                10000,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(7, 0, 0, 0),
                "Test Persona - " + Guid.NewGuid().ToString());
            this.testCampaign.SetOwnerId(this.testCampaignOwner.UserId);

            var testInitialAllocationsJson = JsonConvert.SerializeObject(this.testInitialAllocations);
            this.testAllocationsBlob = BlobEntity.BuildBlobEntity<string>(
                this.testAllocationsBlobEntityId = new EntityId(),
                testInitialAllocationsJson);

            this.testCampaign.AssociateEntities(
                DynamicAllocationEntityProperties.AllocationSetActive,
                "description",
                new HashSet<IEntity>(new[] { this.testAllocationsBlob }),
                AssociationType.Relationship,
                true);

            this.testCampaign.AssociateEntities(
                "Creative",
                "description",
                new HashSet<IEntity>(new[] { this.testCreative }),
                AssociationType.Relationship,
                true);
        }

        /// <summary>Update the test allocation blob entity</summary>
        private void UpdateTestAllocationsBlob()
        {
            var testUpdatedAllocationsJson = JsonConvert.SerializeObject(this.testUpdatedAllocations);
            this.testAllocationsBlob = BlobEntity.BuildBlobEntity<string>(
                this.testAllocationsBlobEntityId,
                testUpdatedAllocationsJson);
        }

        /// <summary>Initializes the mock IEntityRepository</summary>
        private void InitializeEntityRepositoryMock()
        {
            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();

            this.mockRepository.Stub(f =>
                f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.testCompanyEntityId)))
                .Return(this.testCompany);

            this.mockRepository.Stub(f =>
                f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.testCampaignEntityId)))
                .Return(this.testCampaign);

            this.mockRepository.Stub(f =>
                f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.testCreativeEntityId)))
                .Return(this.testCreative);

            this.mockRepository.Stub(f =>
                f.GetUser(
                    Arg<RequestContext>.Is.Anything,
                    Arg<string>.Is.Anything))
                .Return(this.testCampaignOwner);

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
                f.GetEntitiesById(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId[]>.Is.Anything))
                .Return(new HashSet<IEntity>(new[]
                {
                    this.testCreative
                }));
        }

        /// <summary>Initializes the mock measure source provider</summary>
        private void InitializeMeasureSourceProviders()
        {
            var mockMeasureSourceProvider = MockRepository.GenerateMock<IMeasureSourceProvider>();
            mockMeasureSourceProvider.Stub(f => f.Version)
                .Return(0);
            mockMeasureSourceProvider.Stub(f => f.DeliveryNetwork)
                .Return(DeliveryNetworkDesignation.AppNexus);
            mockMeasureSourceProvider.Stub(f => f.GetMeasureSources(Arg<object[]>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var args = (object[])call.Arguments[0];
                    var companyEntity = args.OfType<CompanyEntity>().Single();
                    var campaignEntity = args.OfType<CampaignEntity>().Single();
                    var sources = new[]
                    {
                        companyEntity.GetMeasureSource(),
                        campaignEntity.GetMeasureSource()
                    };
                    call.ReturnValue = sources.Where(source => source != null);
                });

            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
            {
                new AppNexusLegacyMeasureSourceProvider(),
                new AppNexusMeasureSourceProvider(),
            });
        }
        #endregion

        #region Reusable test/verification methods

        /// <summary>Test exporting a single measure of the specified type/subtype</summary>
        /// <param name="measureType">The neasure type</param>
        /// <param name="measureSubType">The measure subtype</param>
        private void TestExportingSingleMeasureCampaign(string measureType, string measureSubType = null)
        {
            // Get a random measure for the test
            var measure = this.GetRandomMeasureByType(measureType, measureSubType);

            // Initialize test allocations and campaign
            this.CreateSingleMeasureTestAllocation(measure);
            this.CreateTestCampaignEntity();

            // Test exporting campaign with the single measure allocation
            this.TestExportingCampaignWithConfiguredAllocations(1);
        }

        /// <summary>
        /// Test exporting a campaign using previousy configured allocations
        /// </summary>
        /// <remarks>
        /// Requires this.testInitialAllocations and this.testInitialExportAllocationIds
        /// to already be configured.
        /// </remarks>
        /// <param name="exporterVersion">Version of the exporter to use</param>
        /// <returns>Activity result for any additional verification</returns>
        private ActivityResult TestExportingCampaignWithConfiguredAllocations(int exporterVersion)
        {
            this.InitializeEntityRepositoryMock();

            // Set the campaign to use the specified exporter
            this.testCampaign.SetExporterVersion(exporterVersion);

            // Preload the measure sources
            MeasureSourceTestHelpers.PreloadMeasureSources(
                this.testCompany,
                this.testCampaign,
                this.testCampaignOwner);

            // Prepare the activity request
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.ExportDACampaign,
                Values =
                {
                    { EntityActivityValues.AuthUserId, "6Az3F8+9BA274Cf0/8gE/q98w13oB6u3==" },
                    { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId.ToString() },
                    { AppNexusActivityValues.CampaignStartDate, DateTime.UtcNow.ToString("o") },
                    { DynamicAllocationActivityValues.ExportAllocationsEntityId, this.testAllocationsBlobEntityId }
                }
            };

            // Export campaign and check activity result
            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);
            Assert.IsTrue(result.Values.ContainsKey(AppNexusActivityValues.LineItemId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values[AppNexusActivityValues.LineItemId]));
            Assert.IsFalse(
                this.testLogger.HasEntriesLoggedWithLevel(LogLevels.Error),
                "One or more errors were logged during export:\n{0}".FormatInvariant(string.Join("\n", this.testLogger.ErrorEntries)));

            return result;
        }

        /// <summary>Gets the line-item profile for the exported campaign</summary>
        /// <returns>The line-item profile</returns>
        private IDictionary<string, object> GetLineItemProfile()
        {
            var lineItemId = this.testCampaign.GetAppNexusLineItemId();
            Assert.IsTrue(lineItemId.HasValue);
            var lineItem = this.appNexusClient.GetLineItemById(this.testAdvertiserId, lineItemId.Value);
            Assert.IsTrue(lineItem.ContainsKey("profile_id"));
            var profileId = (int)lineItem["profile_id"];
            return this.appNexusClient.GetProfileById(this.testAdvertiserId, profileId);
        }

        /// <summary>Verifies the test campaign's line-item profile has a value</summary>
        /// <param name="valueName">The value name</param>
        /// <returns>True if the profile has the value; otherwise, false.</returns>
        private bool LineItemProfileHasValue(string valueName)
        {
            return this.GetLineItemProfile().ContainsKey(valueName);
        }

        /// <summary>Gets the value from the line-item profile</summary>
        /// <param name="valueName">The value name</param>
        /// <returns>The value</returns>
        private object GetLineItemProfileValue(string valueName)
        {
            return this.GetLineItemProfile()[valueName];
        }

        /// <summary>Verify the test campaign's line-item profile has the expected value</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="valueName">The value name</param>
        /// <param name="expectedValue">The expected value</param>
        private void VerifyLineItemProfileValue<TValue>(string valueName, TValue expectedValue)
        {
            var profile = this.GetLineItemProfile();
            Assert.IsTrue(profile.ContainsKey(valueName));
            Assert.AreEqual(expectedValue, profile[valueName]);
        }

        /// <summary>Verify the AppNexus campaign contains the expected value</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="profileId">AppNexus profile id</param>
        /// <param name="valueName">Profile value name</param>
        /// <param name="expectedValue">Expected value</param>
        private void VerifyProfileValue<TValue>(int profileId, string valueName, TValue expectedValue)
        {
            var profile = this.appNexusClient.GetProfileById(this.testAdvertiserId, profileId);
            Assert.IsTrue(profile.ContainsKey(valueName));
            Assert.AreEqual(expectedValue, profile[valueName]);
        }

        /// <summary>Verify the expected include domain list was exported correctly</summary>
        /// <param name="expectedIncludeDomainList">Expected include domain list</param>
        private void VerifyIncludeDomainListExported(string[] expectedIncludeDomainList)
        {
            var includeDomainListId = this.testCampaign.GetAppNexusIncludeDomainListId();
            Assert.IsNotNull(includeDomainListId);
            AppNexusClientHelper.AddDomainListForCleanup((int)includeDomainListId);
            var includeDomainList = this.appNexusClient.TryGetObject("domain-list", "domain-list?id={0}", includeDomainListId);
            Assert.IsNotNull(includeDomainList);
            Assert.IsTrue(includeDomainList.ContainsKey("domains"));
            var domains = includeDomainList["domains"] as object[];
            Assert.IsNotNull(domains);
            Assert.IsTrue(expectedIncludeDomainList.All(s => domains.Contains(s)));
        }

        #endregion
    }
}
