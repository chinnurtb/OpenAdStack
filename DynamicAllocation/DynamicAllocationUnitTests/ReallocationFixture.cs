// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReallocationFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using Diagnostics;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for Reallocation
    /// </summary>
    [TestClass]
    public class ReallocationFixture
    {
        /// <summary>Test list of ineligible nodes for LineagePenalty tests</summary>
        private readonly IList<MeasureSet> ineligibleNodes = new List<MeasureSet>
                {
                    new MeasureSet { 1, 3, 5 },
                    new MeasureSet { 1, 2, 3 },
                    new MeasureSet { 3 },
                };

        /// <summary>Test measures</summary>
        private IList<long> testMeasures;

        /// <summary>Test measure sets</summary>
        private Collection<MeasureSet> testMeasureSets;

        /// <summary>Test allocation Outputs</summary>
        private BudgetAllocation testbudgetAllocation;

        /// <summary>Test node delivery metrics stubs</summary>
        private Dictionary<MeasureSet, IEffectiveNodeMetrics> testNodeDeliveryMetrics;

        /// <summary>Test node scrores matching the test allocation Outputs above</summary>
        private Dictionary<MeasureSet, double> testNodeScores;

        /// <summary>Test per node results</summary>
        private Dictionary<MeasureSet, PerNodeBudgetAllocationResult> testPerNodeResults;

        /// <summary>Test allocation paramters</summary>
        private AllocationParameters testAllocationParameters;

        /// <summary>Reallocation algo instance</summary>
        private Reallocation reallocation;

        /// <summary>Test data generation function for node metrics</summary>
        private Func<MeasureSet, decimal> testMetricsGen;

        /// <summary>
        /// Per test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
            TestUtilities.AllocationParametersDefaults.Initialize();
            this.testAllocationParameters = new AllocationParameters();

            // create a number of measures (small enough we can hand calculate the exact expected values)
            var measureCount = 3;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);
            this.testMeasureSets.Add(new MeasureSet { 1, 3, 5 });

            this.testNodeScores = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 400.0 / 5 }, // 80
                { new MeasureSet { 2 }, 500.0 / 5 }, // 100
                { new MeasureSet { 3 }, 400.0 / 5 }, // 80
                { new MeasureSet { 1, 2 }, (200.0 / 2) + 100 + 80 }, // 280
                { new MeasureSet { 1, 3 }, (200.0 / 3) + 80 + 80 }, // 226.66666667
                { new MeasureSet { 2, 3 }, (200.0 / 2) + 100 + 80 }, // 280
                { new MeasureSet { 1, 2, 3 }, (100.0 / 1) + 80 + 100 + 80 + 100 + 100 + (200.0 / 3) }, // 626.6666667
                { new MeasureSet { 1, 3, 5 }, 0 + (200.0 / 3) + 160 }, // 226.6666667
            };

            this.testPerNodeResults =
                this.testMeasureSets.ToDictionary(
                    ms => ms,
                    ms =>
                    new PerNodeBudgetAllocationResult
                    {
                        NodeScore = this.testNodeScores[ms],
                        LineagePenalty = 1,
                        Valuation = 1
                    });

            var lookback = 24;

            // Set up test data distribution generator based on measureset
            this.testMetricsGen = measureSet 
                => measureSet.Any() ? measureSet.Sum() + (measureSet.Count > 1 ? 1 : 0) : 0;

            // Set up delivery metrics for nodes
            this.testNodeDeliveryMetrics =
                this.testMeasureSets.ToDictionary(
                    ms => ms,
                    ms => SetupNodeDeliveryMetricsStub(100, .1m, lookback));

            // Setup up node with no delivery
            var measureSet135 = new MeasureSet { 1, 3, 5 };
            this.testNodeDeliveryMetrics.Remove(measureSet135);
            
            this.testbudgetAllocation = new BudgetAllocation
            {
                AnticipatedSpendForDay = 1000,
                PerNodeResults = this.testPerNodeResults,
                NodeDeliveryMetricsCollection = this.testNodeDeliveryMetrics,
                AllocationParameters = this.testAllocationParameters,
                PeriodDuration = new TimeSpan(lookback, 0, 0),
                PeriodBudget = 1000
            };

            // create a zero data cost measure map
            var measureMap = new MeasureMap(
                DynamicAllocationTestUtilities.TestMeasureMap
                .ToDictionary(
                    m => m.Key,
                    m => (IDictionary<string, object>)m.Value
                        .ToDictionary(
                            m2 => m2.Key,
                            m2 => m2.Key == MeasureValues.DataProvider ? m2.Value : 0)));
            this.reallocation = new Reallocation(new MeasureInfo(measureMap));
        }

        /// <summary>
        /// A test for AllocateBudget
        /// </summary>
        [TestMethod]
        public void AllocateBudgetFirst()
        {
            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);

            var periodDuration = 24;

            var nodeDeliveryMetricsCollection = this.testMeasureSets.ToDictionary(
                    ms => ms,
                    ms => SetupNodeDeliveryMetricsStub(100, .1m, periodDuration));

            // create a campaign history with enough info for everyone to get default budgets
            this.testbudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = DynamicAllocationTestUtilities.BuildPerNodeResults(this.testMeasureSets),
                NodeDeliveryMetricsCollection = nodeDeliveryMetricsCollection,
                RemainingBudget = 1000,
                CampaignStart = new DateTime(2011, 12, 31).AddDays(-3),
                CampaignEnd = new DateTime(2011, 12, 31),
                PeriodStart = new DateTime(2011, 12, 31).AddDays(-1),
                AllocationParameters = this.testAllocationParameters,
                PeriodDuration = new TimeSpan(periodDuration, 0, 0),
            };

            // give all nodes an export count of 1 and a history
            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = perNodeResult.Key;
                perNodeResult.Value.Valuation = 1;
                perNodeResult.Value.ExportCount = 1;

                // Setup delivery metrics for node
                this.testbudgetAllocation.NodeDeliveryMetricsCollection[ms] = SetupNodeDeliveryMetricsStub(
                    100, .01m, lookBack);
            }

            // canary for making sure export count is preserved. Also make sure it doesn't make the cut
            var canaryMeasureSet = new MeasureSet(new long[] { 1 });
            this.testbudgetAllocation.PerNodeResults[canaryMeasureSet].ExportCount = 10;
            this.testbudgetAllocation.NodeDeliveryMetricsCollection[canaryMeasureSet] = SetupNodeDeliveryMetricsStub(
                1, .001m, lookBack);

            var actual = this.reallocation.AllocateBudget(this.testbudgetAllocation);

            // make sure the correct number of nodes are exported
            Assert.AreEqual(
                this.testAllocationParameters.MaxNodesToExport,
                actual.PerNodeResults.Count(pnr => pnr.Value.ExportBudget > 0));

            // make sure the exported nodes all have export counts of 1 or 2 (for this test case)
            Assert.IsTrue(actual.PerNodeResults
                .Where(pnr => pnr.Value.ExportBudget > 0)
                .All(pnr => pnr.Value.ExportCount == 1 || pnr.Value.ExportCount == 2));

            // make sure the canary kept its info
            Assert.AreEqual(10, actual.PerNodeResults[new MeasureSet(new long[] { 1 })].ExportCount);
        }

        /// <summary>
        /// basic test for CalculateTupleScore
        /// </summary>
        [TestMethod]
        public void TupleScoreTestBasic()
        {
            var expected = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 400.0 / 5 }, // 80
                { new MeasureSet { 2 }, 500.0 / 5 }, // 100
                { new MeasureSet { 3 }, 400.0 / 5 }, // 80
                { new MeasureSet { 1, 2 }, (200.0 / 2) }, // 100
                { new MeasureSet { 1, 3 }, (200.0 / 3) }, // 66.6666667
                { new MeasureSet { 2, 3 }, (200.0 / 2) }, // 100
                { new MeasureSet { 1, 2, 3 }, (100.0 / 1) }, // 100
                { new MeasureSet { 1, 3, 5 }, 0 }, // 0
            };

            // Set up 135 so it exports but is ineligible
            var measureSet135 = new MeasureSet { 1, 3, 5 };
            this.testNodeDeliveryMetrics[measureSet135] = SetupNodeDeliveryMetricsStub(0, 0, 24);

            var actual = new Dictionary<MeasureSet, double>();

            foreach (var measureSet in this.testMeasureSets)
            {
                Reallocation.CalculateTupleScore(measureSet, ref actual, this.testNodeDeliveryMetrics);
            }

            DynamicAllocationTestUtilities.AssertDictionariesAreEqualDouble(expected, actual);
        }

        /// <summary>
        /// A basic test of CalculateNodeScore
        /// </summary>
        [TestMethod]
        public void NodeScoreTestBasic()
        {
            // Set up 135 so it exports but is ineligible
            var measureSet135 = new MeasureSet { 1, 3, 5 };
            this.testNodeDeliveryMetrics[measureSet135] = SetupNodeDeliveryMetricsStub(0, 0, 24);

            var tupleScores = new Dictionary<MeasureSet, double>();

            foreach (var nodeDeliveryMetrics in this.testNodeDeliveryMetrics)
            {
                Reallocation.CalculateTupleScore(nodeDeliveryMetrics.Key, ref tupleScores, this.testNodeDeliveryMetrics);
            }

            var actual = new ConcurrentDictionary<MeasureSet, double>();
            
            foreach (var measureSet in this.testMeasureSets)
            {
                actual[measureSet] = Reallocation.CalculateNodeScore(measureSet, ref tupleScores);
            }

            DynamicAllocationTestUtilities.AssertDictionariesAreEqualDouble(this.testNodeScores, actual);
        }

        /// <summary>Scenario where a node gets a penalty</summary>
        [TestMethod]
        public void CalcLineagePenaltyFound()
        {
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, new MeasureSet { 2, 3 }, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenalty, actualPenalty);
        }

        /// <summary>Scenario where a node gets a penalty</summary>
        [TestMethod]
        public void CalcLineagePenaltyFoundMultiple()
        {
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, new MeasureSet { 1, 3 }, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenalty, actualPenalty);
        }

        /// <summary>Scenario where only ancestor is ineligible</summary>
        [TestMethod]
        public void CalcLineagePenaltyAncestorIneligibleOnly()
        {
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, new MeasureSet { 1, 2 }, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenaltyNeutral, actualPenalty);
        }

        /// <summary>Scenario where only descendant is ineligible</summary>
        [TestMethod]
        public void CalcLineagePenaltyDescendantIneligibleOnly()
        {
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, new MeasureSet { 3, 4 }, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenaltyNeutral, actualPenalty);
        }

        /// <summary>Scenario where export count non-zero</summary>
        [TestMethod]
        public void CalcLineagePenaltyExportCountNonzero()
        {
            var measureSet = new MeasureSet { 1, 3 };
            this.testbudgetAllocation.PerNodeResults[measureSet].ExportCount = 1;
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, measureSet, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenaltyNeutral, actualPenalty);
        }

        /// <summary>Scenario where bid-override present</summary>
        [TestMethod]
        public void CalcLineagePenaltyExportWithBidOverride()
        {
            var measureSet = new MeasureSet { 1, 3 };
            this.testbudgetAllocation.PerNodeResults[measureSet].Valuation = 2;
            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, measureSet, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenaltyNeutral, actualPenalty);
        }

        /// <summary>Scenario where bid-override present but less than one and more than another ancestor</summary>
        [TestMethod]
        public void CalcLineagePenaltyExportWithBidOverrideMultiple()
        {
            var measureSet = new MeasureSet { 1, 3 };
            var ancestor = new MeasureSet { 1, 3, 5 };
            this.testbudgetAllocation.PerNodeResults[measureSet].Valuation = 2;
            this.testbudgetAllocation.PerNodeResults[ancestor].Valuation = 2;

            var actualPenalty = Reallocation.CalculateLineagePenalty(this.testbudgetAllocation, measureSet, this.ineligibleNodes);
            Assert.AreEqual(this.testAllocationParameters.LineagePenalty, actualPenalty);
        }

        /// <summary>Simple happy path sort scenario</summary>
        [TestMethod]
        public void RankSortSimple()
        {
            // Coerce node scores. Valuations are same.
            var nodeScores = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 1 },
                { new MeasureSet { 2 }, 2 },
                { new MeasureSet { 3 }, 3 },
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 2, 3 }, 6 },
                { new MeasureSet { 1, 3, 5 }, 7 },
                { new MeasureSet { 1, 2, 3 }, 8 },
            };

            foreach (var perNodeResult in this.testPerNodeResults)
            {
                perNodeResult.Value.NodeScore = nodeScores[perNodeResult.Key];
            }

            var rankSort = Reallocation.SortByRank(this.testbudgetAllocation, this.testPerNodeResults);

            var expectedSortOrder = new List<MeasureSet>
                {
                    new MeasureSet { 1, 2, 3 },
                    new MeasureSet { 1, 3, 5 },
                    new MeasureSet { 2, 3 },
                    new MeasureSet { 1, 3 },
                    new MeasureSet { 1, 2 },
                    new MeasureSet { 3 },
                    new MeasureSet { 2 },
                    new MeasureSet { 1 },
                };

            var mergedActual = rankSort.Zip(expectedSortOrder, (first, second) => new Tuple<MeasureSet, MeasureSet>(first.Key, second));
            foreach (var result in mergedActual)
            {
                Assert.AreEqual(result.Item1, result.Item2);
            }
        }

        /// <summary>Simple happy path sort scenario</summary>
        [TestMethod]
        public void RankSortOnePenalty()
        {
            // Setup node scores
            var nodeScores = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 1 },
                { new MeasureSet { 2 }, 2 },
                { new MeasureSet { 3 }, 3 },
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 2, 3 }, 6 },
                { new MeasureSet { 1, 3, 5 }, 7 },
                { new MeasureSet { 1, 2, 3 }, 8 },
            };

            foreach (var perNodeResult in this.testPerNodeResults)
            {
                perNodeResult.Value.NodeScore = nodeScores[perNodeResult.Key];
            }

            var penaltyMeasureSet = new MeasureSet { 1, 2, 3 };
            this.testPerNodeResults[penaltyMeasureSet].LineagePenalty = this.testAllocationParameters.LineagePenalty;

            var rankSort = Reallocation.SortByRank(this.testbudgetAllocation, this.testPerNodeResults);

            var expectedSortOrder = new List<MeasureSet>
                {
                    new MeasureSet { 1, 3, 5 },
                    new MeasureSet { 2, 3 },
                    new MeasureSet { 1, 3 },
                    new MeasureSet { 1, 2 },
                    new MeasureSet { 3 },
                    new MeasureSet { 2 },
                    new MeasureSet { 1 },
                    new MeasureSet { 1, 2, 3 },
                  };

            var mergedActual = rankSort.Zip(expectedSortOrder, (first, second) => new Tuple<MeasureSet, MeasureSet>(first.Key, second));
            foreach (var result in mergedActual)
            {
                Assert.AreEqual(result.Item1, result.Item2);
            }
        }

        /// <summary>Simple happy path sort scenario</summary>
        [TestMethod]
        public void SortByTierThenRankSimple()
        {
            // Setup node scores
            var nodeScores = new Dictionary<MeasureSet, double>
            {
                { new MeasureSet { 1 }, 1 },
                { new MeasureSet { 2 }, 2 },
                { new MeasureSet { 3 }, 3 },
                { new MeasureSet { 1, 2 }, 4 },
                { new MeasureSet { 1, 3 }, 5 },
                { new MeasureSet { 2, 3 }, 6 },
                { new MeasureSet { 1, 3, 5 }, 7 },
                { new MeasureSet { 1, 2, 3 }, 8 },
            };

            foreach (var perNodeResult in this.testPerNodeResults)
            {
                perNodeResult.Value.NodeScore = nodeScores[perNodeResult.Key];
            }

            var rankSort = Reallocation.SortByTierThenRank(this.testbudgetAllocation, this.testPerNodeResults);

            var expectedSortOrder = new List<MeasureSet>
                {
                    new MeasureSet { 3 },
                    new MeasureSet { 2 },
                    new MeasureSet { 1 },
                    new MeasureSet { 2, 3 },
                    new MeasureSet { 1, 3 },
                    new MeasureSet { 1, 2 },
                    new MeasureSet { 1, 2, 3 },
                    new MeasureSet { 1, 3, 5 },
                };

            var mergedActual = rankSort.Zip(expectedSortOrder, (first, second) => new Tuple<MeasureSet, MeasureSet>(first.Key, second));
            foreach (var result in mergedActual)
            {
                Assert.AreEqual(result.Item1, result.Item2);
            }
        }

        /// <summary>
        /// test for CalculateLineageScores
        /// </summary>
        [TestMethod]
        public void CalculateLineageScoresTest()
        {
            // set it up such that all nodes should get a lineagepenalty
            var measureSetNull = new MeasureSet();
            var personaNode = new MeasureSet { 1, 2, 3 };
            this.testPerNodeResults[measureSetNull] =
                new PerNodeBudgetAllocationResult
                {
                    LineagePenalty = this.testAllocationParameters.LineagePenaltyNeutral
                };

            this.testNodeDeliveryMetrics[measureSetNull] = SetupNodeDeliveryMetricsStub(0, 0, 24);
            this.testNodeDeliveryMetrics[personaNode] = SetupNodeDeliveryMetricsStub(0, 0, 24);

            foreach (var pnr in this.testPerNodeResults)
            {
                pnr.Value.NodeScore = 0;
            }

            var budgetAllocation = new BudgetAllocation
                {
                    PerNodeResults = this.testPerNodeResults,
                    NodeDeliveryMetricsCollection = this.testNodeDeliveryMetrics
                };

            Assert.IsTrue(budgetAllocation.PerNodeResults.All(pnr => pnr.Value.LineagePenalty == this.testAllocationParameters.LineagePenaltyNeutral));
            Assert.IsTrue(budgetAllocation.PerNodeResults.All(pnr => pnr.Value.NodeScore == 0));

            Reallocation.CalculateLineageScores(this.testbudgetAllocation);

            // 135 is node with node delivery
            var measureSet135 = new MeasureSet { 1, 3, 5 };

            Assert.IsTrue(budgetAllocation.PerNodeResults.All(
                pnr =>
                    pnr.Value.NodeIsIneligible ||
                    pnr.Value.LineagePenalty == this.testAllocationParameters.LineagePenalty ||
                    pnr.Key == measureSet135));
            Assert.AreEqual(
                this.testAllocationParameters.LineagePenaltyNeutral,
                budgetAllocation.PerNodeResults[measureSet135].LineagePenalty);
            Assert.IsTrue(budgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count > 0).All(pnr => pnr.Value.NodeScore != 0));
        }

        /// <summary>
        /// test for GetLargestBudgetedNodes with limited count
        /// </summary>
        [TestMethod]
        public void GetLargestBudgetedNodesTestLimitedCount()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
                node.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
            }

            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.PeriodTotalBudget)
                .OrderByDescending(budget => budget)
                .Take(5)
                .ToList();

            var measureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 5, 10);
            var actual = measureSets
                .Select(ms => this.testbudgetAllocation.PerNodeResults[ms].PeriodTotalBudget)
                .OrderByDescending(budget => budget)
                .ToList();

            Assert.AreEqual(5, actual.Count());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for GetLargestBudgetedNodes with limited budget
        /// </summary>
        [TestMethod]
        public void GetLargestBudgetedNodesTestLimitedBudget()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
                node.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
            }

            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.PeriodTotalBudget)
                .OrderByDescending(budget => budget)
                .Take(5)
                .ToList();

            var measureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 10, .76m);
            var actual = measureSets
                .Select(ms => this.testbudgetAllocation.PerNodeResults[ms].PeriodTotalBudget)
                .OrderByDescending(budget => budget)
                .ToList();

            Assert.AreEqual(5, actual.Count());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for the IsRisePhase method when making budget, not enough insight
        /// </summary>
        [TestMethod]
        public void IsRisePhaseTestWithBudgetNotEnoughInsight()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
            }

            var exportMeasureSets = this.testMeasureSets.Take(3).ToList();
            var budgetAllocation = this.testbudgetAllocation;
            budgetAllocation.PeriodBudget = .16m;
            budgetAllocation.CampaignEnd = budgetAllocation.CampaignStart.AddDays(1);

            var actual = Reallocation.IsRisePhase(exportMeasureSets, budgetAllocation);
            Assert.IsTrue(actual);
        }

        /// <summary>
        /// test for the IsRisePhase method when making budget, enough insight
        /// </summary>
        [TestMethod]
        public void IsRisePhaseTestWithBudgetEnoughInsight()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
                node.Value.LineagePenalty = this.testbudgetAllocation.AllocationParameters.LineagePenalty;
            }

            var exportMeasureSets = this.testMeasureSets.Take(3).ToList();
            var budgetAllocation = this.testbudgetAllocation;
            budgetAllocation.AnticipatedSpendForDay = .16m;

            // Guarantee we're making budget
            budgetAllocation.PeriodBudget = 0;

            var actual = Reallocation.IsRisePhase(exportMeasureSets, budgetAllocation);
            Assert.IsFalse(actual);
        }

        /// <summary>
        /// test for the IsRisePhase method when not making budget, enough insight
        /// </summary>
        [TestMethod]
        public void IsRisePhaseTestNotMakingBudgetEnoughInsight()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
                node.Value.LineagePenalty = this.testbudgetAllocation.AllocationParameters.LineagePenalty;
            }

            var exportMeasureSets = this.testMeasureSets.Take(3).ToList();
            var budgetAllocation = this.testbudgetAllocation;
            budgetAllocation.PeriodBudget = .17m;

            var actual = Reallocation.IsRisePhase(exportMeasureSets, budgetAllocation);
            Assert.IsTrue(actual);
        }

        /// <summary>
        /// test for the InsightScore method
        /// </summary>
        [TestMethod]
        public void InsightScoreTest()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            // these two should not be counted in the no insight group (we have insight into them)
            this.testbudgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count == 2).First().Value.ExportCount = 1;
            this.testbudgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count == 3).First().Value.LineagePenalty = 
                this.testbudgetAllocation.AllocationParameters.LineagePenalty;

            var insightScore = Reallocation.InsightScore(this.testbudgetAllocation);
            Assert.AreEqual(.4, insightScore);
        }

        /// <summary>
        /// a test for the HaveInsight method for false
        /// </summary>
        [TestMethod]
        public void HaveInsightTestNo()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            // these two should not be counted in the no insight group (we have insight into them)
            this.testbudgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count == 2).First().Value.ExportCount = 1;
            this.testbudgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count == 3).First().Value.LineagePenalty =
                this.testbudgetAllocation.AllocationParameters.LineagePenalty;
     
            this.testbudgetAllocation.InsightScore = Reallocation.InsightScore(this.testbudgetAllocation);
            var actual = Reallocation.HaveInsight(this.testbudgetAllocation);
            Assert.IsFalse(actual);
        }

        /// <summary>
        /// a test for the HaveInsight method for true
        /// </summary>
        [TestMethod]
        public void HaveInsightTestYes()
        {
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            foreach (var node in this.testbudgetAllocation.PerNodeResults.Where(pnr => pnr.Key.Count == 2 || pnr.Key.Count == 3))
            {
                node.Value.LineagePenalty = this.testbudgetAllocation.AllocationParameters.LineagePenalty;
            }

            this.testbudgetAllocation.InsightScore = Reallocation.InsightScore(this.testbudgetAllocation);
            var actual = Reallocation.HaveInsight(this.testbudgetAllocation);
            Assert.IsTrue(actual);
        }
        
        /// <summary>
        /// a test for the NodesThatMakeBudget method
        /// </summary>
        [TestMethod]
        public void NodesThatMakeBudgetTest()
        {
            var exportNodes = this.testbudgetAllocation.PerNodeResults.OrderByDescending(pnr => pnr.Value.PeriodTotalBudget);
          
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0;
            }

            this.testbudgetAllocation.PeriodBudget = 40;
            var expected = exportNodes.Take(6).Select(pnr => pnr.Key).ToList(); 
            var actual = Reallocation.NodesThatMakeBudget(this.testbudgetAllocation, exportNodes.ToList(), 2.5m, 8);
            
            Assert.IsTrue(expected.SequenceEqual(actual));
        }
        
        /// <summary>
        /// a test for the RisePhaseAllocation method
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationTestPhaseOne()
        {
            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);
            this.testMeasureSets.Remove(new MeasureSet());

            // create a campaign history with enough info for everyone to get default budgets
            var now = DateTime.Now;
            this.testbudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = DynamicAllocationTestUtilities.BuildPerNodeResults(this.testMeasureSets),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                PeriodBudget = 100,
                AllocationParameters = this.testAllocationParameters,
                CampaignStart = now,
                PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1),
                CampaignEnd = now.AddDays(10)
            };

            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = perNodeResult.Key;
                perNodeResult.Value.Valuation = ms.Sum();
                perNodeResult.Value.ExportCount = 0;
                perNodeResult.Value.LineagePenalty = ms.Sum() > 10 ? 
                    this.testbudgetAllocation.AllocationParameters.LineagePenaltyNeutral :
                    this.testbudgetAllocation.AllocationParameters.LineagePenalty;
                perNodeResult.Value.PeriodTotalBudget = ((ms.Sum() * ms.Sum()) + (ms.Count > 1 ? 1 : 0)) * .0008m;
                perNodeResult.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
                this.testbudgetAllocation.NodeDeliveryMetricsCollection[ms] = SetupNodeDeliveryMetricsStub(
                    100, .1m, lookBack);
            }

            this.testAllocationParameters.InitialMaxNumberOfNodes = 50;
            this.testAllocationParameters.MaxNodesToExport = 10;

            var exportMeasureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 50);
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults.Where(pnr => exportMeasureSets.Contains(pnr.Key)))
            {
                perNodeResult.Value.ExportCount = 1;
            }

            var newExportMeasureSets = this.reallocation.RisePhaseAllocation(exportMeasureSets, this.testbudgetAllocation);

            // with this set up, the top 10 nodes should make budget, the other 40 should come from the experminetal pool
            // which in this case is the no insight swap pool
            Assert.AreEqual(50, newExportMeasureSets.Count);
            Assert.AreEqual(10, newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).Count());

            // Assert that the spread nodes are from the noinsight pool
            var noInsightList = this.testbudgetAllocation
                    .PerNodeResults
                    .Where(pnr => !pnr.Value.NodeIsIneligible && Reallocation.NoInsight(this.testbudgetAllocation, pnr))
                    .Select(pnr => pnr.Key)
                    .ToList();

            Assert.IsTrue(newExportMeasureSets.Where(ms => !exportMeasureSets.Contains(ms)).All(ms => noInsightList.Contains(ms)));
            Assert.IsTrue(newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).All(ms => !noInsightList.Contains(ms)));
        }

        /// <summary>
        /// a test for the RisePhaseAllocation method with zero budget nodes (ie psuedo inelgible)
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationTestPhaseOneZeroBudgetNodes()
        {
            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);
            this.testMeasureSets.Remove(new MeasureSet());

            // create a campaign history with enough info for everyone to get default budgets
            var now = DateTime.Now;
            this.testbudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = DynamicAllocationTestUtilities.BuildPerNodeResults(this.testMeasureSets),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                PeriodBudget = 100,
                AllocationParameters = this.testAllocationParameters,
                CampaignStart = now,
                PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1),
                CampaignEnd = now.AddDays(10)
            };

            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = perNodeResult.Key;
                var measureSum = ms.Sum();
                perNodeResult.Value.Valuation = measureSum;
                perNodeResult.Value.ExportCount = 0;
                perNodeResult.Value.LineagePenalty = measureSum > 10 ?
                    this.testbudgetAllocation.AllocationParameters.LineagePenaltyNeutral :
                    this.testbudgetAllocation.AllocationParameters.LineagePenalty;
                perNodeResult.Value.PeriodTotalBudget = measureSum > 8 ?
                    ((measureSum * measureSum) + (ms.Count > 1 ? 1 : 0)) * .0008m :
                    0;
                perNodeResult.Value.PeriodMediaBudget = perNodeResult.Value.PeriodTotalBudget * .1m; 
                this.testbudgetAllocation.NodeDeliveryMetricsCollection[ms] = SetupNodeDeliveryMetricsStub(
                    100, .1m, lookBack);
            }

            this.testAllocationParameters.InitialMaxNumberOfNodes = 50;
            this.testAllocationParameters.MaxNodesToExport = 10;

            var exportMeasureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 50);
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults.Where(pnr => exportMeasureSets.Contains(pnr.Key)))
            {
                perNodeResult.Value.ExportCount = 1;
            }

            var newExportMeasureSets = this.reallocation.RisePhaseAllocation(exportMeasureSets, this.testbudgetAllocation);

            // with this set up, the top 10 nodes should make budget, the other 40 should come from the experminetal pool
            // which in this case is the no insight swap pool
            Assert.AreEqual(50, newExportMeasureSets.Count);
            Assert.AreEqual(10, newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).Count());

            // Assert that the spread nodes are from the noinsight pool
            var noInsightList = this.testbudgetAllocation
                    .PerNodeResults
                    .Where(pnr => !pnr.Value.NodeIsIneligible && Reallocation.NoInsight(this.testbudgetAllocation, pnr))
                    .Select(pnr => pnr.Key)
                    .ToList();

            Assert.IsTrue(newExportMeasureSets.Where(ms => !exportMeasureSets.Contains(ms)).All(ms => noInsightList.Contains(ms)));
            Assert.IsTrue(newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).All(ms => !noInsightList.Contains(ms)));

            // Nothing that made the export list should have a zero budget
            Assert.IsTrue(newExportMeasureSets.All(ms => this.testbudgetAllocation.PerNodeResults[ms].PeriodTotalBudget > 0));
        }

        /// <summary>
        /// a test for the RisePhaseAllocation method with ineligible nodes
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationTestPhaseOneIneligibleNodes()
        {
            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);
            this.testMeasureSets.Remove(new MeasureSet());

            // create a campaign history with enough info for everyone to get default budgets
            var now = DateTime.Now;
            this.testbudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = DynamicAllocationTestUtilities.BuildPerNodeResults(this.testMeasureSets),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                PeriodBudget = 100,
                AllocationParameters = this.testAllocationParameters,
                CampaignStart = now,
                PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1),
                CampaignEnd = now.AddDays(10)
            };

            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = perNodeResult.Key;
                var measureSum = ms.Sum();
                perNodeResult.Value.Valuation = measureSum;
                perNodeResult.Value.ExportCount = 0;
                perNodeResult.Value.LineagePenalty = measureSum > 10 ?
                    this.testbudgetAllocation.AllocationParameters.LineagePenaltyNeutral :
                    this.testbudgetAllocation.AllocationParameters.LineagePenalty;
                perNodeResult.Value.PeriodTotalBudget = measureSum > 8 ?
                    ((measureSum * measureSum) + (ms.Count > 1 ? 1 : 0)) * .0008m :
                    0;
                perNodeResult.Value.NodeIsIneligible = measureSum <= 8;
                perNodeResult.Value.PeriodMediaBudget = perNodeResult.Value.PeriodTotalBudget * .1m; 
                this.testbudgetAllocation.NodeDeliveryMetricsCollection[ms] = SetupNodeDeliveryMetricsStub(
                    100, .1m, lookBack);
            }

            this.testAllocationParameters.InitialMaxNumberOfNodes = 50;
            this.testAllocationParameters.MaxNodesToExport = 10;

            var exportMeasureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 50);
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults.Where(pnr => exportMeasureSets.Contains(pnr.Key)))
            {
                perNodeResult.Value.ExportCount = 1;
            }

            var newExportMeasureSets = this.reallocation.RisePhaseAllocation(exportMeasureSets, this.testbudgetAllocation);

            // with this set up, the top 10 nodes should make budget, the other 40 should come from the experminetal pool
            // which in this case is the no insight swap pool
            Assert.AreEqual(50, newExportMeasureSets.Count);
            Assert.AreEqual(10, newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).Count());

            // Assert that the spread nodes are from the noinsight pool
            var noInsightList = this.testbudgetAllocation
                    .PerNodeResults
                    .Where(pnr => !pnr.Value.NodeIsIneligible && Reallocation.NoInsight(this.testbudgetAllocation, pnr))
                    .Select(pnr => pnr.Key)
                    .ToList();

            Assert.IsTrue(newExportMeasureSets.Where(ms => !exportMeasureSets.Contains(ms)).All(ms => noInsightList.Contains(ms)));
            Assert.IsTrue(newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).All(ms => !noInsightList.Contains(ms)));

            // Nothing that is ineligible should have made the export list 
            Assert.IsTrue(newExportMeasureSets.All(ms => !this.testbudgetAllocation.PerNodeResults[ms].NodeIsIneligible));
        }

        /// <summary>
        /// a test for the RisePhaseAllocation method no eligible nodes
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationTestPhaseNoEligibleNodes()
        {
            //// create a setup with one experimental node and where all but one previously exported node has either ineglible 
            //// flag set true or has zero budget (pseudo inelgible)
            var experimentalMeasureSet = new MeasureSet(new long[] { 1, 2, 3, 4 });
            var eligibleReExportMeasureSet = new MeasureSet(new long[] { 1, 2, 3, 5 });

            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);
            this.testMeasureSets.Remove(new MeasureSet());

            // create a campaign history with enough info for everyone to get default budgets
            var now = DateTime.Now;
            this.testbudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = DynamicAllocationTestUtilities.BuildPerNodeResults(this.testMeasureSets),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                PeriodBudget = 100,
                AllocationParameters = this.testAllocationParameters,
                CampaignStart = now,
                PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1),
                CampaignEnd = now.AddDays(10)
            };

            // give all nodes an export count of 1 and a history
            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = perNodeResult.Key;
                var measureSum = ms.Sum();
                perNodeResult.Value.Valuation = measureSum;
                perNodeResult.Value.ExportCount = 1;
                perNodeResult.Value.LineagePenalty = measureSum > 9 ?
                    this.testbudgetAllocation.AllocationParameters.LineagePenaltyNeutral :
                    this.testbudgetAllocation.AllocationParameters.LineagePenalty;
                perNodeResult.Value.PeriodTotalBudget = measureSum <= 8 ?
                    ((measureSum * measureSum) + (ms.Count > 1 ? 1 : 0)) * .0008m :
                    0;
                perNodeResult.Value.NodeIsIneligible = measureSum <= 8;
                perNodeResult.Value.PeriodMediaBudget = perNodeResult.Value.PeriodTotalBudget * .1m;
                this.testbudgetAllocation.NodeDeliveryMetricsCollection[ms] = SetupNodeDeliveryMetricsStub(
                    100, .1m, lookBack);
            }

            this.testbudgetAllocation.PerNodeResults[experimentalMeasureSet].ExportCount = 0;
            this.testbudgetAllocation.PerNodeResults[eligibleReExportMeasureSet].PeriodMediaBudget = 1;

            this.testAllocationParameters.InitialMaxNumberOfNodes = 50;
            this.testAllocationParameters.MaxNodesToExport = 10;

            var exportMeasureSets = this.reallocation.GetLargestBudgetedNodes(this.testbudgetAllocation, 50);
            foreach (var perNodeResult in this.testbudgetAllocation.PerNodeResults.Where(pnr => exportMeasureSets.Contains(pnr.Key)))
            {
                perNodeResult.Value.ExportCount = 1;
            }

            var newExportMeasureSets = this.reallocation.RisePhaseAllocation(exportMeasureSets, this.testbudgetAllocation);

            // with this set up, only the special reexport node should be reexported, and only the special experiment should be exported
            Assert.AreEqual(2, newExportMeasureSets.Count);
            Assert.AreEqual(1, newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).Count());

            // Assert that the spread nodes are from the noinsight pool
            var noInsightList = this.testbudgetAllocation
                    .PerNodeResults
                    .Where(pnr => !pnr.Value.NodeIsIneligible && Reallocation.NoInsight(this.testbudgetAllocation, pnr))
                    .Select(pnr => pnr.Key)
                    .ToList();

            Assert.IsTrue(newExportMeasureSets.Where(ms => !exportMeasureSets.Contains(ms)).All(ms => noInsightList.Contains(ms)));
            Assert.IsTrue(newExportMeasureSets.Where(ms => exportMeasureSets.Contains(ms)).All(ms => !noInsightList.Contains(ms)));

            // Assert that only the two special nodes made it
            Assert.IsTrue(newExportMeasureSets.Contains(experimentalMeasureSet));
            Assert.IsTrue(newExportMeasureSets.Contains(eligibleReExportMeasureSet));
        }

        /// <summary>
        /// Tests the decision to go into phase one or two with enough time not enough insight
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationPhaseChoiceTestEnoughTimeNotEnoughInsight()
        {
            // create a campaign with percent of time passed less than the PhaseOneExitPercentage, and with lack of insight
            var now = DateTime.Now;
            this.testbudgetAllocation.InsightScore = this.testAllocationParameters.InsightThreshold - .1;
            this.testbudgetAllocation.CampaignStart = now;
            this.testbudgetAllocation.PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1);
            this.testbudgetAllocation.CampaignEnd = now.AddDays(10);

            this.reallocation.RisePhaseAllocation(new List<MeasureSet>(), this.testbudgetAllocation);
            Assert.AreEqual(1, this.testbudgetAllocation.Phase);
        }

        /// <summary>
        /// Tests the decision to go into phase one or two with not enough time not enough insight
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationPhaseChoiceTestNotEnoughTimeNotEnoughInsight()
        {
            // create a campaign with percent of time passed more than the PhaseOneExitPercentage, and with lack of insight
            var now = DateTime.Now;
            this.testbudgetAllocation.InsightScore = this.testAllocationParameters.InsightThreshold - .1;
            this.testbudgetAllocation.CampaignStart = now;
            this.testbudgetAllocation.PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) + 1);
            this.testbudgetAllocation.CampaignEnd = now.AddDays(10);

            this.reallocation.RisePhaseAllocation(new List<MeasureSet>(), this.testbudgetAllocation);
            Assert.AreEqual(2, this.testbudgetAllocation.Phase);
        }

        /// <summary>
        /// Tests the decision to go into phase one or two with enough time enough insight
        /// </summary>
        [TestMethod]
        public void RisePhaseAllocationPhaseChoiceTestEnoughTimeEnoughInsight()
        {
            // create a campaign with percent of time passed less than the PhaseOneExitPercentage, and with insight
            var now = DateTime.Now;
            this.testbudgetAllocation.InsightScore = this.testAllocationParameters.InsightThreshold + .1;
            this.testbudgetAllocation.CampaignStart = now;
            this.testbudgetAllocation.PeriodStart = now.AddDays((int)(10 * this.testAllocationParameters.PhaseOneExitPercentage) - 1);
            this.testbudgetAllocation.CampaignEnd = now.AddDays(10);

            this.reallocation.RisePhaseAllocation(new List<MeasureSet>(), this.testbudgetAllocation);
            Assert.AreEqual(2, this.testbudgetAllocation.Phase);
        }

        /// <summary>
        /// test for GetLargestBudgetedNodes with limited count
        /// </summary>
        [TestMethod]
        public void GetLargestReturnOnAdSpendNodesTestLimitedCount()
        {
            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.ReturnOnAdSpend = this.testMetricsGen(ms) * .024m;

                // give the node a budget so it doesn't look like a gooseegg
                node.Value.PeriodTotalBudget = .01m;
                node.Value.PeriodMediaBudget = .01m;

                // Setup delivery of 1
                this.testNodeDeliveryMetrics[ms] = SetupNodeDeliveryMetricsStub(1, .001m, lookBack);
            }

            this.testbudgetAllocation.PeriodBudget = 10;
            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.ReturnOnAdSpend)
                .OrderByDescending(returnOnAdSpend => returnOnAdSpend)
                .Take(5)
                .ToList();

            var nodes = this.reallocation.GetLargestReturnOnAdSpendNodes(this.testbudgetAllocation, 5);
            var actual = nodes
                .Select(pnr => pnr.Value.ReturnOnAdSpend)
                .OrderByDescending(returnOnAdSpend => returnOnAdSpend)
                .ToList();

            Assert.AreEqual(5, actual.Count());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for GetLargestBudgetedNodes with limited budget
        /// </summary>
        [TestMethod]
        public void GetLargestReturnOnAdSpendNodesTestLimitedBudget()
        {
            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.ReturnOnAdSpend = this.testMetricsGen(ms) * .024m;
                node.Value.PeriodTotalBudget = this.testMetricsGen(ms) * .024m;
                node.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
      
                // Setup delivery of 1
                this.testNodeDeliveryMetrics[ms] = SetupNodeDeliveryMetricsStub(1, .001m, lookBack);
            }

            this.testbudgetAllocation.PeriodBudget = .76m;
            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.ReturnOnAdSpend)
                .OrderByDescending(returnOnAdSpend => returnOnAdSpend)
                .Take(5)
                .ToList();

            var nodes = this.reallocation.GetLargestReturnOnAdSpendNodes(this.testbudgetAllocation, 10);
            var actual = nodes
                .Select(pnr => this.testbudgetAllocation.PerNodeResults[pnr.Key].ReturnOnAdSpend)
                .OrderByDescending(returnOnAdSpend => returnOnAdSpend)
                .ToList();

            Assert.AreEqual(5, actual.Count());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for SortByValueVolumeScore
        /// </summary>
        [TestMethod]
        public void SortByValueVolumeScoreTest()
        {
            var lookBack = (int)this.testbudgetAllocation.PeriodDuration.TotalHours;
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.Valuation = this.testMetricsGen(ms) * .024m;
                node.Value.LineagePenalty = this.testAllocationParameters.LineagePenaltyNeutral;

                // Setup delivery of 1
                this.testNodeDeliveryMetrics[ms] = SetupNodeDeliveryMetricsStub(1, 1, lookBack);
            }

            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.Valuation)
                .OrderBy(valuation => valuation)
                .ToList();
            
            var actual = Reallocation.SortByValueVolumeScore(
                this.testbudgetAllocation,
                this.testbudgetAllocation.PerNodeResults,
                this.testbudgetAllocation.NodeDeliveryMetricsCollection)
                .Select(pnr =>
                    this.testbudgetAllocation.PerNodeResults[pnr.Key].Valuation)
                .ToList();

            // For volume of 1 valuation sort should equal ValueVolume sort
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for SortByValueVolumeScore
        /// </summary>
        [TestMethod]
        public void SortByBudgetTest()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = (ms.Any() ? ms.Sum() + (ms.Count > 1 ? 1 : 0) : 0) * .024m;
            }

            var expected = this.testbudgetAllocation
                .PerNodeResults.Select(pnr => pnr.Value.PeriodTotalBudget)
                .OrderByDescending(budget => budget)
                .ToList();

            var actual = Reallocation.SortByBudget(
                this.testbudgetAllocation.PerNodeResults)
                .Select(pnr =>
                    this.testbudgetAllocation.PerNodeResults[pnr.Key].PeriodTotalBudget)
                .ToList();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for AddHighBudgetNodesToMakeSpend
        /// </summary>
        [TestMethod]
        public void AddHighBudgetNodesToMakeSpendTest()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = 100 - ms.Sum() - (ms.Count > 1 ? 1 : 0);
                node.Value.Valuation = ms.Sum() + (ms.Count > 1 ? 1 : 0);
            }

            this.testbudgetAllocation.AnticipatedSpendForDay = 379m;

            var exportNodes = this.testbudgetAllocation
                .PerNodeResults
                .OrderByDescending(pnr => pnr.Value.Valuation)
                .Take(4)
                .Reverse()
                .ToList();
            var sortedSwapPool = this.testbudgetAllocation
                .PerNodeResults
                .OrderByDescending(pnr => pnr.Value.PeriodTotalBudget)
                .Take(4)
                .ToList();
            var originalExportNodes = exportNodes.ToList();

            var expected = new List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> 
            { 
                sortedSwapPool[0], 
                sortedSwapPool[1], 
                originalExportNodes[2], 
                originalExportNodes[3] 
            };
              
            Reallocation.AddHighBudgetNodesToMakeSpend(
                 this.testbudgetAllocation,
                 ref exportNodes,
                 sortedSwapPool);

            Assert.IsTrue(expected.SequenceEqual(exportNodes));
        }

        /// <summary>
        /// test for AddHighBudgetNodesToMakeSpend with potential duplicates
        /// </summary>
        [TestMethod]
        public void AddHighBudgetNodesToMakeSpendTestDuplicates()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = 100 - ms.Sum() - (ms.Count > 1 ? 1 : 0);
                node.Value.Valuation = ms.Sum() + (ms.Count > 1 ? 1 : 0);
            }

            // make one node both high budget and high value
            this.testbudgetAllocation.PerNodeResults.Skip(1).First().Value.Valuation = 11;

            this.testbudgetAllocation.AnticipatedSpendForDay = 384m;

            var exportNodes = this.testbudgetAllocation
                .PerNodeResults
                .OrderByDescending(pnr => pnr.Value.Valuation)
                .Take(4)
                .Reverse()
                .ToList();
            var sortedSwapPool = this.testbudgetAllocation
                .PerNodeResults
                .OrderByDescending(pnr => pnr.Value.PeriodTotalBudget)
                .Take(4)
                .ToList();
            var originalExportNodes = exportNodes.ToList();

            var expected = new List<KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>> 
            { 
                sortedSwapPool[0], 
                sortedSwapPool[2], 
                originalExportNodes[2], 
                originalExportNodes[3]
            };

            Reallocation.AddHighBudgetNodesToMakeSpend(
                 this.testbudgetAllocation,
                 ref exportNodes,
                 sortedSwapPool);

            // No duplicates
            Assert.AreEqual(exportNodes.Count, exportNodes.Distinct().Count());

            Assert.IsTrue(expected.SequenceEqual(exportNodes));
        }

        /// <summary>
        /// test for AddExperimentationNodes
        /// </summary>
        [TestMethod]
        public void AddExperimentationNodesTest()
        {
            // at this point the export node list should add up to the full budget, but we should have more than the 
            // lower of the two export nodes counts nodes in the export list
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = ms.Sum() + (ms.Count > 1 ? 1 : 0);
                node.Value.LineagePenalty = this.testAllocationParameters.LineagePenaltyNeutral;
                node.Value.ExportCount = 0;
            }

            this.testbudgetAllocation.AllocationParameters.InitialMaxNumberOfNodes = 6;
            this.testbudgetAllocation.AllocationParameters.AllocationTopTier = 3;
            this.testbudgetAllocation.AllocationParameters.AllocationNumberOfTiersToAllocateTo = 2;

            this.testbudgetAllocation.PeriodBudget = 30;
            this.testbudgetAllocation.AnticipatedSpendForDay = 25;

            var exportNodes = this.testbudgetAllocation.PerNodeResults.OrderByDescending(pnr => pnr.Value.PeriodTotalBudget).Take(6).ToList();

            var actual = this.reallocation.PhaseThreePointFive(this.testbudgetAllocation.PerNodeResults.ToList(), this.testbudgetAllocation);

            // We should have precisley the period budget
            Assert.AreEqual(
                this.testbudgetAllocation.PeriodBudget,
                actual.Sum(ms => this.testbudgetAllocation.PerNodeResults[ms].PeriodTotalBudget));

            // in this setup we should have the top 4 budgeted export nodes still in the output
            Assert.IsTrue(
                exportNodes
                    .OrderByDescending(pnr => pnr.Value.PeriodTotalBudget)
                    .Take(4)
                    .All(pnr => actual.Contains(pnr.Key)));

            // and the top two node score no insight nodes should also be in the output
            Assert.IsTrue(
                this.testbudgetAllocation.PerNodeResults
                    .Where(pnr => pnr.Key.Count > 1)
                    .OrderByDescending(pnr => pnr.Value.NodeScore)
                    .Take(2)
                    .All(pnr => actual.Contains(pnr.Key)));
        }

        /// <summary>
        /// test for PhaseFour
        /// </summary>
        [TestMethod]
        public void PhaseFourTest()
        {
            //// here we are using less than the lower of the two export counts to make budget
            //// and we want to fill out the export count by 'rarifying' our exports

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                node.Value.PeriodTotalBudget = 100 / (decimal)node.Value.NodeScore;
                node.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
                node.Value.ExportCount = 1;
            }

            // our export node list is shorter than the small export count
            this.testAllocationParameters.MaxNodesToExport = 7;
            var exportNodes = this.testbudgetAllocation.PerNodeResults.OrderBy(pnr => pnr.Value.NodeScore).Take(3).ToList();
            var initialExportNodes = this.testbudgetAllocation.PerNodeResults.OrderBy(pnr => pnr.Value.NodeScore).Take(3).ToList();

            this.reallocation.PhaseFour(this.testbudgetAllocation, exportNodes);

            // in this set up we were able to replace a node with some rarer nodes
            Assert.IsTrue(exportNodes.Count > initialExportNodes.Count);

            // the new nodes should have larger nodeScores than the old nodes
            Assert.IsTrue(
                exportNodes
                    .Where(pnr => !initialExportNodes.Contains(pnr))
                    .All(pnr => initialExportNodes.All(pnr2 => pnr2.Value.NodeScore <= pnr.Value.NodeScore)));

            var initialExportBudget = initialExportNodes.Sum(pnr => pnr.Value.PeriodTotalBudget);
            var finalExportBudget = exportNodes.Sum(pnr => pnr.Value.PeriodTotalBudget);

            // the exported budget should have not decreased
            Assert.IsTrue(initialExportBudget <= finalExportBudget);
        }
            
        /// <summary>
        /// test for PhaseFour with a low number of export nodes
        /// </summary>
        [TestMethod]
        public void PhaseFourTestLowNodes()
        {
            //// here we are using less than the lower of the two export counts to make budget
            //// and we want to fill out the export count by 'rarifying' our exports

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = 100 / (decimal)node.Value.NodeScore;
                node.Value.ExportCount = 1;
                node.Value.PeriodMediaBudget = .01m; // all nodes should be elgible for export
            }

            // our export node list is shorter than the small export count
            this.testAllocationParameters.MaxNodesToExport = 70;
            var exportNodes = this.testbudgetAllocation.PerNodeResults.OrderBy(pnr => pnr.Value.NodeScore).Take(3).ToList();
            var initialExportNodes = this.testbudgetAllocation.PerNodeResults.OrderBy(pnr => pnr.Value.NodeScore).Take(3).ToList();

            this.reallocation.PhaseFour(this.testbudgetAllocation, exportNodes);

            // in this set up we were able to replace a node with some rarer nodes
            Assert.IsTrue(exportNodes.Count > initialExportNodes.Count);

            // the new nodes should have larger nodeScores than the old nodes
            Assert.IsTrue(
                exportNodes
                    .Where(pnr => !initialExportNodes.Contains(pnr))
                    .All(pnr => initialExportNodes.All(pnr2 => pnr2.Value.NodeScore <= pnr.Value.NodeScore)));

            var initialExportBudget = initialExportNodes.Sum(pnr => pnr.Value.PeriodTotalBudget);
            var finalExportBudget = exportNodes.Sum(pnr => pnr.Value.PeriodTotalBudget);

            // the exported budget should have not decreased
            Assert.IsTrue(initialExportBudget <= finalExportBudget);
        }
            
        /// <summary>
        /// test for AddTopNodeRankNodes
        /// </summary>
        [TestMethod]
        public void AddTopNodeRankNodesTest()
        {
            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                var ms = node.Key;
                node.Value.PeriodTotalBudget = 100 - ms.Sum() - (ms.Count > 1 ? 1 : 0);
                node.Value.Valuation = ms.Sum() + (ms.Count > 1 ? 1 : 0);
            }

            // bump the nodescore of 1 down to guarantee it sorts lower than 3
            var measureSet1 = new MeasureSet { 1 };
            this.testPerNodeResults[measureSet1].NodeScore--;

            this.testbudgetAllocation.PeriodBudget = 400;
            var nodesToAdd = 3;

            var newExportMeasureSets = this.testbudgetAllocation
                .PerNodeResults
                .Take(4)
                .Reverse()
                .Select(pnr => pnr.Key)
                .ToList();
            var swapSet = this.testbudgetAllocation
                .PerNodeResults
                .Take(4)
                .Select(pnr => pnr.Key)
                .ToList();

            var expectedMeasureSetsToAdd = swapSet
                .Select(ms => this.testbudgetAllocation.PerNodeResults.Single(pnr => pnr.Key == ms))
                .OrderByDescending(pnr => pnr.Value.NodeScore)
                .Select(pnr => pnr.Key)
                .Take(3)
                .ToList();
           
            this.reallocation.AddTopNodeRankNodes(nodesToAdd, this.testbudgetAllocation, swapSet, ref newExportMeasureSets);

            Assert.AreEqual(7, newExportMeasureSets.Count);
            Assert.IsTrue(expectedMeasureSetsToAdd.All(ms => newExportMeasureSets.Contains(ms)));
            Assert.IsTrue(expectedMeasureSetsToAdd.All(ms => this.testbudgetAllocation.PerNodeResults[ms].PeriodTotalBudget == 3.33m));
        }

        /// <summary>
        /// test for the AverageExperimentalSpend method
        /// </summary>
        [TestMethod]
        public void EstimatedExperimentalSpendTest()
        {
            this.testbudgetAllocation.AllocationParameters.MinBudget = 5;
            this.testbudgetAllocation.PeriodDuration = new TimeSpan(3, 0, 0);

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                node.Value.ExportCount = 1;
            }

            var expected = this.testNodeDeliveryMetrics.First().Value.CalcEffectiveTotalSpend(null, null, 0, 0, 0);
            var actual = this.reallocation.EstimatedExperimentalSpend(this.testbudgetAllocation);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// test for the AverageExperimentalSpend method
        /// </summary>
        [TestMethod]
        public void EstimatedExperimentalSpendTestNoExportedNodes()
        {
            this.testbudgetAllocation.AllocationParameters.MinBudget = 5;
            this.testbudgetAllocation.PeriodDuration = new TimeSpan(3, 0, 0);

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                node.Value.ExportCount = 0;
            }

            var expected = this.testbudgetAllocation.AllocationParameters.MinBudget;
            var actual = this.reallocation.EstimatedExperimentalSpend(this.testbudgetAllocation);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// test for the AverageExperimentalSpend method
        /// </summary>
        [TestMethod]
        public void EstimatedExperimentalSpendTestHighSpend()
        {
            this.testbudgetAllocation.AllocationParameters.MinBudget = 3;
            this.testbudgetAllocation.PeriodDuration = new TimeSpan(3, 0, 0);

            foreach (var node in this.testbudgetAllocation.PerNodeResults)
            {
                node.Value.ExportCount = 1;
            }

            var expected = this.testbudgetAllocation.AllocationParameters.MinBudget;
            var actual = this.reallocation.EstimatedExperimentalSpend(this.testbudgetAllocation);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// test BudgetMeasureSet calculates base budget for all nodes.
        /// </summary>
        [TestMethod]
        public void BudgetMeasureSetsTestAllNodes()
        {
            var actualBudgets = this.reallocation.BudgetMeasureSets(this.testbudgetAllocation);
            
            // All nodes should have a budget
            Assert.AreEqual(this.testPerNodeResults.Count, actualBudgets.Count);
            
            // Nodes with no history should have zero budget
            var measureSet135 = new MeasureSet { 1, 3, 5 };
            Assert.AreEqual(1, actualBudgets.Count(b => b.Value == 0));
            Assert.AreEqual(0, actualBudgets[measureSet135]);
        }

        /// <summary>Set up node delivery metrics stubs given multiplier for the node.</summary>
        /// <param name="impressionRate">Impression rate to use.</param>
        /// <param name="mediaSpendRate">Media spend rate to use.</param>
        /// <param name="lookBack">Duration of period.</param>
        /// <returns>The INodeDeliveryMetrics stub.</returns>
        private static IEffectiveNodeMetrics SetupNodeDeliveryMetricsStub(decimal impressionRate, decimal mediaSpendRate, int lookBack)
        {
            // Set the other rates accordingly for a duration of lookBack.
            // Total spend is arbitrarily 50% more than media spend
            var nodeDeliveryMetrics = new FakeEffectiveNodeMetrics
                {
                    EffectiveImpressionRate = impressionRate,
                    EffectiveImpressions = (long)impressionRate * lookBack,
                    EffectiveMediaSpendRate = mediaSpendRate,
                    EffectiveMediaSpend = mediaSpendRate * lookBack,
                    EffectiveTotalSpend = mediaSpendRate * lookBack * 1.5m,
                    TotalEligibleHours = 1
                };

            return nodeDeliveryMetrics;
        }
    }
}