//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryFactory.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
