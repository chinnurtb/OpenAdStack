//-----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Rare Crowds Inc">
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

namespace SqlUtilities.Storage
{
    /// <summary>
    /// Container of constants used in SqlUtilities
    /// </summary>
    internal static class Constants
    {
        /// <summary>Stored procedure names</summary>
        internal static class StoredProcedures
        {
            /// <summary>
            /// Gets the names of the stores
            /// </summary>
            public const string GetStoreNames = "GetStoreNames";

            /// <summary>
            /// Gets the names of the entries for a given store name
            /// </summary>
            public const string GetEntryNames = "GetEntryNames";

            /// <summary>
            /// Deletes the entries for a given store name
            /// </summary>
            public const string DeleteEntries = "DeleteEntries";

            /// <summary>
            /// Deletes the entry for a given store and entry name
            /// </summary>
            public const string DeleteEntry = "DeleteEntry";

            /// <summary>
            /// Gets the content, etag, etc for a given store and entry name
            /// </summary>
            public const string GetEntry = "GetEntry";

            /// <summary>
            /// Sets the content for a given store and entry name if the given etag is current
            /// </summary>
            public const string SetEntry = "SetEntry";
        }

        /// <summary>Stored procedure parameter names</summary>
        internal static class SqlParameterNames
        {
            /// <summary>Name of the store</summary>
            public const string StoreName = "@StoreName";

            /// <summary>Name of the entry</summary>
            public const string EntryName = "@EntryName";

            /// <summary>The entry's content</summary>
            public const string Content = "@Content";

            /// <summary>The entry's eTag</summary>
            public const string ETag = "@ETag";

            /// <summary>Whether the entry is compressed</summary>
            public const string Compressed = "@Compressed";
        }

        /// <summary>Stored procedure result value names</summary>
        internal static class SqlResultValues
        {
            /// <summary>The store name</summary>
            public const string StoreName = "StoreName";
            
            /// <summary>The entry name</summary>
            public const string EntryName = "EntryName";

            /// <summary>The entry's content</summary>
            public const string Content = "Content";

            /// <summary>The entry's eTag value</summary>
            public const string ETag = "ETag";

            /// <summary>Whether the entry is compressed</summary>
            public const string Compressed = "Compressed";

            /// <summary>Count of rows deleted</summary>
            public const string Count = "COUNT";
        }
    }
}
