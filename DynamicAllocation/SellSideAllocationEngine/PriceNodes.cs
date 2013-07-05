// -----------------------------------------------------------------------
// <copyright file="PriceNodes.cs" company="Rare Crowds Inc">
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
    /// Calss for pricing nodes
    /// </summary>
    internal static class PriceNodes
    {
        /// <summary>
        /// Gives each child node an average price
        /// </summary>
        /// <param name="node">the node whose children are to be priced (assumed to be sorted by AverageValue descending)</param>
        /// <returns>the node with the child node's average price populated</returns>
        public static Node PriceNodeChildren(Node node)
        {
            // filter out layers that don't have any exportSlots (since their price doesn't matter)
            // TODO: consider the case where no layers have export slots
            var nodesExportSlots = node.ChildNodes.Where(childNode => childNode.ExportSlots > 0).ToArray();

            // set all layers to the floor price, except the top value layer, which we set as high as needed up to the cap,
            // cascading down until our averageCpm goal is met
            foreach (var nodeExportSlots in nodesExportSlots)
            {
                nodeExportSlots.AverageCostPerMille = nodeExportSlots.FloorPrice;
            }

            var indexOfCurrentGroupBeingPriced = 0;
            var totalGraphSpendRate = node.TotalDesiredSpendRate();
            var priceRequired = CalculatePriceRequired(nodesExportSlots, indexOfCurrentGroupBeingPriced, totalGraphSpendRate);

            // loop over the layers, setting the prices to the cap until we reach a layer whose required price is <= the cap
            // (lower tiers will have the floor price)
            // TODO: consider checking if requiredAverageCostPerMille is compatible with the price caps
            while (priceRequired > nodesExportSlots[indexOfCurrentGroupBeingPriced].PriceCap)
            {
                nodesExportSlots[indexOfCurrentGroupBeingPriced].AverageCostPerMille = nodesExportSlots[indexOfCurrentGroupBeingPriced].PriceCap;

                indexOfCurrentGroupBeingPriced++;
                priceRequired = CalculatePriceRequired(nodesExportSlots, indexOfCurrentGroupBeingPriced, totalGraphSpendRate);
            }

            nodesExportSlots[indexOfCurrentGroupBeingPriced].AverageCostPerMille = (decimal)priceRequired;
            return node;
        }

        /// <summary>
        /// Calculates the price required for the layer so totalGraphSpendRate is satisfied
        /// </summary>
        /// <param name="layers">the layers</param>
        /// <param name="indexOfCurrentLayerBeingPriced">the index of current layer Being priced</param>
        /// <param name="totalGraphSpendRate">the total graph spend rate</param>
        /// <returns>the price required</returns>
        internal static decimal CalculatePriceRequired(Node[] layers, int indexOfCurrentLayerBeingPriced, decimal totalGraphSpendRate)
        {
            var spendOfOtherLayers = layers
                .Where(layer => layer != layers[indexOfCurrentLayerBeingPriced])
                .Sum(layer => layer.TotalDesiredSpendRate());

            var totalImpressionRateOfCurrentLayer = layers[indexOfCurrentLayerBeingPriced].TotalDesiredImpressionRate();

            // todo: consider what to do if this method is called with a zero totalImpressionRateOfCurrentLayer
            return (totalGraphSpendRate - spendOfOtherLayers) / totalImpressionRateOfCurrentLayer;
        }
    }
}
