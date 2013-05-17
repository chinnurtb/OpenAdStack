// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsToExperimentsFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SellSideAllocation;

namespace SellSideAllocationEngineUnitTests
{
    /// <summary>
    /// Test fixture for the AllocateExportSlotsToExperiments class
    /// </summary>
    [TestClass]
    public class AllocateExportSlotsToExperimentsFixture
    {
        /// <summary> a layer for use in tests </summary>
        private static Node[] nodes;

        /// <summary> a list of layers for use in tests </summary>
        private static Node tier;

        /// <summary>
        /// Initialize some commomly used variables before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            nodes = new Node[10];

            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new Node
                {
                    ExperimentalPriorityScore = i
                };
            }

            tier = new Node
            {
                ExportSlots = 4,
                ChildNodes = nodes
            };
        }

        /// <summary>
        /// Test for the AllocateSlots method
        /// </summary>
        [TestMethod]
        public void AllocateSlotsTest()
        {
            var actual = AllocateExportSlotsToExperiments.AllocateSlots(tier);

            Assert.AreEqual(tier.ExportSlots, actual.ChildNodes.Sum(node => node.ExportSlots));
            Assert.IsTrue(new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1 }.SequenceEqual(nodes.Select(node => node.ExportSlots)));
        }
    }
}
