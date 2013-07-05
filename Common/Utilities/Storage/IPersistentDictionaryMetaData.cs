//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionaryMetaData.cs" company="Rare Crowds Inc">
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