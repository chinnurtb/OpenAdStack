//-----------------------------------------------------------------------
// <copyright file="IUserAccessStore.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>Interface for User Access data store</summary>
    public interface IUserAccessStore
    {
        /// <summary>Get a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>A collection of access descriptors.</returns>
        IEnumerable<string> GetUserAccessList(EntityId userEntityId);

        /// <summary>Add a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        bool AddUserAccessList(EntityId userEntityId, IEnumerable<string> accessList);

        /// <summary>Remove a collection of access descriptors for a given user.</summary>
        /// <param name="userEntityId">The user entity id.</param>
        /// <param name="accessList">A collection of access descriptors.</param>
        /// <returns>True on success.</returns>
        bool RemoveUserAccessList(EntityId userEntityId, IEnumerable<string> accessList);
    }
}