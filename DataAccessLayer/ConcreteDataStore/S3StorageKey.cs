// --------------------------------------------------------------------------------------------------------------------
// <copyright file="S3StorageKey.cs" company="Rare Crowds Inc">
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
