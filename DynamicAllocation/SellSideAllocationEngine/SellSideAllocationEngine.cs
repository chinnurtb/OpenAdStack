// -----------------------------------------------------------------------
// <copyright file="SellSideAllocationEngine.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace SellSideAllocation
{
    /// <summary>
    /// Class for the sell side version of the Dynamic Allocation Engine
    /// </summary>
    internal class SellSideAllocationEngine
    {
        /// <summary>
        /// The graph root for the layers/tiers/node hierarchy.
        /// </summary>
        private Node layers;

        /// <summary>
        /// The percent of time left in the campaign run. Used for calcualting the experimentation schedule.
        /// </summary>
        private double fractionOfCampaignLeft;

        /// <summary>
        /// Initializes a new instance of the SellSideAllocationEngine class
        /// </summary>
        /// <param name="layers">a list of Layers with the AverageValue for the layer populated sorted by AverageValue descending</param>
        /// <param name="fractionOfCampaignLeft">the fraction of the campaign runtime that is left</param>
        public SellSideAllocationEngine(Node layers, double fractionOfCampaignLeft)
        {
            this.layers = layers;
            this.fractionOfCampaignLeft = fractionOfCampaignLeft;
        }

        /// <summary>
        /// Will distribute budget and export slots among the tiers to maximize the total value
        /// </summary>
        /// <param name="layer">the layer whose tiers we wish to allocate resources to</param>
        /// <returns>the layer with the ExportSlots and AverageCostPerMille populated to maximize the total value</returns>
        internal static Node AllocateResourcesToTiers(Node layer)
        {
            layer = AllocateExportSlotsQuickly.AllocateSlots(layer);
            return PriceNodes.PriceNodeChildren(layer);
        }

        /// <summary>
        /// Will distribute budget and export slots among the layers to maximize the total value
        /// </summary>
        /// <returns>the layers with the ExportSlots and AverageCostPerMille populated to maximize the total value</returns>
        internal Node AllocateResourcesToLayers()
        {
            this.layers = AllocateExportSlotsOptimally.AllocateSlots(this.layers);
            return PriceNodes.PriceNodeChildren(this.layers);
        }

        /// <summary>
        ///  Will distribute budget and export slots among the nodes of a tier to maximize the total value and follow the 
        ///  experimentation schedule. nodes are expected to have their NumberOfEligibleNodes set to 1 (if they are, in fact, eligible).
        /// </summary>
        /// <param name="tier">the tier whose nodes we are allocating resources to</param>
        /// <returns>The tier node with the ExportSlots and AverageCostPerMille populated to maximize the total value</returns>
        internal Node AllocateResourcesToNodes(Node tier)
        {
            var originalNumberOfExportSlots = tier.ExportSlots;
            var originalChildren = tier.ChildNodes;

            var numberOfReexports = (int)Math.Round(this.CalculateFractionThatAreNewExports() * originalNumberOfExportSlots);

            if (numberOfReexports > 0)
            {
                // allocate the reexports
                // TODO: a special purpose AllocateSlots intended for leaf nodes could be made faster. Do this if needed.
                tier.ExportSlots = numberOfReexports;
                tier.ChildNodes = originalChildren.Where(node => node.ExportCount > 0).ToArray();
                tier = AllocateExportSlotsQuickly.AllocateSlots(tier);
            }

            // allocate the experiments
            tier.ExportSlots = originalNumberOfExportSlots - numberOfReexports;
            tier.ChildNodes = originalChildren.Where(node => node.ExportCount == 0).ToArray();
            tier = AllocateExportSlotsToExperiments.AllocateSlots(tier);

            tier.ExportSlots = originalNumberOfExportSlots;
            tier.ChildNodes = originalChildren;

            return tier;
        }

        /// <summary>
        /// Calculates the fraction of the node exports that should be reexported nodes
        /// </summary>
        /// <returns>the fraction of the node exports that should be reexported nodes</returns>
        internal double CalculateFractionThatAreNewExports()
        {
            // for now this will be the fraction of the campaign that has passed
            // TODO: make the slope (at least) configurable and possibly different per tier.
            return this.fractionOfCampaignLeft;
        }
    }
}
