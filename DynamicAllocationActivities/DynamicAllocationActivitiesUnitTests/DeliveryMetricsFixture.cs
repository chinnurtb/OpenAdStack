// -----------------------------------------------------------------------
// <copyright file="DeliveryMetricsFixture.cs" company="Rare Crowds Inc">
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
using System.Reflection;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Test fixture for DeliveryMetrics class
    /// </summary>
    [TestClass]
    public class DeliveryMetricsFixture
    {
        /// <summary>Time constant - one hour.</summary>
        private static readonly TimeSpan OneHour = new TimeSpan(1, 0, 0);

        /// <summary>Time constant - two hours.</summary>
        private static readonly TimeSpan TwoHours = new TimeSpan(2, 0, 0);

        /// <summary>Time constant - six hours.</summary>
        private static readonly TimeSpan SixHours = new TimeSpan(6, 0, 0);

        /// <summary>Time constant - twelve hours.</summary>
        private static readonly TimeSpan TwelveHours = new TimeSpan(12, 0, 0);

        /// <summary>Time constant - one day.</summary>
        private static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);

        /// <summary>Time constant - two days.</summary>
        private static readonly TimeSpan TwoDays = new TimeSpan(2, 0, 0, 0);

        /// <summary>Time constant - 2012/12/12 12:00 UTC.</summary>
        private static readonly DateTime Utc12 = new DateTime(2012, 12, 12, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>DeliveryMetrics object for testing.</summary>
        private DeliveryMetrics deliveryMetrics;

        /// <summary>Unstubbed DeliveryMetrics object for testing.</summary>
        private DeliveryMetrics nonStubDeliveryMetrics;

        /// <summary>10 pm EST</summary>
        private DateTime time10pmESTasUTC;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet0;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet1;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet2;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet3;

        /// <summary>buget allocation history test data</summary>
        private MeasureSet measureSet4;

        /// <summary>buget allocation history test data</summary>
        private string allocationId0;

        /// <summary>buget allocation history test data</summary>
        private string allocationId1;

        /// <summary>buget allocation history test data</summary>
        private string allocationId2;

        /// <summary>buget allocation history test data</summary>
        private string allocationId3;

        /// <summary>buget allocation history test data</summary>
        private string allocationId4;

        /// <summary>Map of allocationId to measureSet</summary>
        private Dictionary<string, MeasureSet> nodeMap;

        /// <summary>parsed delivery data for testing</summary>
        private CanonicalDeliveryData testCanonicalDeliveryData;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            // Always return 0.1 for data cost unless impressions is zero (then return zero)
            var dataCoster = MockRepository.GenerateStub<IDeliveryDataCost>();
            dataCoster.Stub(f => f.CalculateHourCost(
                Arg<long>.Is.Equal(0), Arg<decimal>.Is.Anything, Arg<MeasureSet>.Is.Anything)).Return(0m);
            dataCoster.Stub(f => f.CalculateHourCost(
                Arg<long>.Matches(a => a != 0), Arg<decimal>.Is.Anything, Arg<MeasureSet>.Is.Anything)).Return(0.1m);

            // Default of 3 hour dead zone
            this.deliveryMetrics = new DeliveryMetrics(
                new TimeSpan(3, 0, 0), 
                dataCoster,
                new Dictionary<MeasureSet, NodeDeliveryMetrics>());

            var measureMap = new MeasureMap(new[]
            {
                new EmbeddedJsonMeasureSource(
                    Assembly.GetExecutingAssembly(),
                    "DynamicAllocationActivitiesUnitTests.Resources.MeasureMap.js")
            });

            this.nonStubDeliveryMetrics = new DeliveryMetrics(
                GetCampaignDeliveryDataActivity.ReportDeadZone,
                new DeliveryDataCoster(measureMap, 1 / 0.85m, 0m), 
                new Dictionary<MeasureSet, NodeDeliveryMetrics>());
            
            this.time10pmESTasUTC = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.Parse("2012-03-16 22:00:00"), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            // Set up node map
            this.measureSet0 = new MeasureSet { 1106005 };
            this.measureSet1 = new MeasureSet { 1106005, 1106006 }; // .25 data cost
            this.measureSet2 = new MeasureSet { 1106007, 1155941 }; // .75 data cost
            this.measureSet3 = new MeasureSet { 1106006, 1106004 }; // .25 data cost
            this.measureSet4 = new MeasureSet { 1106006, 1106007 }; // .25 data cost

            this.allocationId0 = "00000000000000000000000000000000";
            this.allocationId1 = "00000000000000000000000000000001";
            this.allocationId2 = "00000000000000000000000000000002";
            this.allocationId3 = "00000000000000000000000000000003";
            this.allocationId4 = "00000000000000000000000000000004";

            // Build a node map of allocation Id's to measureSet
            this.nodeMap = new Dictionary<string, MeasureSet>
                {
                    { this.allocationId0, this.measureSet0 },
                    { this.allocationId1, this.measureSet1 },
                    { this.allocationId2, this.measureSet2 },
                    { this.allocationId3, this.measureSet3 },
                    { this.allocationId4, this.measureSet4 },
                };

            // Set up raw delivery data
            var rawDeliveryData = EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(DeliveryMetricsFixture), "Resources.ApnxDeliveryData.csv");
            this.testCanonicalDeliveryData = new CanonicalDeliveryData(DeliveryNetworkDesignation.AppNexus);
            this.testCanonicalDeliveryData.AddRawData(rawDeliveryData, this.time10pmESTasUTC, new ApnxReportCsvParser());
        }
        
        /// <summary>Test for node metrics calculations</summary>
        [TestMethod]
        public void CalculateNodeMetrics()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = this.time10pmESTasUTC;

            // Set up an eligibility history for the node. Set period starts for the hour
            // after the hour in which the report is pulled, one day apart. We only
            // need to consider the history that brackets our latest report pull.
            var periodStart = reportDate - OneDay + OneHour;
            var nodeEligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = periodStart - OneDay, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = periodStart - OneDay, EligibilityDuration = OneDay },
                };

            // Set up two measureSets with eligibility, two with empty eligibility, one uninitialized (measureSet4).
            // This shouldn't happen be we should tolerate it.
            var eligibilityHistory = new Dictionary<MeasureSet, List<EligibilityPeriod>>
                {
                    { this.measureSet0, nodeEligibilityHistory },
                    { this.measureSet1, nodeEligibilityHistory },
                    { this.measureSet2, new List<EligibilityPeriod>() },
                    { this.measureSet3, new List<EligibilityPeriod>() },
                };

            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder { EligibilityHistory = eligibilityHistory };

            // Setup activity preconditions that would normally occur in ProcessRequest 
            // before calling CalculateLifetimeMetrics
            var totalBudget = 10000;

            this.testCanonicalDeliveryData.LatestDeliveryReportDate = reportDate;
            this.nonStubDeliveryMetrics.CalculateNodeMetrics(
                this.testCanonicalDeliveryData, eligibilityHistoryBuilder, this.nodeMap, totalBudget);

            // Total spend also includes measureSet 1 plus one record excluded from valid data for day
            Assert.AreEqual(6786.58m, this.nonStubDeliveryMetrics.RemainingBudget);
            Assert.AreEqual(8498.37m, this.nonStubDeliveryMetrics.LifetimeMediaBudgetCap);
        }

        /// <summary>
        /// Test node metrics for delivery occuring after node profile wraps around.
        /// Make sure we don't overwrite the delivery record and it gets correctly 
        /// aggregated in totals.
        /// </summary>
        [TestMethod]
        public void CalculateNodeMetricsWraparound()
        {
            // Set up raw delivery data
            var rawDeliveryData =
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-16 19:00,00000000000000000000000000000001,1,1,1,1";
            this.testCanonicalDeliveryData = new CanonicalDeliveryData(DeliveryNetworkDesignation.AppNexus);
            this.testCanonicalDeliveryData.AddRawData(rawDeliveryData, this.time10pmESTasUTC, new ApnxReportCsvParser());

            // Set first report cycle (pre wrap-around)
            var deliveryHour = this.time10pmESTasUTC;
            var periodStart1 = deliveryHour;
            var nodeEligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart1, EligibilityDuration = OneDay },
                };

            var eligibilityHistory = new Dictionary<MeasureSet, List<EligibilityPeriod>>
                {
                    { this.measureSet1, nodeEligibilityHistory },
                };

            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder { EligibilityHistory = eligibilityHistory };

            // Setup activity preconditions that would normally occur in ProcessRequest 
            // before calling CalculateLifetimeMetrics
            var totalBudget = 10000;

            this.testCanonicalDeliveryData.LatestDeliveryReportDate = periodStart1 + OneDay;
            this.nonStubDeliveryMetrics.CalculateNodeMetrics(
                this.testCanonicalDeliveryData, eligibilityHistoryBuilder, this.nodeMap, totalBudget);

            // Total spend also includes measureSet 1 plus one record excluded from valid data for day
            var totalMediaSpend = this.nonStubDeliveryMetrics.NodeMetricsCollection.Sum(m => m.Value.TotalMediaSpend);
            Assert.AreEqual(1m, totalMediaSpend);

            // Setup second allocation to occur after enough time has passed for the delivery profile
            // to wrap around
            var periodStart2 = periodStart1 + new TimeSpan(8, 0, 0, 0);
            nodeEligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart2, EligibilityDuration = OneDay },
                };

            // Set up eligibility for a second node. By this time the earlier node would no longer
            // be reflected in the unprocessed history even though it is captured in metrics.
            // Make sure we don't drop the captured metrics on the floor when aggregating totals.
            eligibilityHistory = new Dictionary<MeasureSet, List<EligibilityPeriod>>
                {
                    { this.measureSet2, nodeEligibilityHistory },
                };

            eligibilityHistoryBuilder = new EligibilityHistoryBuilder { EligibilityHistory = eligibilityHistory };

            this.testCanonicalDeliveryData = new CanonicalDeliveryData(DeliveryNetworkDesignation.AppNexus);
            this.testCanonicalDeliveryData.LatestDeliveryReportDate = periodStart1 + OneDay;
            this.nonStubDeliveryMetrics.CalculateNodeMetrics(
                this.testCanonicalDeliveryData, eligibilityHistoryBuilder, this.nodeMap, totalBudget);

            // Total spend also includes measureSet 1 plus one record excluded from valid data for day
            totalMediaSpend = this.nonStubDeliveryMetrics.NodeMetricsCollection.Sum(m => m.Value.TotalMediaSpend);
            Assert.AreEqual(1m, totalMediaSpend);
        }

        /// <summary>Test we don't blow up with no delivery data and no previous node metrics.</summary>
        [TestMethod]
        public void CalculateNodeMetricsEmptyInputs()
        {
            // Setup empty delivery data
            var emptyCanonicalDeliveryData = new CanonicalDeliveryData();

            // Set up current report cycle (starts at noon utc)
            var reportDate = this.time10pmESTasUTC;

            // Set up an eligibility history for the node. Set period starts for the hour
            // after the hour in which the report is pulled, one day apart. We only
            // need to consider the history that brackets our latest report pull.
            var periodStart = reportDate - OneDay + OneHour;
            var nodeEligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            // Set up two measureSets with eligibility, one with empty eligibility, two uninitialized (3 & 4)
            var eligibilityHistory = new Dictionary<MeasureSet, List<EligibilityPeriod>>
                {
                    { this.measureSet0, nodeEligibilityHistory },
                    { this.measureSet1, nodeEligibilityHistory },
                    { this.measureSet2, new List<EligibilityPeriod>() },
                };

            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder { EligibilityHistory = eligibilityHistory };

            var totalBudget = 10000;
            this.nonStubDeliveryMetrics.CalculateNodeMetrics(
                emptyCanonicalDeliveryData, eligibilityHistoryBuilder, this.nodeMap, totalBudget);

            Assert.AreEqual(totalBudget, this.nonStubDeliveryMetrics.RemainingBudget);
            Assert.AreEqual(totalBudget, this.nonStubDeliveryMetrics.LifetimeMediaBudgetCap);

            // NodeMetricsCollection should not have entries for nodes with no delivery eligibility
            Assert.AreEqual(2, this.nonStubDeliveryMetrics.NodeMetricsCollection.Count);
        }

        /// <summary>
        /// Update a node history element with no prior data (same boundary conditions
        /// as first realloc after initial allocation).
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsInitialAllocation()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history for the node. Set up two delivery periods
            // with a gap between (similar to initial alloc).
            var periodStart1 = reportDate - OneDay;
            var periodStart2 = periodStart1 + TwelveHours;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart1, EligibilityDuration = SixHours },
                    new EligibilityPeriod { EligibilityStart = periodStart2, EligibilityDuration = SixHours },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Set up the prior state of node metrics (empty)
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up unprocessed delivered hours
            var hourOne = periodStart1;
            var hourTwo = hourOne + OneHour;
            var hourThree = periodStart2;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(hourOne, 200, 2m),
                    this.BuildDeliveryRecord(hourTwo, 100, 1m),
                    this.BuildDeliveryRecord(hourThree, 100, 1m),
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics, 
                measureSet, 
                measureSetDeliveryData, 
                lastValidReportHour, 
                lastCampaignDeliveryHour, 
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against hourOne metrics.
            var hourOneMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(hourOne)];
            Assert.AreEqual(200, hourOneMetrics.AverageImpressions);
            Assert.AreEqual(2m, hourOneMetrics.AverageMediaSpend);
            Assert.AreEqual(1, hourOneMetrics.EligibilityCount);
            Assert.IsTrue(!hourOneMetrics.LastNImpressions.Except(new[] { 200L }).Any());
            Assert.AreEqual(1, hourOneMetrics.LastNImpressions.Count);
            Assert.IsTrue(!hourOneMetrics.LastNMediaSpend.Except(new[] { 2m }).Any());
            Assert.AreEqual(1, hourOneMetrics.LastNMediaSpend.Count);

            // Make assertions against accumulators
            // period2 end should be the last processed eligibility
            Assert.AreEqual(periodStart2 + SixHours - OneHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(hourThree, nodeMetrics.LastProcessedDeliveryHour);
            Assert.AreEqual(12, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(400L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(4m, nodeMetrics.TotalMediaSpend);

            // Make assertions against zero-delivery hour metrics
            var zeroDeliveryHour = hourThree + OneHour;
            var zeroDeliveryHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(zeroDeliveryHour)];
            Assert.AreEqual(0L, zeroDeliveryHourMetrics.AverageImpressions);
            Assert.AreEqual(0m, zeroDeliveryHourMetrics.AverageMediaSpend);
            Assert.AreEqual(1, zeroDeliveryHourMetrics.EligibilityCount);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNImpressions.Except(new[] { 0L }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNMediaSpend.Except(new[] { 0m }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNMediaSpend.Count);
        }

        /// <summary>Update a node history element with no delivery data for node.</summary>
        [TestMethod]
        public void UpdateNodeMetricsNoDeliveryForNode()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history for the node. Set period starts for the hour
            // after the hour in which the report is pulled, one day apart. We only
            // need to consider the history that brackets our latest report pull.
            var periodStart = reportDate - OneDay + OneHour;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Set up the prior state of node metrics (empty)
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up unprocessed delivered hours
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>();

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet, 
                measureSetDeliveryData, 
                lastValidReportHour, 
                lastCampaignDeliveryHour, 
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(20, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(0L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(0m, nodeMetrics.TotalMediaSpend);
            Assert.AreEqual(0m, nodeMetrics.TotalSpend);
        }

        /// <summary>Update a node history element with no eligibility for node.</summary>
        [TestMethod]
        public void UpdateNodeMetricsNoEligibilityForNode()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history for the node. Set period starts for the hour
            // after the hour in which the report is pulled, one day apart. We only
            // need to consider the history that brackets our latest report pull.
            var eligibilityHistory = new List<EligibilityPeriod>();

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Set up the prior state of node metrics (empty)
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up unprocessed delivered hours
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>();

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(0, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(0L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(0m, nodeMetrics.TotalMediaSpend);
            Assert.AreEqual(0m, nodeMetrics.TotalSpend);
        }

        /// <summary>Update a node history element given prior history.</summary>
        [TestMethod]
        public void UpdateNodeMetricsWithPriorHistory()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history for the node. Set period starts for the hour
            // after the hour in which the report is pulled, one day apart. We only
            // need to consider the history that brackets our latest report pull.
            var periodStart = reportDate - OneDay + OneHour;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = periodStart - OneDay, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Set up the prior state of node metrics as if we have just had a full week of delivery
            // and are about to start wrapping around. All hours have delivered except the final deadzone.
            // Report pull is happening in the hour preceding the period start time so going into this pass
            // there will be dead zone + 1 hours that we have not seen yet.
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = (7 * 24) - (int)this.deliveryMetrics.ReportDeadZone.TotalHours - 1;
            nodeMetrics.LastProcessedEligibilityHour = lastValidReportHour - OneDay;
            nodeMetrics.TotalEligibleHours = hoursSeen;
            nodeMetrics.TotalImpressions = hoursSeen * 100L;
            nodeMetrics.TotalMediaSpend = hoursSeen * 1m;
            nodeMetrics.TotalSpend = hoursSeen * 0.1m;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, nodeMetrics.LastProcessedEligibilityHour);

            // Set up a new delivered hour to be aggregated. All other eligible hours
            // should get treated as zero delivery hours for the new period. This includes
            // the time from the previous periods dead-zone. Also set up a delivered hour
            // after the last eligible hour that should be ignored.
            var hourOne = lastValidReportHour;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(hourOne, 200, 2m),
                    this.BuildDeliveryRecord(reportCutoff, 200, 2m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics, 
                measureSet, 
                measureSetDeliveryData, 
                lastValidReportHour, 
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against totals and hourOne metrics.
            // Total eligible hours should increase by 24 hours (period length - current dead-zone + last dead-zone).
            // Other metrics should only increase as appropriate for the single new delivered hour.
            var hourOneMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(hourOne)];
            Assert.AreEqual(150, hourOneMetrics.AverageImpressions);
            Assert.AreEqual(1.5m, hourOneMetrics.AverageMediaSpend);
            Assert.AreEqual(2, hourOneMetrics.EligibilityCount);
            Assert.IsTrue(!hourOneMetrics.LastNImpressions.Except(new[] { 100L, 200L }).Any());
            Assert.AreEqual(2, hourOneMetrics.LastNImpressions.Count);
            Assert.IsTrue(!hourOneMetrics.LastNMediaSpend.Except(new[] { 1m, 2m }).Any());
            Assert.AreEqual(2, hourOneMetrics.LastNMediaSpend.Count);

            // Make assertions against accumulators
            Assert.AreEqual(lastValidReportHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(hourOne, nodeMetrics.LastProcessedDeliveryHour);
            Assert.AreEqual(hoursSeen + 24, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual((hoursSeen * 100L) + 200L, nodeMetrics.TotalImpressions);
            Assert.AreEqual((hoursSeen * 1m) + 2m, nodeMetrics.TotalMediaSpend);

            // Make assertions against zero-delivery hour metrics
            var zeroDeliveryHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(hourOne - OneHour)];
            Assert.AreEqual(50, zeroDeliveryHourMetrics.AverageImpressions);
            Assert.AreEqual(.5m, zeroDeliveryHourMetrics.AverageMediaSpend);
            Assert.AreEqual(2, zeroDeliveryHourMetrics.EligibilityCount);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNImpressions.Except(new[] { 100L, 0L }).Any());
            Assert.AreEqual(2, zeroDeliveryHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNMediaSpend.Except(new[] { 1m, 0m }).Any());
            Assert.AreEqual(2, zeroDeliveryHourMetrics.LastNMediaSpend.Count);
        }

        /// <summary>Update a node history element with delivery outside the eligibility history.</summary>
        [TestMethod]
        public void UpdateNodeMetricsWithDeliveryOutsideEligibleHours()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history with a gap.
            var gapHour = reportDate - OneDay;
            var periodStart = gapHour + OneHour;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = gapHour - OneDay, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up a delivered hour in the gap.
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(gapHour, 200, 2m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics, 
                measureSet, 
                measureSetDeliveryData, 
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Assert metrics for gapHour.
            var gapHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(gapHour)];
            Assert.AreEqual(200, gapHourMetrics.AverageImpressions);
            Assert.AreEqual(2m, gapHourMetrics.AverageMediaSpend);
            Assert.AreEqual(1, gapHourMetrics.EligibilityCount);
            Assert.IsTrue(!gapHourMetrics.LastNImpressions.Except(new[] { 200L }).Any());
            Assert.AreEqual(1, gapHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!gapHourMetrics.LastNMediaSpend.Except(new[] { 2m }).Any());
            Assert.AreEqual(1, gapHourMetrics.LastNMediaSpend.Count);
            Assert.AreEqual(lastValidReportHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(gapHour, nodeMetrics.LastProcessedDeliveryHour);

            // Make assertions against accumulators. TotalEligibleHours will be the eligible hours
            // in the eligibilityHistory (44) plus the hour delivered in the gap.
            Assert.AreEqual(45, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(200L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(2m, nodeMetrics.TotalMediaSpend);
        }

        /// <summary>
        /// Node eligibility without delivery before and after latest campaign delivery hour.
        /// Zero-delivery eligibility until latest campaign delivery hour is treated as significant,
        /// after that it is ignored.
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsTrailingSignificantEligibility()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour - SixHours;

            // Set up an eligibility history
            var periodStart = reportDate - OneDay;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up the last node delivery hour before the last campaign delivery hour.
            var deliveredHour = periodStart + SixHours;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(deliveredHour, 200, 2m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(15, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(200L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(2m, nodeMetrics.TotalMediaSpend);
            Assert.AreEqual(lastCampaignDeliveryHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(deliveredHour, nodeMetrics.LastProcessedDeliveryHour);

            // Make assertions against delivered hour metrics.
            var deliveredHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(deliveredHour)];
            Assert.AreEqual(200, deliveredHourMetrics.AverageImpressions);
            Assert.AreEqual(2m, deliveredHourMetrics.AverageMediaSpend);
            Assert.AreEqual(1, deliveredHourMetrics.EligibilityCount);
            Assert.IsTrue(!deliveredHourMetrics.LastNImpressions.Except(new[] { 200L }).Any());
            Assert.AreEqual(1, deliveredHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!deliveredHourMetrics.LastNMediaSpend.Except(new[] { 2m }).Any());
            Assert.AreEqual(1, deliveredHourMetrics.LastNMediaSpend.Count);

            // Make assertions against zero-delivery hour metrics before last campaign hour
            var zeroDeliveryHour = deliveredHour + OneHour;
            var zeroDeliveryHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(zeroDeliveryHour)];
            Assert.AreEqual(0L, zeroDeliveryHourMetrics.AverageImpressions);
            Assert.AreEqual(0m, zeroDeliveryHourMetrics.AverageMediaSpend);
            Assert.AreEqual(1, zeroDeliveryHourMetrics.EligibilityCount);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNImpressions.Except(new[] { 0L }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNMediaSpend.Except(new[] { 0m }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNMediaSpend.Count);

            // Zero-delivery after last campaign delivery hour is ignored.
            zeroDeliveryHour = lastCampaignDeliveryHour + OneHour;
            Assert.IsFalse(
                nodeMetrics.DeliveryProfile.ContainsKey(DeliveryMetrics.GetProfileHourIndex(zeroDeliveryHour)));
        }

        /// <summary>
        /// Delivery for nodes last period of eligibility doesn't show up in report until node is no
        /// longer being exported (normal for delivery at end of period).
        /// Last processed eligibility hour should be updated to end of eligibility for the node
        /// rather than last campaign delivery hour.
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsEligibilityEndBeforeLastCampaignDelivery()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history that ended before the current period
            var periodStart = reportDate - TwoDays;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up the last node delivery hour before the last campaign delivery hour.
            var deliveredHour = periodStart + SixHours;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(deliveredHour, 200, 2m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // LastProcessedEligibilityHour should reflect end of node eligibility
            Assert.AreEqual(periodStart + OneDay - OneHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(deliveredHour, nodeMetrics.LastProcessedDeliveryHour);
        }

        /// <summary>
        /// After eligible hour marked with zero-delivery a subsequent report 
        /// has non-zero delivery for the same hour.
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsStaleDeliveryOfPreviouslyCountedHour()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history
            var periodStart = lastValidReportHour - OneDay;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up a delivery in the middle of eligibility.
            var deliveredHour = periodStart + SixHours;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(deliveredHour, 100, 1m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Total Eligible hours should not change
            Assert.AreEqual(24, nodeMetrics.TotalEligibleHours);

            // Set up a different reported value for the previously delivered hour
            // and a non-zero value for a previously zero hour
            var previousZeroHour = periodStart + SixHours + OneHour;
            measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(previousZeroHour, 300, 3m),
                };

            // Update the node again like a new report was pulled with no additional
            // data for this node but updates to existing hours
            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour + OneDay,
                lastCampaignDeliveryHour + OneDay,
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(24, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(400L, nodeMetrics.TotalImpressions);
            Assert.AreEqual(4m, nodeMetrics.TotalMediaSpend);
            Assert.AreEqual(periodStart + OneDay - OneHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(previousZeroHour, nodeMetrics.LastProcessedDeliveryHour);

            // Make assertions against zero-delivery hour metrics
            // Updated values but LastN counts should not change
            var zeroDeliveryHourMetrics = nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(previousZeroHour)];
            Assert.AreEqual(300L, zeroDeliveryHourMetrics.AverageImpressions);
            Assert.AreEqual(3m, zeroDeliveryHourMetrics.AverageMediaSpend);
            Assert.AreEqual(1, zeroDeliveryHourMetrics.EligibilityCount);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNImpressions.Except(new[] { 300L }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNImpressions.Count);
            Assert.IsTrue(!zeroDeliveryHourMetrics.LastNMediaSpend.Except(new[] { 3m }).Any());
            Assert.AreEqual(1, zeroDeliveryHourMetrics.LastNMediaSpend.Count);
        }

        /// <summary>
        /// New eligibility added without new delivery
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsNewEligibilityWithoutDelivery()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lastCampaignDeliveryHour = lastValidReportHour;

            // Set up an eligibility history
            var periodStart = lastValidReportHour - OneDay;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up a delivery in the middle of eligibility.
            var deliveredHour = periodStart + SixHours;
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>
                {
                    this.BuildDeliveryRecord(deliveredHour, 100, 1m)
                };

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                OneDay,
                eligibilityHistoryBuilder);

            // Total Eligible hours should get updated
            Assert.AreEqual(24, nodeMetrics.TotalEligibleHours);

            // Set up a new eligibility period
            periodStart += OneDay;
            eligibilityHistory.Add(new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay });

            // Update the node again like a new report was pulled with no additional
            // data for this node but updates to existing hours
            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour + OneDay,
                lastCampaignDeliveryHour + OneDay,
                OneDay,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(48, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(periodStart + OneDay - OneHour, nodeMetrics.LastProcessedEligibilityHour);
            Assert.AreEqual(deliveredHour, nodeMetrics.LastProcessedDeliveryHour);
        }

        /// <summary>
        /// No delivery for more than look back period.
        /// </summary>
        [TestMethod]
        public void UpdateNodeMetricsNoDeliveryForMoreLookBackPeriod()
        {
            // Set up current report cycle (starts at noon utc)
            var reportDate = Utc12;
            var reportCutoff = reportDate - this.deliveryMetrics.ReportDeadZone;
            var lastValidReportHour = reportCutoff - OneHour;
            var lookBackDuration = OneDay;
            var lastCampaignDeliveryHour = CanonicalDeliveryData.MinimumDeliveryDate;

            // Set up an eligibility history
            var periodStart = lastValidReportHour - OneDay + OneHour;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = periodStart - OneDay, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = periodStart - TwoDays, EligibilityDuration = OneDay },
                };

            var measureSet = new MeasureSet();
            var eligibilityHistoryBuilder = BuildEligibilityHistoryBuilder(measureSet, eligibilityHistory);

            // Don't care about prior state for this narrow test
            var nodeMetrics = new NodeDeliveryMetrics();

            // Set up zero delivery.
            var measureSetDeliveryData = new List<Dictionary<string, PropertyValue>>();

            DeliveryMetrics.UpdateNodeMetrics(
                nodeMetrics,
                measureSet,
                measureSetDeliveryData,
                lastValidReportHour,
                lastCampaignDeliveryHour,
                lookBackDuration,
                eligibilityHistoryBuilder);

            // Make assertions against accumulators
            Assert.AreEqual(48, nodeMetrics.TotalEligibleHours);
            Assert.AreEqual(periodStart - OneHour, nodeMetrics.LastProcessedEligibilityHour);
        }

        /// <summary>Eligibility entry fully brackets processed and unprocessed data.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryFullyBrackets()
        {
            // Setup up a period that spans the last processed eligibility hour and
            // the last unprocessed eligibility hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneHour;
            var periodDuration = 30;
            AssertEligibilityHistoryIsIncluded(true, lastProcessedEligibilityHour, lastEligibleHour, periodStart, periodDuration);
        }

        /// <summary>Eligibility entry on leading edge.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryLeadingEdge()
        {
            // Setup up last hour of period (periodStart + duration - 1) 
            // just after last processed eligibility hour.
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneDay + OneHour + OneHour;
            AssertEligibilityHistoryIsIncluded(true, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Eligibility entry early.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryEarly()
        {
            // Setup up last hour of period (periodStart + duration - 1)
            // on last processed eligibility hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneDay + OneHour;
            AssertEligibilityHistoryIsIncluded(false, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Eligibility entry on trailing edge.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryTrailingEdge()
        {
            // Setup up start of period on the last unprocessed eligibility hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastEligibleHour;
            AssertEligibilityHistoryIsIncluded(true, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Eligibility entry late.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryLate()
        {
            // Setup up start of period just after last unprocessed eligibility hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastEligibleHour + OneHour;
            AssertEligibilityHistoryIsIncluded(false, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Eligibility entry with no current or prior delivery - no eligibility.</summary>
        [TestMethod]
        public void GetUnprocessedEligibilityHistoryInitial()
        {
            // Setup up start of period just after last unprocessed eligible hour
            var lastEligibleHour = DateTime.MinValue;
            var lastProcessedEligibilityHour = DateTime.MinValue;
            var periodStart = Utc12;
            AssertEligibilityHistoryIsIncluded(false, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>
        /// Get list of unprocessed hours from eligibility history 
        /// that fully brackets unprocessed data.
        /// </summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursFullyBrackets()
        {
            // Setup up a period start before last processed eligible hour 
            // and last hour of period (periodStart + duration - 1)
            // after last eligible hour (report pull before end of period)
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneHour;
            var periodDuration = 30;
            AssertUnprocessedEligibleHours(24, lastProcessedEligibilityHour, lastEligibleHour, periodStart, periodDuration);
        }

        /// <summary>Unprocessed eligible hour on leading edge.</summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursLeadingEdge()
        {
            // Setup up last hour of period (periodStart + duration - 1) 
            // just after last processed eligible hour.
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneDay + TwoHours;
            AssertUnprocessedEligibleHours(1, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Unprocessed eligible hours too early.</summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursEarly()
        {
            // Setup up last hour of period (periodStart + duration - 1)
            // on last processed eligible hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastProcessedEligibilityHour - OneDay + OneHour;
            AssertUnprocessedEligibleHours(0, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Unprocessed eligible hour on trailing edge.</summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursTrailingEdge()
        {
            // Setup up start of period on the last eligible hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastEligibleHour;
            AssertUnprocessedEligibleHours(1, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Unprocessed eligible hours late.</summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursLate()
        {
            // Setup up start of period just after last eligible hour
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var periodStart = lastEligibleHour + OneHour;
            AssertUnprocessedEligibleHours(0, lastProcessedEligibilityHour, lastEligibleHour, periodStart, 24);
        }

        /// <summary>Unprocessed eligible hours correct when there are overlapping eligibility periods.</summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursOverlapping()
        {
            // Set up two eligibility periods after the last processed hour and one straddling
            // (overlapping next period).
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - OneDay;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = lastProcessedEligibilityHour - OneDay + TwoHours, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = lastProcessedEligibilityHour + OneHour, EligibilityDuration = OneDay },
                };

            AssertUnprocessedEligibleHours(24, lastProcessedEligibilityHour, lastEligibleHour, eligibilityHistory);
        }

        /// <summary>
        /// Unprocessed eligible hours correct when there is a gap 
        /// between unprocessed eligibility periods.
        /// </summary>
        [TestMethod]
        public void GetUnprocessedEligibleHoursGap()
        {
            // Set up two eligibility periods with a gap between
            var lastEligibleHour = Utc12;
            var lastProcessedEligibilityHour = lastEligibleHour - TwoDays;
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = lastProcessedEligibilityHour - OneDay + TwoHours, EligibilityDuration = OneDay },
                    new EligibilityPeriod { EligibilityStart = lastProcessedEligibilityHour + OneDay, EligibilityDuration = OneDay },
                };

            AssertUnprocessedEligibleHours(25, lastProcessedEligibilityHour, lastEligibleHour, eligibilityHistory);
        }

        /// <summary>Build an EligibiltyHistoryBuilder from an eligibility history</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="eligibilityHistory">The eligibility history.</param>
        /// <returns>the EligibiltyHistoryBuilder</returns>
        private static EligibilityHistoryBuilder BuildEligibilityHistoryBuilder(
            MeasureSet measureSet,
            List<EligibilityPeriod> eligibilityHistory)
        {
            var eligibilityHistoryBuilder = new EligibilityHistoryBuilder
            {
                EligibilityHistory =
                    new Dictionary<MeasureSet, List<EligibilityPeriod>> { { measureSet, eligibilityHistory } }
            };
            return eligibilityHistoryBuilder;
        }

        /// <summary>Assert that an eligibility history entry is included or not when filtering.</summary>
        /// <param name="isValid">True if we expect it to be included.</param>
        /// <param name="lastProcessedEligibilityHour">The last processed eligibility hour.</param>
        /// <param name="lastEligibleHour">The last eligible hour to consider.</param>
        /// <param name="periodStart">The period start.</param>
        /// <param name="periodDuration">The period duration.</param>
        private static void AssertEligibilityHistoryIsIncluded(
            bool isValid,
            DateTime lastProcessedEligibilityHour,
            DateTime lastEligibleHour,
            DateTime periodStart,
            int periodDuration)
        {
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = new TimeSpan(periodDuration, 0, 0) },
                };

            var filteredEligibilityHistory = DeliveryMetrics.GetUnprocessedElibilityHistory(
                eligibilityHistory, lastProcessedEligibilityHour, lastEligibleHour);

            Assert.IsTrue(isValid == (filteredEligibilityHistory.Count() == 1));
        }

        /// <summary>Assert the number of unprocessed eligible hours.</summary>
        /// <param name="expectedCount">The expected number of hours.</param>
        /// <param name="lastProcessedEligibleHour">The last processed eligible hour.</param>
        /// <param name="lastEligibleHour">The last eligible hour.</param>
        /// <param name="periodStart">The period start.</param>
        /// <param name="periodDuration">The period duration.</param>
        private static void AssertUnprocessedEligibleHours(
            int expectedCount,
            DateTime lastProcessedEligibleHour,
            DateTime lastEligibleHour,
            DateTime periodStart,
            int periodDuration)
        {
            var eligibilityHistory = new List<EligibilityPeriod>
                {
                    new EligibilityPeriod { EligibilityStart = periodStart, EligibilityDuration = new TimeSpan(periodDuration, 0, 0) },
                };

            AssertUnprocessedEligibleHours(
                expectedCount,
                lastProcessedEligibleHour,
                lastEligibleHour,
                eligibilityHistory);
        }

        /// <summary>Assert the number of unprocessed eligible hours.</summary>
        /// <param name="expectedCount">The expected number of hours.</param>
        /// <param name="lastProcessedEligibilityHour">The last processed eligibility hour.</param>
        /// <param name="lastEligibleHour">The last eligible hour.</param>
        /// <param name="eligibilityHistory">The eligibility history.</param>
        private static void AssertUnprocessedEligibleHours(
            int expectedCount,
            DateTime lastProcessedEligibilityHour,
            DateTime lastEligibleHour,
            List<EligibilityPeriod> eligibilityHistory)
        {
            var unprocessedHours = DeliveryMetrics.GetUnprocessedEligibleHours(
                lastEligibleHour, lastProcessedEligibilityHour, eligibilityHistory);

            // Make sure there are not dupes
            Assert.IsTrue(unprocessedHours.Distinct().Count() == unprocessedHours.Count);

            // Make sure hours are in range
            Assert.AreEqual(expectedCount, unprocessedHours.Count);
            Assert.AreEqual(0, unprocessedHours.Count(h => h <= lastProcessedEligibilityHour));
            Assert.AreEqual(0, unprocessedHours.Count(h => h > lastEligibleHour));
        }

        /// <summary>Initialize a delivery profile from a starting hour back through specified number of hours.</summary>
        /// <param name="nodeMetrics">The node metrics.</param>
        /// <param name="hoursSeen">The hours seen (to initialize back in time).</param>
        /// <param name="startHour">The start hour.</param>
        private static void AddHourToDeliveryProfile(ref NodeDeliveryMetrics nodeMetrics, int hoursSeen, DateTime startHour)
        {
            for (int i = 0; i < hoursSeen; i++)
            {
                AddHourMetrics(ref nodeMetrics, startHour, 100, 1, 1, new[] { 100L }, new[] { 1m });

                // decrement the hour (go back in time from last hour we have seen)
                startHour = startHour.AddHours(-1);
            }
        }

        /// <summary>Build a NodeHourMetrics object.</summary>
        /// <param name="nodeMetrics">The node metrics object.</param>
        /// <param name="deliveryHour">The delivery hour.</param>
        /// <param name="avgImpressions">The avg impressions.</param>
        /// <param name="avgMediaSpend">The avg media spend.</param>
        /// <param name="eligibilityCount">The eligibility count.</param>
        /// <param name="lastNImpressions">The last n impressions.</param>
        /// <param name="lastNMediaSpend">The last n media spend.</param>
        private static void AddHourMetrics(
            ref NodeDeliveryMetrics nodeMetrics, 
            DateTime deliveryHour, 
            decimal avgImpressions, 
            decimal avgMediaSpend, 
            int eligibilityCount, 
            long[] lastNImpressions, 
            decimal[] lastNMediaSpend)
        {
            var hourMetrics = new NodeHourMetrics();
            hourMetrics.AverageImpressions = avgImpressions;
            hourMetrics.AverageMediaSpend = avgMediaSpend;
            hourMetrics.EligibilityCount = eligibilityCount;
            hourMetrics.LastNImpressions.Add(lastNImpressions);
            hourMetrics.LastNMediaSpend.Add(lastNMediaSpend);
            nodeMetrics.DeliveryProfile[DeliveryMetrics.GetProfileHourIndex(deliveryHour)] = hourMetrics;
        }

        /// <summary>Build a delivery data dictionary record</summary>
        /// <param name="hour">The hour.</param>
        /// <param name="impr">The impr.</param>
        /// <param name="spend">The spend.</param>
        /// <param name="allocationId">The allocation id.</param>
        /// <param name="ecpm">The ecpm.</param>
        /// <param name="clicks">The clicks.</param>
        /// <param name="campaignId">The campaign id.</param>
        /// <returns>The dictionary record</returns>
        private Dictionary<string, PropertyValue> BuildDeliveryRecord(
            DateTime hour,
            long impr = 0,
            decimal spend = 0,
            string allocationId = "",
            decimal ecpm = 0,
            long clicks = 0,
            string campaignId = "")
        {
            // Report record hour is bucketized
            return new Dictionary<string, PropertyValue>
                {
                    { RawDeliveryDataParserBase.HourFieldName, DeliveryMetrics.GetUtcHourBucket(hour) },
                    { RawDeliveryDataParserBase.ImpressionsFieldName, impr },
                    { RawDeliveryDataParserBase.MediaSpendFieldName, spend },
                    { RawDeliveryDataParserBase.AllocationIdFieldName, allocationId },
                    { RawDeliveryDataParserBase.EcpmFieldName, ecpm },
                    { RawDeliveryDataParserBase.ClicksFieldName, clicks },
                    { RawDeliveryDataParserBase.CampaignIdFieldName, campaignId },
                };
        }
    }
}
