// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsOptimallyFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// test fixture for the AllocateExportSlotsOptimally class
    /// </summary>
    [TestClass]
    public class AllocateExportSlotsOptimallyFixture
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
        }

        /// <summary>
        /// test for the PartitionExportSlots method
        /// </summary>
        [TestMethod]
        public void PartitionExportSlotsTest()
        {
            var actual = AllocateExportSlotsOptimally.PartitionExportSlots(10, 4).ToList();

            // 10 + (4-1) choose (4-1)
            Assert.AreEqual(286, actual.Count);
            Assert.IsTrue(actual.All(partition => partition.Sum() == 10));
        }

        /// <summary>
        /// test for the AllocateSlots method
        /// </summary>
        [TestMethod]
        public void AllocateSlotsTestBasic()
        {
            highValueAudience.AverageValue = 200;
            exceptionalCrowds.AverageValue = 20;
            rareCrowds.AverageValue = 1;
            layers.ExportSlots = 100;

            var actual = AllocateExportSlotsOptimally.AllocateSlots(layers);

            Assert.AreEqual(100, actual.ChildNodes.Sum(layer => layer.ExportSlots));
            Assert.IsTrue(
                layers.ExportSlots * layers.DesiredAverageImpressionRate <=
                actual.ChildNodes.Sum(layer => layer.ExportSlots * layer.HistoricalMaximumAchievableImpressionRate));

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(8, highValueAudience.ExportSlots);
            Assert.AreEqual(80, exceptionalCrowds.ExportSlots);
            Assert.AreEqual(12, rareCrowds.ExportSlots);
        }
    }
}
