//-----------------------------------------------------------------------
// <copyright file="CachedMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Diagnostics;
using Newtonsoft.Json;
using Utilities.Storage;

namespace DynamicAllocation
{
    /// <summary>Base class for cached measure sources</summary>
    /// <remarks>
    /// Fetches cached measures from persistent storage, updating if needed.
    /// </remarks>
    public abstract class CachedMeasureSource : NetworkMeasureSource, IMeasureSource
    {
        /// <summary>Name of the store for cached measures</summary>
        private const string MeasureCacheStoreName = "measurecache";

        /// <summary>Name of the store for cached measures</summary>
        private const string MeasureCacheUpdateTimeStoreName = "cachedmeasures-updatetimes";

        /// <summary>Default value for ExpiredCacheRefreshWait (5 seconds)</summary>
        private static readonly TimeSpan DefaultExpiredCacheRefreshWait = new TimeSpan(0, 0, 5);

        /// <summary>Default value for CacheUpdateTimeout (5 minutes)</summary>
        private static readonly TimeSpan DefaultCacheUpdateTimeout = new TimeSpan(0, 5, 0);

        /// <summary>The type of dictionary to use</summary>
        private readonly PersistentDictionaryType dictionaryType;

        /// <summary>Backing field for CacheUpdateStartTimes</summary>
        private static IPersistentDictionary<DateTime> cacheUpdateStartTimes;

        /// <summary>Backing field for LocalMeasureCache</summary>
        private static IDictionary<string, MeasureMapCacheEntry> localMeasureCache;

        /// <summary>When the cached data will expire</summary>
        private DateTime cacheExpiryTime;

        /// <summary>The last time the cache was checked for updates</summary>
        private DateTime lastUpdateTime;

        /// <summary>Backing field for IMeasureSource.Measures</summary>
        private IDictionary<long, IDictionary<string, object>> measures;

        /// <summary>Backing field for PersistentMeasureCache</summary>
        private IPersistentDictionary<MeasureMapCacheEntry> persistentMeasureCache;

        /// <summary>Initializes a new instance of the CachedMeasureSource class</summary>
        /// <param name="networkMeasureIdPrefix">Network measure id prefix</param>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        /// <param name="dictionaryType">Persistent dictionary type</param>
        protected CachedMeasureSource(byte networkMeasureIdPrefix, byte sourceMeasureIdPrefix, PersistentDictionaryType dictionaryType)
            : base(networkMeasureIdPrefix, sourceMeasureIdPrefix)
        {
            this.dictionaryType = dictionaryType;
        }

        /// <summary>
        /// Gets a value indicating whether cache updates should be made asynchronously.
        /// Default is True.
        /// </summary>
        public virtual bool AsyncUpdate
        {
            get { return true; }
        }

        /// <summary>Gets the measures from this source</summary>
        public sealed override IDictionary<long, IDictionary<string, object>> Measures
        {
            get
            {
                // Fetch measures from the cache if they have not been initialized or
                // if they have expired and the last check was past the refresh wait.
                if ((this.measures == null || DateTime.UtcNow > this.cacheExpiryTime) &&
                    DateTime.UtcNow > this.lastUpdateTime + this.ExpiredCacheRefreshWait)
                {
                    this.lastUpdateTime = DateTime.UtcNow;
                    this.measures = this.UpdateCachedMeasures();
                }

                return this.measures;
            }
        }

        /// <summary>Gets or sets the persistent dictionary of cache update start times</summary>
        internal static IPersistentDictionary<DateTime> CacheUpdateStartTimes
        {
            get
            {
                return cacheUpdateStartTimes = cacheUpdateStartTimes ??
                    PersistentDictionaryFactory.CreateDictionary<DateTime>(
                        MeasureCacheUpdateTimeStoreName,
                        PersistentDictionaryType.Sql);
            }

            // For testing purposes
            set
            {
                cacheUpdateStartTimes = value;
            }
        }

        /// <summary>Gets or sets the local, in-memory dictionary used to cache measures maps</summary>
        internal static IDictionary<string, MeasureMapCacheEntry> LocalMeasureCache
        {
            get
            {
                return localMeasureCache = localMeasureCache ??
                    new Dictionary<string, MeasureMapCacheEntry>();
            }

            // For testing purposes
            set
            {
                localMeasureCache = value;
            }
        }

        /// <summary>Gets the name of the store in which measures are cached</summary>
        /// <remarks>By default, this is the same as the source id</remarks>
        protected virtual string CacheName
        {
            get { return this.SourceId; }
        }

        /// <summary>Gets the shared, persistent dictionary used to cache measures maps</summary>
        protected IPersistentDictionary<MeasureMapCacheEntry> PersistentMeasureCache
        {
            get
            {
                return this.persistentMeasureCache = this.persistentMeasureCache ??
                    PersistentDictionaryFactory.CreateDictionary<MeasureMapCacheEntry>(
                        MeasureCacheStoreName,
                        this.dictionaryType);
            }
        }

        /// <summary>Gets the interval between checks for updates after the cache expiry time</summary>
        protected virtual TimeSpan ExpiredCacheRefreshWait
        {
            get { return DefaultExpiredCacheRefreshWait; }
        }

        /// <summary>
        /// Gets how long to wait after the last cache update started to start an update
        /// </summary>
        /// <remarks>
        /// This is used as a "best effort" locking mechanism to avoid
        /// multiple, simultaneous updates to the same cache.
        /// </remarks>
        protected virtual TimeSpan CacheUpdateTimeout
        {
            get { return DefaultCacheUpdateTimeout; }
        }

        /// <summary>Gets or sets when an update of this cache last started</summary>
        private DateTime LastCacheUpdateStartTime
        {
            get
            {
                return CacheUpdateStartTimes.ContainsKey(this.CacheName) ?
                    CacheUpdateStartTimes[this.CacheName] :
                    DateTime.MinValue;
            }
            
            set
            {
                CacheUpdateStartTimes[this.CacheName] = value;
            }
        }

        /// <summary>Fetch the latest version of the measure map its source</summary>
        /// <returns>The latest measure map</returns>
        protected abstract MeasureMapCacheEntry FetchLatestMeasureMap();

        /// <summary>Updates the measure map cache or gets the last known good</summary>
        /// <returns>The cached measures, if available; otherwise, null</returns>
        private IDictionary<long, IDictionary<string, object>> UpdateCachedMeasures()
        {
            // Check if the local cache requires refresh
            if (this.CacheRequiresRefresh(LocalMeasureCache))
            {
                // Check if the persistent cache requires refresh
                if (!this.CacheRequiresRefresh(this.PersistentMeasureCache))
                {
                    lock (LocalMeasureCache)
                    {
                        // Persisted cache is good, use it to populate the local cache
                        LocalMeasureCache[this.CacheName] = this.PersistentMeasureCache[this.CacheName];
                    }
                }
                else
                {
                    // Check if the stored cache is in the process of being updated
                    if (this.LastCacheUpdateStartTime + this.CacheUpdateTimeout > DateTime.UtcNow)
                    {
                        LogManager.Log(
                            LogLevels.Trace,
                            "Measure map cache '{0}' for measure source {1} is in the process of being updated (started: {2})",
                            this.CacheName,
                            this.SourceId,
                            this.LastCacheUpdateStartTime);
                    }
                    else
                    {
                        if (!this.AsyncUpdate)
                        {
                            LogManager.Log(
                                LogLevels.Trace,
                                "Synchronously updating measure map cache '{0}' for measure source {1}",
                                this.CacheName,
                                this.SourceId);
                            this.UpdateStoredCache();
                        }
                        else
                        {
                            LogManager.Log(
                                LogLevels.Trace,
                                "Asynchronously updating measure map cache '{0}' for measure source {1}",
                                this.CacheName,
                                this.SourceId);
                            var updateThread =
                                new Thread(this.UpdateStoredCache)
                                {
                                    Name = "CachedMeasureSourceUpdate-{0}-{1}-{2}"
                                    .FormatInvariant(this.CacheName, this.SourceId, Guid.NewGuid())
                                };
                            updateThread.Start();
                            LogManager.Log(
                                LogLevels.Information,
                                "Async update of measure map cache '{0}' for measure source {1} started (thread: {2} [{3}])",
                                this.CacheName,
                                this.SourceId,
                                updateThread.Name,
                                updateThread.ManagedThreadId);
                        }
                    }
                }
            }

            lock (LocalMeasureCache)
            {
                // Return null if no cached measures are available
                if (!LocalMeasureCache.ContainsKey(this.CacheName) ||
                    LocalMeasureCache[this.CacheName] == null)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Measure map cache '{0}' for measure source {1} is not available (last update started: {2})",
                        this.CacheName,
                        this.SourceId,
                        this.LastCacheUpdateStartTime);
                    return null;
                }

                // Update the expiry time and return the measures
                this.cacheExpiryTime = LocalMeasureCache[this.CacheName].Expiry;
                return LocalMeasureCache[this.CacheName].MeasureMap;
            }
        }

        /// <summary>Checks if the cache for this measure source requires updating</summary>
        /// <param name="cache">The cache to check (local or persistent)</param>
        /// <returns>True if the cache needs to be refreshed; otherwise, false.</returns>
        private bool CacheRequiresRefresh(IDictionary<string, MeasureMapCacheEntry> cache)
        {
            lock (cache)
            {
                if (!cache.ContainsKey(this.CacheName))
                {
                    return true;
                }

                var cacheEntry = cache[this.CacheName];
                return cacheEntry.Expiry < DateTime.UtcNow;
            }
        }

        /// <summary>Updates the local cache in memory and the shared cache in persistent storage</summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is logged")]
        private void UpdateStoredCache()
        {
            var updateStartTime = DateTime.UtcNow;
            LogManager.Log(
                LogLevels.Trace,
                "Starting update of measure map cache '{0}' for measure source {1}",
                this.CacheName,
                this.SourceId);

            try
            {
                this.LastCacheUpdateStartTime = DateTime.UtcNow;
                var cacheEntry = this.FetchLatestMeasureMap();
                LocalMeasureCache[this.CacheName] = cacheEntry;
                LogManager.Log(
                    LogLevels.Trace,
                    "Updated local measure map cache '{0}' for measure source {1} (expires: {2})",
                    this.CacheName,
                    this.SourceId,
                    cacheEntry.Expiry);
                if (this.CacheRequiresRefresh(this.PersistentMeasureCache))
                {
                    this.PersistentMeasureCache[this.CacheName] = cacheEntry;
                    LogManager.Log(
                        LogLevels.Trace,
                        "Updated persistent measure map cache '{0}' for measure source {1} (expires: {2})",
                        this.CacheName,
                        this.SourceId,
                        cacheEntry.Expiry);
                }
            }
            catch (InvalidETagException)
            {
                // Someone else updated the cache, just move along
                LogManager.Log(
                    LogLevels.Trace,
                    "Unable to update measure map cache '{0}' for measure source {1} ({2})",
                    this.CacheName,
                    this.SourceId,
                    "the cache has already been updated");
            }
            catch (Exception e)
            {
                // Something unexpected went wrong when updating the cache
                // Log the error and then move along
                // (last known good will continue to be used)
                LogManager.Log(
                    LogLevels.Error,
                    "Error updating measure map cache '{0}' for measure source {1}: {2}",
                    this.CacheName,
                    this.SourceId,
                    e);
            }

            LogManager.Log(
                LogLevels.Trace,
                "Finished update of measure map cache '{0}' for measure source {1} (duration: {2}s)",
                this.CacheName,
                this.SourceId,
                (DateTime.UtcNow - updateStartTime).TotalSeconds);
        }
    }
}
