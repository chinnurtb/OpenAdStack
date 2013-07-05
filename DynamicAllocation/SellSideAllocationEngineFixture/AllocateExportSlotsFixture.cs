// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsFixture.cs" company="Rare Crowds Inc">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// test fixture for the AllocateExportSlots class
    /// </summary>
    [TestClass]
    public class AllocateExportSlotsFixture
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
        /// Test for the ExportSlotsAvailableInLayers method where it should be true
        /// </summary>
        [TestMethod]
        public void ExportSlotsAvailableInlayersTestPass()
        {
            var partition = new int[] { 500000, 500000, 500000 };
            var actual = AllocateExportSlots.ExportSlotsAreAvailableInLayers(partition, layers);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Test for the ExportSlotsAvailableInLayers method where it should be false in various ways
        /// </summary>
        [TestMethod]
        public void ExportSlotsAvailableInlayersTestFail()
        {
            var partition = new int[] { 500001, 500000, 500000 };
            var actual1 = AllocateExportSlots.ExportSlotsAreAvailableInLayers(partition, layers);

            partition = new int[] { 500000, 500001, 500000 };
            var actual2 = AllocateExportSlots.ExportSlotsAreAvailableInLayers(partition, layers);

            partition = new int[] { 500000, 500000, 500001 };
            var actual3 = AllocateExportSlots.ExportSlotsAreAvailableInLayers(partition, layers);

            Assert.IsFalse(actual1);
            Assert.IsFalse(actual2);
            Assert.IsFalse(actual3);
        }

        /// <summary>
        /// Test for the IsAbleToMakeDesiredVolume method where it should return true
        /// </summary>
        [TestMethod]
        public void IsAbleToMakeDesiredVolumeTestPass()
        {
            var partition = new int[] { 0, 9, 1 };
            layers.DesiredAverageImpressionRate = 190;

            var actual = AllocateExportSlots.IsAbleToMakeDesiredVolume(partition, layers);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Test for the IsAbleToMakeDesiredVolume method where it should return false
        /// </summary>
        [TestMethod]
        public void IsAbleToMakeDesiredVolumeTestFail()
        {
            var partition = new int[] { 1, 8, 1 };
            layers.DesiredAverageImpressionRate = 190;
            var actual = AllocateExportSlots.IsAbleToMakeDesiredVolume(partition, layers);

            Assert.IsFalse(actual);
        }

        /// <summary>
        /// Test for the IsCompatibleWithFloorPrices method where it should return true
        /// </summary>
        [TestMethod]
        public void IsCompatibleWithFloorPricesTestPass()
        {
            var partition = new int[] { 2, 7, 1 };
            layers.DesiredAverageImpressionRate = 2 + (7 * 10) + 100;
            var actual = AllocateExportSlots.IsCompatibleWithFloorPrices(partition, layers);

            Assert.IsTrue(actual);
        }

        /// <summary>
        /// Test for the IsCompatibleWithFloorPrices method where it should return false
        /// </summary>
        [TestMethod]
        public void IsCompatibleWithFloorPricesTestFail()
        {
            var partition = new int[] { 3, 7, 0 };
            layers.DesiredAverageImpressionRate = 3 + (7 * 10);
            var actual = AllocateExportSlots.IsCompatibleWithFloorPrices(partition, layers);

            Assert.IsFalse(actual);
        }
    }
}
