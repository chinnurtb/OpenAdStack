//-----------------------------------------------------------------------
// <copyright file="TimeSlottedRegistry.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Diagnostics;
using Utilities.Storage;

namespace ScheduledActivities
{
    /// <summary>Registry of time slotted string key/value pairs collections</summary>
    /// <typeparam name="TValue">Type of the values to store in the slots</typeparam>
    internal class TimeSlottedRegistry<TValue>
    {
        /// <summary>
        /// Slot key for in-progress entries
        /// </summary>
        internal const string InProgressSlotKey = "<InProgress>";

        /// <summary>
        /// DateTime.ToString(string fmt) format for time slotted persistent dictionaries
        /// </summary>
        internal const string TimeSlotKeyFormat = "yyyyMMddHHmm";

        /// <summary>
        /// Persistent dictionary containing the time slot data
        /// </summary>
        private readonly IPersistentDictionary<IDictionary<string, Tuple<DateTime, TValue>>> dictionary;

        /// <summary>Initializes a new instance of the TimeSlottedRegistry class.</summary>
        /// <param name="storeName">
        /// Store name for the backing persistent dictionary
        /// </param>
        public TimeSlottedRegistry(string storeName)
        {
            this.dictionary = PersistentDictionaryFactory.CreateDictionary<IDictionary<string, Tuple<DateTime, TValue>>>(storeName);
            if (!this.dictionary.ContainsKey(InProgressSlotKey))
            {
                this.dictionary[InProgressSlotKey] = new Dictionary<string, Tuple<DateTime, TValue>>();
            }
        }

        /// <summary>Gets the in-progress items</summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic type is appropriate here")]
        public ReadOnlyCollection<KeyValuePair<string, TValue>> InProgress
        {
            get
            {
                return new ReadOnlyCollection<KeyValuePair<string, TValue>>(
                    this.dictionary[InProgressSlotKey]
                    .Select(entry => new KeyValuePair<string, TValue>(entry.Key, entry.Value.Item2))
                    .ToArray());
            }
        }

        /// <summary>Gets the entries for the specified time slot key</summary>
        /// <param name="timeSlotKey">The time slot</param>
        /// <returns>The entries</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic type is appropriate here")]
        public ReadOnlyCollection<KeyValuePair<string, TValue>> this[string timeSlotKey]
        {
            get
            {
                return new ReadOnlyCollection<KeyValuePair<string, TValue>>(
                    this.dictionary[timeSlotKey]
                    .Select(entry => new KeyValuePair<string, TValue>(entry.Key, entry.Value.Item2))
                    .ToArray());
            }
        }

        /// <summary>
        /// Gets all the entries up to the specified date time.
        /// </summary>
        /// <remarks>
        /// Excludes in-progress (use TimeSlottedRegistry.InProgress).
        /// </remarks>
        /// <param name="toDateTime">Time to get entries up to</param>
        /// <returns>The entries us tuples of time slot key, entry key, entry value</returns>
        [SuppressMessage("Microsoft.Design", "CA1043", Justification = "By Design")]
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic type is appropriate here")]
        public ReadOnlyCollection<Tuple<string, string, TValue>> this[DateTime toDateTime]
        {
            get
            {
                // Duplicates the logic in GetTimeSlotKeys to avoid excessive
                // iterations over the persistent dictionary's keys.
                var dateTimeKey = GetTimeSlotKey(toDateTime);
                var entries = this.dictionary
                    .Where(slot =>
                        slot.Key != InProgressSlotKey &&
                        string.CompareOrdinal(slot.Key, dateTimeKey) <= 0)
                    .SelectMany(kvp => kvp.Value
                        .Select(entry => new Tuple<string, string, TValue>(kvp.Key, entry.Key, entry.Value.Item2)))
                    .ToArray();
                return new ReadOnlyCollection<Tuple<string, string, TValue>>(entries);
            }
        }

        /// <summary>Gets the time slot key corresponding to the specified DateTime</summary>
        /// <remarks>Rounds down to the previous hour</remarks>
        /// <param name="dateTime">DateTime to get the slot key for</param>
        /// <returns>The time slot key</returns>
        public static string GetTimeSlotKey(DateTime dateTime)
        {
            var slotTime = dateTime - new TimeSpan(0, dateTime.Minute, dateTime.Second);
            return slotTime.ToString(TimeSlotKeyFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the entries that have been in-progress for longer than the allowed expiry time
        /// </summary>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired"
        /// </param>
        /// <returns>The expired entries</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic type is appropriate here")]
        public ReadOnlyCollection<KeyValuePair<string, TValue>> GetInProgressExpired(TimeSpan inProgressExpiry)
        {
            var expiry = DateTime.UtcNow - inProgressExpiry;
            return new ReadOnlyCollection<KeyValuePair<string, TValue>>(
                this.dictionary[InProgressSlotKey]
                .Where(entry => entry.Value.Item1 <= expiry)
                .Select(entry => new KeyValuePair<string, TValue>(entry.Key, entry.Value.Item2))
                .ToArray());
        }

        /// <summary>Add the entry to the slot corresponding to the specified time</summary>
        /// <param name="timeSlot">Time corresponding to the slot</param>
        /// <param name="entryKey">The entry key</param>
        /// <param name="entryValue">The entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        public bool Add(DateTime timeSlot, string entryKey, TValue entryValue)
        {
            return this.Add(GetTimeSlotKey(timeSlot), entryKey, entryValue);
        }
        
        /// <summary>Adds the entry key value pair to the specified time slot</summary>
        /// <param name="timeSlotKey">The time slot</param>
        /// <param name="entryKey">The entry key</param>
        /// <param name="entryValue">The entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        public bool Add(string timeSlotKey, string entryKey, TValue entryValue)
        {
            if (!this.dictionary.ContainsKey(timeSlotKey))
            {
                try
                {
                    this.dictionary.Add(timeSlotKey, new Dictionary<string, Tuple<DateTime, TValue>>());
                }
                catch (InvalidETagException)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Unable to create time slot '{0}' in '{1}'. It has already been created by another process.",
                        timeSlotKey,
                        this.dictionary.StoreName);
                }
            }

            var entry = new KeyValuePair<string, Tuple<DateTime, TValue>>(
                    entryKey,
                    new Tuple<DateTime, TValue>(
                        DateTime.UtcNow,
                        entryValue));

            return this.dictionary.TryUpdateValue(
                timeSlotKey,
                slot => { slot[entry.Key] = entry.Value; });
        }

        /// <summary>Gets the entry from the specified time slot</summary>
        /// <param name="timeSlot">The time slot</param>
        /// <param name="entryKey">The entry key</param>
        /// <returns>The entry value</returns>
        public TValue Get(DateTime timeSlot, string entryKey)
        {
            var timeSlotKey = GetTimeSlotKey(timeSlot);
            return this.dictionary[timeSlotKey][entryKey].Item2;
        }

        /// <summary>Gets the entry from the specified time slot</summary>
        /// <param name="timeSlotKey">The time slot</param>
        /// <param name="entryKey">The entry key</param>
        /// <returns>The entry value</returns>
        public TValue Get(string timeSlotKey, string entryKey)
        {
            return this.dictionary[timeSlotKey][entryKey].Item2;
        }

        /// <summary>Returns all time slot keys</summary>
        /// <remarks>The result will include the in-progress slot</remarks>
        /// <returns>The slot keys</returns>
        public IEnumerable<string> GetTimeSlotKeys()
        {
            return this.dictionary.Keys;
        }

        /// <summary>Returns the time slot keys up to the specified date time</summary>
        /// <remarks>The result does not include the in-progress slot</remarks>
        /// <param name="toDateTime">The time to retrieve keys up until</param>
        /// <returns>The slot keys</returns>
        public IEnumerable<string> GetTimeSlotKeys(DateTime toDateTime)
        {
            var dateTimeKey = GetTimeSlotKey(toDateTime);
            return this.dictionary.Keys
                .Where(k =>
                    string.CompareOrdinal(k, dateTimeKey) <= 0 ||
                    k == InProgressSlotKey);
        }

        /// <summary>Removes the entry from the specified slot</summary>
        /// <param name="timeSlotKey">The time slot to delete from</param>
        /// <param name="entryKey">The entry to remove</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        public bool Remove(string timeSlotKey, string entryKey)
        {
            if (!this.dictionary.ContainsKey(timeSlotKey))
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to remove '{0}' from timeslot {1} of registry {2}. The timeslot does not exist.",
                    entryKey,
                    timeSlotKey,
                    this.dictionary.StoreName);
                return false;
            }

            var removed = this.dictionary.TryUpdateValue(
                timeSlotKey,
                slot => slot.Remove(entryKey));

            // If the last entry has been removed...
            if (removed &&
                timeSlotKey != InProgressSlotKey &&
                this.dictionary[timeSlotKey].Count == 0)
            {
                // ...delete the time slot.
                this.dictionary.Remove(timeSlotKey);
            }

            return removed;
        }

        /// <summary>Removes all entries with entryKey</summary>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        public bool RemoveAll(string entryKey)
        {
            return RemoveAll(entryKey, DateTime.MaxValue);
        }

        /// <summary>Removes all entries with entryKey up to the specified date time</summary>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        public bool RemoveAll(string entryKey, DateTime toDateTime)
        {
            var success = true;
            foreach (var timeSlotKey in this.GetTimeSlotKeys(toDateTime).ToArray())
            {
                if (!this.Remove(timeSlotKey, entryKey))
                {
                    success = false;
                    LogManager.Log(
                        LogLevels.Error,
                        "Unable to remove entry '{0}' from slot '{1}'",
                        entryKey,
                        timeSlotKey);
                }
            }

            return success;
        }

        /// <summary>Removes the entry from the in-progress slot</summary>
        /// <param name="entryKey">The entry to remove</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        public bool RemoveFromInProgress(string entryKey)
        {
            return this.Remove(InProgressSlotKey, entryKey);
        }

        /// <summary>Moves the entry to the in-progress slot</summary>
        /// <param name="sourceSlotKey">The source time slot key</param>
        /// <param name="entryKey">The entry to move</param>
        /// <returns>True if the entry was successfully moved; Otherwise, false.</returns>
        public bool MoveToInProgress(string sourceSlotKey, string entryKey)
        {
            if (sourceSlotKey == InProgressSlotKey)
            {
                return true;
            }

            return this.Move(sourceSlotKey, InProgressSlotKey, entryKey);
        }

        /// <summary>Moves the entry from the in-progress slot</summary>
        /// <param name="timeSlot">The destination time slot</param>
        /// <param name="entryKey">The entry to move</param>
        /// <returns>True if the entry was successfully moved; Otherwise, false.</returns>
        public bool MoveFromInProgress(DateTime timeSlot, string entryKey)
        {
            return this.MoveFromInProgress(GetTimeSlotKey(timeSlot), entryKey);
        }

        /// <summary>Moves the entry from the in-progress slot</summary>
        /// <param name="destinationSlotKey">The destination time slot key</param>
        /// <param name="entryKey">The entry to move</param>
        /// <returns>True if the entry was successfully moved; Otherwise, false.</returns>
        public bool MoveFromInProgress(string destinationSlotKey, string entryKey)
        {
            if (destinationSlotKey == InProgressSlotKey)
            {
                return true;
            }

            return this.Move(InProgressSlotKey, destinationSlotKey, entryKey);
        }

        /// <summary>
        /// Move expired in-progress entries to the present slot
        /// </summary>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be moved to UtcNow
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if moving an entry fails
        /// </exception>
        public void MoveInProgressExpiredToUtcNow(TimeSpan inProgressExpiry)
        {
            var expiredEntries = this.GetInProgressExpired(inProgressExpiry);
            foreach (var expiredEntry in expiredEntries)
            {
                if (!this.MoveFromInProgress(DateTime.UtcNow, expiredEntry.Key))
                {
                    throw new InvalidOperationException(
                        "Unable to move expired entry from in-progress to present slot ({0}, {1})"
                        .FormatInvariant(expiredEntry.Key, expiredEntry.Value));
                }
            }
        }

        /// <summary>Moves an entry from one slot to another</summary>
        /// <param name="sourceSlotKey">Source time slot key</param>
        /// <param name="destinationSlotKey">Destination time slot key</param>
        /// <param name="entryKey">The entry to move</param>
        /// <returns>True if the entry was successfully moved; Otherwise, false.</returns>
        public bool Move(string sourceSlotKey, string destinationSlotKey, string entryKey)
        {
            return Move(this, sourceSlotKey, this, destinationSlotKey, entryKey);
        }

        /// <summary>
        /// Moves an entry from a slot in one TimeSlottedPersistentDictionary to a slot in another TimeSlottedPersistentDictionary
        /// </summary>
        /// <param name="source">Source TimeSlottedPersistentDictionary</param>
        /// <param name="sourceSlotKey">Source time slot key</param>
        /// <param name="destination">Destination TimeSlottedPersistentDictionary</param>
        /// <param name="destinationSlotKey">Destination time slot key</param>
        /// <param name="entryKey">The entry to move</param>
        /// <returns>True if the entry was successfully moved; Otherwise, false.</returns>
        private static bool Move(TimeSlottedRegistry<TValue> source, string sourceSlotKey, TimeSlottedRegistry<TValue> destination, string destinationSlotKey, string entryKey)
        {
            var entryValue = source.dictionary[sourceSlotKey][entryKey].Item2;
            if (!destination.Add(destinationSlotKey, entryKey, entryValue))
            {
                return false;
            }

            return source.Remove(sourceSlotKey, entryKey);
        }
    }
}
