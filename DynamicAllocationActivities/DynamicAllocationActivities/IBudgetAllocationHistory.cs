// -----------------------------------------------------------------------
// <copyright file="IBudgetAllocationHistory.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Interface definition for a class that manages BudgetAllocationHistory
    /// </summary>
    public interface IBudgetAllocationHistory
    {
        /// <summary>Retrieve the allocation history index.</summary>
        /// <returns>The list of history elements.</returns>
        IEnumerable<HistoryElement> RetrieveAllocationHistoryIndex();
    }
}