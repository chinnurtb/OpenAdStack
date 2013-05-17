// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsQuicklyFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// test fixture for the AllocateExportSlotsQuickly class
    /// </summary>
    [TestClass]
    public class AllocateExportSlotsQuicklyFixture
    {   
        /// <summary>
        /// basic test for the AllocateSlots method with large inputs
        /// </summary>
        [TestMethod]
        public void AllocateSlotsTestBasicLarge()
        {
            // create a tier with a bunch of nodes (with a predictable result)
            var tiers = new Node
            {
                AverageCostPerMille = 2,
                DesiredAverageImpressionRate = 9000,
                ExportSlots = 1000
            };

            var childNodes = new List<Node> 
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

                childNodes.Add(dummyLayer);
            }
            
            tiers.ChildNodes = childNodes.ToArray();

            var actual = AllocateExportSlotsQuickly.AllocateSlots(tiers);
            var actualPartition = childNodes.Select(node => node.ExportSlots).ToArray();
            var expectedPartition = new int[] { 0, 0, 0, 111, 889 }.Concat(new int[95]);

            // assert optimization was performed correctly given the inputs
            Assert.AreEqual(expectedPartition.Count(), actualPartition.Count());
            Assert.IsTrue(expectedPartition.Zip(actualPartition, (i, j) => i == j).All(b => b));
        }
    }
}
