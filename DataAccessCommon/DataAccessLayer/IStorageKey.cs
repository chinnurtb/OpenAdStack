// -----------------------------------------------------------------------
// <copyright file="IStorageKey.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Interface definition for storage key independent of underlying data store provider.</summary>
    public interface IStorageKey
    {
        /// <summary>Gets StorageAccountName (e.g. - account).</summary>
        string StorageAccountName { get; }

        /// <summary>Gets Version Timestamp.</summary>
        DateTime? VersionTimestamp { get; }

        /// <summary>Gets or sets LocalVersion.</summary>
        int LocalVersion { get; set; }

        /// <summary>Gets a map of key field name/value pairs.</summary>
        IDictionary<string, string> KeyFields { get; }

        /// <summary>Interface method to determine equality of keys.</summary>
        /// <param name="otherKey">The key to compare with this key.</param>
        /// <returns>True if the keys refer to the same storage entity.</returns>
        bool IsEqual(IStorageKey otherKey);
    }
}
