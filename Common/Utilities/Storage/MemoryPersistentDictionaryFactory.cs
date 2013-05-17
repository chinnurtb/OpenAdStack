//-----------------------------------------------------------------------
// <copyright file="MemoryPersistentDictionaryFactory.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Storage
{
    /// <summary>Factory for MemoryPersistentDictionary implementations of IPersistentDictionary</summary>
    public class MemoryPersistentDictionaryFactory : IPersistentDictionaryFactory
    {
        /// <summary>Gets the type of dictionaries created</summary>
        public PersistentDictionaryType DictionaryType
        {
            get { return PersistentDictionaryType.Memory; }
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
