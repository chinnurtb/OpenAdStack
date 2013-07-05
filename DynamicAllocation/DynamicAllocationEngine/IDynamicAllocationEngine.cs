// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDynamicAllocationEngine.cs" company="Rare Crowds Inc">
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
