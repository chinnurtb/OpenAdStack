//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryFactory.cs" company="Rare Crowds Inc">
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
using System.Linq;
using ConfigManager;

namespace Utilities.Storage
{
    /// <summary>
    /// Factory for creating persistent dictionaries
    /// </summary>
    public static class PersistentDictionaryFactory
    {
        /// <summary>
        /// Singleton instance of IPersistentDictionaryFactory
        /// </summary>
        private static IDictionary<PersistentDictionaryType, IPersistentDictionaryFactory> factories;

        /// <summary>
        /// Gets the default persistent dictionary type
        /// </summary>
        private static PersistentDictionaryType DefaultDictionaryType
        {
            get { return Config.GetEnumValue<PersistentDictionaryType>("PersistentDictionary.DefaultType"); }
        }

        /// <summary>Initializes the factory</summary>
        /// <param name="factories">The factories to use</param>
        public static void Initialize(IEnumerable<IPersistentDictionaryFactory> factories)
        {
            PersistentDictionaryFactory.factories = factories
                .ToDictionary(f => f.DictionaryType, f => f);
        }

        /// <summary>Gets an index of all the stores for the default dictionary type</summary>
        /// <returns>List of store names</returns>
        public static string[] GetStoreIndex()
        {
            return GetStoreIndex(DefaultDictionaryType);
        }

        /// <summary>Gets an index of all the stores for the specified dictionary type</summary>
        /// <param name="dictionaryType">Type of dictionary to index the stores of</param>
        /// <returns>List of store names</returns>
        public static string[] GetStoreIndex(PersistentDictionaryType dictionaryType)
        {
            if (factories == null)
            {
                throw new InvalidOperationException("PersistentDictionaryFactory must be initialized before dictionaries can be created");
            }

            if (!factories.ContainsKey(dictionaryType))
            {
                throw new ArgumentException("No factory for dictionary type {0} has been initialized".FormatInvariant(dictionaryType), "dictionaryType");
            }

            return factories[dictionaryType].GetStoreIndex();
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <returns>The persistent dictionary</returns>
        public static IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName)
        {
            return CreateDictionary<TValue>(storeName, DefaultDictionaryType);
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        public static IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, bool raw)
        {
            return CreateDictionary<TValue>(storeName, DefaultDictionaryType, raw);
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="dictionaryType">Type of the dictionary to create</param>
        /// <returns>The persistent dictionary</returns>
        public static IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, PersistentDictionaryType dictionaryType)
        {
            return CreateDictionary<TValue>(storeName, dictionaryType, false);
        }

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="dictionaryType">Type of the dictionary to create</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        public static IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, PersistentDictionaryType dictionaryType, bool raw)
        {
            if (factories == null)
            {
                throw new InvalidOperationException("PersistentDictionaryFactory must be initialized before dictionaries can be created");
            }

            if (!factories.ContainsKey(dictionaryType))
            {
                throw new ArgumentException("No factory for dictionary type {0} has been initialized".FormatInvariant(dictionaryType), "dictionaryType");
            }

#pragma warning disable 618
            // IPersistentDictionaryFactory.CreateDictionary is marked obsolete to ensure it is not used directly
            return factories[dictionaryType].CreateDictionary<TValue>(storeName, raw);
#pragma warning restore 618
        }
    }
}
