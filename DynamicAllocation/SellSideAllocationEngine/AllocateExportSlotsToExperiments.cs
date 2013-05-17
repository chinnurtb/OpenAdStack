// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocateExportSlotsToExperiments.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SellSideAllocation
{
    /// <summary>
    /// Class for allocated export slots to experimental nodes according to their ExperimentalPriorityScore
    /// </summary>
    internal class AllocateExportSlotsToExperiments
    {
        /// <summary>
        /// Will distribute export slots among the nodes according to their ExperimentalPriorityScore
        /// </summary>
        /// <param name="tier">a node whose children have an ExperimentalPriorityScore and who have a NumberOfEligibleNodes == 1</param>
        /// <returns>the tier with the ExportSlots populated</returns>
        internal static Node AllocateSlots(Node tier)
        {
            // choose among eligible unchosen nodes according to their ExperimentalPriorityScore
            var experimentalNodes = tier
                .ChildNodes
                .OrderByDescending(node => node.ExperimentalPriorityScore)
                .Take(tier.ExportSlots);

            foreach (var node in experimentalNodes)
            {
                node.ExportSlots = 1;
            }

            return tier;
        }
    }
}
