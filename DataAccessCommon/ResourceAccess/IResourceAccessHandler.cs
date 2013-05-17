//-----------------------------------------------------------------------
// <copyright file="IResourceAccessHandler.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using DataAccessLayer;

namespace ResourceAccess
{
    /// <summary>Handler interface for resource access checking.</summary>
    public interface IResourceAccessHandler
    {
        /// <summary>Check whether a user has access to a resource.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <param name="userEntityId">The user entity id.</param>
        /// <returns>True if access is granted.</returns>
        bool CheckAccess(CanonicalResource canonicalResource, EntityId userEntityId);

        /// <summary>Check if a resource has global access rights.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <returns>True if global access is granted.</returns>
        bool CheckGlobalAccess(CanonicalResource canonicalResource);

        /// <summary>Return an array of EnityId's based on the user's access list</summary>
        /// <param name="userEntityId">The user's entity id</param>
        /// <returns>Array of Company Ids.</returns>
        EntityId[] GetUserCompanyByAccessList(EntityId userEntityId);
    }
}