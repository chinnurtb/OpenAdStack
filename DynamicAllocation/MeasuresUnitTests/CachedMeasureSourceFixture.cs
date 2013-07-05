// -----------------------------------------------------------------------
// <copyright file="CachedMeasureSourceFixture.cs" company="Rare Crowds Inc">
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
using System.Threading;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace MeasuresUnitTests
{
    /// <summary>Tests for the CachedMeasureSource class</summary>
    [TestClass]
    public class CachedMeasureSourceFixture
    {
        /// <summary>Unique id for each test</summary>
        private string uniqueId;

        /// <summary>Test logger</summary>
        private TestLogger logger;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.uniqueId = Guid.NewGuid().ToString("N");
            CachedMeasureSource.CacheUpdateStartTimes = null;
            CachedMeasureSource.LocalMeasureCache = null;
            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new[] { this.logger = new TestLogger() });
            TestCachedMeasureSource.TestCacheUpdateTimeout = new TimeSpan(0, 0, 0, 0, 100);
            TestCachedMeasureSource.TestExpiredCacheRefreshWait = new TimeSpan(0, 0, 0, 0, 10);
        }

        /// <summary>Test updating an empty cache</summary>
        [TestMethod]
        public void UpdateEmptyCache()
        {
            var sourceMeasures = new Dictionary<string, object>[]
            {
                new Dictionary<string, object> { { "ID", 1 }, { "Value", this.uniqueId } },
                new Dictionary<string, object> { { "ID", 2 }, { "Value", this.uniqueId } },
            };
            var expiry = DateTime.UtcNow.AddHours(1);
            IMeasureSource source = new TestCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () =>
                {
                    return sourceMeasures;
                }
            };

            Assert.AreEqual(2002000000000000000, source.BaseMeasureId);
            Assert.AreEqual("NETWORK:2002:test", source.SourceId);

            var cachedMeasures = source.Measures;
            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(sourceMeasures.Length, cachedMeasures.Count);
            foreach (var pair in sourceMeasures.Zip(cachedMeasures))
            {
                var expectedKey = "200200000000000000" + pair.Item1["ID"].ToString();
                Assert.AreEqual(expectedKey, pair.Item2.Key.ToString());
                Assert.AreEqual(pair.Item1.Count, pair.Item2.Value.Count);
                Assert.AreEqual(pair.Item1.First().Key, pair.Item2.Value.First().Key);
                Assert.AreEqual((long)(int)pair.Item1.First().Value, (long)pair.Item2.Value.First().Value);
                Assert.AreEqual(pair.Item1.Last().Key, pair.Item2.Value.Last().Key);
                Assert.AreEqual(pair.Item1.Last().Value, pair.Item2.Value.Last().Value);
            }
        }

        /// <summary>Test getting previously cached measures</summary>
        [TestMethod]
        public void GetCachedMeasures()
        {
            // First, get the cache populated
            var sourceMeasures = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "ID", 1 }, { "Value", this.uniqueId } },
                new Dictionary<string, object> { { "ID", 2 }, { "Value", this.uniqueId } },
            };
            var expiry = DateTime.UtcNow.AddSeconds(3);
            IMeasureSource primingSource = new TestCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () => { return sourceMeasures; }
            };
            Assert.AreEqual(sourceMeasures.Count, primingSource.Measures.Count);

            // Create a new source and set it to fail if it tries to get fresh measures
            var triedToGetMeasures = false;
            IMeasureSource source = new TestCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () =>
                {
                    triedToGetMeasures = true;
                    return null;
                }
            };

            // Get the cached measures
            var cachedMeasures = source.Measures;
            Assert.IsFalse(triedToGetMeasures);
            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(sourceMeasures.Count, cachedMeasures.Count);
            foreach (var pair in sourceMeasures.Zip(cachedMeasures))
            {
                var expectedKey = "200200000000000000" + pair.Item1["ID"].ToString();
                Assert.AreEqual(expectedKey, pair.Item2.Key.ToString());
                Assert.AreEqual(pair.Item1.Count, pair.Item2.Value.Count);
                Assert.AreEqual(pair.Item1.First().Key, pair.Item2.Value.First().Key);
                Assert.AreEqual((long)(int)pair.Item1.First().Value, (long)pair.Item2.Value.First().Value);
                Assert.AreEqual(pair.Item1.Last().Key, pair.Item2.Value.Last().Key);
                Assert.AreEqual(pair.Item1.Last().Value, pair.Item2.Value.Last().Value);
            }

            // Add another measure to the source measures
            sourceMeasures.Add(new Dictionary<string, object> { { "ID", 3 }, { "Value", this.uniqueId } });

            // Set the source to get the updated source measures
            ((TestCachedMeasureSource)source).GetNetworkMeasures = () =>
            {
                return sourceMeasures;
            };

            // Wait until the cache expires
            var timeUntilExpiry = expiry.AddSeconds(1) - DateTime.UtcNow;
            if (timeUntilExpiry.TotalMilliseconds > 0)
            {
                Thread.Sleep(timeUntilExpiry);
            }

            // Get the updated measures
            var updatedMeasures = source.Measures;
            Assert.AreEqual(sourceMeasures.Count, updatedMeasures.Count);
        }

        /// <summary>Test getting cached measures from persistent storage</summary>
        [TestMethod]
        public void GetPersistentCachedMeasures()
        {
            // First, get the cache populated
            var sourceMeasures = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "ID", 1 }, { "Value", this.uniqueId } },
                new Dictionary<string, object> { { "ID", 2 }, { "Value", this.uniqueId } },
            };
            var expiry = DateTime.UtcNow.AddSeconds(3);
            IMeasureSource primingSource = new TestCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () => { return sourceMeasures; }
            };
            Assert.AreEqual(sourceMeasures.Count, primingSource.Measures.Count);

            // Clear the local, in-memory cache
            CachedMeasureSource.LocalMeasureCache = null;

            // Create a new source and set it to fail if it tries to get fresh measures
            var triedToGetMeasures = false;
            IMeasureSource source = new TestCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () =>
                {
                    triedToGetMeasures = true;
                    return null;
                }
            };

            // Get the cached measures
            var cachedMeasures = source.Measures;
            Assert.IsFalse(triedToGetMeasures);
            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(sourceMeasures.Count, cachedMeasures.Count);
            foreach (var pair in sourceMeasures.Zip(cachedMeasures))
            {
                var expectedKey = "200200000000000000" + pair.Item1["ID"].ToString();
                Assert.AreEqual(expectedKey, pair.Item2.Key.ToString());
                Assert.AreEqual(pair.Item1.Count, pair.Item2.Value.Count);
                Assert.AreEqual(pair.Item1.First().Key, pair.Item2.Value.First().Key);
                Assert.AreEqual((long)(int)pair.Item1.First().Value, (long)pair.Item2.Value.First().Value);
                Assert.AreEqual(pair.Item1.Last().Key, pair.Item2.Value.Last().Key);
                Assert.AreEqual(pair.Item1.Last().Value, pair.Item2.Value.Last().Value);
            }

            // Add another measure to the source measures
            sourceMeasures.Add(new Dictionary<string, object> { { "ID", 3 }, { "Value", this.uniqueId } });

            // Set the source to get the updated source measures
            ((TestCachedMeasureSource)source).GetNetworkMeasures = () =>
            {
                return sourceMeasures;
            };

            // Wait until the cache expires
            var timeUntilExpiry = expiry.AddSeconds(1) - DateTime.UtcNow;
            if (timeUntilExpiry.TotalMilliseconds > 0)
            {
                Thread.Sleep(timeUntilExpiry);
            }

            // Get the updated measures
            var updatedMeasures = source.Measures;
            Assert.AreEqual(sourceMeasures.Count, updatedMeasures.Count);
        }

        /// <summary>Test updating an empty cache asynchronously</summary>
        [TestMethod]
        public void UpdateEmptyCacheAsync()
        {
            const int CacheUpdateDelay = 250;

            var sourceMeasures = new Dictionary<string, object>[]
            {
                new Dictionary<string, object> { { "ID", 1 }, { "Value", this.uniqueId } },
                new Dictionary<string, object> { { "ID", 2 }, { "Value", this.uniqueId } },
            };
            var expiry = DateTime.UtcNow.AddHours(1);
            IMeasureSource source = new TestAsyncCachedMeasureSource
            {
                Expiry = expiry,
                GetNetworkMeasures = () =>
                {
                    Thread.Sleep(CacheUpdateDelay);
                    return sourceMeasures;
                }
            };

            Assert.AreEqual(2002000000000000000, source.BaseMeasureId);
            Assert.AreEqual("NETWORK:2002:test", source.SourceId);

            // Trigger initial cache update
            var cachedMeasures = source.Measures;
            Assert.IsNull(cachedMeasures);

            // Wait for the cache to be updated
            Thread.Sleep(CacheUpdateDelay * 2);

            // Cached measures should be updated now
            cachedMeasures = source.Measures;
            Assert.IsNotNull(cachedMeasures);
            Assert.AreEqual(sourceMeasures.Length, cachedMeasures.Count);
        }

        /// <summary>Simple sanity check for the method used to create measure display names</summary>
        [TestMethod]
        public void MakeMeasureDisplayName()
        {
            const string Expected = "Master Category:Category:Measure  SubType:Measure Name";
            var actual = new TestCachedMeasureSource()
                .TestMakeMeasureDisplayName("Measure: SubType", "Measure Name");
            Assert.AreEqual(Expected, actual);
        }

        /// <summary>Derived class for testing CachedMeasureSource</summary>
        private class TestCachedMeasureSource : CachedMeasureSource, IMeasureSource
        {
            /// <summary>Initializes a new instance of the TestCachedMeasureSource class</summary>
            public TestCachedMeasureSource()
                : base(20, 2, PersistentDictionaryType.Memory)
            {
            }

            /// <summary>Gets or sets how long to wait after a refresh has started before trying again</summary>
            public static TimeSpan TestCacheUpdateTimeout { get; set; }

            /// <summary>Gets or sets how long to wait between attempts to refresh an expired cache</summary>
            public static TimeSpan TestExpiredCacheRefreshWait { get; set; }

            /// <summary>Gets or sets when the network measures expire</summary>
            public DateTime Expiry { get; set; }

            /// <summary>Gets or sets the network measures used when updating the cache</summary>
            public Func<IEnumerable<IDictionary<string, object>>> GetNetworkMeasures { get; set; }

            /// <summary>Gets a value indicating whether cache updates should be made asynchronously</summary>
            public override bool AsyncUpdate
            {
                get { return false; }
            }

            /// <summary>Gets the source id</summary>
            protected override string SourceName
            {
                get { return "test"; }
            }

            /// <summary>Gets the master category display name</summary>
            protected override string MasterCategoryDisplayName
            {
                get { return "Master Category"; }
            }

            /// <summary>Gets the category display name</summary>
            protected override string CategoryDisplayName
            {
                get { return "Category"; }
            }

            /// <summary>Gets how long to wait between attempts to update the cache</summary>
            protected override TimeSpan CacheUpdateTimeout
            {
                get { return TestCacheUpdateTimeout; }
            }

            /// <summary>Gets how long to wait between attempts to refresh an expired cache</summary>
            protected override TimeSpan ExpiredCacheRefreshWait
            {
                get { return TestExpiredCacheRefreshWait; }
            }

            /// <summary>Public method for testing protected MakeMeasureDisplayName</summary>
            /// <param name="measureNameParts">Measure name parts</param>
            /// <returns>The measure name</returns>
            public string TestMakeMeasureDisplayName(params string[] measureNameParts)
            {
                return this.MakeMeasureDisplayName(measureNameParts);
            }

            /// <summary>Gets the latest measure map</summary>
            /// <returns>The latest measure map values</returns>
            protected override MeasureMapCacheEntry FetchLatestMeasureMap()
            {
                var measureMap =
                    this.GetNetworkMeasures()
                    .Select(measure =>
                        new KeyValuePair<long, IDictionary<string, object>>(
                            this.GetMeasureId((long)(int)measure["ID"]),
                            measure))
                    .ToDictionary();
                return new MeasureMapCacheEntry
                {
                    Expiry = this.Expiry,
                    MeasureMapJson = JsonConvert.SerializeObject(measureMap)
                };
            }
        }

        /// <summary>Derived class for testing CachedMeasureSource with Async updates</summary>
        private class TestAsyncCachedMeasureSource : TestCachedMeasureSource
        {
            /// <summary>Gets a value indicating whether cache updates should be made asynchronously</summary>
            public override bool AsyncUpdate
            {
                get { return true; }
            }
        }
    }
}
