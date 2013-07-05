//-----------------------------------------------------------------------
// <copyright file="MemoryPersistentDictionary.cs" company="Rare Crowds Inc">
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Diagnostics;

namespace Utilities.Storage
{
    /// <summary>Simulated, in memory implementation of IPersistentDictionary for unit testing</summary>
    /// <typeparam name="TValue">Value type. Must be a valid data contract</typeparam>
    public sealed class MemoryPersistentDictionary<TValue> : 
        AbstractPersistentDictionary<TValue>,
        IPersistentDictionary<TValue>
    {
        /// <summary>
        /// Name of the simulated persistent store
        /// </summary>
        private readonly string storeName;

        /// <summary>Initializes a new instance of the MemoryPersistentDictionary class.</summary>
        /// <param name="storeName">Name of the simulated store to use</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        internal MemoryPersistentDictionary(string storeName)
            : this(storeName, false)
        {
        }

        /// <summary>Initializes a new instance of the MemoryPersistentDictionary class.</summary>
        /// <param name="storeName">Name of the simulated store to use</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <exception cref="System.Runtime.Serialization.InvalidDataContractException">
        /// The type specified by <typeparamref name="TValue"/> is not a data contract.
        /// </exception>
        /// <seealso cref="System.Runtime.Serialization.DataContract"/>
        internal MemoryPersistentDictionary(string storeName, bool raw)
            : base(raw)
        {
            this.storeName = storeName;
            SimulatedPersistentStorage.CreateIfNotExist(this.StoreName);
        }

        /// <summary>Gets the name of the store for this dictionary</summary>
        public override string StoreName
        {
            get { return this.storeName; }
        }

        /// <summary>Gets the number of elements</summary>
        public override int Count
        {
            get { return this.Store.Count; }
        }

        /// <summary>Gets the names of the value entries</summary>
        public override ICollection<string> Keys
        {
            get { return this.Store.Keys; }
        }

        /// <summary>Gets the simulated store for this simulated persistent dictionary</summary>
        /// <remarks>
        /// This should only be used to get information. For anything that actually makes
        /// changes use the methods provided by SimulatedPersistentStorage.
        /// </remarks>
        private IDictionary<string, MemoryPersistentDictionaryEntry> Store
        {
            get { return SimulatedPersistentStorage.GetStore(this.StoreName); }
        }

        /// <summary>Determines whether the dictionary contains an entry with the sepecified key</summary>
        /// <param name="key">The key of the value entry to locate</param>
        /// <exception cref="System.ArgumentNullException">Thrown if key is null</exception>
        /// <returns>True if an entry with the key is found; otherwise, false.</returns>
        public override bool ContainsKey(string key)
        {
            return this.Store.ContainsKey(key);
        }

        /// <summary>Returns an enumerator that iterates through the entry value.</summary>
        /// <returns>An IEnumerator&lt;WorkItemScheduleEntry&gt; that can be used to iterate through the entry value.</returns>
        public override IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return this.Store
                .Select(kvp => new KeyValuePair<string, TValue>(
                    kvp.Key,
                    (TValue)this.Serializer.ReadObject(new MemoryStream(kvp.Value.ReadAllBytes()))))
                .GetEnumerator();
        }

        /// <summary>
        /// Gets the entry reference for the specified key
        /// </summary>
        /// <param name="key">Key to get the entry for</param>
        /// <param name="mustExist">Whether the entry must already exist</param>
        /// <param name="checkETag">Whether to check that the cached ETag is current</param>
        /// <param name="updateETag">Whether to update the cached ETag</param>
        /// <returns>The entry reference</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// <paramref name="mustExist"/> is true and no entry exists for the specified key
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <paramref name="checkETag"/> is true and the ETag does not match the one cached
        /// </exception>
        protected override IPersistentDictionaryEntry GetEntry(string key, bool mustExist, bool checkETag, bool updateETag)
        {
            if (mustExist && !this.ContainsKey(key))
            {
                throw new KeyNotFoundException(key);
            }

            var entry = SimulatedPersistentStorage.GetEntry(this.StoreName, key);

            if (checkETag &&
                this.ETags.ContainsKey(key) &&
                entry.ETag != this.ETags[key])
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "The entry for '{0}' has been modified. Expected ETag: {1} Current ETag: {2}",
                    key,
                    this.ETags[key],
                    entry.ETag);
                throw new InvalidETagException(this.StoreName, key, this.ETags[key]);
            }

            if (updateETag)
            {
                this.ETags[key] = entry.ETag;
            }

            return entry;
        }

        /// <summary>Removes the entry with the specified key</summary>
        /// <param name="key">The key of the entry to remove</param>
        /// <returns>
        /// True if the entry is successfully removed; otherwise, false.
        /// Also returns false if the entry was not found.
        /// </returns>
        protected override bool RemoveEntry(string key)
        {
            return SimulatedPersistentStorage.DeleteEntryIfExists(this.StoreName, key);
        }

        /// <summary>Removes all value entries</summary>
        /// <remarks>Deletes the container and recreates it</remarks>
        protected override void ClearEntries()
        {
            SimulatedPersistentStorage.DeleteIfExists(this.StoreName);
            SimulatedPersistentStorage.CreateIfNotExist(this.StoreName);
        }

        /// <summary>Deletes the underlying store where the dictionary is persisted</summary>
        protected override void DeleteStore()
        {
            SimulatedPersistentStorage.DeleteIfExists(this.StoreName);
        }
    }
}
