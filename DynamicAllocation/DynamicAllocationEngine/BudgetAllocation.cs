// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BudgetAllocation.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>
    /// Class to contain the outputs of GetBudgetAllocations
    /// </summary>
    public class BudgetAllocation
    {
        /// <summary>
        /// Gets or sets PeriodStart. replaces PeriodStart where that is used
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets PeriodDuration. replaces PeriodDuration where that is used
        /// </summary>
        public TimeSpan PeriodDuration { get; set; }

        /// <summary>
        /// Gets or sets total budget for the campaign, including budget that may already be allocated or spent
        /// </summary>
        public decimal TotalBudget { get; set; }

        /// <summary>
        /// Gets or sets CampaignStart. 
        /// </summary>
        public DateTime CampaignStart { get; set; }

        /// <summary>
        /// Gets or sets CampaignEnd. 
        /// </summary>
        public DateTime CampaignEnd { get; set; }

        /// <summary>
        /// Gets or sets RemainingBudget. The money left to spend in this campaign considering all costs: data costs, serving fees, media costs, etc.
        /// </summary>
        public decimal RemainingBudget { get; set; }

        /// <summary>
        /// Gets or sets PeriodBudget. The money allocated for the current period.
        /// </summary>
        public decimal PeriodBudget { get; set; }

        /// <summary>
        /// Gets or sets the set of allocation paramters. These enable some control over the details of the allocation
        /// on a per campiagn basis
        /// </summary>
        public AllocationParameters AllocationParameters { get; set; }

        /// <summary>
        /// Gets or sets AnticipatedSpendForDay.
        /// If this number is less than the sum of the period budgets above, we are falling off the end of the graph.
        /// </summary>
        public decimal AnticipatedSpendForDay { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the allocation outputs
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the value-volume score of the allocation outputs
        /// </summary>
        public decimal ValueVolumeScore { get; set; }

        /// <summary>
        /// Gets or sets the insight score of the allocation outputs
        /// </summary>
        public double InsightScore { get; set; }

        /// <summary>
        /// Gets or sets the phase of the campaign
        /// </summary>
        public double Phase { get; set; }

        /// <summary>
        /// Gets or sets allocated node results. 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
        public Dictionary<MeasureSet, PerNodeBudgetAllocationResult> PerNodeResults { get; set; }

        /// <summary>
        /// Gets or sets node delivery metrics. 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
        public Dictionary<MeasureSet, IEffectiveNodeMetrics> NodeDeliveryMetricsCollection { get; set; }

        /// <summary>Gets a string representation of the object</summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            var perNodeResultsStrings = this.PerNodeResults
                .Select(kvp => string.Format(
                    CultureInfo.InvariantCulture,
                    "{{({0}), ({1})}}",
                    kvp.Key,
                    kvp.Value));

            return string.Format(
                CultureInfo.InvariantCulture,
                "BudgetAllocation: AnticipatedSpendForDay={0} PerNodeResults=[{1}]",
                this.AnticipatedSpendForDay,
                string.Join(", ", perNodeResultsStrings));
        }
    }
}