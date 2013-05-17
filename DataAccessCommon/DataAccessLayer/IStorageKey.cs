// -----------------------------------------------------------------------
// <copyright file="IStorageKey.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
