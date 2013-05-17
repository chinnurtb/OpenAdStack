// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDynamicAllocationEngine.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace DynamicAllocation
{
    /// <summary>
    /// Interface for DynamicAllocationEngine
    /// </summary>
    public interface IDynamicAllocationEngine
    {
        /// <summary>
        /// Signature for method to calculate the set of targeting attribute set valuations
        /// </summary>
        /// <param name="campaign">allocationInputs definition</param>
        /// <returns>Dictionary mapping MeasureSets to their valuations </returns>
        IDictionary<MeasureSet, decimal> GetValuations(CampaignDefinition campaign);

        /// <summary>
        /// Signature for method to calculate the budget allocations
        /// </summary>
        /// <param name="allocationInputs">budgetAllocation instance containing Valuations, CampaignHistory, RemaingBudget, and RemainingFlightTime </param>
        /// <returns>budgetAllocation instance containing an AnticipatedSpendForDay, and a dictionary mapping MeasureSets to their new period budgets </returns>
        BudgetAllocation GetBudgetAllocations(BudgetAllocation allocationInputs);

        /// <summary>
        /// Signature for method to calculate the budget allocations
        /// </summary>
        /// <param name="allocationInputs">budgetAllocation instance containing Valuations, CampaignHistory, RemaingBudget, and RemainingFlightTime </param>
        /// <param name="forceInitial">
        /// Whether to force initial allocation even if there are previously exported per-node-results.
        /// </param>
        /// <returns>budgetAllocation instance containing an AnticipatedSpendForDay, and a dictionary mapping MeasureSets to their new period budgets </returns>
        BudgetAllocation GetBudgetAllocations(BudgetAllocation allocationInputs, bool forceInitial);

        /// <summary>
        /// Signature for method to increment the export count of each measure set in the measureSets array in the allocation
        /// </summary>
        /// <param name="allocation">the allocation</param>
        /// <param name="measureSets">the measure sets whose export count is to be incremented</param>
        /// <returns>the allocation with the export counts incremented</returns>
        BudgetAllocation IncrementExportCounts(BudgetAllocation allocation, IEnumerable<MeasureSet> measureSets);
    }
}
