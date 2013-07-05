// -----------------------------------------------------------------------
// <copyright file="AllocateExportSlots.cs" company="Rare Crowds Inc">
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
    /// Class for methods common to AllocateExportSlots classes
    /// </summary>
    internal static class AllocateExportSlots
    {
        /// <summary>
        /// Gives each child node of the parent input node an average desired volume
        /// </summary>
        /// <param name="node">the parent node whose children are to be volumed (assumed sorted by AverageValue descending)</param>
        /// <returns>the parent nose whose children now have the average desired volume populated</returns>
        public static Node VolumeBudgetLayers(Node node)
        {
            // filter out layers that don't have any exportSlots (since they should not recieve volume)
            var layersWithExportSlots = node.ChildNodes.Where(layer => layer.ExportSlots > 0).ToList();

            var volumeAvaialable = node.TotalDesiredImpressionRate();

            // set all the layers with export slots to the max volume they can take
            foreach (var layer in layersWithExportSlots)
            {
                layer.DesiredAverageImpressionRate = layer.HistoricalMaximumAchievableImpressionRate;
            }

            // correct the least valuable layer's volume to something smaller than its max if the available volume is smaller (as would usually be the case)
            // todo: this should always be the correct thing to do if the rest of the calculation is correct - make sure this is tested to be the case.
            layersWithExportSlots.Last().DesiredAverageImpressionRate +=
                (volumeAvaialable - layersWithExportSlots.Sum(layer => layer.TotalDesiredImpressionRate())) /
                layersWithExportSlots.Last().ExportSlots;

            return node;
        }

        /// <summary>
        /// Calculates if the partition satifies the constraints
        /// </summary>
        /// <param name="partition">the partition</param>
        /// <param name="node">the layers</param>
        /// <returns>true is the partition is feasible</returns>
        public static bool PartitionIsFeasible(int[] partition, Node node)
        {
            return ExportSlotsAreAvailableInLayers(partition, node) &&
                IsAbleToMakeDesiredVolume(partition, node) &&
                IsCompatibleWithFloorPrices(partition, node);
        }

        /// <summary>
        /// Detrmines whether there are enough export slots available in all layers given the partition
        /// </summary>
        /// <param name="partition">the partition under consideration</param>
        /// <param name="node">the parent node</param>
        /// <returns>true if there are enough export slots</returns>
        internal static bool ExportSlotsAreAvailableInLayers(int[] partition, Node node)
        {
            return partition.Zip(node.ChildNodes, (exportSlots, layer) => exportSlots <= layer.NumberOfEligibleNodes).All(boolResult => boolResult);
        }

        /// <summary>
        /// Determines whether there is enough volume avaialable in the layers given the partition
        /// </summary>
        /// <param name="partition">the partition under consideration</param>
        /// <param name="node">the parent node</param>
        /// <returns>true if there is enough volume</returns>
        internal static bool IsAbleToMakeDesiredVolume(int[] partition, Node node)
        {
            return (decimal)partition.Zip(node.ChildNodes, (exportSlots, layer) => exportSlots * layer.HistoricalMaximumAchievableImpressionRate).Sum() >=
                node.TotalDesiredImpressionRate();
        }

        /// <summary>
        /// Determines if the given partition is compatible with the floor prices
        /// </summary>
        /// <param name="partition">the partition under consideration</param>
        /// <param name="node">the parent node</param>
        /// <returns>true if the partition is compatible with the floor prices</returns>
        internal static bool IsCompatibleWithFloorPrices(int[] partition, Node node)
        {
            var indexOfLeastValuableLayerWithExportSlots = 0;
            var totalSpendRateOfTheLayers = 0m;
            var totalVolumeRateOfTheLayers = 0m;

            // don't include the last one that has a non-zero exportSlot count
            var skip = true;
            for (var i = partition.Length - 1; i >= 0; i--)
            {
                if (!skip)
                {
                    totalVolumeRateOfTheLayers += partition[i] * node.ChildNodes[i].HistoricalMaximumAchievableImpressionRate;
                    totalSpendRateOfTheLayers += partition[i] *
                        node.ChildNodes[i].HistoricalMaximumAchievableImpressionRate *
                        node.ChildNodes[i].FloorPrice;
                }
                else if (partition[i] > 0)
                {
                    skip = false;
                    indexOfLeastValuableLayerWithExportSlots = i;
                }
            }

            var volumeOfLeastValuableLayerWithExportSlots = node.TotalDesiredImpressionRate() - totalVolumeRateOfTheLayers;

            totalSpendRateOfTheLayers += volumeOfLeastValuableLayerWithExportSlots *
                node.ChildNodes[indexOfLeastValuableLayerWithExportSlots].FloorPrice;

            return totalSpendRateOfTheLayers <= node.TotalDesiredSpendRate();
        }

        /// <summary>
        /// The function we are maximizing
        /// </summary>
        /// <param name="partition">the currently considered partition of export slots</param>
        /// <param name="node">the layers</param>
        /// <returns>the score</returns>
        internal static decimal LayerAllocationScore(int[] partition, Node node)
        {
            // TODO: reduce volume of lowest value layer to reflect total volume correctly
            return partition.Zip(node.ChildNodes, (exportSlots, layer) => exportSlots * layer.HistoricalMaximumAchievableImpressionRate * layer.AverageValue).Sum();
        }
    }
}
