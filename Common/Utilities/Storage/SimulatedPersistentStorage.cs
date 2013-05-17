//-----------------------------------------------------------------------
// <copyright file="SimulatedPersistentStorage.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Utilities.Storage
{
    /// <summary>
    /// Used by MemoryPersistentDictionary in place of real persistent storage.
    /// </summary>
    /// <remarks>
    /// Simulates persistent storage using dictionaries of StoreEntry.
    /// </remarks>
    /// <seealso cref="MemoryPersistentDictionaryEntry"/>
    public static class SimulatedPersistentStorage
    {
        /// <summary>
        /// In memory dictionaries of StoreEntry used to simulate persistent storage.
        /// </summary>
        private static readonly IDictionary<string, IDictionary<string, MemoryPersistentDictionaryEntry>> Stores
            = new Dictionary<string, IDictionary<string, MemoryPersistentDictionaryEntry>>();

        /// <summary>Gets the names of the simulated persistent stores</summary>
        /// <returns>The store names</returns>
        public static string[] GetStoreNames()
        {
            return Stores.Keys.ToArray();
        }

        /// <summary>Clears simulated storage.</summary>
        public static void Clear()
        {
            Stores.Clear();
        }

        /// <summary>
        /// Creates a simulated persistent store if it does not already exist.
        /// </summary>
        /// <param name="storeName">Name of the store to create</param>
        /// <returns>
        /// False if the store already existed; otherwise, true.
        /// </returns>
        internal static bool CreateIfNotExist(string storeName)
        {
            lock (Stores)
            {
                if (Stores.ContainsKey(storeName))
                {
                    return false;
                }

                Stores.Add(storeName, new Dictionary<string, MemoryPersistentDictionaryEntry>());
                return true;
            }
        }

        /// <summary>
        /// Deletes a simulated persistent store if it exists.
        /// </summary>
        /// <param name="storeName">Name of the store to delete</param>
        /// <returns>
        /// True if the store existed and was deleted; otherwise, false.
        /// </returns>
        internal static bool DeleteIfExists(string storeName)
        {
            lock (Stores)
            {
                if (!Stores.ContainsKey(storeName))
                {
                    return false;
                }

                Stores.Remove(storeName);
                return true;
            }
        }

        /// <summary>
        /// Gets the simulated store entry dictionary with the specified name
        /// </summary>
        /// <param name="storeName">The store name</param>
        /// <returns>The simulated store entry dictionary</returns>
        internal static IDictionary<string, MemoryPersistentDictionaryEntry> GetStore(string storeName)
        {
            CreateIfNotExist(storeName);
            return Stores[storeName];
        }

        /// <summary>
        /// Gets an entry from a store, creating it if it does not exist.
        /// </summary>
        /// <param name="storeName">Store to get the entry from.</param>
        /// <param name="entryName">Name of the entry to get</param>
        /// <returns>The entry</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// No store exists for <paramref name="storeName"/>.
        /// </exception>
        internal static MemoryPersistentDictionaryEntry GetEntry(string storeName, string entryName)
        {
            // Throws KeyNotFoundException if the store does not exist
            var store = Stores[storeName];
            lock (store)
            {
                if (!store.ContainsKey(entryName))
                {
                    store.Add(entryName, new MemoryPersistentDictionaryEntry());
                }

                return store[entryName];
            }
        }

        /// <summary>Deletes the entry if it exists.</summary>
        /// <param name="storeName">Name of the store containing the entry</param>
        /// <param name="entryName">Name of the entry to delete</param>
        /// <returns>True if the entry was deleted; otherwise, if it did not exist, false.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// No store exists for <paramref name="storeName"/>.
        /// </exception>
        internal static bool DeleteEntryIfExists(string storeName, string entryName)
        {
            // Throws KeyNotFoundException if the store does not exist
            var store = Stores[storeName];
            lock (store)
            {
                if (!store.ContainsKey(entryName))
                {
                    return false;
                }

                store.Remove(entryName);
                return true;
            }
        }
    }
}
