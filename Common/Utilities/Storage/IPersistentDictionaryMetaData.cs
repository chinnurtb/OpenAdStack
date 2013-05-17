//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryMetaData.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Utilities.Storage
{
    /// <summary>
    /// Interface for accessing the metadata of the dictionary entries
    /// </summary>
    public interface IPersistentDictionaryMetadata
    {
        /// <summary>Gets or sets the metadata collection for the dictionary entry with the given key</summary>
        /// <param name="key">Key for the dictionary entry</param>
        /// <param name="name">Name of the metadata entry</param>
        /// <returns>The metadata collection</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// The property is retrieved and an entry with the specified key is not found.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// The property is set and the value is null.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1023", Justification = "Legitimate use of a multi-dimensional indexer.")]
        string this[string key, string name] { get; set; }
    }
}