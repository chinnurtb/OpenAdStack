//-----------------------------------------------------------------------
// <copyright file="SchedulerFixture.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScheduledActivities;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace ScheduledActivityUnitTests
{
    /// <summary>Tests for the SchedulerFixture</summary>
    [TestClass]
    public class SchedulerFixture
    {
        /// <summary>Name of the schedule to use for the test</summary>
        private string scheduleName;

        /// <summary>Initializes things for the test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
            this.scheduleName = Guid.NewGuid().ToString("n");
        }

        /// <summary>Add an schedule entry and get it using registry name and entry key</summary>
        [TestMethod]
        public void RoundtripScheduleEntry()
        {
            var now = DateTime.UtcNow;
            var entryKey = Guid.NewGuid().ToString("n");
            var expectedValue = Guid.NewGuid();
            
            Assert.IsTrue(Scheduler.AddToSchedule(this.scheduleName, now, entryKey, expectedValue));
            Assert.AreEqual(1, Scheduler.GetScheduledCount<Guid>(this.scheduleName, now.AddHours(1)));

            var registry = Scheduler.GetRegistry<Guid>(this.scheduleName);
            Assert.IsNotNull(registry);

            var entryValue = registry.Get(now, entryKey);
            Assert.AreEqual(expectedValue, entryValue);
        }

        /// <summary>Remove all entries for a specified entry key</summary>
        [TestMethod]
        public void RemoveAllScheduledEntries()
        {
            var now = DateTime.UtcNow;
            var times = Enumerable.Range(0, 5).Select(t => now.AddHours(t)).ToArray();
            var entryKey = Guid.NewGuid().ToString("n");

            // Add hourly entries for the entry key
            Assert.IsTrue(
                times.Select(t => Scheduler.AddToSchedule(this.scheduleName, t, entryKey, Guid.NewGuid())).All(r => r),
                "Failed to add entries to schedule");
            Assert.AreEqual(times.Length, Scheduler.GetScheduledCount<Guid>(this.scheduleName, DateTime.UtcNow.AddDays(1)));

            // Remove all entries for the entry key
            Assert.IsTrue(
                Scheduler.RemoveFromSchedule<Guid>(this.scheduleName, entryKey),
                "Failed to remove entries from schedule");
            Assert.AreEqual(0, Scheduler.GetScheduledCount<Guid>(this.scheduleName, DateTime.UtcNow.AddDays(1)));
        }

        /// <summary>Remove all entries for a specified entry key with other entries present</summary>
        [TestMethod]
        public void RemoveMixedScheduledEntries()
        {
            var now = DateTime.UtcNow;
            var times = Enumerable.Range(0, 5).Select(t => now.AddHours(t)).ToArray();
            var oddTimes = Enumerable.Range(0, 5).Where(t => t % 2 != 0).Select(t => now.AddHours(t)).ToArray();
            var entryKeyA = Guid.NewGuid().ToString("n");
            var entryKeyB = Guid.NewGuid().ToString("n");

            // Add hourly entries for entryKeyA
            Assert.IsTrue(
                times.Select(t => Scheduler.AddToSchedule(this.scheduleName, t, entryKeyA, Guid.NewGuid())).All(r => r),
                "Failed to add entries for entryKeyA to schedule");
            Assert.AreEqual(times.Length, Scheduler.GetScheduledCount<Guid>(this.scheduleName, DateTime.UtcNow.AddDays(1)));

            // Add hourly entries for entryKeyB
            Assert.IsTrue(
                oddTimes.Select(t => Scheduler.AddToSchedule(this.scheduleName, t, entryKeyB, Guid.NewGuid())).All(r => r),
                "Failed to add entries for entryKeyB to schedule");
            Assert.AreEqual(times.Length + oddTimes.Length, Scheduler.GetScheduledCount<Guid>(this.scheduleName, DateTime.UtcNow.AddDays(1)));

            // Remove entries only for entryKeyA
            Assert.IsTrue(
                Scheduler.RemoveFromSchedule<Guid>(this.scheduleName, entryKeyA),
                "Failed to remove entries from schedule");
            Assert.AreEqual(oddTimes.Length, Scheduler.GetScheduledCount<Guid>(this.scheduleName, DateTime.UtcNow.AddDays(1)));
        }
    }
}
