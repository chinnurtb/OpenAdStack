//-----------------------------------------------------------------------
// <copyright file="TimeSlottedRegistryFixture.cs" company="Rare Crowds Inc">
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
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScheduledActivities;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace ScheduledActivityUnitTests
{
    /// <summary>Tests for the TimeSlottedRegistry</summary>
    [TestClass]
    public class TimeSlottedRegistryFixture
    {
        /// <summary>In-progress slot key</summary>
        private static readonly string InProgressSlotKey = TimeSlottedRegistry<object>.InProgressSlotKey;

        /// <summary>Name of the store to use for the test</summary>
        private string storeName;

        /// <summary>Initializes things for the test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
            this.storeName = Guid.NewGuid().ToString("n");
        }

        /// <summary>Create a time slotted registry</summary>
        [TestMethod]
        public void Create()
        {
            var registry = this.CreateRegistry();
            Assert.IsNotNull(registry);
        }

        /// <summary>Add an entry and get it using slot and entry keys</summary>
        [TestMethod]
        public void AddGetByTimeslotKeyAndItemKey()
        {
            var slotKey = InProgressSlotKey;
            var itemKey = Guid.NewGuid().ToString("n");
            var itemValue = Guid.NewGuid().ToString("n");

            var registry = this.CreateRegistry();
            registry.Add(slotKey, itemKey, itemValue);
            var value = registry.Get(slotKey, itemKey);

            Assert.AreEqual(itemValue, value);
        }

        /// <summary>Add an entry and get it by DateTime and entry key</summary>
        [TestMethod]
        public void AddGetByDateTimeAndItemKey()
        {
            var slotDateTime = DateTime.UtcNow;
            var itemKey = Guid.NewGuid().ToString("n");
            var itemValue = Guid.NewGuid().ToString("n");

            var registry = this.CreateRegistry();
            registry.Add(slotDateTime, itemKey, itemValue);
            var value = registry.Get(slotDateTime, itemKey);

            Assert.AreEqual(itemValue, value);
        }

        /// <summary>Get items selectively based on date time</summary>
        [TestMethod]
        public void GetSelectiveByDateTime()
        {
            var registry = this.CreateRegistry();

            // Add some items
            registry.Add(InProgressSlotKey, Guid.NewGuid().ToString("n"), "X");
            registry.Add(DateTime.UtcNow + new TimeSpan(-4, 0, 0), Guid.NewGuid().ToString("n"), "A");
            registry.Add(DateTime.UtcNow + new TimeSpan(-2, 0, 0), Guid.NewGuid().ToString("n"), "B");
            registry.Add(DateTime.UtcNow + new TimeSpan(0, 0, 0), Guid.NewGuid().ToString("n"), "C");
            registry.Add(DateTime.UtcNow + new TimeSpan(2, 0, 0), Guid.NewGuid().ToString("n"), "D");
            registry.Add(DateTime.UtcNow + new TimeSpan(4, 0, 0), Guid.NewGuid().ToString("n"), "E");

            // Get some items
            var noItems = registry[DateTime.MinValue];
            Assert.IsNotNull(noItems);
            Assert.AreEqual(0, noItems.Count);

            var inProgressItems = registry.InProgress;
            Assert.IsNotNull(inProgressItems);
            Assert.AreEqual(1, inProgressItems.Count);

            var nowItems = registry[DateTime.UtcNow];
            Assert.IsNotNull(nowItems);
            Assert.AreEqual(3, nowItems.Count);

            var pastItems = registry[DateTime.UtcNow + new TimeSpan(-1, 0, 0)];
            Assert.IsNotNull(pastItems);
            Assert.AreEqual(2, pastItems.Count);

            var futureItems = registry[DateTime.UtcNow + new TimeSpan(6, 0, 0)];
            Assert.IsNotNull(futureItems);
            Assert.AreEqual(5, futureItems.Count);
        }

        /// <summary>Test removing entries</summary>
        [TestMethod]
        public void Remove()
        {
            var now = DateTime.UtcNow;
            var registry = this.CreateRegistry();
            var timeSlotKey = TimeSlottedRegistry<object>.GetTimeSlotKey(now);

            registry.Add(now, "A", "B");
            registry.Add(now, "X", "Y");
            Assert.AreEqual(2, registry[now].Count);
            Assert.IsTrue(registry.GetTimeSlotKeys().Contains(timeSlotKey));

            var item = registry[now].First();
            registry.Remove(item.Item1, item.Item2);
            Assert.AreEqual(1, registry[now].Count);
            Assert.IsTrue(registry.GetTimeSlotKeys().Contains(timeSlotKey));

            item = registry[now].First();
            registry.Remove(item.Item1, item.Item2);
            Assert.AreEqual(0, registry[now].Count);

            // Verify slot is removed when its last entry is removed
            Assert.IsFalse(registry.GetTimeSlotKeys().Contains(timeSlotKey));
        }

        /// <summary>Test removing all entries</summary>
        [TestMethod]
        public void RemoveAll()
        {
            var now = DateTime.UtcNow;

            // Create a registry and add multiple entries
            var times = Enumerable.Range(0, 5).ToArray();
            var entryKey = Guid.NewGuid().ToString();
            var registry = this.CreateRegistry();
            Assert.IsTrue(
                times.Select(t => registry.Add(now.AddHours(t), entryKey, Guid.NewGuid().ToString())).All(r => r),
                "Error adding entries to registry");

            // Verify the number of slots are Count + 1 (for in-progress)
            Assert.AreEqual(times.Length + 1, registry.GetTimeSlotKeys().Count());

            // Remove all entries
            Assert.IsTrue(registry.RemoveAll(entryKey), "Error removing entries from registry");

            // Verify all time slots are removed (leaving only the in-progress slot)
            Assert.AreEqual(1, registry.GetTimeSlotKeys().Count());
        }

        /// <summary>Test removing all entries for a given key when others are present</summary>
        [TestMethod]
        public void RemoveAllMixed()
        {
            var now = DateTime.UtcNow;

            // Create a registry and add multiple entries
            var times = Enumerable.Range(0, 5).ToArray();
            var oddTimes = times.Where(t => t % 2 != 0).ToArray();
            var entryKeyA = Guid.NewGuid().ToString();
            var entryKeyB = Guid.NewGuid().ToString();
            var registry = this.CreateRegistry();

            // Add entries with entryKeyA to all times
            Assert.IsTrue(
                times.Select(t => registry.Add(now.AddHours(t), entryKeyA, Guid.NewGuid().ToString())).All(r => r),
                "Error adding entries for entryKeyA to registry");

            // Add entries with entryKeyB to only odd times
            Assert.IsTrue(
                oddTimes.Select(t => registry.Add(now.AddHours(t), entryKeyB, Guid.NewGuid().ToString())).All(r => r),
                "Error adding entries for entryKeyB to registry");

            // Verify the number of slots are Count + 1 (for in-progress)
            Assert.AreEqual(times.Length + 1, registry.GetTimeSlotKeys().Count());

            // Remove all entries
            Assert.IsTrue(registry.RemoveAll(entryKeyA), "Error removing entries from registry");

            // Verify all time slots are removed (leaving only the in-progress slot)
            Assert.AreEqual(oddTimes.Length + 1, registry.GetTimeSlotKeys().Count());
        }

        /// <summary>Test removing all entries up to a specified date time</summary>
        [TestMethod]
        public void RemoveAllToDateTime()
        {
            var now = DateTime.UtcNow;

            // Create a registry and add multiple entries
            var times = Enumerable.Range(0, 5).ToArray();
            var entryKey = Guid.NewGuid().ToString();
            var registry = this.CreateRegistry();
            Assert.IsTrue(
                times.Select(t => registry.Add(now.AddHours(t), entryKey, Guid.NewGuid().ToString())).All(r => r),
                "Error adding entries to registry");

            // Verify the number of slots are Count + 1 (for in-progress)
            Assert.AreEqual(times.Length + 1, registry.GetTimeSlotKeys().Count());

            // Remove all entries up to Count - 2 hours
            var toDateTime = now.AddHours(times[times.Length - 2]);
            Assert.IsTrue(registry.RemoveAll(entryKey, toDateTime), "Error removing entries from registry");

            // Verify all time slots are removed except the in-progress and last time slot
            Assert.AreEqual(2, registry.GetTimeSlotKeys().Count());
        }

        /// <summary>Move entry</summary>
        [TestMethod]
        public void Move()
        {
            var now = DateTime.UtcNow;
            var later = now.AddDays(3);
            var registry = this.CreateRegistry();

            registry.Add(now, "A", "B");
            registry.Add(later, "X", "Y");
            
            var itemA = registry[now].Single();
            var itemX = registry[later].Single(item => item.Item2 != itemA.Item2);

            Assert.AreEqual(1, registry[now].Count);
            Assert.AreEqual(2, registry[later].Count);

            registry.Move(itemA.Item1, itemX.Item1, itemA.Item2);

            var nowItems = registry[now].ToArray();
            var laterItems = registry[later].ToArray();

            Assert.AreEqual(0, nowItems.Length);
            Assert.AreEqual(2, laterItems.Length);
        }

        /// <summary>Move entry to in-progress</summary>
        [TestMethod]
        public void MoveToInProgress()
        {
            var now = DateTime.UtcNow;

            var registry = this.CreateRegistry();
            registry.Add(now, "A", "B");

            var item = registry[now].Single();
            registry.MoveToInProgress(item.Item1, item.Item2);

            var nowNotInProgress = registry[now]
                .Where(i => i.Item1 != InProgressSlotKey);

            Assert.AreEqual(0, nowNotInProgress.Count());
            Assert.AreEqual(1, registry[InProgressSlotKey].Count);
        }

        /// <summary>Move entry to in-progress</summary>
        [TestMethod]
        public void MoveFromInProgress()
        {
            var now = DateTime.UtcNow;

            var registry = this.CreateRegistry();
            registry.Add(InProgressSlotKey, "A", "B");

            var item = registry.InProgress.Single();
            registry.MoveFromInProgress(now, item.Key);

            Assert.AreEqual(1, registry[now].Count());
            Assert.AreEqual(0, registry[InProgressSlotKey].Count);
        }

        /// <summary>Move expired in-progress entries to present</summary>
        [TestMethod]
        public void MoveInProgressExpiredToUtcNow()
        {
            var expiry = new TimeSpan(0, 0, 0, 0, 10);
            var registry = new TimeSlottedRegistry<string>(this.storeName);

            registry.Add(InProgressSlotKey, "A", "B");
            Assert.AreEqual(1, registry.InProgress.Count);
            Thread.Sleep(expiry);

            registry.MoveInProgressExpiredToUtcNow(expiry);
            Assert.AreEqual(0, registry.InProgress.Count);
            Assert.AreEqual(1, registry[DateTime.UtcNow].Count);
        }

        /// <summary>Creates a new time slotted registry for testing</summary>
        /// <returns>The time slotted registry</returns>
        private TimeSlottedRegistry<string> CreateRegistry()
        {
            return new TimeSlottedRegistry<string>(this.storeName);
        }
    }
}
