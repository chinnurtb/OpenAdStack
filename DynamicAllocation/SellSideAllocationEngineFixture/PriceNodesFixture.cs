// -----------------------------------------------------------------------
// <copyright file="PriceNodesFixture.cs" company="Rare Crowds Inc">
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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// test fixture for the PriceNodes class
    /// </summary>
    [TestClass]
    public class PriceNodesFixture
    {
        /// <summary> a layer for use in tests </summary>
        private static Node highValueAudience;

        /// <summary> a layer for use in tests </summary>
        private static Node exceptionalCrowds;

        /// <summary> a layer for use in tests </summary>
        private static Node rareCrowds;

        /// <summary> a list of layers for use in tests </summary>
        private static Node layers;

        /// <summary>
        /// Initialize some commomly used variables before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            highValueAudience = new Node 
            { 
                AverageCostPerMille = 5,
                HistoricalMaximumAchievableImpressionRate = 10, 
                FloorPrice = 5, 
                PriceCap = 15, 
                NumberOfEligibleNodes = 500000,
                ExportSlots = 10,
                DesiredAverageImpressionRate = 10
            };
            
            exceptionalCrowds = new Node 
            {
                AverageCostPerMille = 3,
                HistoricalMaximumAchievableImpressionRate = 100, 
                FloorPrice = 3, 
                PriceCap = 5,
                NumberOfEligibleNodes = 500000,
                ExportSlots = 20,
                DesiredAverageImpressionRate = 100 
            };

            rareCrowds = new Node 
            {
                AverageCostPerMille = 1,
                HistoricalMaximumAchievableImpressionRate = 1000, 
                FloorPrice = 1, 
                PriceCap = 2,
                NumberOfEligibleNodes = 500000,
                ExportSlots = 40,
                DesiredAverageImpressionRate = 1000
            };

            layers = new Node
            {
                ChildNodes = new Node[] { highValueAudience, exceptionalCrowds, rareCrowds },
                AverageCostPerMille = 2
           };

            layers.ExportSlots = layers.ChildNodes.Sum(layer => layer.ExportSlots);
            layers.DesiredAverageImpressionRate = layers.ChildNodes.Sum(layer => layer.TotalDesiredImpressionRate()) / layers.ExportSlots;
         }

        /// <summary>
        /// test for the CalculatePriceRequired method
        /// </summary>
        [TestMethod]
        public void CalculatePriceRequiredTestBasic()
        {
            var totalGraphSpendRate = layers.TotalDesiredSpendRate();
            var actual = PriceNodes.CalculatePriceRequired(layers.ChildNodes, 0, totalGraphSpendRate);

            // with these inputs we get 10*10*x + 40*1000*1 + 20*100*3 = 2*(10*10+40*1000+20*100) => x == 382
            Assert.AreEqual(382, actual);
            layers.ChildNodes[0].AverageCostPerMille = actual;
            this.AssertAveragePriceIsCorrect(layers, .005);
        }

        /// <summary>
        /// basic test for the PriceLayers method
        /// </summary>
        [TestMethod]
        public void PriceLayersTestBasic()
        {
            var actual = PriceNodes.PriceNodeChildren(layers);

            // with these inputs the top two layers should be at the price cap, the bottom should be in range, and average price should be correct
            Assert.AreEqual(actual.ChildNodes[0].PriceCap, actual.ChildNodes[0].AverageCostPerMille);
            Assert.AreEqual(actual.ChildNodes[1].PriceCap, actual.ChildNodes[1].AverageCostPerMille);
            Assert.IsTrue(actual.ChildNodes[2].AverageCostPerMille <= actual.ChildNodes[2].PriceCap);
            Assert.IsTrue(actual.ChildNodes[2].AverageCostPerMille >= actual.ChildNodes[2].FloorPrice);
            this.AssertAveragePriceIsCorrect(actual, .005);
        }

        /// <summary>
        /// basic test for the PriceLayers method
        /// </summary>
        [TestMethod]
        public void PriceLayersTestOneTier()
        {
            layers.ChildNodes = new Node[] { layers.ChildNodes[2] };
            layers.DesiredAverageImpressionRate = layers.ChildNodes.Sum(layer => layer.TotalDesiredImpressionRate()) / layers.ExportSlots;
            var actual = PriceNodes.PriceNodeChildren(layers);

            // with these inputs the single tier's price should be in range and equal to the average price.
            Assert.IsTrue(actual.ChildNodes[0].AverageCostPerMille <= actual.ChildNodes[0].PriceCap);
            Assert.IsTrue(actual.ChildNodes[0].AverageCostPerMille >= actual.ChildNodes[0].FloorPrice);
            this.AssertAveragePriceIsCorrect(actual, .005);
        }

        /// <summary>
        /// basic test for the PriceLayers method
        /// </summary>
        [TestMethod]
        public void PriceLayersTestOneTierWithExportSlots()
        {
            layers.ChildNodes[0].ExportSlots = 0;
            layers.ChildNodes[1].ExportSlots = 0;
            layers.DesiredAverageImpressionRate = layers.ChildNodes.Sum(layer => layer.TotalDesiredImpressionRate()) / layers.ExportSlots;
            var actual = PriceNodes.PriceNodeChildren(layers);

            // with these inputs the single tier's price should be in range and equal to the average price.
            Assert.IsTrue(actual.ChildNodes[2].AverageCostPerMille <= actual.ChildNodes[2].PriceCap);
            Assert.IsTrue(actual.ChildNodes[2].AverageCostPerMille >= actual.ChildNodes[2].FloorPrice);
            this.AssertAveragePriceIsCorrect(actual, .005);
        }

        /// <summary>
        /// Asserts that the average price is correct to within the threshold
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="threshold">the tolerance for equality</param>
        private void AssertAveragePriceIsCorrect(Node node, double threshold)
        {
            var averagePrice = node.ChildNodes.Sum(child => child.TotalDesiredSpendRate()) / node.ChildNodes.Sum(child => child.TotalDesiredImpressionRate());
            Assert.AreEqual((double)averagePrice, (double)node.AverageCostPerMille, threshold);
        }
    }
}
