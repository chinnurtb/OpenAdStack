// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasureMapFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diagnostics;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Rhino.Mocks;

namespace MeasuresUnitTests
{
    /// <summary>
    /// Measure Map Fixture
    /// </summary>
    [TestClass]
    public class MeasureMapFixture
    {
        /// <summary>The measure map</summary>
        private static MeasureMap measureMap;

        /// <summary>Per test initialization.</summary>
        /// <param name="context">Text Context</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
            measureMap = new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "MeasuresUnitTests.Resources.MeasureMap.js") });
        }

        /// <summary>Test we can load measure information</summary>
        [TestMethod]
        public void LoadMap()
        {
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("displayName")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("APNXId")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("APNXId_Sandbox")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("dataProvider")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("type")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("DataCost")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("HistoricalVolume")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("MinCPM")));
            Assert.IsTrue(measureMap.Map.Any(measure => measure.Value.ContainsKey("PercentOfMedia")));
        }

        /// <summary> test to GetMinCPM </summary>
        [TestMethod]
        public void TryGetMinCostPerMille()
        {
            var minCpm = measureMap.TryGetMinCostPerMille(1200051);
            Assert.AreEqual(.05m, minCpm);
        }

        /// <summary> test to GetMinCPM when missing </summary>
        [TestMethod]
        public void TryGetMinCostPerMilleWhenMissing()
        {
            var minCpm = measureMap.TryGetMinCostPerMille(1106006);
            Assert.IsNull(minCpm);
        }

        /// <summary> test to GetPercentOfMedia </summary>
        [TestMethod]
        public void TryGetPercentOfMedia()
        {
            decimal? percentOfMedia = measureMap.TryGetPercentOfMedia(1200051);
            Assert.AreEqual(.15m, percentOfMedia);
        }

        /// <summary> test to GetPercentOfMedia when missing </summary>
        [TestMethod]
        public void TryGetPercentOfMediaWhenMissing()
        {
            var percentOfMedia = measureMap.TryGetPercentOfMedia(1106006);
            Assert.IsNull(percentOfMedia);
        }

        /// <summary> test to GetPercentOfMedia </summary>
        [TestMethod]
        public void TryGetDataCost()
        {
            decimal? dataCost = measureMap.TryGetDataCost(1106006);
            Assert.AreEqual(.25m, dataCost);
        }

        /// <summary> test to GetPercentOfMedia when missing </summary>
        [TestMethod]
        public void TryGetDataCostWhenMissing()
        {
            var dataCost = measureMap.TryGetDataCost(1200051);
            Assert.IsNull(dataCost);
        }
    }
}
