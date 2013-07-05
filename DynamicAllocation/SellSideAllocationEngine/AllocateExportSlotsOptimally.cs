// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlotsOptimally.cs" company="Rare Crowds Inc">
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
using System.Text;

namespace SellSideAllocation
{
    /// <summary>
    /// Class for optimally allocating export slots
    /// </summary>
    internal static class AllocateExportSlotsOptimally
    {
        /// <summary>
        /// Will distribute export slots among the layers to maximize the total value
        /// </summary>
        /// <param name="node">a node whose children have the AverageValue populated sorted by AverageValue descending</param>
        /// <returns>the list of Layers with the ExportSlots populated to maximize the total value</returns>
        internal static Node AllocateSlots(Node node)
        {
            var bestPartition = new int[node.ChildNodes.Count()];
            var bestScore = 0.0m;

            // loop over all possible partitions of export slots into the layers and find the best feasible partition.
            foreach (var partition in PartitionExportSlots(node.ExportSlots, node.ChildNodes.Count()))
            {
                var layerScoreForPartition = AllocateExportSlots.LayerAllocationScore(partition, node);
                if (layerScoreForPartition > bestScore &&
                    AllocateExportSlots.PartitionIsFeasible(partition, node))
                {
                    bestScore = layerScoreForPartition;
                    bestPartition = partition;
                }
            }

            for (var i = 0; i < bestPartition.Length; i++)
            {
                node.ChildNodes[i].ExportSlots = bestPartition[i];
            }

            node = AllocateExportSlots.VolumeBudgetLayers(node);

            return node;
        }

        /// <summary>
        /// Generates all the partitions of the export slots into the layers
        /// </summary>
        /// <param name="numberOfExportSlots">the numberOfExportSlots</param>
        /// <param name="numberOfLayers">the numberOfLayers</param>
        /// <returns>an enumerable of partitions</returns>
        internal static IEnumerable<int[]> PartitionExportSlots(int numberOfExportSlots, int numberOfLayers)
        {
            var partition = new int[numberOfLayers - 1];
            var max = numberOfExportSlots;
            partition[0] = 0;
            var sum = 0;
            var index = 0;

            while (true)
            {
                if (sum > max)
                {
                    sum -= partition[index];
                    partition[index] = 0;
                    index++;

                    if (index == numberOfLayers - 1)
                    {
                        break;
                    }
                }
                else
                {
                    index = 0;
                    yield return new int[] { numberOfExportSlots - sum }.Concat(partition).ToArray();
                }

                partition[index]++;
                sum++;
            }
        }
    }
}
