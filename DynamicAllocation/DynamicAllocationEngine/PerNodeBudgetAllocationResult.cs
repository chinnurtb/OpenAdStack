// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PerNodeBudgetAllocationResult.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DynamicAllocation
{
    /// <summary>
    /// Container for the budget reallocation results that are different for each node. 
    /// </summary>
    public class PerNodeBudgetAllocationResult
    {
        /// <summary>
        /// Gets or sets the AllocationID value
        /// </summary>
        public string AllocationId { get; set; }

        /// <summary>
        /// Gets or sets the total budget (calcuated for a whole day regardless of period lengths)
        /// </summary>
        public decimal PeriodTotalBudget { get; set; }

        /// <summary>
        /// Gets or sets the budget for media (calcuated for a whole day regardless of period lengths) 
        /// </summary>
        public decimal PeriodMediaBudget { get; set; }

        /// <summary>
        /// Gets or sets the budget for export to appNexus (calcuated for a whole day regardless of period lengths) 
        /// </summary>
        public decimal ExportBudget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether node is ineligible to export (e.g. - low performance).
        /// </summary>
        public bool NodeIsIneligible { get; set; }

        /// <summary>
        /// Gets or sets count of times node has been exported.
        /// </summary>
        public int ExportCount { get; set; }

        /// <summary>
        /// Gets or sets the impression cap (calcuated for a whole day regardless of period lengths)
        /// </summary>
        public long PeriodImpressionCap { get; set; }

        /// <summary>
        /// Gets or sets the maximum we will bid for this node (data cost is already removed - this would be the input into appnexus, for example)
        /// </summary>
        public decimal MaxBid { get; set; }

        /// <summary>
        /// Gets or sets NodeScore.
        /// </summary>
        public double NodeScore { get; set; }

        /// <summary>
        /// Gets or sets LineagePenalty
        /// (multiplier to node score based on performance of ancestors and descendants).
        /// </summary>
        public double LineagePenalty { get; set; }

        /// <summary>
        /// Gets or sets Valuation.
        /// </summary>
        public decimal Valuation { get; set; }
        
        /// <summary>
        /// Gets or sets media spend for lifetime.
        /// </summary>
        public decimal LifetimeMediaSpend { get; set; }
        
        /// <summary>
        /// Gets or sets impressions for lifetime.
        /// </summary>
        public long LifetimeImpressions { get; set; }
        
        /// <summary>
        /// Gets or sets effective hourly media spend rate.
        /// </summary>
        public decimal EffectiveMediaSpendRate { get; set; }
        
        /// <summary>
        /// Gets or sets effective hourly impression rate.
        /// </summary>
        public decimal EffectiveImpressionRate { get; set; }
        
        /// <summary>
        /// Gets or sets return on ad spend.
        /// </summary>
        public decimal ReturnOnAdSpend { get; set; }
        
        /// <summary>Gets a string representation of the object</summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            var values = this.ToDictionaryValues();
            return "PerNodeBudgetAllocationResult: {0}"
                .FormatInvariant(values.ToString<string, object>());
        }
    }
}
