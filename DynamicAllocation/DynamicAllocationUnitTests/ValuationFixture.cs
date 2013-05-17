// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValuationFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for Valuation
    /// </summary>
    [TestClass]
    public class ValuationFixture
    {
        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet Auto = new MeasureSet { 1106001 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet Male = new MeasureSet { 1106002 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet Wealthy = new MeasureSet { 1106003 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet TwentiesThirties = new MeasureSet { 1106004 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet ThirtiesForties = new MeasureSet { 1106005 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet AutoMale = new MeasureSet { 1106001, 1106002 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet AutoWealthy = new MeasureSet { 1106001, 1106003 };

        /// <summary> sample MeasureSet to use for testing </summary>
        private static readonly MeasureSet AutoMaleWealthy = new MeasureSet { 1106001, 1106002, 1106003 };

        /// <summary>
        /// DynamicAllocationService instance used for tests
        /// </summary>
        private static DynamicAllocationEngine dynamicAllocationService;

        /// <summary>
        /// needed for DataSource
        /// </summary>
        private static TestContext testContextInstance;

        /// <summary>
        /// Gets or sets TestContext. (needed for DataSource)
        /// </summary>
        public TestContext TestContext
        {
            get { return ValuationFixture.testContextInstance; }
            set { ValuationFixture.testContextInstance = value; }
        }

        /// <summary>
        /// Initialize the dynamic allocation service before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            var measureMap = new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "DynamicAllocationUnitTests.Resources.MeasureMap.js") });
            ValuationFixture.dynamicAllocationService = new DynamicAllocationEngine(measureMap);
        }

        /// <summary>
        /// Reads test data from a csv and calls TestGetValuations 
        /// </summary>
        [DeploymentItem("DynamicAllocationUnitTests\\GetValuationsTestDataSource.csv"), TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "|DataDirectory|\\GetValuationsTestDataSource.csv", "GetValuationsTestDataSource#csv", DataAccessMethod.Sequential)]
        public void GetValuationsTestWithDataSource()
        {
            string inputXML = Convert.ToString(ValuationFixture.testContextInstance.DataRow["input"]);
            string expectedOutputXml = Convert.ToString(ValuationFixture.testContextInstance.DataRow["expected"]);
            this.TestGetValuations(inputXML, expectedOutputXml);
        }

        /// <summary>
        /// test the GetValuations method
        /// </summary>
        /// <param name="inputXml">the input as xml</param>
        /// <param name="expectedXml">the expected output as xml</param>
        public void TestGetValuations(string inputXml, string expectedXml)
        {
            var input = DynamicAllocationTestUtilities.DeserializeFromXml<CampaignDefinition>(inputXml);
            var actual = ValuationFixture.dynamicAllocationService.GetValuations(input);
            var expected = DynamicAllocationTestUtilities.DeserializeFromXml<Dictionary<MeasureSet, decimal>>(expectedXml);

            // Assert that the two dictionaries are equal
            Assert.IsTrue(actual.OrderBy(kvp => kvp.Key).SequenceEqual(expected.OrderBy(kvp => kvp.Key)));
        }

        /// <summary>
        /// test calculate bids with overrides
        /// </summary>
        [TestMethod]
        public void TestGetValuationsWithOverrides()
        {
            // override with increase
            var original = ValuationFixture.dynamicAllocationService.GetValuations(new CampaignDefinition
            {
                ExplicitValuations = new Dictionary<MeasureSet, decimal>
                 {      
                     { ValuationFixture.Auto, 2m },
                     { ValuationFixture.Male, 1m },
                     { ValuationFixture.TwentiesThirties, 3m },
                     { ValuationFixture.Wealthy, 4m },
                 },
                MaxPersonaValuation = 10m,
                PinnedMeasures = null,
                MeasureGroupings = null,
            });

            var originalByMeasureSet = original.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var actualIncreased = ValuationFixture.dynamicAllocationService.GetValuations(new CampaignDefinition
            {
                ExplicitValuations = new Dictionary<MeasureSet, decimal>
                 {      
                     { ValuationFixture.Auto, 2m },
                     { ValuationFixture.Male, 1m },
                     { ValuationFixture.TwentiesThirties, 3m },
                     { ValuationFixture.Wealthy, 4m },
                     { new MeasureSet { ValuationFixture.Male.Single(), ValuationFixture.Auto.Single() }, 100m },
                 },
                MaxPersonaValuation = 10m,
                PinnedMeasures = null,
                MeasureGroupings = null,
            });

            foreach (var kvp in actualIncreased)
            {
                if (kvp.Key.Contains(ValuationFixture.Male.Single()) && 
                    kvp.Key.Contains(ValuationFixture.Auto.Single()) && 
                    !kvp.Key.Equals(new MeasureSet 
                        { 
                            ValuationFixture.Auto.Single(), 
                            ValuationFixture.Male.Single(), 
                            ValuationFixture.TwentiesThirties.Single(), 
                            ValuationFixture.Wealthy.Single() 
                        }))
                {
                    // Bid should have increased due to higher overridden value.
                    var failMessage = "Bid should have increased for measure set '" + kvp.Key + ", but did not. Original: "
                        + originalByMeasureSet[kvp.Key] + ", new: " + kvp.Value;
                    Assert.IsTrue(
                        kvp.Value > originalByMeasureSet[kvp.Key],
                        failMessage);
                }
            }

            // override with decrease
            var actualDecreased = ValuationFixture.dynamicAllocationService.GetValuations(new CampaignDefinition
            {
                ExplicitValuations = new Dictionary<MeasureSet, decimal>
                 {      
                     { ValuationFixture.Auto, 2m },
                     { ValuationFixture.Male, 1m },
                     { ValuationFixture.TwentiesThirties, 3m },
                     { ValuationFixture.Wealthy, 4m },
                     { new MeasureSet { ValuationFixture.Male.Single(), ValuationFixture.Auto.Single() }, 1m },
                 },
                MaxPersonaValuation = 10m,
                PinnedMeasures = null,
                MeasureGroupings = null,
            });

            foreach (var kvp in actualDecreased)
            {
                if (kvp.Key.Contains(ValuationFixture.Male.Single()) &&
                    kvp.Key.Contains(ValuationFixture.Auto.Single()) &&
                    !kvp.Key.Equals(new MeasureSet 
                    { 
                        ValuationFixture.Auto.Single(), 
                        ValuationFixture.Male.Single(), 
                        ValuationFixture.TwentiesThirties.Single(), 
                        ValuationFixture.Wealthy.Single() 
                    }))
                {
                    // Bid should have decreased due to lower overridden value.
                    var failMessage = "Bid should have decreased for measure set '" + kvp.Key + ", but did not. Original: "
                        + originalByMeasureSet[kvp.Key] + ", new: " + kvp.Value;
                    Assert.IsTrue(
                        kvp.Value < originalByMeasureSet[kvp.Key],
                        failMessage);
                }
            }
        }

        /// <summary>
        /// A test for ExtractMeasureValuations
        /// </summary>
        [TestMethod]
        public void ExtractMeasureValuationsTest()
        {
            var input = new Dictionary<MeasureSet, decimal>
            {
                { new MeasureSet { 1 }, 1 },
                { new MeasureSet { 2 }, 2 },
                { new MeasureSet { 3 }, 3 },
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 1, 2, 3 }, 6 },
            };

            var expected = new Dictionary<long, decimal>
            {
                { 1, 1 },
                { 2, 2 },
                { 3, 3 }, 
            };

            var actual = Valuation.ExtractMeasureValuations(input);

            DynamicAllocationTestUtilities.AssertDictionariesAreEqual(expected, actual);
        }

        /// <summary>
        /// A test for ExtractOverrides
        /// </summary>
        [TestMethod]
        public void ExtractOverridesTest()
        {
            var input = new Dictionary<MeasureSet, decimal>
            {
                { new MeasureSet { 1 }, 1 },
                { new MeasureSet { 2 }, 2 },
                { new MeasureSet { 3 }, 3 },
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 1, 2, 3 }, 6 },
            };

            var expected = new Dictionary<MeasureSet, decimal>
            {
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 1, 2, 3 }, 6 },
            };

            var actual = Valuation.ExtractOverrides(input);

            DynamicAllocationTestUtilities.AssertDictionariesAreEqual(expected, actual);
        }

        /// <summary>
        /// A test for CollectMeasuresByGrouping
        /// </summary>
        [TestMethod]
        public void CollectMeasuresByGroupingTest()
        {
            IEnumerable<long> measures = new long[] { 1, 2, 3, 4, 5 };

            var measureGroupings = new Dictionary<long, string>
            {
                { 1, "grp1" },
                { 2, "grp1" },
                { 3, "grp2" },
                { 4, "grp2" },
            };

            var expected = new Dictionary<string, MeasureSet>
            {
                { "grp1", new MeasureSet { 1, 2 } },
                { "grp2", new MeasureSet { 3, 4 } },
                { "5", new MeasureSet { 5 } }
            };

            var actual = Valuation.CollectMeasuresByGrouping(measures, measureGroupings);
            DynamicAllocationTestUtilities.AssertDictionariesAreEqual(expected, actual);
        }

        /// <summary>
        /// A test for CollectMeasuresByGrouping
        /// </summary>
        [TestMethod]
        public void CollectMeasuresByGroupingWithDuplicateMeasureTest()
        {
            IEnumerable<long> measures = new long[] { 1, 1, 2, 3, 3, 4, 5 };

            var measureGroupings = new Dictionary<long, string>
            {
                { 1, "grp1" },
                { 2, "grp1" },
                { 3, "grp2" },
                { 4, "grp2" },
            };

            var expected = new Dictionary<string, MeasureSet>
            {
                { "grp1", new MeasureSet { 1, 2 } },
                { "grp2", new MeasureSet { 3, 4 } },
                { "5", new MeasureSet { 5 } }
            };

            var actual = Valuation.CollectMeasuresByGrouping(measures, measureGroupings);
            DynamicAllocationTestUtilities.AssertDictionariesAreEqual(expected, actual);
        }

        /// <summary>
        /// A test for CalculatePersonaMeasureSet
        /// </summary>
        [TestMethod]
        public void CalculatePersonaMeasureSetTest()
        {
            var groupingDictionary = new Dictionary<string, MeasureSet>
            {
                { "grp1", new MeasureSet { 1, 2 } },
                { "grp2", new MeasureSet { 3, 4 } },
                { "5", new MeasureSet { 5 } }
            };

            var measureValuations = new Dictionary<long, decimal>
            {
                { 1, 1 },
                { 2, 2 },
                { 3, 3 }, 
                { 4, 4 }, 
                { 5, 5 }, 
            };

            var expected = new MeasureSet { 2, 4, 5 };
            var actual = Valuation.CalculatePersonaMeasureSet(groupingDictionary, measureValuations);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for FindClosestOverrideOrMaxMeasure with an override for the measure set
        /// </summary>
        [TestMethod]
        public void FindClosestOverrideOrMaxMeasureTestWithOverride()
        {
            // set the measureSet to have an override and make sure this returns it
            var measureSet = ValuationFixture.AutoMale;
            var overrides = new Dictionary<MeasureSet, decimal> { { ValuationFixture.AutoMale, 100 } };
            var measureValuations = ValuationFixture.AutoMaleWealthy.Select(ms => new KeyValuePair<long, decimal>(ms, 1m)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var expected = new KeyValuePair<MeasureSet, decimal>(ValuationFixture.AutoMale, 100);
            var actual = Valuation.FindClosestOverrideOrMaxMeasure(measureSet, overrides, measureValuations);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for FindClosestOverrideOrMaxMeasure with an override for a subset of the measure set
        /// </summary>
        [TestMethod]
        public void FindClosestOverrideOrMaxMeasureTestWithSubsetOverride()
        {
            // set the measureSet to have a subset override and make sure this returns it
            var measureSet = ValuationFixture.AutoMale;
            var overrides = new Dictionary<MeasureSet, decimal> { { ValuationFixture.Male, 100 } };
            var measureValuations = ValuationFixture.AutoMaleWealthy.Select(ms => new KeyValuePair<long, decimal>(ms, 1m)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var expected = new KeyValuePair<MeasureSet, decimal>(ValuationFixture.Male, 100);
            var actual = Valuation.FindClosestOverrideOrMaxMeasure(measureSet, overrides, measureValuations);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for FindClosestOverrideOrMaxMeasure no override
        /// </summary>
        [TestMethod]
        public void FindClosestOverrideOrMaxMeasureTestWithNoOverride()
        {
            // set the measureSet to have no override and make sure this returns the max measure value
            var measureSet = ValuationFixture.AutoMale;
            var overrides = new Dictionary<MeasureSet, decimal>();
            var measureValuations = ValuationFixture.AutoMaleWealthy.Select(ms => new KeyValuePair<long, decimal>(ms, 1m)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            measureValuations[ValuationFixture.Male.Single()] = 2m;
            var expected = new KeyValuePair<MeasureSet, decimal>(ValuationFixture.Male, 2);
            var actual = Valuation.FindClosestOverrideOrMaxMeasure(measureSet, overrides, measureValuations);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for FindClosestOverrideOrMaxMeasure with two overrides
        /// </summary>
        [TestMethod]
        public void FindClosestOverrideOrMaxMeasureTestWithTwoOverrides()
        {
            // set the measureSet to have two overrides and make sure this returns the most relevant one
            var measureSet = ValuationFixture.AutoMaleWealthy;
            var overrides = new Dictionary<MeasureSet, decimal> { { ValuationFixture.Male, 100 }, { ValuationFixture.AutoWealthy, .1m } };
            var measureValuations = new Dictionary<long, decimal>
            {
                { Auto.Single(), 1 },
                { Male.Single(), 1 },
                { TwentiesThirties.Single(), 1 },
                { Wealthy.Single(), 1 },
            };
            var expected = new KeyValuePair<MeasureSet, decimal>(ValuationFixture.AutoWealthy, .1m);
            var actual = Valuation.FindClosestOverrideOrMaxMeasure(measureSet, overrides, measureValuations);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// A test for SumWithoutLargestMeasureOrOverride
        /// </summary>
        [TestMethod]
        public void SumWithoutLargestMeasureOrOverrideTest()
        {
            // the result should be the sum of the measures in the measure set that are not in the maxMeasureOrOverride
            var measureSet = new MeasureSet 
            { 
                Auto.Single(), 
                Male.Single(), 
                TwentiesThirties.Single(), 
                ThirtiesForties.Single(), 
                Wealthy.Single() 
            };
            var maxMeasureOrOverride = new KeyValuePair<MeasureSet, decimal>(ValuationFixture.AutoMale, 100);
            var measureValuations = new Dictionary<long, decimal>
            {
                { Auto.Single(), 1 },
                { Male.Single(), 1 },
                { TwentiesThirties.Single(), 1 },
                { ThirtiesForties.Single(), 1 },
                { Wealthy.Single(), 1 },
            }; 
            const decimal Expected = 3m;
            var actual = Valuation.SumWithoutLargestMeasureOrOverride(measureSet, maxMeasureOrOverride, measureValuations);
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>
        /// A test for FilterForPinnedMeasures with a single pinned measure
        /// </summary>
        [TestMethod]
        public void FilterForPinnedMeasuresTestSingle()
        {
            var valuations = new List<MeasureSet>
            {
                new MeasureSet { 1 },
                new MeasureSet { 2 },
                new MeasureSet { 3 },
                new MeasureSet { 5 },
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 2, 3 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3 },
                new MeasureSet { 1, 2, 3, 5 }
            };

            var pinnedMeasures = new[] { 5L };

            var measureGroupings = new Dictionary<long, string>
            {
                { 1, "grp1" },
                { 2, "grp1" },
                { 3, "grp2" },
                { 4, "grp2" },
            };

            var expected = new List<MeasureSet>
            {
                new MeasureSet { 5 },
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3, 5 },
            };

            var actual = Valuation.FilterForPinnedMeasures(valuations, pinnedMeasures, measureGroupings);
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// A test for FilterForPinnedMeasures with an 'Or' group pinned
        /// </summary>
        [TestMethod]
        public void FilterForPinnedMeasuresTestOrGroup()
        {
            var valuations = new List<MeasureSet>
            {
                new MeasureSet { 1 },
                new MeasureSet { 2 },
                new MeasureSet { 3 },
                new MeasureSet { 5 },
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 2, 3 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3 },
                new MeasureSet { 1, 2, 3, 5 }
            }; 

            var pinnedMeasures = new long[] { 1, 2 };

            var measureGroupings = new Dictionary<long, string>
            {
                { 1, "grp1" },
                { 2, "grp1" },
                { 3, "grp2" },
                { 4, "grp2" },
            };

            var expected = new List<MeasureSet>
            {
                new MeasureSet { 1 },
                new MeasureSet { 2 },
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 2, 3 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3 },
                new MeasureSet { 1, 2, 3, 5 }
            };

            var actual = Valuation.FilterForPinnedMeasures(valuations, pinnedMeasures, measureGroupings);
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// A test for FilterForPinnedMeasures with an 'Or' group pinned and a single measure pinned
        /// </summary>
        [TestMethod]
        public void FilterForPinnedMeasuresTestOrGroupAndSingle()
        {
            var valuations = new List<MeasureSet>
            {
                new MeasureSet { 1 },
                new MeasureSet { 2 },
                new MeasureSet { 3 },
                new MeasureSet { 5 },
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 2, 3 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3 },
                new MeasureSet { 1, 2, 3, 5 }
            };

            var pinnedMeasures = new long[] { 1, 2, 5 };

            var measureGroupings = new Dictionary<long, string>
            {
                { 1, "grp1" },
                { 2, "grp1" },
                { 3, "grp2" },
                { 4, "grp2" },
            };

            var expected = new List<MeasureSet>
            {
                new MeasureSet { 1, 5 },
                new MeasureSet { 2, 5 },
                new MeasureSet { 1, 2, 5 },
                new MeasureSet { 1, 2, 3, 5 }
            };

            var actual = Valuation.FilterForPinnedMeasures(valuations, pinnedMeasures, measureGroupings);
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
