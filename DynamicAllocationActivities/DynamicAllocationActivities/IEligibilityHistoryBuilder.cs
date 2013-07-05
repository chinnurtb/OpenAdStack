// -----------------------------------------------------------------------
// <copyright file="IEligibilityHistoryBuilder.cs" company="Rare Crowds Inc">
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