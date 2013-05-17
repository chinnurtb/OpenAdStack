﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationServiceFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for the DynamicAllocationService
    /// </summary>
    [TestClass]
    public class DynamicAllocationServiceFixture
    {   
        /// <summary>Test allocation paramters</summary>
        private static AllocationParameters testAllocationParameters;

        /// <summary>Test measure map</summary>
        private static MeasureMap measureMap;

        /// <summary>
        /// Per test initialization
        /// </summary>
        /// <param name="context">text context</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            TestUtilities.AllocationParametersDefaults.Initialize();
            testAllocationParameters = new AllocationParameters();
            measureMap = new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "DynamicAllocationUnitTests.Resources.MeasureMap.js") });
        }

        /// <summary>
        /// A test for the GetValuations method
        /// </summary>
        [TestMethod]
        public void GetValuationsTest()
        {
            // verfify that valuations get created
            var campaign = new CampaignDefinition
            {
                ExplicitValuations = new Dictionary<MeasureSet, decimal> { { new MeasureSet { 1 }, 2 }, { new MeasureSet { 2 }, 3 } },
                MaxPersonaValuation = 10m,
            };

            var actual = new DynamicAllocationEngine(measureMap).GetValuations(campaign);
            Assert.IsTrue(actual.ContainsKey(new MeasureSet { 1 }));
            Assert.AreEqual(2, actual[new MeasureSet(new long[] { 1 })]);
            Assert.IsTrue(actual.ContainsKey(new MeasureSet { 2 }));
            Assert.AreEqual(3, actual[new MeasureSet(new long[] { 2 })]);
        }

        /// <summary>
        /// A test for GetBudgetAllocations with History
        /// </summary>
        [TestMethod]
        public void GetBudgetAllocationsTestWithHistory()
        {
            var measureSets = new List<MeasureSet> { new MeasureSet { 1106006 } };

            // verify we get budget allocations when there is a history
            var campaign = new BudgetAllocation
            {
                PerNodeResults = measureSets.ToDictionary(
                    ms => ms,
                    ms => new PerNodeBudgetAllocationResult
                    {
                        Valuation = (decimal)ms.Count,
                        ExportCount = 1,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = testAllocationParameters,
                PeriodDuration = TimeSpan.FromDays(1)
            };

            var actual = new DynamicAllocationEngine(measureMap).GetBudgetAllocations(campaign);
            Assert.IsNotNull(actual.PerNodeResults);
            Assert.AreNotEqual(0, actual.Phase);
            Assert.AreEqual(1, actual.PerNodeResults.Count);
        }

        /// <summary>
        /// A test for GetBudgetAllocations with History and forceInitial
        /// </summary>
        [TestMethod]
        public void GetInitialBudgetAllocationsTestWithHistory()
        {
            var measureSets = new List<MeasureSet> { new MeasureSet { 1106006 } };

            // verify we get budget allocations when there is a history
            var campaign = new BudgetAllocation
            {
                PerNodeResults = measureSets.ToDictionary(
                    ms => ms,
                    ms => new PerNodeBudgetAllocationResult
                    {
                        Valuation = (decimal)ms.Count,
                        ExportCount = 1,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = testAllocationParameters,
                PeriodDuration = TimeSpan.FromDays(1)
            };

            var actual = new DynamicAllocationEngine(measureMap).GetBudgetAllocations(campaign, true);
            Assert.IsNotNull(actual.PerNodeResults);
            Assert.AreEqual(0, actual.Phase);
            Assert.AreEqual(1, actual.PerNodeResults.Count);
        }

        /// <summary>
        /// A test for GetBudgetAllocations without History
        /// </summary>
        [TestMethod]
        public void GetBudgetAllocationsTestWithoutHistory()
        {
            var measureSets = new List<MeasureSet> { new MeasureSet { 1106006 } };

            // verify we get budget allocations when there is a history
            var campaign = new BudgetAllocation
            {
                PerNodeResults = measureSets.ToDictionary(
                    ms => ms, 
                    ms => new PerNodeBudgetAllocationResult
                    {
                        Valuation = (decimal)ms.Count,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = testAllocationParameters,
                PeriodDuration = new TimeSpan(1, 0, 0, 0),
                CampaignStart = new DateTime(2011, 12, 31).AddDays(-3),
                CampaignEnd = new DateTime(2011, 12, 31),
            };

            var actual = new DynamicAllocationEngine(measureMap).GetBudgetAllocations(campaign);
            Assert.IsNotNull(actual.PerNodeResults);
            Assert.AreEqual(0, actual.Phase);
            Assert.AreEqual(1, actual.PerNodeResults.Count);
        }

        /// <summary>
        /// A test for the IncrementExportCounts method
        /// </summary>
        [TestMethod]
        public void IncrementExportCounts()
        {
            var measureSets = new List<MeasureSet> 
            { 
                new MeasureSet { 1 }, 
                new MeasureSet { 1, 2 }, 
                new MeasureSet { 1, 2, 3 } 
            };

            var exportMeasureSets = new MeasureSet[] 
            {
                new MeasureSet { 1 }, 
                new MeasureSet { 1, 2 }, 
            };

            var campaign = new BudgetAllocation
            {
                PerNodeResults = measureSets.ToDictionary(
                    ms => ms,
                    ms => new PerNodeBudgetAllocationResult
                    {
                        Valuation = (decimal)ms.Count,
                        ExportCount = 1,
                    })
            };
            var actual = new DynamicAllocationEngine(measureMap).IncrementExportCounts(campaign, exportMeasureSets);

            Assert.IsTrue(actual
                .PerNodeResults
                .Where(pnr => exportMeasureSets.Contains(pnr.Key))
                .All(pnr => pnr.Value.ExportCount == 2));
            Assert.IsTrue(actual
                .PerNodeResults
                .Where(pnr => !exportMeasureSets.Contains(pnr.Key))
                .All(pnr => pnr.Value.ExportCount == 1));
        }
    }
}
