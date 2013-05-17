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
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using DynamicAllocationTestUtilities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Utilities.Serialization;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>
    /// Tests for the AppNexus measure sources
    /// </summary>
    [TestClass]
    public class AppNexusMeasureSourcesFixture
    {
        /// <summary>Data costs for the mock segments</summary>
        private const string SegmentDataCosts =
@"APNXId,displayName,dataProvider,DataCost,MinCPM,PercentOfMedia,subtype
102,Custom Segment Name,exelate,0.25,null,null,Targeting
3103,,Content,Lotame,0.05,0.15,null";

        /// <summary>Test logger</summary>
        private TestLogger testLogger;

        /// <summary>Mock AppNexus API client</summary>
        private IAppNexusApiClient mockApiClient;

        /// <summary>Test company entity</summary>
        private CompanyEntity companyEntity;

        /// <summary>Test campaign entity</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Test campaign owner</summary>
        private UserEntity campaignOwner;

        /// <summary>Per-test case initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["AppNexus.Username"] = "APNXUser";
            ConfigurationManager.AppSettings["AppNexus.DataProviders"] = "Lotame|eXelate|Peer39|BlueKai";
            ConfigurationManager.AppSettings["AppNexus.App.UserId"] = "6543";
            ConfigurationManager.AppSettings["AppNexus.SegmentCacheExpiry"] = "12:00:00";
            ConfigurationManager.AppSettings["AppNexus.SegmentDataCostsRequired"] = "false";

            // Initialize the test logger
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });

            // Create the API client mock
            this.mockApiClient = MockRepository.GenerateMock<IAppNexusApiClient>();

            // Initialize the delivery network client factory with a mock that returns the mocked API client
            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType).Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything)).Return(this.mockApiClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });

            // Initialize simulated storage
            SimulatedPersistentDictionaryFactory.Initialize();

            // Clear the persisted cache start times
            CachedMeasureSource.CacheUpdateStartTimes = null;
            
            // Clear local cache
            CachedMeasureSource.LocalMeasureCache = null;
            
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
            this.campaignOwner =
                EntityTestHelpers.CreateTestUserEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"),
                "nobody@rc.dev");
        }

        /// <summary>
        /// Test creating the AppNexusLegacyMeasureSourceProvider
        /// </summary>
        [TestMethod]
        public void CreateAppNexusLegacyMeasureSourceProviderWithoutEntityMeasures()
        {
            // Create provider instance
            var provider = new AppNexusLegacyMeasureSourceProvider();
            Assert.IsNotNull(provider);
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, provider.DeliveryNetwork);
            
            // If created with entities that don't have any custom measures,
            // then only the embedded LegacyMeasureMap.js should be returned.
            var measureSources = provider.GetMeasureSources(this.companyEntity, this.campaignEntity);
            Assert.AreEqual(1, measureSources.Count());
            Assert.AreEqual(1, measureSources.OfType<EmbeddedJsonMeasureSource>().Count());

            // Check the number and range of measures
            var embeddedMeasureSource = measureSources.OfType<EmbeddedJsonMeasureSource>().Single();
            Assert.AreEqual(102000000000500000, embeddedMeasureSource.BaseMeasureId);
            Assert.AreEqual(111000000002042511, embeddedMeasureSource.MaxMeasureId);
            Assert.AreEqual(2332, embeddedMeasureSource.Measures.Count);
        }

        /// <summary>
        /// Test creating the AppNexusMeasureSourceProvider
        /// Only verifies expected measure sources are provided.
        /// Actual measures of the sources are tested separately.
        /// </summary>
        [TestMethod]
        public void CreateAppNexusMeasureSourceProviderWithoutEntityMeasures()
        {
            // Create provider instance
            var provider = new AppNexusMeasureSourceProvider();
            Assert.IsNotNull(provider);
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, provider.DeliveryNetwork);

            // If created with entities that don't have any custom measures,
            // then only the sourced measures should be returned.
            var measureSources = provider.GetMeasureSources(
                this.companyEntity,
                this.campaignEntity,
                this.campaignOwner)
                .ToArray();
            Assert.AreEqual(9, measureSources.Length);
            Assert.IsNotNull(measureSources.OfType<EmbeddedJsonMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<CategoryMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<AgeRangeMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<SegmentMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<InventoryMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<DomainListMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<RegionMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<CityMeasureSource>().SingleOrDefault());
            Assert.IsNotNull(measureSources.OfType<MetroCodeMeasureSource>().SingleOrDefault());
        }

        /// <summary>
        /// Test loading metro codes from the embedded CSV downloaded from google
        /// </summary>
        /// <remarks>
        /// AppNexus uses MaxMind which in turn refers to Google's metrocode table
        /// </remarks>
        [TestMethod]
        public void LoadMetroCodesCsvMeasureSource()
        {
            var source = new MetroCodeMeasureSource(new IEntity[0]);

            // TODO: More meaningful validation?
            Assert.IsTrue(source.Measures.Count > 0);
        }

        /// <summary>
        /// Test loading ISO 3166-2 regions from the embedded CSV copied from MaxMind
        /// </summary>
        /// <remarks>
        /// Downloaded from MaxMind's website: <see href="http://www.maxmind.com/app/iso3166_2"/>
        /// </remarks>
        [TestMethod]
        public void LoadRegionsCsvMeasureSource()
        {
            var source = new RegionMeasureSource(new IEntity[0]);

            // 50 states + 3 territories
            var unitedStatesRegions =
                source.Measures
                .Where(region =>
                    ((string)region.Value[AppNexusMeasureValues.AppNexusId]).StartsWith("US"));
            Assert.AreEqual(53, unitedStatesRegions.Count());
            
            // 10 provinces + 3 territories
            var canadianRegions =
                source.Measures
                .Where(region =>
                    ((string)region.Value[AppNexusMeasureValues.AppNexusId]).StartsWith("CA"));
            Assert.AreEqual(13, canadianRegions.Count());

            // Verify display name format
            Assert.IsNotNull(
                unitedStatesRegions
                .Where(region =>
                    ((string)region.Value[MeasureValues.DisplayName]) ==
                    "AppNexus:Geotargeting:Regions:United States:New York")
                .SingleOrDefault());
        }

        /// <summary>
        /// Test loading age ranges from the embedded CSV
        /// </summary>
        [TestMethod]
        public void LoadAgeRangeMeasureSource()
        {
            var source = new AgeRangeMeasureSource(new IEntity[0]);
            var measures = source.Measures;

            // Check for the allow/exclude unknown age range measures
            var unknownAges = measures
                .Where(measure =>
                    ((string)measure.Value[AppNexusMeasureValues.AppNexusId])
                    .Contains("Unknown"));
            Assert.AreEqual(2, unknownAges.Count());
            Assert.IsNotNull(
                unknownAges
                .SingleOrDefault(
                    measure =>
                    ((string)measure.Value[AppNexusMeasureValues.AppNexusId])
                    .Contains("Allow")));
            Assert.IsNotNull(
                unknownAges
                .SingleOrDefault(
                    measure =>
                    ((string)measure.Value[AppNexusMeasureValues.AppNexusId])
                    .Contains("Exclude")));

            // Check that the known age range measures' AppNexusIds are well-formed
            var knownAges = measures.Except(unknownAges);
            Assert.IsTrue(
                knownAges
                .Select(measure =>
                    ((string)measure.Value[AppNexusMeasureValues.AppNexusId]))
                .All(appNexusId =>
                    !string.IsNullOrWhiteSpace(appNexusId) &&
                    appNexusId.Split('-').Count() == 2 &&
                    appNexusId.Split('-').All(age =>
                    {
                        int value;
                        return int.TryParse(age, out value);
                    })));
        }

        /// <summary>
        /// Test loading cities from the AppNexus API client
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/City+Service"/>
        [TestMethod]
        public void LoadCitiesMeasureSource()
        {
            this.mockApiClient.Stub(f =>
                f.GetCities(Arg<string>.Is.Anything))
                .Return(MockData.Cities);

            var source = new CityMeasureSource(new IEntity[0]);
            var cities = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(cities);
            Assert.AreEqual(MockData.Cities.Length, cities.Count);
            foreach (var city in MockData.Cities)
            {
                Assert.IsNotNull(
                    cities.Values
                    .SingleOrDefault(c =>
                        (long)c[AppNexusMeasureValues.AppNexusId] == (int)city["id"]));
            }

            Assert.IsNotNull(
                cities.Values
                .Where(city =>
                    ((string)city[MeasureValues.DisplayName]) ==
                    "AppNexus:Geotargeting:Cities:Pennsylvania:Aaronsburg")
                .SingleOrDefault());
        }

        /// <summary>
        /// Test loading categories from the AppNexus API client
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Content+SubType+Service"/>
        [TestMethod]
        public void LoadCategoriesMeasureSource()
        {
            this.mockApiClient.Stub(f =>
                f.GetContentCategories())
                .Return(MockData.Categories);

            var source = new CategoryMeasureSource(new IEntity[0]);
            var categories = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(categories);
            Assert.AreEqual(MockData.Categories.Length * 2, categories.Count);
            Assert.IsNotNull(
                categories.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]) ==
                    "AppNexus:Categories:Exclude:Animals")
                .SingleOrDefault());
            Assert.IsNotNull(
                categories.Values
                .Where(category =>
                    ((string)category[MeasureValues.DisplayName]) ==
                    "AppNexus:Categories:Include:Travel")
                .SingleOrDefault());
        }

        /// <summary>
        /// Test loading inventory sources from the AppNexus API client
        /// </summary>
        /// <seealso href="https://wiki.appnexus.com/display/api/Inventory+Source+Service"/>
        [TestMethod]
        public void LoadInventoryMeasureSource()
        {
            this.mockApiClient.Stub(f =>
                f.GetInventoryAttributes())
                .Return(MockData.InventoryAttributes);

            var source = new InventoryMeasureSource(new IEntity[0]);
            var inventorySources = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(inventorySources);
            Assert.AreEqual(MockData.InventoryAttributes.Length, inventorySources.Count);
            foreach (var inventorySource in MockData.InventoryAttributes)
            {
                Assert.IsNotNull(
                    inventorySources.Values
                    .FirstOrDefault(i =>
                        (long)i[AppNexusMeasureValues.AppNexusId] == 
                        (int)inventorySource["id"]));
            }

            Assert.IsNotNull(
                inventorySources.Values
                .Where(measure =>
                    ((string)measure[MeasureValues.DisplayName]) ==
                    "AppNexus:Inventory Attributes:Political")
                .SingleOrDefault());
        }

        /// <summary>
        /// Test loading segment sources from the AppNexus API client
        /// </summary>
        [TestMethod]
        public void LoadSegmentsMeasureSource()
        {
            this.mockApiClient.Stub(f =>
                f.GetMemberSegments())
                .Return(MockData.MemberSegments);

            var source = new SegmentMeasureSource(null, null);
            source.DataCostStore[source.SegmentDataCostsCsvName] = SegmentDataCosts;

            var segments = MeasureSourceTestHelpers.LoadMeasures(source);
            Assert.IsNotNull(segments);
            Assert.AreEqual(3, segments.Count);
            Assert.IsTrue(segments.Any(segment => segment.Value[MeasureValues.DisplayName] as string == "AppNexus:Segments:exelate:Custom Segment Name"));
            Assert.IsTrue(segments.Any(segment => segment.Value[MeasureValues.SubType] as string == "Targeting"));
        }

        /// <summary>
        /// Test creating the segment data cost CSV template
        /// </summary>
        [TestMethod]
        public void CreateSegmentDataCostCsvTemplate()
        {
            var expectedColumns = new[]
                {
                    AppNexusMeasureValues.AppNexusId,
                    MeasureValues.DisplayName,
                    MeasureValues.DataProvider,
                    MeasureValues.DataCost,
                    MeasureValues.MinCostPerMille,
                    MeasureValues.PercentOfMedia,
                    SegmentMeasureSource.SegmentValues.MemberId,
                    SegmentMeasureSource.SegmentValues.Provider,
                    SegmentMeasureSource.SegmentValues.Code,
                    MeasureValues.SubType,
                };

            this.mockApiClient.Stub(f =>
                f.GetMemberSegments())
                .Return(MockData.MemberSegments);
            var source = new SegmentMeasureSource(null, null);
            source.DataCostStore[source.SegmentDataCostsCsvName] = SegmentDataCosts;
            MeasureSourceTestHelpers.LoadMeasures(source);

            var dataCostCsv = source.CreateSegmentDataCostCsvTemplate(false);
            Assert.IsNotNull(dataCostCsv);
            
            var rows = CsvParser.Parse(dataCostCsv);
            Assert.AreEqual(3, rows.Count());
            var columns = rows.First().Keys;
            var includedColumns = expectedColumns.Intersect(columns);
            Assert.AreEqual(expectedColumns.Count(), includedColumns.Count(), "Missing one or more expected columns");
        }

        /// <summary>Test that none of the sources' measure id ranges overlap</summary>
        [TestMethod]
        public void NoMeasureIdOverlap()
        {
            var provider = new AppNexusMeasureSourceProvider();
            var sources = provider.GetMeasureSources(
                this.companyEntity,
                this.campaignEntity,
                this.campaignOwner);
            var ranges = sources
                .ToDictionary(
                    source => source.SourceId,
                    source =>
                        new Tuple<long, long>(
                            source.BaseMeasureId,
                            source.MaxMeasureId));

            // Verify none of the ranges overlap
            foreach (var rangeA in ranges)
            {
                foreach (var rangeB in ranges.Where(kvp => kvp.Key != rangeA.Key))
                {
                    // Test if rangeA's BaseMeasureId overlaps rangeB
                    Assert.IsFalse(
                        rangeB.Value.Item1 <= rangeA.Value.Item1 && rangeA.Value.Item1 <= rangeB.Value.Item2,
                        "BaseMeasureId of {0} overlaps {1} ({2} <= {3} <= {4})",
                        rangeA.Key,
                        rangeB.Key,
                        rangeB.Value.Item1,
                        rangeA.Value.Item1,
                        rangeB.Value.Item2);

                    // Test if rangeA's MaxMeasureId overlaps rangeB
                    Assert.IsFalse(
                        rangeB.Value.Item1 <= rangeA.Value.Item2 && rangeA.Value.Item2 <= rangeB.Value.Item2,
                        "MaxMeasureId of {0} overlaps {1} ({2} <= {3} <= {4})",
                        rangeA.Key,
                        rangeB.Key,
                        rangeB.Value.Item1,
                        rangeA.Value.Item2,
                        rangeB.Value.Item2);
                }
            }
        }

        /// <summary>Mock AppNexus API Client Data</summary>
        private static class MockData
        {
            /// <summary>Mock IAppNexusApiClient.GetCities response</summary>
            public static readonly IDictionary<string, object>[] Cities = new[]
                {
                    new Dictionary<string, object>
                    {
                        { "id", 201052 },
                        { "country", "US" },
                        { "country_name", "United States" },
                        { "region", "PA" },
                        { "region_name", "Pennsylvania" },
                        { "city", "Aaronsburg" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 183833 },
                        { "country", "US" },
                        { "country_name", "United States" },
                        { "region", "GA" },
                        { "region_name", "Georgia" },
                        { "city", "Abbeville" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 193712 },
                        { "country", "US" },
                        { "country_name", "United States" },
                        { "region", "MS" },
                        { "region_name", "Mississippi" },
                        { "city", "Abbeville" },
                    },
                };

            /// <summary>Mock IAppNexusApiClient.GetCategories response</summary>
            public static readonly IDictionary<string, object>[] Categories = new[]
                {
                    new Dictionary<string, object>
                    {
                        { "id", 3035 },
                        { "name", "Animals" },
                        { "description", null },
                        { "is_system", "false" },
                        { "reselling_exposure", "private" },
                        { "reselling_exposed_on", "0000-00-00 00:00:00" },
                        { "last_activity", "2010-05-12 22:46:42" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 3036 },
                        { "name", "Arts & Humanities" },
                        { "description", null },
                        { "is_system", "false" },
                        { "reselling_exposure", "private" },
                        { "reselling_exposed_on", "0000-00-00 00:00:00" },
                        { "last_activity", "2010-05-12 22:46:42" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 3062 },
                        { "name", "Travel" },
                        { "description", null },
                        { "is_system", "false" },
                        { "reselling_exposure", "private" },
                        { "reselling_exposed_on", "0000-00-00 00:00:00" },
                        { "last_activity", "2010-05-12 22:46:42" },
                    },
                };

            /// <summary>Mock IAppNexusApiClient.GetInventoryAttributes response</summary>
            public static readonly IDictionary<string, object>[] InventoryAttributes = new[]
                {
                    new Dictionary<string, object>
                    {
                        { "id", 2 },
                        { "last_activity", "2012-06-29 00:00:03" },
                        { "name", "Political" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 4 },
                        { "last_activity", "2012-06-29 00:00:03" },
                        { "name", "Social media" },
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 8 },
                        { "last_activity", "2012-06-29 00:00:03" },
                        { "name", "Forums (moderated)" },
                    },
                };

            /// <summary>Mock IAppNexusApiClient.GetMemberSegments response</summary>
            public static readonly IDictionary<string, object>[] MemberSegments = new[]
                {
                    new Dictionary<string, object>
                    {
                        { "id", 11836 },
                        { "code", null },
                        { "state", "active" },
                        { "short_name", "Age: 18-24 (Exelate)" },
                        { "description", null },
                        { "member_id", 185 },
                        { "category", null },
                        { "price", "0" },
                        { "expire_minutes", null },
                        { "enable_rm_piggyback", true },
                        { "max_usersync_pixels", 0 },
                        { "last_modified", "2010-03-10 23:23:48" },
                        { "provider", null },
                        { "parent_segment_id", null },
                        { "advertiser_id", 51 },
                        { "piggyback_pixels", null }
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 102 },
                        { "code", "29" },
                        { "state", "active" },
                        { "short_name", "Shopping - Fashion (Exelate)" },
                        { "description", null },
                        { "member_id", 185 },
                        { "category", null },
                        { "price", "0" },
                        { "expire_minutes", null },
                        { "enable_rm_piggyback", true },
                        { "max_usersync_pixels", 0 },
                        { "last_modified", "2010-03-10 23:23:48" },
                        { "provider", "exelate" },
                        { "parent_segment_id", null },
                        { "advertiser_id", 51 },
                        { "piggyback_pixels", null }
                    },
                    new Dictionary<string, object>
                    {
                        { "id", 3103 },
                        { "code", "bk_2222" },
                        { "state", "active" },
                        { "short_name", "Shopping - Fashion (Peer39)" },
                        { "description", null },
                        { "member_id", 185 },
                        { "category", "BlueKai" },
                        { "price", 0 },
                        { "expire_minutes", null },
                        { "enable_rm_piggyback", true },
                        { "max_usersync_pixels", 0 },
                        { "last_modified", "2010-03-10 23:23:48" },
                        { "provider", null },
                        { "parent_segment_id", null },
                        { "advertiser_id", 51 },
                        { "piggyback_pixels", null }
                    },
                };
        }
    }
}
