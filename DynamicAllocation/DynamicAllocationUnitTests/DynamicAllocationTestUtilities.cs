// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationTestUtilities.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{  
    /// <summary>
    /// contains utilities for testing
    /// </summary>
    public static class DynamicAllocationTestUtilities
    {
        /// <summary>
        /// test measure map
        /// </summary>
        public static readonly Dictionary<long, IDictionary<string, object>> TestMeasureMap =
            Enumerable.Range(1, 10)
            .Select(m => (long)m)
            .ToDictionary(
            m => m,
            m => (IDictionary<string, object>)new Dictionary<string, object>
            {
                { MeasureValues.DataProvider, "Lotame" },
                { MeasureValues.DataCost, m == 3 ? 2 : .25 },
            });

        /// <summary>
        /// helper method that asserts two decimals are within a certain double error margin
        /// </summary>
        /// <param name="expected">the first value</param>
        /// <param name="actual">the second value</param>
        /// <param name="errorMargin">the allowed error margin</param>
        public static void AssertWithin(decimal expected, decimal actual, double errorMargin)
        {
            Assert.IsTrue((double)Math.Abs(actual - expected) <= errorMargin);
        }

        /// <summary>
        /// asserts two dictionaries of the same type are equal
        /// </summary>
        /// <typeparam name="TKey">key type</typeparam>
        /// <typeparam name="TValue">value type</typeparam>
        /// <param name="expected">the expected dictionary</param>
        /// <param name="actual">the actual dictionary</param>
        public static void AssertDictionariesAreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual)
        {
            Assert.IsTrue(actual.OrderBy(kvp => kvp.Key).SequenceEqual(expected.OrderBy(kvp => kvp.Key)));
        }

        /// <summary>
        /// asserts two dictionaries of the same type are equal
        /// </summary>
        /// <typeparam name="TKey">key type</typeparam>
        /// <typeparam name="TValue">value type</typeparam>
        /// <param name="expected">the expected dictionary</param>
        /// <param name="actual">the actual dictionary</param>
        public static void AssertDictionariesAreEqual<TKey, TValue>(Dictionary<TKey, TValue> expected, ConcurrentDictionary<TKey, TValue> actual)
        {
            Assert.IsTrue(actual.OrderBy(kvp => kvp.Key).SequenceEqual(expected.OrderBy(kvp => kvp.Key)));
        }

        /// <summary>
        /// asserts two dictionaries of measureSet - double are equal
        /// </summary>
        /// <param name="expected">the expected dictionary</param>
        /// <param name="actual">the actual dictionary</param>
        public static void AssertDictionariesAreEqualDouble(Dictionary<MeasureSet, double> expected, ConcurrentDictionary<MeasureSet, double> actual)
        {
            Assert.AreEqual(0, expected.Keys.Except(actual.Keys).Count());
            foreach (var result in actual)
            {
                Assert.AreEqual(Math.Round(result.Value, 6), Math.Round(expected[result.Key], 6));
            }
        }

        /// <summary>
        /// asserts two dictionaries of measureSet - double are equal
        /// </summary>
        /// <param name="expected">the expected dictionary</param>
        /// <param name="actual">the actual dictionary</param>
        public static void AssertDictionariesAreEqualDouble(Dictionary<MeasureSet, double> expected, Dictionary<MeasureSet, double> actual)
        {
            Assert.AreEqual(0, expected.Keys.Except(actual.Keys).Count());
            foreach (var result in actual)
            {
                Assert.AreEqual(Math.Round(result.Value, 6), Math.Round(expected[result.Key], 6));
            }
        }

        /// <summary>
        /// helper method that asserts the allocated budget sum is correct
        /// </summary>
        /// <param name="totalBudget">total budget for the campaign</param>
        /// <param name="remainingTime">campaign remaining time</param>
        /// <param name="actual">the budget allocation outputs</param>
        /// <param name="budgetBuffer">the budgetBuffer</param>
        public static void AssertTotalAllocatedBudgetSumIsCorrect(
            decimal totalBudget, 
            TimeSpan remainingTime, 
            BudgetAllocation actual,
            decimal budgetBuffer)
        {
            // Ensure that the sum of the individual nodes' allocations equals the total expected allocation amount for the first day.
            var oneDayInTicks = new TimeSpan(1, 0, 0, 0).Ticks;
            AssertWithin(
                Allocation.CalculatePeriodBudget(totalBudget, remainingTime, oneDayInTicks) * budgetBuffer,
                actual.PerNodeResults.Sum(pnbar => pnbar.Value.PeriodTotalBudget),
                0.01 * actual.PerNodeResults.Count);
        }

        /// <summary>
        /// helper method that asserts the per node results are self consistant
        /// </summary>
        /// <param name="outputs">budget allocation Outputs</param>
        /// <param name="perNodeResults">the per node results to be checked</param>
        /// <param name="measureInfo">the measure info</param>
        public static void AssertPerNodeResultsAreSelfConsistent(
            BudgetAllocation outputs,
            IDictionary<MeasureSet, PerNodeBudgetAllocationResult> perNodeResults,
            MeasureInfo measureInfo)
        {
            foreach (var perNodeResult in perNodeResults)
            {
                // cost
                var cost = measureInfo.CalculateTotalSpend(
                    perNodeResult.Key,
                    perNodeResult.Value.PeriodImpressionCap,
                    perNodeResult.Value.PeriodMediaBudget,
                    outputs.AllocationParameters.Margin,
                    outputs.AllocationParameters.PerMilleFees);

                // historyMetrics
                var historyMetrics = new Allocation(measureInfo).GetHistoryMetrics(
                    perNodeResult.Key, 
                    outputs, 
                    perNodeResult.Value.PeriodTotalBudget);

                // TODO: update this test
                // caps
                ////var caps = Allocation.CalculateCaps(
                ////    perNodeResult.Key,
                ////    perNodeResult.Value.PeriodTotalBudget,
                ////    outputs);

                // check total budget
                AssertWithin(
                    perNodeResult.Value.PeriodTotalBudget,
                    cost,
                    .01);

                // check that max bid is correct
                // (if a node has data costs that are too high it will have a max bid of zero)
                if (perNodeResult.Value.MaxBid > 0)
                {
                    AssertWithin(
                        outputs.PerNodeResults[perNodeResult.Key].Valuation,
                        perNodeResult.Value.MaxBid + historyMetrics.EstimatedNonMediaCostPerMille,
                        .01);
                }
                else
                {
                    // if the node was filtered, it should not have any budgets or bid
                    Assert.AreEqual(0, perNodeResult.Value.MaxBid);
                    Assert.AreEqual(0, perNodeResult.Value.PeriodTotalBudget);
                    Assert.AreEqual(0, perNodeResult.Value.PeriodMediaBudget);
                    Assert.AreEqual(0, perNodeResult.Value.PeriodImpressionCap);
                    Assert.AreEqual(0, perNodeResult.Value.ExportBudget);
                }

                // check that impression cap is correct
                // (have to use an error margin of 10 due to prior rounding compounded with multiplying by 1000)
                ////AssertWithin(
                ////    caps.PeriodImpressionCap,
                ////    perNodeResult.Value.PeriodImpressionCap,
                ////    10);
            }
        }

        /// <summary>
        /// deserializes an object from Xml
        /// </summary>
        /// <typeparam name="TOutput">the type of object</typeparam>
        /// <param name="requestXml">the Xml string to be deserialized </param>
        /// <returns>the deserialized object</returns>
        internal static TOutput DeserializeFromXml<TOutput>(string requestXml)
        {
            // TODO: this code is copied from the DA Activity - this should probably be factored out of both and put somewhere common
            using (var reader = XmlReader.Create(new StringReader(requestXml)))
            {
                var deserializer = new DataContractSerializer(typeof(TOutput));
                return (TOutput)deserializer.ReadObject(reader);
            }
        }

        /// <summary>
        /// Creates a set of budgetAllocation for testing
        /// </summary>
        /// <returns>a budgetAllocation for testing</returns>
        internal static BudgetAllocation CreateOutputs()
        {
            // create a number of measures (small enough we can hand calculate the exact expected values)
            var measureCount = 3;
            var testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            var testMeasureSets = MeasureSet.PowerSet(testMeasures);

            var history = BuildPerNodeResults(testMeasureSets);

            var testNodeScores = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 4.25 },
                { new MeasureSet { 2 }, 4.75 },
                { new MeasureSet { 3 }, 5.25 },
                { new MeasureSet { 1, 2 }, 14.5 },
                { new MeasureSet { 1, 3 }, 15.5 },
                { new MeasureSet { 2, 3 }, 16.5 },
                { new MeasureSet { 1, 2, 3 }, 39.25 },
            };

            var testPerNodeResults =
                history.Select(pnr =>
                    new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(
                        pnr.Key,
                        new PerNodeBudgetAllocationResult
                        {
                            NodeScore = testNodeScores[pnr.Key],
                            LineagePenalty = 1
                        })).ToDictionary(pnr => pnr.Key, pnr => pnr.Value);

            return new BudgetAllocation
            {
                AnticipatedSpendForDay = 1000,
                PerNodeResults = testPerNodeResults,
            };
        }

        /// <summary>Build a history given measuresets</summary>
        /// <param name="measureSets">The measure sets.</param>
        /// <returns>a history collection</returns>
        internal static Dictionary<MeasureSet, PerNodeBudgetAllocationResult> BuildPerNodeResults(Collection<MeasureSet> measureSets)
        {
            ////var measureBase = measureSets.Min(ms => ms.Min()) - 1;
            return measureSets.ToDictionary(
                ms => ms, ms => new PerNodeBudgetAllocationResult());

                ////{
                ////    ImpressionRate48 = ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0,
                ////    ImpressionRate168 = ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0,
                ////    LifetimeImpressions = (ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0) * 168,
                ////    Impressions48 = (ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0) * 48,
                ////    Impressions168 = (ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0) * 168,
                ////    MediaSpend48 = (ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .048m,
                ////    MediaSpend168 = (ms.Any() ? ms.Select(m => m - measureBase).Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .168m,
                ////});
        }

        /// <summary>
        /// A modified version of budgetAllocation to test deserializing Json after a schema change
        /// </summary>
        internal class NewOutputs
        {
            /// <summary>
            /// Gets or sets AnticipatedSpendForDay.
            /// If this number is less than the sum of the daily budgets above, we are falling off the end of the graph.
            /// </summary>
            public decimal AnticipatedSpendForDayX { get; set; }

            /// <summary>
            /// Gets or sets the last modified date of the allocation outputs
            /// </summary>
            public DateTime LastModifiedDate { get; set; }

            /// <summary>
            /// Gets or sets PerNodeResults. 
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
            public Dictionary<MeasureSet, PerNodeBudgetAllocationResultNew> PerNodeResults { get; set; }
        }

        /// <summary>
        /// Class for testing object changes wrt json serialization
        /// </summary>
        internal class PerNodeBudgetAllocationResultNew
        {
            /// <summary>
            /// Gets or sets the AllocationID value
            /// </summary>
            public string AllocationIdX { get; set; }

            /// <summary>
            /// Gets or sets the total budget (calcuated for a whole day regardless of period lengths)
            /// </summary>
            public decimal PeriodTotalBudget { get; set; }

            /// <summary>
            /// Gets or sets the budget for media (calcuated for a whole day regardless of period lengths) 
            /// </summary>
            public decimal PeriodMediaBudgetX { get; set; }

            /// <summary>
            /// Gets or sets the budget for export to appNexus (calcuated for a whole day regardless of period lengths) 
            /// </summary>
            public decimal ExportBudget { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether node is ineligible to export (e.g. - low performance).
            /// </summary>
            public bool NodeIsIneligibleX { get; set; }

            /// <summary>
            /// Gets or sets count of times node has been exported.
            /// </summary>
            public int ExportCount { get; set; }

            /// <summary>
            /// Gets or sets the impression cap (calcuated for a whole day regardless of period lengths)
            /// </summary>
            public long PeriodImpressionCapX { get; set; }

            /// <summary>
            /// Gets or sets the maximum we will bid for this node (data cost is already removed - this would be the input into appnexus, for example)
            /// </summary>
            public decimal MaxBid { get; set; }

            /// <summary>
            /// Gets or sets LifetimeMediaSpend.
            /// This is a pass-through and is not use dy DA. It will not be considered in equality calculations.
            /// </summary>
            public decimal LifetimeMediaSpend { get; set; }

            /// <summary>
            /// Gets or sets LifetimeImpressions.
            /// This is a pass-through and is not use dy DA. It will not be considered in equality calculations.
            /// </summary>
            public long LifetimeImpressions { get; set; }

            /// <summary>
            /// Gets or sets NodeScore.
            /// </summary>
            public double NodeScore { get; set; }

            /// <summary>
            /// Gets or sets LineagePenalty
            /// (multiplier to node score based on performance of ancestors and descendants).
            /// </summary>
            public double LineagePenaltyX { get; set; }
        }
    }
}
