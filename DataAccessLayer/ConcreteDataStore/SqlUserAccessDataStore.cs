//-----------------------------------------------------------------------
// <copyright file="SqlUserAccessDataStore.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Sql implementation of IUserAccessStore.</summary>
    internal class SqlUserAccessDataStore : IUserAccessStore
    {
        /// <summary>Initializes a new instance of the <see cref="SqlUserAccessDataStore"/> class.</summary>
        /// <param name="sqlStore">The sql store.</param>
        internal SqlUserAccessDataStore(ISqlStore sqlStore)
        {
            this.SqlStore = sqlStore;
        }

        /// <summary>Gets SqlStore.</summary>
        internal ISqlStore SqlStore { get; private set; }

        /// <summary>Get a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>A collection of access descriptors.</returns>
        public IEnumerable<string> GetUserAccessList(EntityId userEntityId)
        {
            var commandName = "UserAccess_GetUserAccess";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)userEntityId },
                };

            // If we have a failure at the transport level we return and empty list and AuthZ will fail.
            IEnumerable<string> result = new List<string>();

            var resultSets = this.SqlStore.TryExecuteStoredProcedure(commandName, parameters);
            if (resultSets != null && !resultSets.IsEmpty())
            {
                // Each row should be a dictionary with one key value pair - we want the values
                result = resultSets.GetRecords().Select(r => r.GetValue<string>("AccessDescriptor"));
            }

            return result;
        }

        /// <summary>Add a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        public bool AddUserAccessList(EntityId userEntityId, IEnumerable<string> accessList)
        {
            var commandName = "UserAccess_InsertUserAccess";

            foreach (var accessDescriptor in accessList)
            {
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)userEntityId },
                    new SqlParameter("@AccessDescriptor", SqlDbType.VarChar, 240) { Value = accessDescriptor }
                };

                var result = this.SqlStore.TryExecuteStoredProcedure(commandName, parameters);
                if (result == null)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>Remove a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        public bool RemoveUserAccessList(EntityId userEntityId, IEnumerable<string> accessList)
        {
            var commandName = "UserAccess_RemoveUserAccess";

            foreach (var accessDescriptor in accessList)
            {
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ExternalEntityId", SqlDbType.UniqueIdentifier) { Value = (Guid)userEntityId },
                    new SqlParameter("@AccessDescriptor", SqlDbType.VarChar, 240) { Value = accessDescriptor }
                };

                var result = this.SqlStore.TryExecuteStoredProcedure(commandName, parameters);
                if (result == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
