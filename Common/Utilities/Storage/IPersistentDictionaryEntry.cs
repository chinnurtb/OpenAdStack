//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryEntry.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Storage
{
    /// <summary>
    /// Interface for persistent dictionaries' internal entry elements
    /// </summary>
    public interface IPersistentDictionaryEntry
    {
        /// <summary>Gets the entry's ETag</summary>
        string ETag { get; }

        /// <summary>Reads the entry's content bytes</summary>
        /// <returns>The bytes</returns>
        byte[] ReadAllBytes();

        /// <summary>Writes the entry's content bytes</summary>
        /// <param name="content">The bytes</param>
        /// <param name="compress">Whether to compress the content</param>
        /// <exception cref="System.InvalidOperationException">
        /// The ETag has changed since the entry was initialized
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// An unknown error occured writing the content to the underlying store
        /// </exception>
        void WriteAllBytes(byte[] content, bool compress);
    }
}
