// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationEngine.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Diagnostics;

namespace DynamicAllocation
{
    /// <summary>
    /// Class for the DynamicAllocationEngine
    /// </summary>        
    public class DynamicAllocationEngine : IDynamicAllocationEngine
    {
        /// <summary>The initial allocation algo instance</summary>
        private readonly InitialAllocation initialAllocation;

        /// <summary>The reallocation algo instance</summary>
        private readonly Reallocation reallocation;

        /// <summary>
        /// Initializes a new instance of the DynamicAllocationEngine class
        /// </summary>
        /// <param name="measureMap">the measure map</param>
        public DynamicAllocationEngine(MeasureMap measureMap)
        {
            var measureInfo = new MeasureInfo(measureMap);
            this.initialAllocation = new InitialAllocation(measureInfo);
            this.reallocation = new Reallocation(measureInfo);
        }

        /// <summary>
        /// Calculates the set of MeasureSets and their valuations from a CampaignDefinition
        /// </summary>
        /// <param name="campaign">campaign definition</param>
        /// <returns>Dictionary mapping MeasureSets to their valuations </returns>
        public IDictionary<MeasureSet, decimal> GetValuations(CampaignDefinition campaign)
        {
            return Valuation.GetValuations(campaign);
        }

        /// <summary>
        /// Method to calculate the budget allocations
        /// </summary>
        /// <param name="allocationInputs">Budget allocation updated with last delivery results.</param>
        /// <returns>budgetAllocation instance containing the new budget allocation information.</returns>
        public BudgetAllocation GetBudgetAllocations(BudgetAllocation allocationInputs)
        {
            return this.GetBudgetAllocations(allocationInputs, false);
        }

        /// <summary>
        /// Method to calculate the budget allocations
        /// </summary>
        /// <param name="allocationInputs">Budget allocation updated with last delivery results.</param>
        /// <param name="forceInitial">
        /// Whether to force initial allocation even if there are previously exported per-node-results.
        /// </param>
        /// <returns>budgetAllocation instance containing the new budget allocation information.</returns>
        public BudgetAllocation GetBudgetAllocations(BudgetAllocation allocationInputs, bool forceInitial)
        {
            // if the history is empty, we do a budget allocation, otherwise a reallocation.
            // TODO: make sure this the correct criteria for there being a history
            if (allocationInputs.PerNodeResults.All(pnr => pnr.Value.ExportCount == 0) || forceInitial)
            {
                return this.initialAllocation.AllocateBudget(allocationInputs);
            }

            return this.reallocation.AllocateBudget(allocationInputs);
        }

        /// <summary>
        /// Method to increment the export count of each measure set in the measureSets array in the allocation 
        /// </summary>
        /// <param name="allocation">the allocation</param>
        /// <param name="measureSets">the measure sets whose export counts are to be incremented</param>
        /// <returns>the allocation with the export counts incremented</returns>
        public BudgetAllocation IncrementExportCounts(BudgetAllocation allocation, IEnumerable<MeasureSet> measureSets)
        {
            foreach (var measureSet in measureSets)
            {
                allocation.PerNodeResults[measureSet].ExportCount++;
            }

            return allocation;
        }
    }
}
