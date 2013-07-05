// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsToExperimentsFixture.cs" company="Rare Crowds Inc">
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
