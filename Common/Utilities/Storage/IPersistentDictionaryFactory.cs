//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryFactory.cs" company="Rare Crowds Inc">
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

namespace Utilities.Storage
{
    /// <summary>
    /// Describes the interface of the factories for IPersistentDictionary
    /// </summary>
    public interface IPersistentDictionaryFactory
    {
        /// <summary>Gets the type of dictionaries created</summary>
        PersistentDictionaryType DictionaryType { get; }

        /// <summary>Gets an index of all the stores</summary>
        /// <returns>List of store names</returns>
        string[] GetStoreIndex();

        /// <summary>Creates an instance of IPersistentDictionary</summary>
        /// <typeparam name="TValue">Type of values that will be stored</typeparam>
        /// <param name="storeName">Name of the store the dictionary will persist to</param>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The persistent dictionary</returns>
        [System.Obsolete("Use PersistentDictionaryFactory.CreateDictionary instead")]
        IPersistentDictionary<TValue> CreateDictionary<TValue>(string storeName, bool raw);
    }
}
