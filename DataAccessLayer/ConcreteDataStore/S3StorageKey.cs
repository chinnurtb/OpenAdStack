// --------------------------------------------------------------------------------------------------------------------
// <copyright file="S3StorageKey.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Implemenation of a storage key for an S3 Table based store.
    /// </summary>
    internal class S3StorageKey : IStorageKey
    {
        /// <summary>Initializes a new instance of the <see cref="S3StorageKey"/> class.</summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="tbdS3">'To Be Determined S3' key element.</param>
        public S3StorageKey(string accountId, string tbdS3)
        {
            this.StorageAccountName = accountId;
            this.TbdS3 = tbdS3;
        }

        /// <summary>Gets or sets 'To Be Determined S3' key element.</summary>
        public string TbdS3 { get; set; }

        ////
        // Begin IStorageKey members
        ////
        
        /// <summary>Gets or sets StorageAccountName (e.g. - account).</summary>
        public string StorageAccountName { get; set; }

        /// <summary>Gets or sets Version Timestamp.</summary>
        public DateTime? VersionTimestamp { get; set; }

        /// <summary>Gets or sets LocalVersion.</summary>
        public int LocalVersion { get; set; }

        /// <summary>
        /// Gets a map of key field name/value pairs.
        /// </summary>
        public IDictionary<string, string> KeyFields
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Interface method to determine equality of keys.</summary>
        /// <param name="otherKey">The key to compare with this key.</param>
        /// <returns>True if the keys refer to the same storage entity.</returns>
        public bool IsEqual(IStorageKey otherKey)
        {
            throw new NotImplementedException();
        }

        ////
        // End IStorageKey members
        ////
    }
}
