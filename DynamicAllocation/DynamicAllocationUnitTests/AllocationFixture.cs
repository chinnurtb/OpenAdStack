// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for Allocation
    /// </summary>
    [TestClass]
    public class AllocationFixture
    {
        /// <summary>Test allocation paramters</summary>
        private static AllocationParameters testAllocationParameters;

        /// <summary>Allocation algo instance</summary>
        private static Allocation allocation;

        /// <summary>
        /// Per test initialization
        /// </summary>
        /// <param name="context">test context</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            TestUtilities.AllocationParametersDefaults.Initialize();
            testAllocationParameters = new AllocationParameters();
            var measureMap = new MeasureMap(new[] { new EmbeddedJsonMeasureSource(Assembly.GetExecutingAssembly(), "DynamicAllocationUnitTests.Resources.MeasureMap.js") });
            allocation = new Allocation(new MeasureInfo(measureMap));
        }

        /// <summary>
        /// test caluculate period budget with exactly one day left
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetOneDay()
        {
            // exactly a day left - allocate the whole remaining budget
            var budget = Allocation.CalculatePeriodBudget(1000, new TimeSpan(1, 0, 0, 0), new TimeSpan(1, 0, 0, 0));
            Assert.AreEqual(1000, budget);
        }

        /// <summary>
        /// test caluculate period budget with less than one day left
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetLessThanOneDay()
        {
            // less than a day left - allocate the whole remaining budget
            var budget = Allocation.CalculatePeriodBudget(1000, new TimeSpan(0, 12, 0, 0), new TimeSpan(1, 0, 0, 0));
            Assert.AreEqual(1000, budget);
        }

        /// <summary>
        /// test caluculate period budget with a little more than one day left
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetMoreThanOneDay()
        {
            // a little more than a day left - allocate most of remaining budget
            var budget = Allocation.CalculatePeriodBudget(1000, new TimeSpan(1, 1, 0, 0), new TimeSpan(1, 0, 0, 0));
            Assert.AreEqual(1000 * 24m / 25m, budget);
        }

        /// <summary>
        /// test caluculate period budget with two days left
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetTwoDays()
        {
            // two daya left - allocate half of remaining budget
            var budget = Allocation.CalculatePeriodBudget(1000, new TimeSpan(2, 0, 0, 0), new TimeSpan(1, 0, 0, 0));
            Assert.AreEqual(500, budget);
        }

        /// <summary>
        /// test caluculate period budget with a non day length period
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetNonDayLengthPeriod()
        {
            // six four-hour periods left - allocate a sixth of the remaining budget
            var budget = Allocation.CalculatePeriodBudget(1000, TimeSpan.FromDays(1), TimeSpan.FromHours(4));
            Assert.AreEqual(1000 / 6m, budget);
        }

        /// <summary>
        /// test calculate period budget is zero with no remaining time.
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetZeroRemainingTime()
        {
            // No time left, duration doesn't matter
            var budget = Allocation.CalculatePeriodBudget(1000, TimeSpan.FromTicks(0), TimeSpan.FromHours(4));
            Assert.AreEqual(0m, budget);
        }

        /// <summary>
        /// test calculate period budget is zero with zero period duration.
        /// </summary>
        [TestMethod]
        public void TestCalculatePeriodBudgetZeroDuration()
        {
            // No time left, duration doesn't matter
            var budget = Allocation.CalculatePeriodBudget(1000, TimeSpan.FromHours(4), TimeSpan.FromTicks(0));
            Assert.AreEqual(0m, budget);
        }

        /// <summary>
        /// test for PerNodeResultsFromBudgets
        /// </summary>
        [TestMethod]
        public void PerNodeResultsFromBudgetsTest()
        {
            // given a set of budgets, populate a set of pernoderesults.
            var measureBase = 1106005;
            var measureSets = new List<MeasureSet> 
                                  {
                                      new MeasureSet { measureBase + 1 },
                                      new MeasureSet { measureBase + 2 },
                                      new MeasureSet { measureBase + 3 },
                                      new MeasureSet { measureBase + 1, measureBase + 2 },
                                      new MeasureSet { measureBase + 1, measureBase + 3 },
                                      new MeasureSet { measureBase + 2, measureBase + 3 },
                                      new MeasureSet { measureBase + 1, measureBase + 2, measureBase + 3 },
                                  };

            var budgets = measureSets.ToDictionary(ms => ms, ms => (decimal)(ms.Sum() + ms.Count()));

            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = this.CreatePerNodeResults(measureSets),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = testAllocationParameters
            };

            testAllocationParameters.DefaultEstimatedCostPerMille = 1;

            allocation.PerNodeResultsFromBudgets(ref budgetAllocation, budgets);

            // make sure each measureSet has a pernoderesult (its contents will be checked in the CalculateCaps test)
            Assert.IsTrue(budgets.Keys.All(budgetAllocation.PerNodeResults.ContainsKey));
        }

        /// <summary>
        /// A test for GetHistoryMetrics when there is no history for this measureSet
        /// </summary>
        [TestMethod]
        public void GetHistoryMetricsTestWithNoHistory()
        {
            var measureSet = new MeasureSet { 1106006 };
            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult> 
                { 
                    { 
                        measureSet, 
                        new PerNodeBudgetAllocationResult
                        { 
                            Valuation = 1 
                        } 
                    } 
                },
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = testAllocationParameters
            };

            var overallBudget = 10;
            testAllocationParameters.DefaultEstimatedCostPerMille = 1;
            testAllocationParameters.PerMilleFees = 0;

            var actual = allocation.GetHistoryMetrics(measureSet, budgetAllocation, overallBudget);

            // this measure has a datacost of .25/mille
            Assert.AreEqual(8m, actual.MediaSpend);
            Assert.AreEqual(10667, actual.Impressions);
            Assert.AreEqual(.25m, actual.EstimatedNonMediaCostPerMille);
        }

        /// <summary>
        /// A test for GetHistoryMetrics when there is history for this measureSet with values of zero
        /// </summary>
        [TestMethod]
        public void GetHistoryMetricsTestWithZeroHistory()
        {
            var measureSet = new MeasureSet { 1106006 };
            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                    {
                        { 
                            measureSet, 
                            new PerNodeBudgetAllocationResult
                            {
                                Valuation = 1
                            }
                        }
                    },
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>
                    {
                        {
                            measureSet,
                            new FakeEffectiveNodeMetrics
                                {
                                    EffectiveMediaSpend = 0,
                                    EffectiveImpressions = 0,
                                    EffectiveTotalSpend = 0
                                }
                         }
                    },
                AllocationParameters = testAllocationParameters
            };
        
            var overallBudget = 10;
            testAllocationParameters.DefaultEstimatedCostPerMille = 1;
            testAllocationParameters.PerMilleFees = 0;

            var actual = allocation.GetHistoryMetrics(measureSet, budgetAllocation, overallBudget);
            
            // this measure has a datacost of .25/mille
            Assert.AreEqual(8m, actual.MediaSpend);
            Assert.AreEqual(10667, actual.Impressions);
            Assert.AreEqual(.25m, actual.EstimatedNonMediaCostPerMille);
        }

        /// <summary>
        /// A test for GetHistoryMetrics when there is history for this measureSet with nonzero values
        /// </summary>
        [TestMethod]
        public void GetHistoryMetricsTestWithNonzeroHistory()
        {
            var measureSet = new MeasureSet { 1106006 };
            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                {
                    {
                        measureSet,
                        new PerNodeBudgetAllocationResult
                        {
                            Valuation = 1
                        }
                    }
                },
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>
                    {
                        {
                            measureSet,
                            new FakeEffectiveNodeMetrics
                                {
                                    EffectiveMediaSpend = 10.1m,
                                    EffectiveImpressions = 1001,
                                    EffectiveTotalSpend = 10.35025m
                                }
                         }
                    },
                AllocationParameters = testAllocationParameters
            };

            var overallBudget = 10;
            testAllocationParameters.DefaultEstimatedCostPerMille = 1;
            testAllocationParameters.PerMilleFees = 0;

            var actual = allocation.GetHistoryMetrics(measureSet, budgetAllocation, overallBudget);
            Assert.AreEqual(10.1m, actual.MediaSpend);
            Assert.AreEqual(1001, actual.Impressions);
            Assert.AreEqual(.25m, actual.EstimatedNonMediaCostPerMille);
        }
        
        /// <summary>
        /// A test for CalculateCaps
        /// </summary>
        [TestMethod]
        public void CalculateCapsTest()
        {
            var measureSet = new MeasureSet { 1106001 };
            const int OverallBudget = 100;
            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                {
                    {
                        measureSet,
                        new PerNodeBudgetAllocationResult
                        {
                            Valuation = 2
                        }
                    }
                },
                AllocationParameters = testAllocationParameters,
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>()
            };

            var actual = budgetAllocation.PerNodeResults.Single();

            allocation.CalculateCaps(
                actual,
                budgetAllocation, 
                OverallBudget);
            
            // Assert the MaxBid is plausible
            Assert.IsTrue(actual.Value.MaxBid > 0);
            Assert.IsTrue(actual.Value.MaxBid < actual.Value.Valuation);

            // Assert PeriodMediaBudget, PeriodTotalBudget, PeriodImpressionCap get set
            // and are plausible
            Assert.IsTrue(actual.Value.PeriodMediaBudget > 0 && actual.Value.PeriodTotalBudget > actual.Value.PeriodMediaBudget);
            Assert.AreEqual(OverallBudget, actual.Value.PeriodTotalBudget);
            Assert.IsTrue(actual.Value.PeriodImpressionCap > 0);
        }

        /// <summary>
        /// Test ToString override of PerNodeBudgetAllocationResult
        /// </summary>
        [TestMethod]
        public void PerNodeBudgetAllocationResultToString()
        {
            var r = new Random();
            var result = new PerNodeBudgetAllocationResult
            {
                AllocationId = Guid.NewGuid().ToString("N"),
                ExportBudget = (decimal)r.NextDouble(),
                ExportCount = r.Next(),
                LifetimeImpressions = r.Next(),
                EffectiveImpressionRate = (decimal)r.NextDouble(),
                LifetimeMediaSpend = r.Next(),
                EffectiveMediaSpendRate = (decimal)r.NextDouble(),
                NodeScore = r.NextDouble()
            };

            var resultString = result.ToString();
            Assert.IsNotNull(resultString);
        }

        /// <summary>
        /// create a set of test valuations from a list of measureSets
        /// </summary>
        /// <param name="measureSets">the list of measureSetd</param>
        /// <returns>the valuations</returns>
        private Dictionary<MeasureSet, PerNodeBudgetAllocationResult> CreatePerNodeResults(List<MeasureSet> measureSets)
        {
            return measureSets.ToDictionary(
                ms => ms, 
                ms => new PerNodeBudgetAllocationResult
                {
                    Valuation = (decimal)(ms.Sum() + ms.Count()),
                });
        }
    }
}
