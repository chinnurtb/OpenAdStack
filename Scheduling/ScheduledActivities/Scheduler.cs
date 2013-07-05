//-----------------------------------------------------------------------
// <copyright file="Scheduler.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Diagnostics;

namespace ScheduledActivities
{
    /// <summary>
    /// Used to add entries to a schedule from which scheduled activity
    /// sources will create activity requests.
    /// </summary>
    public static class Scheduler
    {
        /// <summary>Backing field for Registires. DO NOT USE DIRECTLY.</summary>
        private static IDictionary<string, object> registries;

        /// <summary>Gets or sets the dictionary of registries</summary>
        internal static IDictionary<string, object> Registries
        {
            get { return registries = registries ?? new Dictionary<string, object>(); }
            
            // For testing only
            set { registries = value; }
        }

        /// <summary>
        /// Gets the time slot key under which entries for the specified date time will be stored.
        /// This is intended for diagnostics and testing purposes only.
        /// </summary>
        /// <param name="dateTime">The DateTime</param>
        /// <returns>The time slot key</returns>
        public static string GetTimeSlotKey(DateTime dateTime)
        {
            return TimeSlottedRegistry<object>.GetTimeSlotKey(dateTime);
        }

        /// <summary>Adds an entry to a schedule at the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot for the entry</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <param name="item1">First component of the entry value</param>
        /// <param name="item2">Second component of the entry value</param>
        /// <param name="item3">Third component of the entry value</param>
        /// <param name="item4">Fourth component of the entry value</param>
        /// <param name="item5">Fifth component of the entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match existing entry values.
        /// </exception>
        public static bool AddToSchedule<T1, T2, T3, T4, T5>(string scheduleName, DateTime timeSlot, string entryKey, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            return AddToSchedule(scheduleName, timeSlot, entryKey, new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5));
        }

        /// <summary>Adds an entry to a schedule at the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot for the entry</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <param name="item1">First component of the entry value</param>
        /// <param name="item2">Second component of the entry value</param>
        /// <param name="item3">Third component of the entry value</param>
        /// <param name="item4">Fourth component of the entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match existing entry values.
        /// </exception>
        public static bool AddToSchedule<T1, T2, T3, T4>(string scheduleName, DateTime timeSlot, string entryKey, T1 item1, T2 item2, T3 item3, T4 item4)
        {
            return AddToSchedule(scheduleName, timeSlot, entryKey, new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4));
        }

        /// <summary>Adds an entry to a schedule at the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot for the entry</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <param name="item1">First component of the entry value</param>
        /// <param name="item2">Second component of the entry value</param>
        /// <param name="item3">Third component of the entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match existing entry values.
        /// </exception>
        public static bool AddToSchedule<T1, T2, T3>(string scheduleName, DateTime timeSlot, string entryKey, T1 item1, T2 item2, T3 item3)
        {
            return AddToSchedule(scheduleName, timeSlot, entryKey, new Tuple<T1, T2, T3>(item1, item2, item3));
        }

        /// <summary>Adds an entry to a schedule at the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot for the entry</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <param name="item1">First component of the entry value</param>
        /// <param name="item2">Second component of the entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match existing entry values.
        /// </exception>
        public static bool AddToSchedule<T1, T2>(string scheduleName, DateTime timeSlot, string entryKey, T1 item1, T2 item2)
        {
            return AddToSchedule(scheduleName, timeSlot, entryKey, new Tuple<T1, T2>(item1, item2));
        }

        /// <summary>Adds an entry to a schedule at the specified time</summary>
        /// <typeparam name="TValue">Type of the entry's value</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot for the entry</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <param name="item">The entry value</param>
        /// <returns>True if the entry was successfully added; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match existing entry values.
        /// </exception>
        public static bool AddToSchedule<TValue>(string scheduleName, DateTime timeSlot, string entryKey, TValue item)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            return registry.Add(timeSlot, entryKey, item);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3, T4, T5>(string scheduleName, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3, T4, T5>>(scheduleName, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3, T4>(string scheduleName, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3, T4>>(scheduleName, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3>(string scheduleName, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3>>(scheduleName, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2>(string scheduleName, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2>>(scheduleName, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule</summary>
        /// <typeparam name="TValue">Type of the entry's value</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<TValue>(string scheduleName, string entryKey)
        {
            return RemoveFromSchedule<TValue>(scheduleName, DateTime.MaxValue, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3, T4, T5>(string scheduleName, DateTime toDateTime, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3, T4, T5>>(scheduleName, toDateTime, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3, T4>(string scheduleName, DateTime toDateTime, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3, T4>>(scheduleName, toDateTime, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2, T3>(string scheduleName, DateTime toDateTime, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2, T3>>(scheduleName, toDateTime, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<T1, T2>(string scheduleName, DateTime toDateTime, string entryKey)
        {
            return RemoveFromSchedule<Tuple<T1, T2>>(scheduleName, toDateTime, entryKey);
        }

        /// <summary>Removes entries for a key from a schedule up to the specified time</summary>
        /// <typeparam name="TValue">Type of the entry's value</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="toDateTime">Time to remove entries up to</param>
        /// <param name="entryKey">Key of the entries to remove</param>
        /// <returns>True if all entries were successfully removed; Otherwise, false.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Explicit type parameters are required to get the registry")]
        public static bool RemoveFromSchedule<TValue>(string scheduleName, DateTime toDateTime, string entryKey)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            if (!registry.RemoveAll(entryKey, toDateTime))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to remove one or more entries for '{0}' from schedule '{1}'",
                    entryKey,
                    scheduleName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Process the schedule entries up to the specified time using the provied function.
        /// </summary>
        /// <typeparam name="T1">Type of the entries' first components</typeparam>
        /// <typeparam name="T2">Type of the entries' second components</typeparam>
        /// <typeparam name="T3">Type of the entries' third components</typeparam>
        /// <typeparam name="T4">Type of the entries' fourth components</typeparam>
        /// <typeparam name="T5">Type of the entries' fifth components</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up until which entries should be processed</param>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be re-processed.
        /// </param>
        /// <param name="func">Function used to process the schedule entries</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if value types do not match the schedule entry values.
        /// </exception>
        public static void ProcessEntries<T1, T2, T3, T4, T5>(
            string scheduleName,
            DateTime timeSlot,
            TimeSpan inProgressExpiry,
            Func<string, T1, T2, T3, T4, T5, bool> func)
        {
            ProcessEntries<Tuple<T1, T2, T3, T4, T5>>(
                scheduleName,
                timeSlot,
                inProgressExpiry,
                (key, tuple) =>
                {
                    return func(
                        key,
                        tuple.Item1,
                        tuple.Item2,
                        tuple.Item3,
                        tuple.Item4,
                        tuple.Item5);
                });
        }

        /// <summary>
        /// Process the schedule entries up to the specified time using the provied function.
        /// </summary>
        /// <typeparam name="T1">Type of the entries' first components</typeparam>
        /// <typeparam name="T2">Type of the entries' second components</typeparam>
        /// <typeparam name="T3">Type of the entries' third components</typeparam>
        /// <typeparam name="T4">Type of the entries' fourth components</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up until which entries should be processed</param>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be re-processed.
        /// </param>
        /// <param name="func">Function used to process the schedule entries</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if value types do not match the schedule entry values.
        /// </exception>
        public static void ProcessEntries<T1, T2, T3, T4>(
            string scheduleName,
            DateTime timeSlot,
            TimeSpan inProgressExpiry,
            Func<string, T1, T2, T3, T4, bool> func)
        {
            ProcessEntries<Tuple<T1, T2, T3, T4>>(
                scheduleName,
                timeSlot,
                inProgressExpiry,
                (key, tuple) =>
                {
                    return func(
                        key,
                        tuple.Item1,
                        tuple.Item2,
                        tuple.Item3,
                        tuple.Item4);
                });
        }

        /// <summary>
        /// Process the schedule entries up to the specified time using the provied function.
        /// </summary>
        /// <typeparam name="T1">Type of the entries' first components</typeparam>
        /// <typeparam name="T2">Type of the entries' second components</typeparam>
        /// <typeparam name="T3">Type of the entries' third components</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up until which entries should be processed</param>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be re-processed.
        /// </param>
        /// <param name="func">Function used to process the schedule entries</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if value types do not match the schedule entry values.
        /// </exception>
        public static void ProcessEntries<T1, T2, T3>(
            string scheduleName,
            DateTime timeSlot,
            TimeSpan inProgressExpiry,
            Func<string, T1, T2, T3, bool> func)
        {
            ProcessEntries<Tuple<T1, T2, T3>>(
                scheduleName,
                timeSlot,
                inProgressExpiry,
                (key, tuple) =>
                {
                    return func(
                        key,
                        tuple.Item1,
                        tuple.Item2,
                        tuple.Item3);
                });
        }

        /// <summary>
        /// Process the schedule entries up to the specified time using the provied function.
        /// </summary>
        /// <typeparam name="T1">Type of the entries' first components</typeparam>
        /// <typeparam name="T2">Type of the entries' second components</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up until which entries should be processed</param>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be re-processed.
        /// </param>
        /// <param name="func">Function used to process the schedule entries</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if value types do not match the schedule entry values.
        /// </exception>
        public static void ProcessEntries<T1, T2>(
            string scheduleName,
            DateTime timeSlot,
            TimeSpan inProgressExpiry,
            Func<string, T1, T2, bool> func)
        {
            ProcessEntries<Tuple<T1, T2>>(
                scheduleName,
                timeSlot,
                inProgressExpiry,
                (key, tuple) =>
                {
                    return func(
                        key,
                        tuple.Item1,
                        tuple.Item2);
                });
        }

        /// <summary>
        /// Process the schedule entries up to the specified time using the provied function.
        /// </summary>
        /// <typeparam name="TValue">Type of the entries' values</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up until which entries should be processed</param>
        /// <param name="inProgressExpiry">
        /// Time after which in-progress entries are considered to be "expired" and should be re-scheduled.
        /// </param>
        /// <param name="func">Function used to process the schedule entries</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exceptions are logged before processing continues")]
        public static void ProcessEntries<TValue>(
            string scheduleName,
            DateTime timeSlot,
            TimeSpan inProgressExpiry,
            Func<string, TValue, bool> func)
        {
            var registry = GetRegistry<TValue>(scheduleName);

            // Pull expired entries from in-progress to now
            registry.MoveInProgressExpiredToUtcNow(inProgressExpiry);

            // Process each entry using the provided function
            foreach (var entry in registry[timeSlot])
            {
                var entryTimeSlot = entry.Item1;
                var entryKey = entry.Item2;
                var entryValue = entry.Item3;

                try
                {
                    if (func(entryKey, entryValue))
                    {
                        // The entry was successfully processed.
                        // Move to in-progress.
                        registry.MoveToInProgress(entryTimeSlot, entryKey);
                    }
                }
                catch (Exception e)
                {
                    // Log errors and continue
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "Error processing schedule '{0}' (timeSlot: {1}; entryKey: {2}; entryValue: {3})\n{4}",
                        scheduleName,
                        entryTimeSlot,
                        entryKey,
                        entryValue,
                        e);
                }
            }
        }

        /// <summary>Removes a completed entry from the schedule</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static bool RemoveCompletedEntry<T1, T2, T3, T4, T5>(string scheduleName, string entryKey)
        {
            return RemoveCompletedEntry<Tuple<T1, T2, T3, T4, T5>>(scheduleName, entryKey);
        }

        /// <summary>Removes a completed entry from the schedule</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static bool RemoveCompletedEntry<T1, T2, T3, T4>(string scheduleName, string entryKey)
        {
            return RemoveCompletedEntry<Tuple<T1, T2, T3, T4>>(scheduleName, entryKey);
        }

        /// <summary>Removes a completed entry from the schedule</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Typeparams required to get the registry")]
        public static bool RemoveCompletedEntry<T1, T2, T3>(string scheduleName, string entryKey)
        {
            return RemoveCompletedEntry<Tuple<T1, T2, T3>>(scheduleName, entryKey);
        }

        /// <summary>Removes a completed entry from the schedule</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "Typeparams required to get the registry")]
        public static bool RemoveCompletedEntry<T1, T2>(string scheduleName, string entryKey)
        {
            return RemoveCompletedEntry<Tuple<T1, T2>>(scheduleName, entryKey);
        }

        /// <summary>Removes a completed entry from the schedule</summary>
        /// <typeparam name="TValue">Type of entry's value</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="entryKey">Key of the entry</param>
        /// <returns>True if the entry was successfully removed; Otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static bool RemoveCompletedEntry<TValue>(string scheduleName, string entryKey)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            return registry.RemoveFromInProgress(entryKey);
        }

        /// <summary>Gets the number of in-progress schedule entries</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <returns>The number of in-progress entries.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetInProgressCount<T1, T2, T3, T4, T5>(string scheduleName)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4, T5>>(scheduleName);
            return registry.InProgress.Count();
        }

        /// <summary>Gets the number of in-progress schedule entries</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <returns>The number of in-progress entries.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetInProgressCount<T1, T2, T3, T4>(string scheduleName)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4>>(scheduleName);
            return registry.InProgress.Count();
        }

        /// <summary>Gets the number of in-progress schedule entries</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <returns>The number of in-progress entries.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetInProgressCount<T1, T2, T3>(string scheduleName)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3>>(scheduleName);
            return registry.InProgress.Count();
        }

        /// <summary>Gets the number of in-progress schedule entries</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <returns>The number of in-progress entries.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetInProgressCount<T1, T2>(string scheduleName)
        {
            var registry = GetRegistry<Tuple<T1, T2>>(scheduleName);
            return registry.InProgress.Count();
        }

        /// <summary>Gets the number of in-progress schedule entries</summary>
        /// <typeparam name="TValue">Type of the entries' values</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <returns>The number of in-progress entries.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetInProgressCount<TValue>(string scheduleName)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            return registry.InProgress.Count();
        }

        /// <summary>Gets the number of in-progress schedule entries that satisfy a condition.</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="predicate">Function to test each entry for a condition.</param>
        /// <returns>The number of entries that satisfy the predicate condition.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        public static int GetInProgressCount<T1, T2, T3, T4, T5>(string scheduleName, Func<string, T1, T2, T3, T4, T5, bool> predicate)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4, T5>>(scheduleName);
            return registry.InProgress.Count(
                (kvp) =>
                predicate(
                    kvp.Key,
                    kvp.Value.Item1,
                    kvp.Value.Item2,
                    kvp.Value.Item3,
                    kvp.Value.Item4,
                    kvp.Value.Item5));
        }

        /// <summary>Gets the number of in-progress schedule entries that satisfy a condition.</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="predicate">Function to test each entry for a condition.</param>
        /// <returns>The number of entries that satisfy the predicate condition.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        public static int GetInProgressCount<T1, T2, T3, T4>(string scheduleName, Func<string, T1, T2, T3, T4, bool> predicate)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4>>(scheduleName);
            return registry.InProgress.Count(
                (kvp) =>
                predicate(
                    kvp.Key,
                    kvp.Value.Item1,
                    kvp.Value.Item2,
                    kvp.Value.Item3,
                    kvp.Value.Item4));
        }

        /// <summary>Gets the number of in-progress schedule entries that satisfy a condition.</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="predicate">Function to test each entry for a condition.</param>
        /// <returns>The number of entries that satisfy the predicate condition.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        public static int GetInProgressCount<T1, T2, T3>(string scheduleName, Func<string, T1, T2, T3, bool> predicate)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3>>(scheduleName);
            return registry.InProgress.Count(
                (kvp) =>
                predicate(
                    kvp.Key,
                    kvp.Value.Item1,
                    kvp.Value.Item2,
                    kvp.Value.Item3));
        }

        /// <summary>Gets the number of in-progress schedule entries that satisfy a condition.</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="predicate">Function to test each entry for a condition.</param>
        /// <returns>The number of entries that satisfy the predicate condition.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        public static int GetInProgressCount<T1, T2>(string scheduleName, Func<string, T1, T2, bool> predicate)
        {
            var registry = GetRegistry<Tuple<T1, T2>>(scheduleName);
            return registry.InProgress.Count(
                (kvp) =>
                predicate(
                    kvp.Key,
                    kvp.Value.Item1,
                    kvp.Value.Item2));
        }

        /// <summary>Gets the number of in-progress schedule entries that satisfy a condition.</summary>
        /// <typeparam name="TValue">Type of the entries' values</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="predicate">Function to test each entry for a condition.</param>
        /// <returns>The number of entries that satisfy the predicate condition.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if entry component types do not match the schedule entry values.
        /// </exception>
        public static int GetInProgressCount<TValue>(string scheduleName, Func<string, TValue, bool> predicate)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            return registry.InProgress.Count((kvp) => predicate(kvp.Key, kvp.Value));
        }

        /// <summary>Gets the number of schedule entries up to the specified timeSlot</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <typeparam name="T5">Type of the entry's fifth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up to which entries are to be counted</param>
        /// <returns>The number of entries up to the specified time slot.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetScheduledCount<T1, T2, T3, T4, T5>(string scheduleName, DateTime timeSlot)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4, T5>>(scheduleName);
            return registry[timeSlot].Count;
        }

        /// <summary>Gets the number of schedule entries up to the specified timeSlot</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <typeparam name="T4">Type of the entry's fourth component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up to which entries are to be counted</param>
        /// <returns>The number of entries up to the specified time slot.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetScheduledCount<T1, T2, T3, T4>(string scheduleName, DateTime timeSlot)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3, T4>>(scheduleName);
            return registry[timeSlot].Count;
        }

        /// <summary>Gets the number of schedule entries up to the specified timeSlot</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <typeparam name="T3">Type of the entry's third component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up to which entries are to be counted</param>
        /// <returns>The number of entries up to the specified time slot.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetScheduledCount<T1, T2, T3>(string scheduleName, DateTime timeSlot)
        {
            var registry = GetRegistry<Tuple<T1, T2, T3>>(scheduleName);
            return registry[timeSlot].Count;
        }

        /// <summary>Gets the number of schedule entries up to the specified timeSlot</summary>
        /// <typeparam name="T1">Type of the entry's first component</typeparam>
        /// <typeparam name="T2">Type of the entry's second component</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up to which entries are to be counted</param>
        /// <returns>The number of entries up to the specified time slot.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetScheduledCount<T1, T2>(string scheduleName, DateTime timeSlot)
        {
            var registry = GetRegistry<Tuple<T1, T2>>(scheduleName);
            return registry[timeSlot].Count;
        }

        /// <summary>Gets the number of schedule entries up to the specified timeSlot</summary>
        /// <typeparam name="TValue">Type of the entries' values</typeparam>
        /// <param name="scheduleName">Name of the schedule</param>
        /// <param name="timeSlot">Time slot up to which entries are to be counted</param>
        /// <returns>The number of entries up to the specified time slot.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match the schedule entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "TValue is required to get the registry")]
        public static int GetScheduledCount<TValue>(string scheduleName, DateTime timeSlot)
        {
            var registry = GetRegistry<TValue>(scheduleName);
            return registry[timeSlot].Count;
        }

        /// <summary>Gets a schedule registry, creating it if it does not exist.</summary>
        /// <typeparam name="TValue">Type of registry entry values</typeparam>
        /// <param name="scheduleName">The schedule name</param>
        /// <returns>The schedule</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <typeparamref name="TValue"/> does not match existing entry values.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "TValue is typeparam")]
        internal static TimeSlottedRegistry<TValue> GetRegistry<TValue>(string scheduleName)
        {
            try
            {
                if (!Registries.ContainsKey(scheduleName))
                {
                    Registries.Add(scheduleName, new TimeSlottedRegistry<TValue>(scheduleName));
                }
            }
            catch (Exception e)
            {
                var message =
                    "Unable to create TimeSlottedRegistry '{0}' ({1}).\n{2}"
                    .FormatInvariant(
                        scheduleName,
                        typeof(TValue).Name,
                        e);
                LogManager.Log(LogLevels.Error, message);
                throw new ArgumentException(message, "TValue", e);
            }

            var registry = Registries[scheduleName] as TimeSlottedRegistry<TValue>;
            if (registry == null)
            {
                var message =
                    "Incorrect signature for schedule '{0}': '{1}'"
                    .FormatInvariant(
                        scheduleName,
                        typeof(TValue).Name);
                LogManager.Log(LogLevels.Error, message);
                throw new ArgumentException(message, "TValue");
            }

            return registry;
        }
    }
}
