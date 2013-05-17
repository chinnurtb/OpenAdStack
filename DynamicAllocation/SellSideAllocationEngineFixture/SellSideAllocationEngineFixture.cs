// -----------------------------------------------------------------------
// <copyright file="SellSideAllocationEngineFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// test fixture for the sell side dynamic allocation engine
    /// </summary>
    [TestClass]
    public class SellSideAllocationEngineFixture
    {
        /// <summary> a layer for use in tests </summary>
        private static Node highValueAudience;

        /// <summary> a layer for use in tests </summary>
        private static Node exceptionalCrowds;

        /// <summary> a layer for use in tests </summary>
        private static Node rareCrowds;

        /// <summary> a list of layers for use in tests </summary>
        private static Node layers;

        /// <summary>a SellSideAllocationEngine for use in tests </summary>
        private static SellSideAllocationEngine sellSideAllocationEngine;

        /// <summary> the fraction of the campaign that is left </summary>
        private static double fractionOfCampaignLeft; 

        /// <summary>
        /// Initialize some commomly used variables before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            highValueAudience = new Node { AverageValue = 1, HistoricalMaximumAchievableImpressionRate = 10, FloorPrice = 5, PriceCap = 15, NumberOfEligibleNodes = 500000 };
            exceptionalCrowds = new Node { AverageValue = 0, HistoricalMaximumAchievableImpressionRate = 100, FloorPrice = 3, PriceCap = 5, NumberOfEligibleNodes = 500000 };
            rareCrowds = new Node { AverageValue = 0, HistoricalMaximumAchievableImpressionRate = 1000, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 500000 };
            layers = new Node 
            { 
                AverageCostPerMille = 2, 
                ExportSlots = 10, 
                DesiredAverageImpressionRate = 200,
                ChildNodes = new Node[] { highValueAudience, exceptionalCrowds, rareCrowds } 
            };

            fractionOfCampaignLeft = .5;
            sellSideAllocationEngine = new SellSideAllocationEngine(layers, fractionOfCampaignLeft);
        }

        /// <summary>
        /// test for the AllocateResourcesToLayers method
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToLayersTestSmallHighValueAudienceOnly()
        {
            var actual = sellSideAllocationEngine.AllocateResourcesToLayers();

            Assert.AreEqual(layers.ExportSlots, actual.ChildNodes.Sum(layer => layer.ExportSlots));
            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <= 
                actual.ChildNodes.Sum(layer => layer.MaximumAchievableTotalImpressionRate()));
            
            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(8, highValueAudience.ExportSlots);

            // assert prices are correct
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille <= layer.PriceCap));
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille >= layer.FloorPrice || layer.AverageCostPerMille == 0));
            Assert.AreEqual(highValueAudience.PriceCap, highValueAudience.AverageCostPerMille);
            Assert.AreEqual(
                (double)layers.TotalDesiredSpendRate(),
                (double)actual.ChildNodes.Sum(layer => layer.TotalDesiredSpendRate()),
                .01);
        }

        /// <summary>
        /// test for the AllocateResourcesToLayers method
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToLayersTestLargeHighValueAudienceOnly()
        {
            layers.ExportSlots = 500;
          
            var actual = sellSideAllocationEngine.AllocateResourcesToLayers();

            Assert.AreEqual(500, actual.ChildNodes.Sum(layer => layer.ExportSlots));
            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <=
                actual.ChildNodes.Sum(layer => layer.MaximumAchievableTotalImpressionRate()));

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(404, highValueAudience.ExportSlots);

            // assert prices are correct
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille <= layer.PriceCap));
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille >= layer.FloorPrice || layer.AverageCostPerMille == 0));
            Assert.AreEqual(highValueAudience.PriceCap, highValueAudience.AverageCostPerMille);
            Assert.AreEqual(
                (double)layers.TotalDesiredSpendRate(),
                (double)actual.ChildNodes.Sum(layer => layer.TotalDesiredSpendRate()),
                .01);
        }

        /// <summary>
        /// test for the AllocateResourcesToLayers method
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToLayersTestLarge()
        {
            highValueAudience.AverageValue = 101.1m;
            exceptionalCrowds.AverageValue = 10.1m;
            rareCrowds.AverageValue = 1;

            layers.ExportSlots = 500;

            var actual = sellSideAllocationEngine.AllocateResourcesToLayers();
            
            Assert.AreEqual(500, actual.ChildNodes.Sum(layer => layer.ExportSlots));
            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <=
                actual.ChildNodes.Sum(layer => layer.MaximumAchievableTotalImpressionRate()));

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(4, highValueAudience.ExportSlots);
            Assert.AreEqual(440, exceptionalCrowds.ExportSlots);
            Assert.AreEqual(56, rareCrowds.ExportSlots);

            // assert prices are correct
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille <= layer.PriceCap));
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille >= layer.FloorPrice || layer.AverageCostPerMille == 0));
            Assert.AreEqual(highValueAudience.PriceCap, highValueAudience.AverageCostPerMille);
            Assert.AreEqual(
                (double)layers.TotalDesiredSpendRate(),
                (double)actual.ChildNodes.Sum(layer => layer.TotalDesiredSpendRate()),
                .01);
        }

        /// <summary>
        /// test for the AllocateResourcesToLayers method
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToLayersTestFourLayers()
        {
            highValueAudience.AverageValue = 200;
            exceptionalCrowds.AverageValue = 20;
            rareCrowds.AverageValue = 1;
            var display = new Node { AverageValue = 0, HistoricalMaximumAchievableImpressionRate = 10000, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 500 };

            var childNodes = layers.ChildNodes.ToList();
            childNodes.Add(display);
            layers.ChildNodes = childNodes.ToArray();
            layers.ExportSlots = 100;

            var actual = sellSideAllocationEngine.AllocateResourcesToLayers();

            Assert.AreEqual(100, actual.ChildNodes.Sum(layer => layer.ExportSlots));
            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <=
                actual.ChildNodes.Sum(layer => layer.ExportSlots * layer.HistoricalMaximumAchievableImpressionRate));

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(8, highValueAudience.ExportSlots);
            Assert.AreEqual(90, exceptionalCrowds.ExportSlots);
            Assert.AreEqual(1, rareCrowds.ExportSlots);
            Assert.AreEqual(1, display.ExportSlots);

            // assert prices are correct
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille <= layer.PriceCap));
            Assert.IsTrue(layers.ChildNodes.All(layer => layer.AverageCostPerMille >= layer.FloorPrice || layer.AverageCostPerMille == 0));
            Assert.AreEqual(highValueAudience.PriceCap, highValueAudience.AverageCostPerMille);
            Assert.AreEqual(rareCrowds.FloorPrice, rareCrowds.AverageCostPerMille);

            var actualBudgetSum = (double)actual.ChildNodes.Sum(
                    layer =>
                    layer.TotalDesiredSpendRate());

            Assert.AreEqual(
                (double)layers.TotalDesiredSpendRate(),
                actualBudgetSum,
                .01);
        }

        /// <summary>
        /// basic test for the AllocateResourcesToTiers method with large inputs
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToTiersTestBasicLarge()
        {
            // create a layer with a bunch of tiers (with a predictable result)
            var tiers = new List<Node> 
            { 
                new Node { AverageValue = 100001, HistoricalMaximumAchievableImpressionRate = 1, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 1000 },
                new Node { AverageValue = 10000, HistoricalMaximumAchievableImpressionRate = 10, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 1000 },
                new Node { AverageValue = 1, HistoricalMaximumAchievableImpressionRate = 100, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 1000 },
                new Node { AverageValue = 1, HistoricalMaximumAchievableImpressionRate = 1000, FloorPrice = 1, PriceCap = 2, NumberOfEligibleNodes = 1000 },
            };

            Node dummyLayer;

            for (var i = 0; i < 96; i++)
            {
                dummyLayer = new Node
                {
                    AverageValue = 0,
                    HistoricalMaximumAchievableImpressionRate = 10000,
                    FloorPrice = 1,
                    PriceCap = 2,
                    NumberOfEligibleNodes = 1000
                };
                tiers.Add(dummyLayer);
            }
            
            layers.DesiredAverageImpressionRate = 9000;
            layers.ExportSlots = 1000;
            layers.ChildNodes = tiers.ToArray();

            var actual = SellSideAllocationEngine.AllocateResourcesToTiers(layers);

            Assert.AreEqual(1000, actual.ChildNodes.Sum(tier => tier.ExportSlots));

            var impressionRateSum = actual.ChildNodes.Sum(tier => tier.MaximumAchievableTotalImpressionRate());

            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <=
                impressionRateSum);

            var actualPartition = tiers.Select(node => node.ExportSlots).ToArray();
            var expectedPartition = new int[] { 0, 0, 0, 111, 889 }.Concat(new int[95]);

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(expectedPartition.Count(), actualPartition.Count());
            Assert.IsTrue(expectedPartition.Zip(actualPartition, (i, j) => i == j).All(b => b));
         
            // assert prices are correct
            Assert.IsTrue(tiers.All(tier => tier.AverageCostPerMille <= tier.PriceCap));
            Assert.IsTrue(tiers.All(tier => tier.AverageCostPerMille >= tier.FloorPrice || tier.AverageCostPerMille == 0));
            Assert.AreEqual(
                (double)layers.TotalDesiredSpendRate(),
                (double)actual.ChildNodes.Sum(tier => tier.TotalDesiredSpendRate()),
                .01);
        }

        /// <summary>
        /// test for the AllocateResourcesToNodes method
        /// </summary>
        [TestMethod]
        public void AllocateResourcesToNodesTest()
        {
            // create a layer with a bunch of nodes
            var nodes = new List<Node>();
            var tier = new Node();

            for (var i = 0; i < 100; i++)
            {
                var node = new Node
                {
                    AverageValue = 0,
                    HistoricalMaximumAchievableImpressionRate = 10000,
                    FloorPrice = 1,
                    PriceCap = 2,
                    NumberOfEligibleNodes = 1,
                    ExportCount = i < 50 ? 0 : i
                };
                nodes.Add(node);
            }

            tier.DesiredAverageImpressionRate = 9000;
            tier.ExportSlots = 30;
            tier.ChildNodes = nodes.ToArray();

            var actual = sellSideAllocationEngine.AllocateResourcesToNodes(tier);

            Assert.AreEqual(Math.Round(actual.ExportSlots * fractionOfCampaignLeft, 0), actual.ChildNodes.Where(node => node.ExportSlots == 1).Count());
            Assert.IsTrue(actual.ChildNodes.Where(node => node.ExportCount == 0).Count() > 0);
            Assert.IsTrue(actual.ChildNodes.Where(node => node.ExportCount > 0).Count() > 0);
        }

        /// <summary>
        /// test for the CalculateFractionThatAreReexports method
        /// </summary>
        [TestMethod]
        public void CalculateFractionThatAreNewExports()
        {
            Assert.AreEqual(fractionOfCampaignLeft, sellSideAllocationEngine.CalculateFractionThatAreNewExports());
        }
    }
}
