//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryEntry.cs" company="Rare Crowds Inc">
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
