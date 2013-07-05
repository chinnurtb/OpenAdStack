//-----------------------------------------------------------------------
// <copyright file="SimulatedPersistentDictionaryFactory.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Linq;

namespace Utilities.Storage.Testing
{
    /// <summary>Factory for MemoryPersistentDictionary implementations of IPersistentDictionary</summary>
    public class SimulatedPersistentDictionaryFactory : IPersistentDictionaryFactory
    {
        /// <summary>
        /// Initializes a new instance of the SimulatedPersistentDictionaryFactory class.
        /// </summary>
        /// <param name="dictionaryType">Dictionary type to simulate</param>
        public SimulatedPersistentDictionaryFactory(PersistentDictionaryType dictionaryType)
        {
            this.DictionaryType = dictionaryType;
        }

        /// <summary>Gets the type of dictionaries created</summary>
        public PersistentDictionaryType DictionaryType { get; private set; }

        /// <summary>
        /// Initialize PersistentDictionaryFactory with SimulatedPersistentDictionaryFactory
        /// instances initialized with the specified types.
        /// </summary>
        /// <remarks>
        /// Also clears SimulatedPersistentStorage and sets PersistentDictionary.DefaultType
        /// to the first type specified.
        /// </remarks>
        public static void Initialize()
        {
            var types = Enum.GetValues(typeof(PersistentDictionaryType)) as PersistentDictionaryType[];
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = types.First().ToString();
            SimulatedPersistentStorage.Clear();
            PersistentDictionaryFactory.Initialize(
                types.Select(
                type => new SimulatedPersistentDictionaryFactory(type)));
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        public IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, bool raw)
        {
            return new MemoryPersistentDictionary<TValue>(storeName, raw);
        }

        /// <summary>Gets an index of all the stores</summary>
        /// <returns>List of store names</returns>
        public string[] GetStoreIndex()
        {
            return SimulatedPersistentStorage.GetStoreNames();
        }
    }
}
