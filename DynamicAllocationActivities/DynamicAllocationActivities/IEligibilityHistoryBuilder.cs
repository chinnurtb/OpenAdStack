// -----------------------------------------------------------------------
// <copyright file="IEligibilityHistoryBuilder.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Interface definition for EligibilityHistoryBuilder.</summary>
    public interface IEligibilityHistoryBuilder
    {
        /// <summary>
        /// Gets EligibilityHistory.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "It's a set of collections keyed by measureset.")]
        Dictionary<MeasureSet, List<EligibilityPeriod>> EligibilityHistory { get; }

        /// <summary>Process the allocation history and update each nodes eligibility history.</summary>
        /// <param name="allocationHistory">The allocation history.</param>
        void AddEligibilityHistory(BudgetAllocation allocationHistory);

        /// <summary>
        /// Limit the entries we look at to a lookBackDuration before the previous latest delivery
        /// data we have seen in a report or the last allocation period start, whichever is earlier.
        /// This will include up to the duration of a period more than the lookBackDuration because
        /// the cutoff is based on period start.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="lookBackDuration">The look back duration.</param>
        /// <param name="lastDeliveryDataDate">The last delivery data date.</param>
        /// <returns>The filtered index.</returns>
        IList<HistoryElement> FilterIndex(IList<HistoryElement> index, TimeSpan lookBackDuration, DateTime lastDeliveryDataDate);
    }
}