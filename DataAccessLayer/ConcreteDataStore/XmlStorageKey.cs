// -----------------------------------------------------------------------
// <copyright file="XmlStorageKey.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Implementation of a storage key for an Xml (DataSet) based store modeled after Azure Table.
    /// </summary>
    internal class XmlStorageKey : IStorageKey
    {
        /// <summary>Initializes a new instance of the <see cref="XmlStorageKey"/> class.</summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="rowId">The row id.</param>
        public XmlStorageKey(string accountId, string tableName, string partition, EntityId rowId)
        {
            this.StorageAccountName = accountId;
            this.TableName = tableName;
            this.Partition = partition;
            this.RowId = rowId;
        }
        
        /// <summary>Gets RowId.</summary>
        public EntityId RowId { get; private set; }

        /// <summary>Gets Partition.</summary>
        public string Partition { get; private set; }

        /// <summary>Gets TableName.</summary>
        public string TableName { get; private set; }

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
