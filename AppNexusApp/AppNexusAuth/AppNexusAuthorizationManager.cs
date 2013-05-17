//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthorizationManager.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Security;
using DataAccessLayer;
using Diagnostics;
using ResourceAccess;
using Utilities.IdentityFederation;

namespace AppNexusApp.AppNexusAuth
{
    /// <summary>Manages authorization for AppNexus App Single-Sign-On</summary>
    public class AppNexusAuthorizationManager : IAuthorizationManager
    {
        /// <summary>Initializes a new instance of the <see cref="AppNexusAuthorizationManager"/> class.</summary>
        /// <param name="userAccessRepository">The user access repository.</param>
        /// <param name="entityRepository">The entity repository.</param>
        /// <param name="claimRetriever">The claim retriever.</param>
        public AppNexusAuthorizationManager(
            IUserAccessRepository userAccessRepository,
            IEntityRepository entityRepository,
            IClaimRetriever claimRetriever)
        {
            this.ResourceAccessHandler = new ResourceAccessHandler(userAccessRepository, entityRepository);
            this.EntityRepository = entityRepository;
            this.ClaimRetriever = claimRetriever;
        }

        /// <summary>Gets user-based resource access handler.</summary>
        internal IResourceAccessHandler ResourceAccessHandler { get; private set; }

        /// <summary>Gets the entity repository.</summary>
        internal IEntityRepository EntityRepository { get; private set; }

        /// <summary>Gets the claim retriever</summary>
        internal IClaimRetriever ClaimRetriever { get; private set; }

        /// <summary>
        /// Checks if the authorization context is authorized to perform action
        /// specified in the authorization context on the specified resoure.
        /// </summary>
        /// <param name="action">Action to authorize</param>
        /// <param name="resource">Resource to authorize</param>
        /// <returns>True if authorized; otherwise, false.</returns>
        public bool CheckAccess(string action, string resource)
        {
            // Verify that we can build canonical resources
            var canonicalResource = CanonicalResource.BuildCanonicalResource(resource, action);
            if (canonicalResource == null)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Canonical resource could not be constructed from Uri {0} and Action {1} is not valid.",
                    resource,
                    action);
                return false;
            }

            // Check if this is a global resource with global permissions
            if (this.ResourceAccessHandler.CheckGlobalAccess(canonicalResource))
            {
                return true;
            }

            // Determine if we can resolve a matching user in the system from the identity collection or the canonicalResource.
            var nameIdentifier = this.ClaimRetriever.GetClaimValue("NameIdentifier");
            var userEntity = this.GetUser(nameIdentifier);

            if (userEntity == null)
            {
                var msg = "No valid user was found in system for any identities on the supplied principal.";
                LogManager.Log(LogLevels.Information, msg);

                // For Api layer we just want to let the auth failure bubble up (return false)
                if (canonicalResource.IsApiResource)
                {
                    return false;
                }

                // For web layer we throw so the global error handler and redirect to an error page.
                throw new SecurityException(msg);
            }
            else
            {
                // Todo: verify this is ok with all.
                // For Api layer we will allow any valid user to make calls to the api. Resource access is limited to the users permissions.
                if (canonicalResource.IsApiResource)
                {
                    return true;
                }
            }

            return this.ResourceAccessHandler.CheckAccess(canonicalResource, userEntity.ExternalEntityId);
        }

        /// <summary>Check if a user exists in the system corresponding to a name identifier.</summary>
        /// <param name="nameIdentifier">The name identifier.</param>
        /// <returns>If the user was found, the user entityId; otherwise, null.</returns>
        private UserEntity GetUser(string nameIdentifier)
        {
            try
            {
                return this.EntityRepository.GetUser(new RequestContext(), nameIdentifier);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
