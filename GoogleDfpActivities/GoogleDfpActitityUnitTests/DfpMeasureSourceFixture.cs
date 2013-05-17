//-----------------------------------------------------------------------
// <copyright file="DfpMeasureSourceFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using GoogleDfpActivities.Measures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace GoogleDfpActivitiesUnitTests
{
    /// <summary>Tests for the Google DFP MeasureSource classes</summary>
    [TestClass]
    public class DfpMeasureSourceFixture
    {
        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
        }

        /// <summary>
        /// Test creating DFP measure sources using the dfp measure provider
        /// with entities that do not have custom measure maps or configs.
        /// </summary>
        /// <remarks>
        /// The primary purpose of this test is to ensure all measure sources
        /// have a constructor that take the expected arguments.
        /// </remarks>
        [TestMethod]
        public void GetMeasureSourcesFromProviderWithoutEntitySources()
        {
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"));
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"),
                123456,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                "???");

            // Create the measure source provider and get sources using
            // test entities that do not have their own measure maps.
            var dfpMeasureSourceProvider = new DfpMeasureSourceProvider();
            var measureSourcesEnumerable =
                dfpMeasureSourceProvider.GetMeasureSources(
                    companyEntity,
                    campaignEntity);

            // Force Linq evaluation using ToArray
            var measureSources = measureSourcesEnumerable.ToArray();

            // Verify there are measure sources and that they all derive from DfpMeasureSourceBase
            Assert.IsTrue(measureSources.Count() > 0);
            Assert.IsTrue(measureSources.All(source =>
                typeof(DfpMeasureSourceBase).IsAssignableFrom(source.GetType())));
        }

        /// <summary>Test creating DFP measure sources using the provider</summary>
        [TestMethod]
        public void GetMeasureSourcesFromProviderWithEntitySources()
        {
            // Setup entities with custom measure maps
            var companyName = Guid.NewGuid().ToString("N");
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId().ToString(),
                companyName);
            var companyMeasureMapJson = JsonConvert.SerializeObject(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1, new Dictionary<string, object> { { "displayName", companyName } } }
                });
            companyEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                new PropertyValue(PropertyType.String, companyMeasureMapJson));

            var campaignName = Guid.NewGuid().ToString("N");
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                campaignName,
                123456,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                "???");
            var campaignMeasureMapJson = JsonConvert.SerializeObject(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1, new Dictionary<string, object> { { "displayName", campaignName } } }
                });
            companyEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                new PropertyValue(PropertyType.String, companyMeasureMapJson));

            // Create the measure source provider and get sources using
            // test entities that have their own measure maps.
            var dfpMeasureSourceProvider = new DfpMeasureSourceProvider();
            var measureSourcesEnumerable =
                dfpMeasureSourceProvider.GetMeasureSources(
                    companyEntity,
                    campaignEntity);

            // Force Linq evaluation using ToArray
            var measureSources = measureSourcesEnumerable.ToArray();

            // Verify there are measure sources and that they all derive from DfpMeasureSourceBase
            Assert.IsTrue(measureSources.Count() > 0);
            Assert.IsTrue(measureSources.All(source =>
                typeof(DfpMeasureSourceBase).IsAssignableFrom(source.GetType()) ||
                typeof(EntityMeasureSource).IsAssignableFrom(source.GetType())));
            Assert.IsTrue(measureSources.OfType<DfpMeasureSourceBase>().Count() > 0);
            Assert.IsTrue(measureSources.OfType<EntityMeasureSource>().Count() > 0);

            // Verify the expected entity measures are present
            var entityMeasures = measureSources
                .OfType<EntityMeasureSource>()
                .SelectMany(source => source.Measures);
            Assert.IsNotNull(entityMeasures.SingleOrDefault(m =>
                (string)m.Value["displayName"] == companyName));
            Assert.IsNotNull(entityMeasures.SingleOrDefault(m =>
                (string)m.Value["displayName"] == campaignName));
        }

        /// <summary>Test creating a DFP measure source</summary>
        [TestMethod]
        public void CreateDfpMeasureSource()
        {
            var uniqueId = Guid.NewGuid().ToString("N").Left(10);
            var configJson = JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "GoogleDfp.NetworkId", uniqueId }
                });
            
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString());
            companyEntity.SetEntityProperty(
                new EntityProperty("CONFIG", new PropertyValue(PropertyType.String, configJson), PropertyFilter.System));

            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"),
                12345,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                "???");

            var source = new TestDfpMeasureSource(companyEntity, campaignEntity);

            Assert.AreEqual(299000000000000000, source.BaseMeasureId);
            Assert.AreEqual(299999999999999999, source.MaxMeasureId);

            var expectedSourceId = "NETWORK:0299:dfp-test-" + uniqueId;
            Assert.AreEqual(expectedSourceId, source.SourceId);
        }

        /// <summary>
        /// Test the method used to create browser technology measure display names
        /// </summary>
        [TestMethod]
        public void GetBrowserTechnologyMeasureDisplayName()
        {
            var majorMinorVersionRow = new Dictionary<string, object>
            {
                { "browsername", "Browser" },
                { "majorversion", "0" },
                { "minorversion", "1" },
            };
            var majorMinorVersionName =
                TechnologyMeasureSource.GetBrowserVersionDisplayName(majorMinorVersionRow);
            Assert.AreEqual("Browser 0.1", majorMinorVersionName);

            var majorAnyVersionRow = new Dictionary<string, object>
            {
                { "browsername", "Browser" },
                { "majorversion", "0" },
                { "minorversion", "Any" },
            };
            var majorAnyVersionName =
                TechnologyMeasureSource.GetBrowserVersionDisplayName(majorAnyVersionRow);
            Assert.AreEqual("Browser 0.*", majorAnyVersionName);

            var anyAnyVersionRow = new Dictionary<string, object>
            {
                { "browsername", "Browser" },
                { "majorversion", "Any" },
                { "minorversion", "any" },
            };
            var anyAnyVersionName =
                TechnologyMeasureSource.GetBrowserVersionDisplayName(anyAnyVersionRow);
            Assert.AreEqual("Browser *.*", anyAnyVersionName);

            var majorOtherVersionRow = new Dictionary<string, object>
            {
                { "browsername", "Browser" },
                { "majorversion", "0" },
                { "minorversion", "Other" },
            };
            var majorOtherVersionName =
                TechnologyMeasureSource.GetBrowserVersionDisplayName(majorOtherVersionRow);
            Assert.AreEqual("Browser 0 (Other)", majorOtherVersionName);

            var otherOtherVersionRow = new Dictionary<string, object>
            {
                { "browsername", "Browser" },
                { "majorversion", "Other" },
                { "minorversion", "other" },
            };
            var otherOtherVersionName =
                TechnologyMeasureSource.GetBrowserVersionDisplayName(otherOtherVersionRow);
            Assert.AreEqual("Browser (Other)", otherOtherVersionName);
        }

        /// <summary>Derived class for testing DfpMeasureSourceBase</summary>
        private class TestDfpMeasureSource : DfpMeasureSourceBase, IMeasureSource
        {
            /// <summary>Initializes a new instance of the TestDfpMeasureSource class</summary>
            /// <param name="companyEntity">CompanyEntity (for config)</param>
            /// <param name="campaignEntity">CampaignEntity (for config)</param>
            public TestDfpMeasureSource(CompanyEntity companyEntity, CampaignEntity campaignEntity)
                : base(99, "test", companyEntity, campaignEntity, PersistentDictionaryType.Memory)
            {
            }

            /// <summary>Gets or sets the test MeasureMapCacheEntry</summary>
            public MeasureMapCacheEntry TestMeasureMapCacheEntry { get; set; }

            /// <summary>Gets the category display name</summary>
            protected override string CategoryDisplayName
            {
                get { return "Test Measure"; }
            }

            /// <summary>Gets the measure type</summary>
            protected override string MeasureType
            {
                get { return "test"; }
            }

            /// <summary>Fetch the latest AdUnit measure map</summary>
            /// <returns>The latest MeasureMap</returns>
            protected override MeasureMapCacheEntry FetchLatestMeasureMap()
            {
                return this.TestMeasureMapCacheEntry;
            }
        }
    }
}
