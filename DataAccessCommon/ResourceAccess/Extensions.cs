// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Diagnostics;

namespace ResourceAccess
{
    /// <summary>Extension methods for IResourceAccessHandler</summary>
    public static class Extensions
    {
        /// <summary>Check access for a given resource and user id.</summary>
        /// <param name="accessHandler">The resource access handler instance.</param>
        /// <param name="repository">The entity repository instance.</param>
        /// <param name="resource">The canonical resource.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>True if access is granted.</returns>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if user not found.</exception>
        public static bool CheckAccessByUserId(this IResourceAccessHandler accessHandler, IEntityRepository repository, CanonicalResource resource, string userId)
        {
            try
            {
                var user = repository.GetUser(new RequestContext(), userId);
                return accessHandler.CheckAccess(resource, user.ExternalEntityId);
            }
            catch (ArgumentException ex)
            {
                var msg = "User Id not found: {0}".FormatInvariant(userId);
                LogManager.Log(LogLevels.Warning, msg);
                throw new DataAccessEntityNotFoundException(msg, ex);
            }
        }
    }
}
