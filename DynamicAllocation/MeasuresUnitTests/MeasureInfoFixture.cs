// -----------------------------------------------------------------------
// <copyright file="MeasureInfoFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Reflection;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MeasuresUnitTests
{
    /// <summary>Test fixture for DataCost class</summary>
    [TestClass]
    public class MeasureInfoFixture
    {
        /// <summary>serving fees for tests</summary>
        private const decimal PerMilleFees = 0.06m;

        /// <summary>margin for tests</summary>
        private const decimal Margin = 1 / .85m;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure1;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure2;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure3;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure4;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure5;

        /// <summary>measure to use in tests </summary>
        private Dictionary<string, object> measure6;

        /// <summary>The measure map</summary>
        private MeasureMap measureMap = new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "MeasuresUnitTests.Resources.MeasureMap.js") });

        /// <summary>the measure info</summary>
        private MeasureInfo measureInfo = new MeasureInfo(new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "MeasuresUnitTests.Resources.MeasureMap.js") }));

        /// <summary>
        /// per test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.measure1 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "Lotame" },
                { MeasureValues.DataCost, .25 },
            };

            this.measure2 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "Lotame" },
                { MeasureValues.DataCost, .75 },
            };

            this.measure3 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "exelate" },
                { MeasureValues.DataCost, .25 },
            };

            this.measure4 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "exelate" },
                { MeasureValues.DataCost, .75 },
            };

            this.measure5 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "Peer39" },
                { MeasureValues.DataCost, .05 },
            };

            this.measure6 = new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "Peer39" },
                { MeasureValues.MinCostPerMille, .05 },
                { MeasureValues.PercentOfMedia, .15 }
            };
       }

        /// <summary>
        /// Test to make sure the measureMap records match the business rules of MeasureInfo.
        /// </summary>
        [TestMethod]
        public void MeasureMapIsValid()
        {
            foreach (var measure in this.measureMap.Map.Keys)
            {
                var dataProvider = this.measureMap.TryGetDataProviderForMeasure(measure);
                Assert.IsTrue(this.IsDataProviderValid(dataProvider));

                var dataCost = this.measureMap.TryGetDataCost(measure);
                var minCpm = this.measureMap.TryGetMinCostPerMille(measure);
                var percentOfMedia = this.measureMap.TryGetPercentOfMedia(measure);

                // Data cost present and percent of spend fields null
                if (MeasureInfo.CompareDataProviderName(dataProvider, MeasureInfo.DataProviderNameExelate) ||
                    MeasureInfo.CompareDataProviderName(dataProvider, MeasureInfo.DataProviderNameLotame) ||
                    MeasureInfo.CompareDataProviderName(dataProvider, MeasureInfo.DataProviderNameBlueKai))
                {
                    Assert.IsNotNull(dataCost);
                    Assert.IsNull(minCpm);
                    Assert.IsNull(percentOfMedia);
                }

                // Data cost field or percent of spend fields available (not both)
                if (MeasureInfo.CompareDataProviderName(dataProvider, MeasureInfo.DataProviderNamePeer39))
                {
                    if (dataCost != null)
                    {
                        Assert.IsNull(minCpm);
                        Assert.IsNull(percentOfMedia);
                        continue;
                    }

                    Assert.IsNotNull(minCpm);
                    Assert.IsNotNull(percentOfMedia);
                }

                // Data cost is zero for DMA
                if (MeasureInfo.CompareDataProviderName(dataProvider, "2") ||
                    MeasureInfo.CompareDataProviderName(dataProvider, "3"))
                {
                    Assert.AreEqual(0m, dataCost);
                    Assert.IsNull(minCpm);
                    Assert.IsNull(percentOfMedia);
                }
            }
        }

        /// <summary>
        /// Confirm that we are case insensitive relative to data provider name when calculating cost.
        /// </summary>
        [TestMethod]
        public void CalculateDataCostMixedCaseDataProviderNames()
        {
            // Data cost .25 & .75 respectively
            var measureSet = new MeasureSet { 1106003, 1106004 };

            // Set up so one of the measures has different case 'E' than then other 'e'
            this.measure4[MeasureValues.DataProvider] = "Exelate";

            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                });
            var measureInfo = new MeasureInfo(measureMap);

            // We should not double count the two measures - should just pick the max
            var actualValue = measureInfo.CalculateTotalSpend(measureSet, 1000, 0, 1, 0);
            Assert.AreEqual(0.75m, actualValue);
        }

        /// <summary>
        /// Confirm that we are case insensitive when getting the measures for a data provider.
        /// </summary>
        [TestMethod]
        public void GetMeasureForProviderCaseInsensitive()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 
                        1, 
                        new Dictionary<string, object>
                        {
                            { MeasureValues.DataProvider, "Exelate" },
                            { MeasureValues.DataCost, .75 },
                        }
                    }
                });
            var measureInfo = new MeasureInfo(measureMap);

            var providers = measureInfo.ExtractDataProviders(new MeasureSet { 1 });
            var measures = MeasureInfo.GetMeasuresForProvider(providers, MeasureInfo.DataProviderNameExelate);
            Assert.AreEqual(1, measures.Single());
        }

        /// <summary>
        /// Extract the canonical names for the data providers in the measureSet
        /// </summary>
        [TestMethod]
        public void ExtractDataProviders()
        {
            var measureSet = new MeasureSet { 1, 2, 3 };

            // Set up so one of the measures has different case 'E' than then other 'e'
            this.measure1[MeasureValues.DataProvider] = "lotame";
            this.measure2[MeasureValues.DataProvider] = "Exelate";
            this.measure3[MeasureValues.DataProvider] = "peeR39";
            this.measure4[MeasureValues.DataProvider] = null;

            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1, this.measure1 },
                    { 2, this.measure2 },
                    { 3, this.measure3 },
                    { 4, this.measure4 },
                });
            var measureInfo = new MeasureInfo(measureMap);

            var dataProviders = measureInfo.ExtractDataProviders(measureSet).Values.Distinct();
            Assert.AreEqual(3, measureInfo.DataProviderInfo.Keys.Intersect(dataProviders).Count());
        }
        
        /// <summary>
        /// Extract the canonical names for the data providers when a measure does not have a provider
        /// in the measure map
        /// </summary>
        [TestMethod]
        public void ExtractDataProvidersNull()
        {
            var measureSet = new MeasureSet { 1, 2 };

            this.measure1[MeasureValues.DataProvider] = "lotame";
            this.measure2[MeasureValues.DataProvider] = null;

            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1, this.measure1 },
                    { 2, this.measure2 },
                });
            var measureInfo = new MeasureInfo(measureMap);

            var dataProviders = measureInfo.ExtractDataProviders(measureSet).Values.Distinct();
            Assert.AreEqual(1, measureInfo.DataProviderInfo.Keys.Intersect(dataProviders).Count());
        }

        /// <summary>
        /// Canonical name for the data provider not found
        /// </summary>
        [TestMethod]
        public void ExtractDataProvidersNotFound()
        {
            var measureSet = new MeasureSet { 1 };

            this.measure1[MeasureValues.DataProvider] = "dataRus";

            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1, this.measure1 },
                });
            var measureInfo = new MeasureInfo(measureMap);
            var dataProviders = measureInfo.ExtractDataProviders(measureSet);
            Assert.AreEqual(0, dataProviders.Count);
        }
        
        /// <summary>
        /// Test for CalculateDataCost with multiple data providers
        /// </summary>
        [TestMethod]
        public void CalculateDataCostPercentOfSpend()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106001, this.measure1 },
                    { 1106002, this.measure2 },
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                    { 1106005, this.measure5 },
                    { 1106006, this.measure6 }
                });
            var measureInfo = new MeasureInfo(measureMap);
    
            var impressionCount = 100000;
            var milles = impressionCount / 1000;
            var mediaSpend = 100.00m;
            var measureSet = new MeasureSet { 1106001, 1106002, 1106003, 1106004, 1106005, 1106006 };
            var cost = measureInfo.CalculateTotalSpend(measureSet, impressionCount, mediaSpend, Margin, PerMilleFees);

            var expectedCost = Math.Round(((15 + (.75m * milles) + (.75m * milles) + mediaSpend) * Margin) + (PerMilleFees * milles), 2);
            Assert.AreEqual(expectedCost, Math.Round(cost, 2));
        }

        /// <summary>
        /// Test for CalculateDataCost where the peer39 only has cost per mille
        /// </summary>
        [TestMethod]
        public void CalculateDataCostMinCostPerMille()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106001, this.measure1 },
                    { 1106002, this.measure2 },
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                    { 1106005, this.measure5 },
                });
            var measureInfo = new MeasureInfo(measureMap);

            var impressionCount = 100000;
            var milles = impressionCount / 1000;
            var mediaSpend = 100.00m;
            var measureSet = new MeasureSet { 1106001, 1106002, 1106003, 1106004, 1106005 };
            var cost = measureInfo.CalculateTotalSpend(measureSet, impressionCount, mediaSpend, Margin, PerMilleFees);

            var expectedCost = Math.Round((((.05m * milles) + (.75m * milles) + (.75m * milles) + mediaSpend) * Margin) + (PerMilleFees * milles), 2);
            Assert.AreEqual(expectedCost, Math.Round(cost, 2));
        }

        /// <summary>
        /// Test for CalculateDataCost where there are only cost per mille costs
        /// </summary>
        [TestMethod]
        public void CalculateDataCostCostPerMille()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106001, this.measure1 },
                    { 1106002, this.measure2 },
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                });
            var measureInfo = new MeasureInfo(measureMap);

            var impressionCount = 100000;
            var milles = impressionCount / 1000;
            var mediaSpend = 100.00m;
            var measureSet = new MeasureSet { 1106001, 1106002, 1106003, 1106004 };
            var cost = measureInfo.CalculateTotalSpend(measureSet, impressionCount, mediaSpend, Margin, PerMilleFees);

            var expectedCost = Math.Round((((.75m * milles) + (.75m * milles) + mediaSpend) * Margin) + (PerMilleFees * milles), 2);
            Assert.AreEqual(expectedCost, Math.Round(cost, 2));
        }

        /// <summary>
        /// Test for CalculateDataCost where the minCpm for peer39 out costs the percentOfSpend cost
        /// </summary>
        [TestMethod]
        public void CalculateDataCostMinCostOutCostsSpend()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106001, this.measure1 },
                    { 1106002, this.measure2 },
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                    { 1106005, this.measure5 },
                    { 1106006, this.measure6 }
                });
            var measureInfo = new MeasureInfo(measureMap);

            var impressionCount = 1000000;
            var milles = impressionCount / 1000;
            var mediaSpend = 100.00m;
            var measureSet = new MeasureSet { 1106001, 1106002, 1106003, 1106004, 1106005, 1106006 };
            var cost = measureInfo.CalculateTotalSpend(measureSet, impressionCount, mediaSpend, Margin, PerMilleFees);

            var expectedCost = Math.Round((((.05m * milles) + (.75m * milles) + (.75m * milles) + mediaSpend) * Margin) + (PerMilleFees * milles), 2);
            Assert.AreEqual(expectedCost, Math.Round(cost, 2));
        }

        /// <summary>Test we can calculate media cost given total cost</summary>
        [TestMethod]
        public void CalculateMediaBudgetForMultipleProviders()
        {
            var measureMap = new MeasureMap(
                new Dictionary<long, IDictionary<string, object>>
                {
                    { 1106001, this.measure1 },
                    { 1106002, this.measure2 },
                    { 1106003, this.measure3 },
                    { 1106004, this.measure4 },
                    { 1106005, this.measure5 },
                    { 1106006, this.measure6 }
                });
            var measureInfo = new MeasureInfo(measureMap);
            var measureSet = new MeasureSet { 1106001, 1106002, 1106003, 1106004, 1106005, 1106006 };

            var availableBudget = 150;
            var previousMediaSpend = 100;
            var previousImpressions = 100000;
            var mediaBudget = measureInfo.CalculateMediaSpend(measureSet, availableBudget, previousMediaSpend, previousImpressions, Margin, PerMilleFees);

            Assert.AreEqual(47.20m, Math.Round(mediaBudget, 2));
        }

        /// <summary>Test we can calculate media cost given total cost with zero spend/impr</summary>
        [TestMethod]
        public void CalculateMediaBudgetForMultipleProvidersZeroOutputs()
        {
            var measureInfo = new MeasureInfo(this.measureMap);
            var measureSet = new MeasureSet { 1106001 };

            var availableBudget = 150;
            var previousMediaSpend = 0;
            var previousImpressions = 0;
            var mediaBudget = measureInfo.CalculateMediaSpend(measureSet, availableBudget, previousMediaSpend, previousImpressions, Margin, PerMilleFees);

            Assert.AreEqual(0m, Math.Round(mediaBudget, 2));
        }

        /// <summary>Test we can caldculate data cost (using the max cost method) for a measureset</summary>
        [TestMethod]
        public void CalculateDataCostMaxMethod()
        {
            // Measures with cost of .25, .75, 0
            // are 1106006, 1132749, 1374855 respectively
            var measureSet = new MeasureSet { 1106006, 1132749, 1374855 };
            Assert.AreEqual(0.75m, this.measureInfo.CalculateCostRateUsingMaxMethod(measureSet));
            measureSet = new MeasureSet { 1106006 };
            Assert.AreEqual(0.25m, this.measureInfo.CalculateCostRateUsingMaxMethod(measureSet));
            measureSet = new MeasureSet { 1374855 };
            Assert.AreEqual(0m, this.measureInfo.CalculateCostRateUsingMaxMethod(measureSet));
        }

        /// <summary>Helper method to determine if a data provider name is valid</summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <returns>True if valid</returns>
        private bool IsDataProviderValid(string dataProvider)
        {
            var result = false;
            foreach (var dataProviderName in this.measureInfo.DataProviderInfo.Keys)
            {
                result = result || MeasureInfo.CompareDataProviderName(dataProvider, dataProviderName);
            }

            return result;
        }
    }
}
