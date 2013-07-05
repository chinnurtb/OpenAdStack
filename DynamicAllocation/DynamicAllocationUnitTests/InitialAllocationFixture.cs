// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InitialAllocationFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for InitialAllocation
    /// </summary>
    [TestClass]
    public class InitialAllocationFixture
    {
        /// <summary>a DynamicAllocationService used for testing</summary>
        private DynamicAllocationEngine dynamicAllocationEngine;

        /// <summary>Test measures</summary>
        private List<long> testMeasures;

        /// <summary>Test measure sets</summary>
        private Collection<MeasureSet> testMeasureSets;

        /// <summary>test allocation parameters</summary>
        private AllocationParameters testAllocationParameters;

        /// <summary>test budgetAllocation</summary>
        private BudgetAllocation testBudgetAllocation;

        /// <summary>Measure info</summary>
        private MeasureInfo measureInfo;

        /// <summary>an intialAllocation instance </summary>
        private InitialAllocation intialAllocation;
        
        /// <summary>
        /// per test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // create a zero data cost measure map
            var measureMap = new MeasureMap(
                DynamicAllocationTestUtilities.TestMeasureMap
                .ToDictionary(
                    m => m.Key,
                    m => (IDictionary<string, object>)m.Value
                        .ToDictionary(
                            m2 => m2.Key,
                            m2 => m2.Key == MeasureValues.DataProvider ? m2.Value : 0)));

            TestUtilities.AllocationParametersDefaults.Initialize();
            
            this.testAllocationParameters = new AllocationParameters();
            this.testAllocationParameters.DefaultEstimatedCostPerMille = .75m;
            this.testAllocationParameters.PerMilleFees = 0;
            this.testAllocationParameters.Margin = 1;

            // create a number of measures (small enough we can hand calculate the exact expected values)
            var measureCount = 3;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);

            // create a three measure campaign history
            this.testBudgetAllocation = new BudgetAllocation
            {  
                PerNodeResults = this.testMeasureSets.ToDictionary(
                    ms => ms, 
                    ms => new PerNodeBudgetAllocationResult
                    {
                        Valuation = 2m * ms.Count,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = this.testAllocationParameters,
                TotalBudget = 2000,
                RemainingBudget = 2000,
                CampaignStart = new DateTime(2011, 12, 31).AddDays(-3),
                CampaignEnd = new DateTime(2011, 12, 31),
                PeriodDuration = new TimeSpan(1, 0, 0, 0)
            };

            this.measureInfo = new MeasureInfo(measureMap);

            this.dynamicAllocationEngine = new DynamicAllocationEngine(measureMap);

            this.intialAllocation = new InitialAllocation(this.measureInfo);
        }

        /// <summary>
        /// a basic test of of GetBudgetAllocations
        /// </summary>
        [TestMethod]
        public void TestInitialAllocationSimple()
        {
            this.testBudgetAllocation.PeriodStart = this.testBudgetAllocation.CampaignStart;
            var actual = this.dynamicAllocationEngine.GetBudgetAllocations(this.testBudgetAllocation);

            // check that the right number of nodes were allocated to
            Assert.AreEqual(7, actual.PerNodeResults.Count);

            DynamicAllocationTestUtilities.AssertTotalAllocatedBudgetSumIsCorrect(
                this.testBudgetAllocation.RemainingBudget * 2,
                this.testBudgetAllocation.CampaignEnd - this.testBudgetAllocation.CampaignStart,
                actual, 
                this.testAllocationParameters.BudgetBuffer);
            DynamicAllocationTestUtilities.AssertPerNodeResultsAreSelfConsistent(this.testBudgetAllocation, actual.PerNodeResults, this.measureInfo);

            foreach (var perNodeResult in actual.PerNodeResults)
            {
                Assert.AreEqual(perNodeResult.Value.PeriodMediaBudget, perNodeResult.Value.ExportBudget);

                // the export count should not be updated by DA during allocation
                Assert.AreEqual(0, perNodeResult.Value.ExportCount);
            }
        }

        /// <summary>
        /// test of GetBudgetAllocations with per mille cost
        /// </summary>
        [TestMethod]
        public void TestAllocationWithNonMediaCost()
        {
            var measureMap = new MeasureMap(DynamicAllocationTestUtilities.TestMeasureMap);
            this.measureInfo = new MeasureInfo(measureMap);
            this.dynamicAllocationEngine = new DynamicAllocationEngine(measureMap);

            this.testBudgetAllocation.PeriodStart = this.testBudgetAllocation.CampaignStart;
            var actual = this.dynamicAllocationEngine.GetBudgetAllocations(this.testBudgetAllocation);

            // check that the 'wealthy' node is in the perNodeResults but has zero budget - its data costs are too high to serve.
            Assert.AreEqual(7, actual.PerNodeResults.Count);
            Assert.IsTrue(actual.PerNodeResults.ContainsKey(new MeasureSet { 3 }));
            Assert.AreEqual(0, actual.PerNodeResults[new MeasureSet(new long[] { 3 })].PeriodTotalBudget);

            DynamicAllocationTestUtilities.AssertTotalAllocatedBudgetSumIsCorrect(
                this.testBudgetAllocation.TotalBudget * 2, 
                this.testBudgetAllocation.CampaignEnd - this.testBudgetAllocation.CampaignStart, 
                actual,
                this.testAllocationParameters.BudgetBuffer);
            DynamicAllocationTestUtilities.AssertPerNodeResultsAreSelfConsistent(this.testBudgetAllocation, actual.PerNodeResults, this.measureInfo);
        }

        /// <summary>
        /// test of GetBudgetAllocations with per mille fees
        /// </summary>
        [TestMethod]
        public void TestAllocationFees()
        {
            this.testBudgetAllocation.PeriodStart = this.testBudgetAllocation.CampaignStart;
            this.testBudgetAllocation.AllocationParameters.PerMilleFees = .1m;
            var actual = this.dynamicAllocationEngine.GetBudgetAllocations(this.testBudgetAllocation);

            // check that all the nodes were allocated to
            Assert.AreEqual(7, actual.PerNodeResults.Count);

            DynamicAllocationTestUtilities.AssertTotalAllocatedBudgetSumIsCorrect(
                this.testBudgetAllocation.TotalBudget * 2,
                this.testBudgetAllocation.CampaignEnd - this.testBudgetAllocation.CampaignStart, 
                actual, 
                this.testAllocationParameters.BudgetBuffer);
            DynamicAllocationTestUtilities.AssertPerNodeResultsAreSelfConsistent(this.testBudgetAllocation, actual.PerNodeResults, this.measureInfo);
        }

        /// <summary>
        /// test of GetBudgetAllocations with margin, data costs, and serving fees 
        /// </summary>
        [TestMethod]
        public void TestAllocationWithMarginNonMediaCosts()
        {
            var measureMap = new MeasureMap(DynamicAllocationTestUtilities.TestMeasureMap);
            this.measureInfo = new MeasureInfo(measureMap);
            this.dynamicAllocationEngine = new DynamicAllocationEngine(measureMap);

            this.testBudgetAllocation.PeriodStart = this.testBudgetAllocation.CampaignStart;
            this.testBudgetAllocation.AllocationParameters.Margin = 1.15m; 
            this.testBudgetAllocation.AllocationParameters.PerMilleFees = .1m;
            var actual = this.dynamicAllocationEngine.GetBudgetAllocations(this.testBudgetAllocation);

            // check that the 'wealthy' node is in the perNodeResults but has zero budget - its data costs are too high to serve.
            Assert.AreEqual(7, actual.PerNodeResults.Count);
            Assert.IsTrue(actual.PerNodeResults.ContainsKey(new MeasureSet { 3 }));
            Assert.AreEqual(0, actual.PerNodeResults[new MeasureSet(new long[] { 3 })].PeriodTotalBudget);

            DynamicAllocationTestUtilities.AssertTotalAllocatedBudgetSumIsCorrect(
                this.testBudgetAllocation.TotalBudget * 2,
                this.testBudgetAllocation.CampaignEnd - this.testBudgetAllocation.CampaignStart,
                actual,
                this.testAllocationParameters.BudgetBuffer);

            // TODO: for this test we might need to be more stringant than simply self consistancy
            DynamicAllocationTestUtilities.AssertPerNodeResultsAreSelfConsistent(this.testBudgetAllocation, actual.PerNodeResults, this.measureInfo);
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
            var budget = Allocation.CalculatePeriodBudget(1000, TimeSpan.FromDays(1), TimeSpan.FromHours(4).Ticks);
            Assert.AreEqual(1000 / 6m, budget);
        }

        /// <summary>
        /// test for AddBestGreedyMeasureSet with fullcoverage on a small graph 
        /// </summary>
        [TestMethod]
        public void AddBestGreedyMeasureSetTestFullCoverageSmallGraph()
        {
            // create a number of measures
            var measures = Enumerable.Range(1, 3).Select(m => (long)m).ToList();

            // create a full powerset of them
            var measureSets = MeasureSet.PowerSet(measures);

            // set up the tier numbers we are looking at
            var allocationTopTier = 2;
            var tierNumber = 2;
            var measureSetsToCover = measureSets
                .Where(ms => ms.Count <= allocationTopTier - 1)
                .GroupBy(ms => ms.Count)
                .ToDictionary(grp => grp.Key, grp => grp.ToList()); 
            var tier = measureSets.Where(ms => ms.Count == tierNumber).ToList();

            // try to add sets 3 times (the third time should add the last measureSet)
            var actual = new List<MeasureSet>();
            InitialAllocation.AddBestGreedyMeasureSet(ref actual, ref tier, ref measureSetsToCover, 1);
            InitialAllocation.AddBestGreedyMeasureSet(ref actual, ref tier, ref measureSetsToCover, 1);
            InitialAllocation.AddBestGreedyMeasureSet(ref actual, ref tier, ref measureSetsToCover, 1);

            // should have three measureSets in actual and have full coverage
            var expected = new List<MeasureSet> { new MeasureSet { 1, 2 }, new MeasureSet { 1, 3 }, new MeasureSet { 2, 3 } };
            Assert.IsTrue(expected.SequenceEqual(actual.OrderBy(ms => ms)));
            Assert.AreEqual(0, measureSetsToCover.Sum(kvp => kvp.Value.Count));
        }

        /// <summary>
        /// test for AddBestGreedyMeasureSet where we add the first two measure sets on a big graph 
        /// (these are the only two I know for sure how much should get covered and which set should get added)
        /// </summary>
        [TestMethod]
        public void AddBestGreedyMeasureSetTestFirstTwoAddsOnABigGraph()
        {
            // create a number of measures
            var measureCount = 12;
            var measures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            var measureSets = MeasureSet.PowerSet(measures);

            // set up the tier numbers we are looking at
            var allocationTopTier = 8;
            var tierNumber = 8;
            var measureSetsToCover = measureSets
                .Where(ms => ms.Count <= allocationTopTier - 1)
                .GroupBy(ms => ms.Count)
                .ToDictionary(grp => grp.Key, grp => grp.ToList()); 
            var tier = measureSets.Where(ms => ms.Count == tierNumber).ToList();

            // add the first set. 
            var actual = new List<MeasureSet>();
            var intialCoverCount = measureSetsToCover.Sum(kvp => kvp.Value.Count);
            InitialAllocation.AddBestGreedyMeasureSet(ref actual, ref tier, ref measureSetsToCover, 1);

            // should have the some measureSet in actual and have coverage of 2^tierNumber-2
            Assert.AreEqual((1 << tierNumber) - 2, measureSets.Where(ms => ms.Count <= allocationTopTier - 1).Count(ms => ms.IsSubsetOf(actual.First())));

            // make sure the right number of elements where removed from the measureSetsToCover
            Assert.AreEqual(measureCount - tierNumber, measureSetsToCover[1].Count);
            Assert.AreEqual(measureCount * (measureCount - 1) / 2, measureSetsToCover[2].Count);

            // add the second set. 
            var actual2 = new List<MeasureSet>();
            InitialAllocation.AddBestGreedyMeasureSet(ref actual2, ref tier, ref measureSetsToCover, 1);

            // should have the the measureSet in actual that is the first containing all the uncovered single measures.
            // (this assumes the number of measures is less than twice the tierNumber)
            // and we should have coverage of 2^tierNumber-1 - 2^(measureCount - tierNumber)
            Assert.IsTrue(measures.Where(ms => !actual.First().Contains(ms)).All(ms => actual2.First().Contains(ms)));
            Assert.AreEqual(
                (1 << tierNumber) - 1 - (1 << ((2 * tierNumber) - measureCount)), 
                measureSets
                    .Where(ms => ms.Count <= allocationTopTier - 1)
                    .Count(ms => !ms.IsSubsetOf(actual.First()) && ms.IsSubsetOf(actual2.First())));

            // make sure the right number of elements where removed from the measureSetsToCover
            Assert.AreEqual(0, measureSetsToCover[1].Count);
        }

        /// <summary>
        /// test for GreedyMaxCoverTest that just checks a couple basic things, 
        /// including that the correct number of sets gets added to the cover
        /// </summary>
        [TestMethod]
        public void GreedyMaxCoverTest()
        {
            // create a number of measures
            var measureCount = 13;
            var measures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            var measureSets = MeasureSet.PowerSet(measures);

            // set up the tier numbers we are looking at
            var allocationTopTier = 5;
            var tierNumber = 5;

            // create the Outputs for GreedyMaxCover
            var measureSetsToCover = measureSets
                .Where(ms => ms.Count <= allocationTopTier - 1)
                .GroupBy(ms => ms.Count)
                .ToDictionary(grp => grp.Key, grp => grp.ToList());
            var tier = measureSets.Where(ms => ms.Count == tierNumber).ToList();
            var numberOfNodesToAllocateOnThisTier = 10;

            var actual = InitialAllocation.GreedyMaxCover(ref tier, ref measureSetsToCover, numberOfNodesToAllocateOnThisTier);

            Assert.AreEqual(10, actual.Count());

            // the singles should be covered
            Assert.AreEqual(0, measureSetsToCover[1].Count);

            // in this set up, 4 pairs are left uncovered
            Assert.AreEqual(4, measureSetsToCover[2].Count);
        }

        /// <summary>
        /// test for BudgetMeasureSets that just checks that the correct number of nodes get added for each tier
        /// in the large full graph case
        /// </summary>
        [TestMethod]
        public void BudgetMeasureSetsTest()
        {   
            this.testAllocationParameters.AllocationNumberOfNodes = 200;

            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            // create a full powerset of them
            this.testMeasureSets = MeasureSet.PowerSet(this.testMeasures);

            var valuations = this.testMeasureSets.ToDictionary(ms => ms, ms => 1m);

            var actual = this.intialAllocation.BudgetMeasureSets(
                new BudgetAllocation
                {
                    PerNodeResults = this.testMeasureSets.ToDictionary(
                        ms => ms, 
                        ms => new PerNodeBudgetAllocationResult
                        {
                            Valuation = 1
                        }),
                    AllocationParameters = this.testAllocationParameters,
                    NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>()
                }, 
                1000);
            var groupedActual = actual.GroupBy(budget => budget.Key.Count).ToDictionary(grp => grp.Key, grp => grp.ToList());
            var totalAllocation = actual.Sum(budget => budget.Value);

            // there is one persona node, so they expected number of nodes is one less than the total number of nodes
            var expectedNumber = this.testAllocationParameters.AllocationNumberOfNodes - 1;

            Assert.AreEqual(this.testAllocationParameters.AllocationNumberOfNodes, actual.Count());
            DynamicAllocationTestUtilities.AssertWithin(8m / 15 * expectedNumber, groupedActual[4].Count, .5);
            DynamicAllocationTestUtilities.AssertWithin(4m / 15 * expectedNumber, groupedActual[5].Count, .5);
            DynamicAllocationTestUtilities.AssertWithin(2m / 15 * expectedNumber, groupedActual[6].Count, .5);
            DynamicAllocationTestUtilities.AssertWithin(1m / 15 * expectedNumber, groupedActual[7].Count, .5);
            DynamicAllocationTestUtilities.AssertWithin(1000, totalAllocation, .01);
        }

        /// <summary>
        /// basic test for NumberOfNodesToAllocateOnThisTier
        /// </summary>
        [TestMethod]
        public void NumberOfNodesToAllocateOnThisTierTestBasic()
        {
            // if there is only one tier being allocated to, then it should get the AllocationNumberOfNodes
            var actual = InitialAllocation.NumberOfNodesToAllocateOnThisTier(
                7, 
                7, 
                1, 
                this.testAllocationParameters.AllocationNumberOfNodes);
            var expected = this.testAllocationParameters.AllocationNumberOfNodes;
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// test for NumberOfNodesToAllocateOnThisTier with multiple tiers getting allocations
        /// </summary>
        [TestMethod]
        public void NumberOfNodesToAllocateOnThisTierTestSeveralTiers()
        {
            // TODO: add more tests for edge cases with small graphs
            // if there are several tiers being allocated to, 
            // then they should get a total of AllocationNumberOfNodes, with the correct ratios
            // TODO: revisit this test and make sure it really is supposed to pass in general (i think no)
            var allocationNumberOfNodes = this.testAllocationParameters.AllocationNumberOfNodes;
            var actual5 = InitialAllocation.NumberOfNodesToAllocateOnThisTier(5, 5, 4, allocationNumberOfNodes);
            var actual4 = InitialAllocation.NumberOfNodesToAllocateOnThisTier(4, 5, 4, allocationNumberOfNodes);
            var actual3 = InitialAllocation.NumberOfNodesToAllocateOnThisTier(3, 5, 4, allocationNumberOfNodes);
            var actual2 = InitialAllocation.NumberOfNodesToAllocateOnThisTier(2, 5, 4, allocationNumberOfNodes);
            var actualTotal = actual5 + actual4 + actual3 + actual2;

            var expected = this.testAllocationParameters.AllocationNumberOfNodes;
            Assert.AreEqual(expected, actualTotal);
            Assert.AreEqual(Math.Round(actual2 / (decimal)actual3), 2);
            Assert.AreEqual(Math.Round(actual3 / (decimal)actual4), 2);
            Assert.AreEqual(Math.Round(actual4 / (decimal)actual5), 2);
        }

        /// <summary>
        /// exhaustive test for NumberOfNodesToAllocateOnThisTier under our current scheme
        /// </summary>
        [TestMethod]
        public void NumberOfNodesToAllocateOnThisTierTestExhaustive()
        {
            // In the current scheme it is impossible to allocate to more than log2(AllocationNumberOfNodes) tiers
            // so we will test the total is correct for that many allocationNumberofTiersToAllocateTo + 1
            var depthsToCheck = (int)Math.Log(this.testAllocationParameters.AllocationNumberOfNodes, 2) + 1;

            // top tier has to be high enough to cover depth
            var allocationTopTier = 2 * depthsToCheck;

            for (var allocationNumberofTiersToAllocateTo = 1; allocationNumberofTiersToAllocateTo <= depthsToCheck; allocationNumberofTiersToAllocateTo++)
            {
                var actuals = new List<int>();
                for (var tierNumber = allocationTopTier; tierNumber > allocationTopTier - allocationNumberofTiersToAllocateTo; tierNumber--)
                {
                    actuals.Add(InitialAllocation.NumberOfNodesToAllocateOnThisTier(
                        tierNumber, 
                        allocationTopTier, 
                        allocationNumberofTiersToAllocateTo, 
                        this.testAllocationParameters.AllocationNumberOfNodes));
                }

                var total = actuals.Sum();
                Assert.AreEqual(this.testAllocationParameters.AllocationNumberOfNodes, total);
            }
        }
    }
}
