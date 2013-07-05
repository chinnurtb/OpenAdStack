//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryExtensions.cs" company="Rare Crowds Inc">
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
using System.Threading;

namespace Utilities.Storage
{
    /// <summary>Extensions for IPersistentDicitonary</summary>
    public static class PersistentDictionaryExtensions
    {
        /// <summary>Attempts to update the dictionary entry</summary>
        /// <remarks>
        /// If the entry is modified by another instance of IPersistentDictionary between the time
        /// it is retrieved and the time it is persisted then IPersistedDictionary will throw an
        /// InvalidOperationException. This extension gets the value, updates it and attempts to
        /// persist it. It will do this up to 100 times (with 10ms waits between attempts).
        /// </remarks>
        /// <param name="dictionary">Dictionary containing the entry to update</param>
        /// <param name="key">Key of the entry to update</param>
        /// <param name="update">Update to make to the entry</param>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <returns>True if the entry was updated; otherwise, False.</returns>
        public static bool TryUpdateValue<TValue>(this IPersistentDictionary<TValue> dictionary, string key, Func<TValue, TValue> update)
        {
            int tries = 100;
            while (tries-- > 0)
            {
                try
                {
                    var value = dictionary[key];
                    dictionary[key] = update(value);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    Thread.Sleep(10);
                }
            }

            return false;
        }

        /// <summary>Attempts to update the dictionary entry</summary>
        /// <remarks>
        /// If the entry is modified by another instance of IPersistentDictionary between the time
        /// it is retrieved and the time it is persisted then IPersistedDictionary will throw an
        /// InvalidOperationException. This extension gets the value, updates it and attempts to
        /// persist it. It will do this up to 100 times (with 10ms waits between attempts).
        /// </remarks>
        /// <param name="dictionary">Dictionary containing the entry to update</param>
        /// <param name="key">Key of the entry to update</param>
        /// <param name="update">Update to make to the entry</param>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <returns>True if the entry was updated; otherwise, False.</returns>
        public static bool TryUpdateValue<TValue>(this IPersistentDictionary<TValue> dictionary, string key, Action<TValue> update)
            where TValue : class
        {
            int tries = 100;
            while (tries-- > 0)
            {
                try
                {
                    var value = dictionary[key];
                    update(value);
                    dictionary[key] = value;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    Thread.Sleep(10);
                }
            }

            return false;
        }
    }
}
