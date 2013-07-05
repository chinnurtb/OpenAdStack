// -----------------------------------------------------------------------
// <copyright file="EligibilityHistoryBuilder.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Eligibility history builder class.</summary>
    internal class EligibilityHistoryBuilder : IEligibilityHistoryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EligibilityHistoryBuilder"/> class.
        /// </summary>
        internal EligibilityHistoryBuilder()
        {
            this.EligibilityHistory = new Dictionary<MeasureSet, List<EligibilityPeriod>>();
        }

        /// <summary>
        /// Gets or sets EligibilityHistory.
        /// </summary>
        public Dictionary<MeasureSet, List<EligibilityPeriod>> EligibilityHistory { get; set; }

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
        public IList<HistoryElement> FilterIndex(IList<HistoryElement> index, TimeSpan lookBackDuration, DateTime lastDeliveryDataDate)
        {
            var latestIndexEntry = index.Max(i => (DateTime)new PropertyValue(PropertyType.Date, i.AllocationStartTime));
            var lookbackStart = lastDeliveryDataDate < latestIndexEntry ? lastDeliveryDataDate : latestIndexEntry;

            var earlyLookBackCutoff = ApplyLookBack(lookBackDuration, lookbackStart);

            return index.Where(i =>
                (DateTime)new PropertyValue(PropertyType.Date, i.AllocationStartTime) >= earlyLookBackCutoff)
                .ToList();
        }

        /// <summary>Process the allocation history and update each nodes eligibility history.</summary>
        /// <param name="allocationHistory">The allocation history.</param>
        public void AddEligibilityHistory(BudgetAllocation allocationHistory)
        {
            var eligibilityPeriod = new EligibilityPeriod
                {
                    EligibilityStart = allocationHistory.PeriodStart,
                    EligibilityDuration = allocationHistory.PeriodDuration 
                };

            // Accumulate a list of delivery periods for each node when it had export budget
            foreach (var node in allocationHistory.PerNodeResults)
            {
                if (node.Value.ExportBudget <= 0)
                {
                    continue;
                }

                // If this is the first entry we are adding for this node initialize the dictionary
                // entry for this node with an empty collection
                if (!this.EligibilityHistory.ContainsKey(node.Key))
                {
                    this.EligibilityHistory[node.Key] = new List<EligibilityPeriod>();
                }

                this.AddExclusiveEligibilityPeriods(node.Key, eligibilityPeriod);
            }
        }

        /// <summary>Apply a lookback making sure it is not an invalid DateTime</summary>
        /// <param name="lookBackDuration">The look back duration.</param>
        /// <param name="lookbackStart">The lookback start.</param>
        /// <returns>The new date with lookback subtracted, or DateTime.MinValue.</returns>
        private static DateTime ApplyLookBack(TimeSpan lookBackDuration, DateTime lookbackStart)
        {
            var newStart = lookbackStart;
            if (DateTime.MinValue + lookBackDuration < newStart)
            {
                newStart -= lookBackDuration;
            }

            return newStart;
        }

        /// <summary>Break a new period into one or more periods that are exclusive relative to an existing period.</summary>
        /// <param name="newPeriod">The new period.</param>
        /// <param name="existingPeriod">The existing period.</param>
        /// <returns>A collection of new existing periods.</returns>
        private static IEnumerable<EligibilityPeriod> BreakApartPeriod(EligibilityPeriod newPeriod, EligibilityPeriod existingPeriod)
        {
            var resultPeriods = new List<EligibilityPeriod>();

            var newFirstHour = newPeriod.EligibilityStart;
            var newLastHour = newPeriod.EligibilityStart + newPeriod.EligibilityDuration;
            var existingFirstHour = existingPeriod.EligibilityStart;
            var existingLastHour = existingPeriod.EligibilityStart + existingPeriod.EligibilityDuration;

            // There are at most two periods that can be created. The period of time before existing period, and the
            // period of time after existing period.
            if (newFirstHour < existingFirstHour)
            {
                var lastHour = newLastHour <= existingFirstHour ? newLastHour : existingFirstHour;
                resultPeriods.Add(new EligibilityPeriod
                {
                    EligibilityStart = newFirstHour,
                    EligibilityDuration = lastHour - newFirstHour
                });
            }

            if (newLastHour > existingLastHour)
            {
                var firstHour = newFirstHour >= existingLastHour ? newFirstHour : existingLastHour;
                resultPeriods.Add(new EligibilityPeriod
                {
                    EligibilityStart = firstHour,
                    EligibilityDuration = newLastHour - firstHour
                });
            }

            return resultPeriods;
        }

        /// <summary>Add a range of hours exlcusive to the hours of eligibility already present.</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="eligibilityPeriod">The eligibility period representing a range of hours to be added.</param>
        private void AddExclusiveEligibilityPeriods(MeasureSet measureSet, EligibilityPeriod eligibilityPeriod)
        {
            var history = this.EligibilityHistory[measureSet];
            var exclusiveNewPeriods = new List<EligibilityPeriod> { eligibilityPeriod };

            foreach (var existingPeriod in history)
            {
                var newPeriods = new List<EligibilityPeriod>();
                foreach (var newPeriod in exclusiveNewPeriods)
                {
                    newPeriods.AddRange(BreakApartPeriod(newPeriod, existingPeriod));
                }

                exclusiveNewPeriods = newPeriods;
            }

            this.EligibilityHistory[measureSet].AddRange(exclusiveNewPeriods);
        }
    }
}