// -----------------------------------------------------------------------
// <copyright file="Node.cs" company="Rare Crowds Inc">
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
    /// Class for layer level allocation information
    /// </summary>
    internal class Node
    {
        /// <summary>
        /// List of child nodes for this node (kept sorted by average value descending)
        /// </summary>
        private Node[] childNodes;

        /// <summary>
        /// Gets or sets the list of child nodes of this node (should be sorted by average value descending)
        /// </summary>
        public Node[] ChildNodes 
        { 
            get
            {
                return this.childNodes;
            }

            set
            {
                // the child nodes are expected to be maintained in sorted order.
                // TODO: may want to implement a node list class in order to strictly enforce this.
                this.childNodes = value.OrderByDescending(child => child.AverageValue).ToArray();
            }
        }        
        
        /// <summary>
        /// Gets or sets the AverageValue
        /// </summary>
        public decimal AverageValue { get; set; }

        /// <summary>
        /// Gets or sets the ExportSlots
        /// </summary>
        public int ExportSlots { get; set; }

        /// <summary>
        /// Gets or sets the HistoricalMaximumAchievableImpressionRate
        /// </summary>
        public decimal HistoricalMaximumAchievableImpressionRate { get; set; }

        /// <summary>
        /// Gets or sets the DesiredAverageImpressionRate
        /// </summary>
        public decimal DesiredAverageImpressionRate { get; set; }

        /// <summary>
        /// Gets or sets the AverageCostPerMille
        /// </summary>
        public decimal AverageCostPerMille { get; set; }

        /// <summary>
        ///  Gets or sets the lowest average price a layer is allowed to recieve
        /// </summary>
        public decimal FloorPrice { get; set; }

        /// <summary>
        ///  Gets or sets the highest average price a layer is allowed to recieve
        /// </summary>
        public decimal PriceCap { get; set; }

        /// <summary>
        ///  Gets or sets the number of eligible nodes on the layer
        /// </summary>
        public decimal NumberOfEligibleNodes { get; set; }

        /// <summary>
        /// Gets or sets the eperimental priority score. 
        /// This is used to determine inter-tier priority among nodes for experimentation.
        /// </summary>
        public double ExperimentalPriorityScore { get; set; } 
        
        /// <summary>
        /// Gets or sets the export count. 
        /// </summary>
        public double ExportCount { get; set; } 

        /// <summary>
        /// The desired total impression rate of the node (in milles)
        /// </summary>
        /// <returns>the total desired impression rate for the node</returns> 
        public decimal TotalDesiredImpressionRate()
        {
            return this.ExportSlots * this.DesiredAverageImpressionRate;
        }

        /// <summary>
        /// The maximum achievable total impression rate of the node (in milles)
        /// </summary>
        /// <returns>the maximum achievable impression rate for the node</returns> 
        public decimal MaximumAchievableTotalImpressionRate()
        {
            return this.ExportSlots * this.HistoricalMaximumAchievableImpressionRate;
        }

        /// <summary>
        /// The desired total spend rate of the node
        /// </summary>
        /// <returns>the total desired spend rate for the node</returns> 
        public decimal TotalDesiredSpendRate()
        {
            return this.TotalDesiredImpressionRate() * this.AverageCostPerMille;
        }
    }
}
