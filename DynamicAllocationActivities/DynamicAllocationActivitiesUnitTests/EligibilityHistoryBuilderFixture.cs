// -----------------------------------------------------------------------
// <copyright file="EligibilityHistoryBuilderFixture.cs" company="Rare Crowds Inc">
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
using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Test fixture for EligibilityHistoryBuilder.</summary>
    [TestClass]
    public class EligibilityHistoryBuilderFixture
    {
        /// <summary>Time constant - history lookback.</summary>
        private static readonly TimeSpan LookBackDuration = new TimeSpan(72, 0, 0);

        /// <summary>Time constant - 2012/12/12 12:00 UTC.</summary>
        private static readonly DateTime Utc12 = new DateTime(2012, 12, 12, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Time constant - one day.</summary>
        private static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);

        /// <summary>Time constant - one hour.</summary>
        private static readonly TimeSpan OneHour = new TimeSpan(1, 0, 0);

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet0;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet1;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet2;

        /// <summary>buget allocation for testing</summary>
        private BudgetAllocation allocationHistory1;

        /// <summary>buget allocation for testing</summary>
        private BudgetAllocation allocationHistory2;

        /// <summary>
        /// Per test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Set up node map
            this.measureSet0 = new MeasureSet { 0 };
            this.measureSet1 = new MeasureSet { 1 };
            this.measureSet2 = new MeasureSet { 2 };

            // Build the node results
            var nodeResultNoExport = new PerNodeBudgetAllocationResult
            {
                ExportBudget = 0m,
            };

            var nodeResultExport = new PerNodeBudgetAllocationResult
            {
                ExportBudget = 1m,
            };

            var perNodeResult = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                {
                    { this.measureSet0, nodeResultNoExport }, 
                    { this.measureSet1, nodeResultExport }, 
                    { this.measureSet2, nodeResultExport }, 
                };

            this.allocationHistory1 = new BudgetAllocation
            {
                PeriodStart = Utc12,
                PeriodDuration = OneDay,
                PerNodeResults = perNodeResult
            };

            this.allocationHistory2 = new BudgetAllocation
            {
                PeriodStart = Utc12 - OneDay,
                PeriodDuration = OneDay,
                PerNodeResults = perNodeResult
            };
        }

        /// <summary>
        /// Filter allocation history when there is delivery after latest allocation start.
        /// Lookback should start from latest allocation start.
        /// </summary>
        [TestMethod]
        public void FilterIndexLatestDeliveryAfterLatestAllocation()
        {
            var latestDeliveryDataDate = Utc12;
            var latestAllocationStart = Utc12 - OneDay;
            var start1 = new PropertyValue(PropertyType.Date, latestAllocationStart).ToString();
            var index1 = new HistoryElement { AllocationStartTime = start1 };
            var start2 = new PropertyValue(PropertyType.Date, latestAllocationStart - LookBackDuration).ToString();
            var index2 = new HistoryElement { AllocationStartTime = start2 };
            var start3 = new PropertyValue(PropertyType.Date, latestAllocationStart - LookBackDuration - OneDay).ToString();
            var index3 = new HistoryElement { AllocationStartTime = start3 };
            var index = new List<HistoryElement> { index1, index2, index3 };
            
            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder();
            var filteredIndex = eligibilityHistoryBuilder.FilterIndex(index, LookBackDuration, latestDeliveryDataDate);
            Assert.AreEqual(2, filteredIndex.Count);
            Assert.IsFalse(filteredIndex.Contains(index3));
        }

        /// <summary>
        /// Filter allocation history when there is delivery only before the latest allocation start.
        /// Lookback should start from latest reported delivery.
        /// </summary>
        [TestMethod]
        public void FilterIndexLatestDeliveryBeforeLatestAllocation()
        {
            var latestDeliveryDataDate = Utc12 - OneDay;
            var latestAllocationStart = Utc12;
            var start1 = new PropertyValue(PropertyType.Date, latestAllocationStart).ToString();
            var index1 = new HistoryElement { AllocationStartTime = start1 };
            var start2 = new PropertyValue(PropertyType.Date, latestDeliveryDataDate - LookBackDuration).ToString();
            var index2 = new HistoryElement { AllocationStartTime = start2 };
            var start3 = new PropertyValue(PropertyType.Date, latestDeliveryDataDate - LookBackDuration - OneDay).ToString();
            var index3 = new HistoryElement { AllocationStartTime = start3 };
            var index = new List<HistoryElement> { index1, index2, index3 };

            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder();
            var filteredIndex = eligibilityHistoryBuilder.FilterIndex(index, LookBackDuration, latestDeliveryDataDate);
            Assert.AreEqual(2, filteredIndex.Count);
            Assert.IsFalse(filteredIndex.Contains(index3));
        }

        /// <summary>Happy path scenario</summary>
        [TestMethod]
        public void AddEligibilityHistory()
        {
            var historyBuilder = new EligibilityHistoryBuilder();

            historyBuilder.AddEligibilityHistory(this.allocationHistory1);
            historyBuilder.AddEligibilityHistory(this.allocationHistory2);
            var eligibilityHistory = historyBuilder.EligibilityHistory;
            
            // There should be two measure sets in the history
            Assert.AreEqual(2, eligibilityHistory.Count);

            // MeasureSet0 should not be included (no export budget)
            Assert.IsFalse(eligibilityHistory.ContainsKey(this.measureSet0));

            // Each measure set should have two eligibility periods
            Assert.AreEqual(2, eligibilityHistory[this.measureSet1].Count());
            Assert.AreEqual(1, eligibilityHistory[this.measureSet1].Count(h => h.EligibilityStart == this.allocationHistory1.PeriodStart));
            Assert.AreEqual(1, eligibilityHistory[this.measureSet1].Count(h => h.EligibilityStart == this.allocationHistory2.PeriodStart));
            Assert.AreEqual(2, eligibilityHistory[this.measureSet2].Count());
            Assert.AreEqual(1, eligibilityHistory[this.measureSet2].Count(h => h.EligibilityStart == this.allocationHistory1.PeriodStart));
            Assert.AreEqual(1, eligibilityHistory[this.measureSet2].Count(h => h.EligibilityStart == this.allocationHistory2.PeriodStart));
        }

        /// <summary>Adjacent eligibility period end should not change.</summary>
        [TestMethod]
        public void AddEligibilityHistoryEndToBeginning()
        {
            var historyBuilder = new EligibilityHistoryBuilder();

            // Set up non-overlapping periods
            this.allocationHistory1.PeriodStart = Utc12;
            this.allocationHistory1.PeriodDuration = OneDay;

            historyBuilder.AddEligibilityHistory(this.allocationHistory1);

            // Now add a second period adjacent and before the existing period
            ////      | p1 |
            //// | p2 |
            //// | 2  |  1 |
            this.allocationHistory2.PeriodStart = Utc12 - OneDay;
            this.allocationHistory2.PeriodDuration = OneDay;
            historyBuilder.AddEligibilityHistory(this.allocationHistory2);

            // There should still be two measure sets in the history
            Assert.AreEqual(2, historyBuilder.EligibilityHistory.Count);

            var nodeHistory = historyBuilder.EligibilityHistory[this.measureSet1];

            // We should have created three additional non-overlapping eligibility periods
            Assert.AreEqual(2, nodeHistory.Count());
            Assert.AreEqual(48, nodeHistory.Sum(h => h.EligibilityDuration.TotalHours));
            AssertExclusiveEligibilityPeriods(nodeHistory);
        }

        /// <summary>Adjacent eligibility period begin should not change.</summary>
        [TestMethod]
        public void AddEligibilityHistoryBeginningToEnd()
        {
            var historyBuilder = new EligibilityHistoryBuilder();

            // Set up non-overlapping periods
            this.allocationHistory1.PeriodStart = Utc12;
            this.allocationHistory1.PeriodDuration = OneDay;

            historyBuilder.AddEligibilityHistory(this.allocationHistory1);

            // Now add a second period that is adjacent and after the existing period
            //// | p1 |
            ////      | p2 |
            //// | 1  |  2 |
            this.allocationHistory2.PeriodStart = Utc12 + OneDay;
            this.allocationHistory2.PeriodDuration = OneDay;
            historyBuilder.AddEligibilityHistory(this.allocationHistory2);

            // There should still be two measure sets in the history
            Assert.AreEqual(2, historyBuilder.EligibilityHistory.Count);

            var nodeHistory = historyBuilder.EligibilityHistory[this.measureSet1];

            // We should have created three additional non-overlapping eligibility periods
            Assert.AreEqual(2, nodeHistory.Count());
            Assert.AreEqual(48, nodeHistory.Sum(h => h.EligibilityDuration.TotalHours));
            AssertExclusiveEligibilityPeriods(nodeHistory);
        }

        /// <summary>Overlapping eligibility periods should be exculsive</summary>
        [TestMethod]
        public void AddEligibilityHistoryExcludeOverlappingEligibility()
        {
            var historyBuilder = new EligibilityHistoryBuilder();

            // Set up non-overlapping periods
            this.allocationHistory1.PeriodStart = Utc12;
            this.allocationHistory1.PeriodDuration = OneDay;
            this.allocationHistory2.PeriodStart = Utc12 - OneDay - OneHour;
            this.allocationHistory2.PeriodDuration = OneDay;

            historyBuilder.AddEligibilityHistory(this.allocationHistory1);
            historyBuilder.AddEligibilityHistory(this.allocationHistory2);

            // There should be two measure sets in the history
            Assert.AreEqual(2, historyBuilder.EligibilityHistory.Count);

            var nodeHistory = historyBuilder.EligibilityHistory[this.measureSet1];

            // Two non-overlapping eligibility periods expected
            Assert.AreEqual(2, nodeHistory.Count());
            Assert.AreEqual(48, nodeHistory.Sum(h => h.EligibilityDuration.TotalHours));
            AssertExclusiveEligibilityPeriods(nodeHistory);

            // Now add a third period that overlaps the entire range of existing
            // periods extending before and after
            ////     | p1 | gap | p2  |
            //// |          p3            |
            //// | 4 | 1  |  3  |  2  | 5 |
            this.allocationHistory2.PeriodStart = Utc12 - OneDay - OneDay;
            this.allocationHistory2.PeriodDuration = OneDay + OneDay + OneDay + OneDay;
            historyBuilder.AddEligibilityHistory(this.allocationHistory2);

            // There should still be two measure sets in the history
            Assert.AreEqual(2, historyBuilder.EligibilityHistory.Count);

            nodeHistory = historyBuilder.EligibilityHistory[this.measureSet1];

            // We should have created three additional non-overlapping eligibility periods
            Assert.AreEqual(5, nodeHistory.Count());
            Assert.AreEqual(96, nodeHistory.Sum(h => h.EligibilityDuration.TotalHours));
            AssertExclusiveEligibilityPeriods(nodeHistory);
        }

        /// <summary>Assert all periods are mutually exclusive ranges</summary>
        /// <param name="nodeHistory">The eligibility history for a node.</param>
        private static void AssertExclusiveEligibilityPeriods(List<EligibilityPeriod> nodeHistory)
        {
            foreach (var eligibilityPeriod in nodeHistory)
            {
                var startTime = eligibilityPeriod.EligibilityStart;
                var endTime = eligibilityPeriod.EligibilityEnd;

                var exclusivePeriods =
                    nodeHistory.Where(
                        p => p.EligibilityStart > endTime || p.EligibilityEnd < startTime);

                Assert.AreEqual(nodeHistory.Count - 1, exclusivePeriods.Count());
            }
        }
    }
}
