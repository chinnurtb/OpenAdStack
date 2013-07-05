// -----------------------------------------------------------------------
// <copyright file="NodeDeliveryMetricsFixture.cs" company="Rare Crowds Inc">
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
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for NodeDeliveryMetrics class
    /// </summary>
    [TestClass]
    public class NodeDeliveryMetricsFixture
    {
        /// <summary>Time constant - one day.</summary>
        private static readonly DateTime Utc12 = new DateTime(2012, 12, 12, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Get hour metrics when 168 hours or more are requested.</summary>
        [TestMethod]
        public void GetHourMetricsInRangeAll()
        {
            var lastEligibleHour = DateTime.UtcNow;

            // Set up a fully populated delivery profile
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 168;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, lastEligibleHour);

            // Assert we properly wrap around and get all 168 hours
            var hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(lastEligibleHour, new TimeSpan(168, 0, 0));
            Assert.AreEqual(168, hourMetricsInRange.Count);

            // Assert we properly wrap around and get all 168 hours when a range greater than 168 is specified
            hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(lastEligibleHour, new TimeSpan(172, 0, 0));
            Assert.AreEqual(168, hourMetricsInRange.Count);
        }

        /// <summary>Get hour metrics when the profile has hours missing in the range.</summary>
        [TestMethod]
        public void GetHourMetricsInRangeSparse()
        {
            // Start 12 hours from a wrap around point (midnight Sunday)
            var lastEligibleHour = new DateTime(2012, 08, 26, 12, 0, 0, 0, DateTimeKind.Utc);
            var lookback = new TimeSpan(48, 0, 0);

            // Set up a profile with partial data (less than the lookback asked for)
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, lastEligibleHour);

            // Assert we properly wrap around and get the hours that have data
            var hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(lastEligibleHour, lookback);
            Assert.AreEqual(hoursSeen, hourMetricsInRange.Count);
        }

        /// <summary>Get hour metrics when the profile has hours on both sides of the range.</summary>
        [TestMethod]
        public void GetHourMetricsInRangeBracketed()
        {
            // Start 12 hours from a wrap around point (midnight Sunday)
            var lastEligibleHour = new DateTime(2012, 08, 26, 12, 0, 0, 0, DateTimeKind.Utc);
            var lookback = new TimeSpan(48, 0, 0);

            // Set up a profile with data on both sides of the requested range
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 72;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, lastEligibleHour + new TimeSpan(12, 0, 0));

            // Assert we properly wrap around and get the hours that have data
            var hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(lastEligibleHour, lookback);
            Assert.AreEqual(lookback.TotalHours, hourMetricsInRange.Count);
        }

        /// <summary>Get hour metrics when the profile has the exact hours requested.</summary>
        [TestMethod]
        public void GetHourMetricsInRangeExactFit()
        {
            // Start 12 hours from a wrap around point (midnight Sunday)
            var lastEligibleHour = new DateTime(2012, 08, 26, 12, 0, 0, 0, DateTimeKind.Utc);
            var lookback = new TimeSpan(48, 0, 0);

            // Set up a profile with data on both sides of the requested range
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 48;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, lastEligibleHour);

            // Assert we properly wrap around and get the hours that have data
            var hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(lastEligibleHour, lookback);
            Assert.AreEqual(lookback.TotalHours, hourMetricsInRange.Count);
        }

        /// <summary>
        /// Get hour metrics when the profile has only zero delivery nodes and
        /// last processed eligible hour has not been advanced.
        /// </summary>
        [TestMethod]
        public void GetHourMetricsInRangeNoNonzeroEligibleDelivery()
        {
            // Start 12 hours from a wrap around point (midnight Sunday)
            var lastEligibleHour = new DateTime(2012, 08, 26, 12, 0, 0, 0, DateTimeKind.Utc);
            var lookback = new TimeSpan(48, 0, 0);

            // Set up a profile with one zero delivery eligible hour
            var nodeMetrics = new NodeDeliveryMetrics();
            AddHourMetrics(ref nodeMetrics, lastEligibleHour, 0, 0, 1, new[] { 0L }, new[] { 0m });

            // Assert we return empty range when last processed eligible hour is it's default value
            var hourMetricsInRange = nodeMetrics.GetHourMetricsInRange(nodeMetrics.LastProcessedEligibilityHour, lookback);
            Assert.AreEqual(0, hourMetricsInRange.Count);
        }

        /// <summary>Single measure set lifetime media spend</summary>
        [TestMethod]
        public void CalcLifetimeMediaSpend()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.TotalMediaSpend = 2;
            Assert.AreEqual(nodeMetrics.TotalMediaSpend, nodeMetrics.CalcMediaSpend(-1));
        }

        /// <summary>Single measure set lifetime media spend rate</summary>
        [TestMethod]
        public void CalcLifetimeMediaSpendRate()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.TotalEligibleHours = 2;
            nodeMetrics.TotalMediaSpend = 2;
            Assert.AreEqual(1, nodeMetrics.CalcMediaSpendRate(-1));
        }

        /// <summary>Single measure set lifetime impressions</summary>
        [TestMethod]
        public void CalcLifetimeImpressions()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.TotalImpressions = 2;
            Assert.AreEqual(nodeMetrics.TotalImpressions, nodeMetrics.CalcImpressions(-1));
        }

        /// <summary>Single measure set lifetime impression rate</summary>
        [TestMethod]
        public void CalcLifetimeImpressionRate()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.TotalEligibleHours = 2;
            nodeMetrics.TotalImpressions = 2;
            Assert.AreEqual(1, nodeMetrics.CalcImpressionRate(-1));
        }

        /// <summary>Single measure set 48 media spend - no delivery data</summary>
        [TestMethod]
        public void CalcMediaSpend48Empty()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualMediaSpend = nodeMetrics.CalcMediaSpend(48);

            Assert.AreEqual(0, actualMediaSpend);
        }

        /// <summary>Single measure set media spend - zero lookback</summary>
        [TestMethod]
        public void CalcMediaSpendZeroLookBack()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var actualMediaSpend = nodeMetrics.CalcMediaSpend(0);

            Assert.AreEqual(0, actualMediaSpend);
        }

        /// <summary>Single measure set 48 hour media spend - more than 48 hours eligible</summary>
        [TestMethod]
        public void CalcMediaSpend48Full()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 168;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNMediaSpend,
                new List<decimal> { 1.2m });

            nodeMetrics.LastProcessedEligibilityHour = startHour;
            var actualMediaSpend = nodeMetrics.CalcMediaSpend(48);

            Assert.AreEqual(48.2m, actualMediaSpend);
        }

        /// <summary>Single measure set 48 hour media spend - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcMediaSpend48Partial()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNMediaSpend,
                new List<decimal> { 1.2m });

            nodeMetrics.LastProcessedEligibilityHour = startHour;
            var actualMediaSpend = nodeMetrics.CalcMediaSpend(48);

            Assert.AreEqual(24.2m, actualMediaSpend);
        }

        /// <summary>Single measure set 48 hour media spend - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcMediaSpend48IgnoreInvalid()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Create an invalid entry (record present but LastN not populated.
            // TODO: Try to make this impossible
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNMediaSpend,
                new List<decimal>());

            nodeMetrics.LastProcessedEligibilityHour = startHour;
            var actualMediaSpend = nodeMetrics.CalcMediaSpend(48);

            Assert.AreEqual(23m, actualMediaSpend);
        }

        /// <summary>Single measure set 48 hour impressions - no delivery data</summary>
        [TestMethod]
        public void CalcImpressions48Empty()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressions = nodeMetrics.CalcImpressions(48);

            Assert.AreEqual(0, actualImpressions);
        }

        /// <summary>Single measure set impressions - zero lookback</summary>
        [TestMethod]
        public void CalcImpressionsZeroLookBack()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var actualImpressions = nodeMetrics.CalcImpressions(0);

            Assert.AreEqual(0, actualImpressions);
        }

        /// <summary>Single measure set 48 hour impressions - more than 48 hours eligible</summary>
        [TestMethod]
        public void CalcImpressions48Full()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 168;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNImpressions,
                new List<long> { 200 });

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressions = nodeMetrics.CalcImpressions(48);

            Assert.AreEqual(4900, actualImpressions);
        }

        /// <summary>Single measure set 48 hour impressions - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcImpressions48Partial()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNImpressions,
                new List<long> { 200 });

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressions = nodeMetrics.CalcImpressions(48);

            Assert.AreEqual(2500, actualImpressions);
        }

        /// <summary>Single measure set 48 hour impressions - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcImpressions48IgnoreInvalid()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Create an invalid entry (record present but LastN not populated.
            // TODO: Try to make this impossible
            ReplaceLastN(
                nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].LastNImpressions,
                new List<long>());

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressions = nodeMetrics.CalcImpressions(48);

            Assert.AreEqual(2300m, actualImpressions);
        }

        /// <summary>Single measure set 48 hour media spend rate - no delivery data</summary>
        [TestMethod]
        public void CalcMediaSpendRate48Empty()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualMediaSpendRate = nodeMetrics.CalcMediaSpendRate(48);

            Assert.AreEqual(0, actualMediaSpendRate);
        }

        /// <summary>Single measure set media spend rate - zero lookback</summary>
        [TestMethod]
        public void CalcMediaSpendRateZeroLookBack()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var actualMediaSpendRate = nodeMetrics.CalcMediaSpendRate(0);

            Assert.AreEqual(0, actualMediaSpendRate);
        }

        /// <summary>Single measure set 48 hour media spend rate - more than 48 hours eligible</summary>
        [TestMethod]
        public void CalcMediaSpendRate48Full()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 168;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour different for better validation.
            nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].AverageMediaSpend = 49m;

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualMediaSpendRate = nodeMetrics.CalcMediaSpendRate(48);

            Assert.AreEqual(2m, actualMediaSpendRate);
        }

        /// <summary>Single measure set 48 hour media spend rate - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcMediaSpendRate48Partial()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour different for better validation.
            nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].AverageMediaSpend = 25m;

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualMediaSpendRate = nodeMetrics.CalcMediaSpendRate(48);

            Assert.AreEqual(2m, actualMediaSpendRate);
        }

        /// <summary>Single measure set 48 hour impression rate - no delivery data</summary>
        [TestMethod]
        public void CalcImpressionRate48Empty()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressionsRate = nodeMetrics.CalcImpressionRate(48);

            Assert.AreEqual(0, actualImpressionsRate);
        }

        /// <summary>Single measure set impression rate - zero lookback</summary>
        [TestMethod]
        public void CalcImpressionRate48ZeroLookBack()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var actualImpressionsRate = nodeMetrics.CalcImpressionRate(0);

            Assert.AreEqual(0, actualImpressionsRate);
        }

        /// <summary>Single measure set 48 hour impression rate - more than 48 hours eligible</summary>
        [TestMethod]
        public void CalcImpressionRate48Full()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 168;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].AverageImpressions = 4900;

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressionRate = nodeMetrics.CalcImpressionRate(48);

            Assert.AreEqual(200, actualImpressionRate);
        }

        /// <summary>Single measure set 48 hour impression rate - less than 48 hours eligible</summary>
        [TestMethod]
        public void CalcImpressionRate48Partial()
        {
            var nodeMetrics = new NodeDeliveryMetrics();
            var hoursSeen = 24;
            var startHour = Utc12;
            AddHourToDeliveryProfile(ref nodeMetrics, hoursSeen, startHour);

            // Make one hour slightly different for better validation.
            nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(startHour)].AverageImpressions = 2500;

            nodeMetrics.LastProcessedEligibilityHour = Utc12;
            var actualImpressionRate = nodeMetrics.CalcImpressionRate(48);

            Assert.AreEqual(200, actualImpressionRate);
        }

        /// <summary>Calculate the effective media spend rate.</summary>
        [TestMethod]
        [Ignore]
        public void CalcEffectiveMediaSpendRate()
        {
        }

        /// <summary>Calculate the effective impression rate.</summary>
        [TestMethod]
        [Ignore]
        public void CalcEffectiveImpressionRate()
        {
        }

        /// <summary>Calculate the effective media spend for a look-back.</summary>
        [TestMethod]
        [Ignore]
        public void CalcEffectiveMediaSpend()
        {
        }

        /// <summary>Calculate the effective impressions for a look-back.</summary>
        [TestMethod]
        [Ignore]
        public void CalcEffectiveImpressions()
        {
        }

        /// <summary>Calculate the effective total spend for a look-back.</summary>
        [TestMethod]
        [Ignore]
        public void CalcEffectiveTotalSpend()
        {
        }

        /// <summary>Add to LastN history fifo with limit.</summary>
        [TestMethod]
        public void AddToLastNHistoryFirstN()
        {
            var hourMetrics = new NodeHourMetrics();

            for (int i = 1; i <= (NodeHourMetrics.LastNMax + 1); i++)
            {
                hourMetrics.AddToLastNImpressions(i);
                hourMetrics.AddToLastNMediaSpend(i);

                // First element will always be the new one
                Assert.AreEqual(i, hourMetrics.LastNImpressions[0]);
                Assert.AreEqual(i, hourMetrics.LastNMediaSpend[0]);

                // Expected count will not exceed limit
                var expectedCount = i <= NodeHourMetrics.LastNMax
                                        ? i
                                        : NodeHourMetrics.LastNMax;
                Assert.AreEqual(expectedCount, hourMetrics.LastNImpressions.Count);
                Assert.AreEqual(expectedCount, hourMetrics.LastNMediaSpend.Count);
            }
        }

        /// <summary>Replace contents of a collection with a new contents.</summary>
        /// <param name="oldLastN">The old collection.</param>
        /// <param name="newLastN">The new collection.</param>
        /// <typeparam name="T">Type of collection</typeparam>
        private static void ReplaceLastN<T>(IList<T> oldLastN, List<T> newLastN)
        {
            oldLastN.Clear();
            oldLastN.Add(newLastN);
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
            nodeMetrics.DeliveryProfile[NodeDeliveryMetrics.GetProfileHourIndex(deliveryHour)] = hourMetrics;
        }
    }
}
