// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocateExportSlotsToExperiments.cs" company="Rare Crowds Inc">
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
