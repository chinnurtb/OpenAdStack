//-----------------------------------------------------------------------
// <copyright file="AppNexusMeasureSourcesFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AppNexusActivities.Measures;
using AppNexusClient;
using AppNexusTestUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using DynamicAllocationTestUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Serialization;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesE2ETests
{
    /// <summary>
    /// Integration tests for the AppNexus measure sources
    /// </summary>
    [TestClass]
    public class AppNexusMeasureSourcesFixture
    {
        /// <summary>Test logger</summary>
        private ILogger testLogger;

        /// <summary>Test company entity</summary>
        private CompanyEntity companyEntity;

        /// <summary>Test campaign entity</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Per-test case initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["AppNexus.DataProviders"] = "exelate|Lotame|Peer39|BlueKai";
            ConfigurationManager.AppSettings["AppNexus.SegmentCacheExpiry"] = "12:00:00";
            ConfigurationManager.AppSettings["AppNexus.SegmentDataCostsRequired"] = "false";

            // Initialize logging
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });

            // Initialize simulated storage
            SimulatedPersistentDictionaryFactory.Initialize();

            // Clear the persisted cache start times
            CachedMeasureSource.CacheUpdateStartTimes = null;

            // Clear local cache
            CachedMeasureSource.LocalMeasureCache = null;
            
            // Initialize delivery network client factory
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();

            // Create test entities
            this.companyEntity =
                EntityTestHelpers.CreateTestCompanyEntity(
                    new EntityId().ToString(),
                    "Test Company");
            this.campaignEntity =
                EntityTestHelpers.CreateTestCampaignEntity(
                    new EntityId().ToString(),
                    "Test Campaign",
                    12345,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddDays(20),
                    "???");
        }

        /// <summary>Cleanup any AppNexus objects created by the test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            AppNexusClientHelper.Cleanup();
        }

        /// <summary>
        /// Test downloading cities from the AppNexus City Service
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/City+Service"/>
        [TestMethod]
        public void DownloadCitiesMeasureSource()
        {
            var source = new CityMeasureSource(new IEntity[0]);
            var cities = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(cities);
            Assert.IsNotNull(
                cities.Values
                .Where(city =>
                    ((string)city[MeasureValues.DisplayName]) ==
                    "AppNexus:Geotargeting:Cities:Pennsylvania:Aaronsburg")
                .SingleOrDefault());
        }

        /// <summary>
        /// Test downloading categories from the AppNexus Content Category Service
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Content+Category+Service"/>
        [TestMethod]
        public void DownloadCategoriesMeasureSource()
        {
            var source = new CategoryMeasureSource(new IEntity[0]);
            var categories = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(categories);
            Assert.IsNotNull(
                categories.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]) ==
                    "AppNexus:Categories:Exclude:Health")
                .FirstOrDefault());
            Assert.IsNotNull(
                categories.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]) ==
                    "AppNexus:Categories:Include:Industries")
                .FirstOrDefault());
        }

        /// <summary>
        /// Test downloading inventory sources from the AppNexus Inventory Source Service
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Inventory+Source+Service"/>
        [TestMethod]
        public void DownloadInventorySourcesMeasureSource()
        {
            var source = new InventoryMeasureSource(new IEntity[0]);
            var inventorySources = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(inventorySources);
            Assert.IsNotNull(
                inventorySources.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]) ==
                    "AppNexus:Inventory Attributes:File sharing")
                .FirstOrDefault());
        }

        /// <summary>
        /// Test downloading domain lists from the AppNexus Domain List Service
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Domain+List+Service"/>
        [TestMethod]
        public void DownloadDomainListsMeasureSource()
        {
            // TODO: Use client to create domain lists for deterministic validation
            var source = new DomainListMeasureSource(new IEntity[0]);
            var domainLists = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(domainLists);
            Assert.IsNotNull(
                domainLists.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]).StartsWith("AppNexus:Domain Lists:"))
                .FirstOrDefault());
        }

        /// <summary>
        /// Test downloading inventory sources from the AppNexus Segments Source Service
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Segments+Source+Service"/>
        [TestMethod]
        public void DownloadSegmentsMeasureSource()
        {
            ConfigurationManager.AppSettings["AppNexus.SegmentDataCostsRequired"] = "true";

            var source = new SegmentMeasureSource(null, null);
            var segments = MeasureSourceTestHelpers.LoadMeasures(source, 300);
            Assert.IsNotNull(segments);

            // Verify expected segment is present
            Assert.IsNotNull(
                segments.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]).ToLowerInvariant() ==
                    "appnexus:segments:exelate:shopping:fashion")
                .FirstOrDefault());

            // Verify segments lacking data cost are not present
            Assert.AreEqual(0, segments.Values.Count(s => !SegmentMeasureSource.HasDataCosts(s)));
        }

        /// <summary>Test creating the segment data cost CSV template</summary>
        [TestMethod]
        public void CreateSegmentDataCostCsvTemplate()
        {
            IDictionary<string, object>[] segments = null;
            using (var client = new AppNexusClient.AppNexusApiClient())
            {
                segments = client.GetMemberSegments()
                    .Where(s => !string.IsNullOrWhiteSpace((string)s["short_name"]))
                    .ToArray();
            }

            var source = new SegmentMeasureSource(null, null);
            MeasureSourceTestHelpers.LoadMeasures(source, 300);
            var dataCostCsv = source.CreateSegmentDataCostCsvTemplate(true);
            Assert.IsNotNull(dataCostCsv);
            var rows = CsvParser.Parse(dataCostCsv);

            var measureApnxIds = rows.Select(row => Convert.ToInt32(row["APNXId"]));
            var segmentApnxIds = segments.Select(segment => (int)segment["id"]);
            var missingSegments = segmentApnxIds
                .Except(measureApnxIds)
                .Select(id =>
                    segments.FirstOrDefault(segment => (int)segment["id"] == id))
                .ToArray();

            Assert.AreEqual(0, missingSegments.Length);
        }
    }
}
