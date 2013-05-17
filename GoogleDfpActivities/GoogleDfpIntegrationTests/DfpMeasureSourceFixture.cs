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
using EntityTestUtilities;
using GoogleDfpActivities.Measures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Utilities.Storage.Testing;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Base class for DfpActivity fixtures</summary>
    [TestClass]
    public class DfpMeasureSourceFixture
    {
        /// <summary>Unique id for the test case</summary>
        private string uniqueId;

        /// <summary>Test company entity</summary>
        private CompanyEntity companyEntity;

        /// <summary>Test campaign entity</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
            CachedMeasureSource.CacheUpdateStartTimes = null;
            CachedMeasureSource.LocalMeasureCache = null;

            this.uniqueId = Guid.NewGuid().ToString("N");

            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString());

            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"),
                12345,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                "???");
        }

        /// <summary>Test getting measures for AdUnits</summary>
        [TestMethod]
        public void GetAdUnitMeasures()
        {
            var source = new AdUnitMeasureSource(this.companyEntity, this.campaignEntity);
            var measures = GoogleDfpTestHelpers.LoadMeasures(source);
            Assert.IsTrue(TestNetwork.AdUnitCodes.All(code =>
                measures.Values.Select(measure => measure["displayName"] as string)
                .Any(name => name.Contains(code))));
        }

        /// <summary>Test getting measures for placements</summary>
        [TestMethod]
        public void GetPlacementMeasures()
        {
            var source = new PlacementMeasureSource(this.companyEntity, this.campaignEntity);
            var measures = GoogleDfpTestHelpers.LoadMeasures(source);
            Assert.IsTrue(TestNetwork.Placements.Values.All(placementName =>
                measures.Values.Select(measure => measure["displayName"] as string)
                .Any(measureName => measureName.Contains(placementName))));
        }

        /// <summary>Test getting measures for locations</summary>
        [TestMethod]
        public void GetLocationMeasures()
        {
            var source = new LocationMeasureSource(this.companyEntity, this.campaignEntity);
            var measures = GoogleDfpTestHelpers.LoadMeasures(source, 300);

            // TODO: More meaningful validation
            Assert.IsNotNull(measures);
            Assert.AreNotEqual(0, measures.Count);

            var cachedSource = new LocationMeasureSource(this.companyEntity, this.campaignEntity);
            var cachedMeasures = cachedSource.Measures;

            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(measures.Count, cachedMeasures.Count);
        }

        /// <summary>Test getting measures for technologies</summary>
        [TestMethod]
        public void GetTechnologyMeasures()
        {
            var source = new TechnologyMeasureSource(this.companyEntity, this.campaignEntity);
            var measures = GoogleDfpTestHelpers.LoadMeasures(source);

            // TODO: More meaningful validation
            Assert.IsNotNull(measures);
            Assert.AreNotEqual(0, measures.Count);

            var cachedSource = new TechnologyMeasureSource(this.companyEntity, this.campaignEntity);

            var startTime = DateTime.UtcNow;
            var cachedMeasures = cachedSource.Measures;
            var fetchTime = DateTime.UtcNow - startTime;

            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(measures.Count, cachedMeasures.Count);

            // Cached measures shouldn't take long to get
            Assert.IsTrue(fetchTime.TotalMilliseconds < 500, "Getting cached measures took longer than expected");
        }

        /// <summary>Test that none of the sources' measure id ranges overlap</summary>
        [TestMethod]
        public void NoMeasureIdOverlap()
        {
            var provider = new DfpMeasureSourceProvider();
            var sources = provider.GetMeasureSources(this.companyEntity, this.campaignEntity);
            foreach (var source in sources)
            {
                GoogleDfpTestHelpers.LoadMeasures(source);
            }

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
    }
}