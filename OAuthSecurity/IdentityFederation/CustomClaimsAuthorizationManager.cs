//-----------------------------------------------------------------------
// <copyright file="CustomClaimsAuthorizationManager.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security;
using DataAccessLayer;
using Diagnostics;
using Microsoft.IdentityModel.Claims;
using Microsoft.Practices.Unity;

using ResourceAccess;

using RuntimeIoc.WebRole;

namespace IdentityFederation
{
    /// <summary>
    /// Class to manage authorization
    /// </summary>
    public class CustomClaimsAuthorizationManager : ClaimsAuthorizationManager
    {
        /// <summary>Initializes a new instance of the <see cref="CustomClaimsAuthorizationManager"/> class.</summary>
        public CustomClaimsAuthorizationManager()
            : this(
                new ResourceAccessHandler(
                    RuntimeIocContainer.Instance.Resolve<IUserAccessRepository>(),
                    RuntimeIocContainer.Instance.Resolve<IEntityRepository>()),
                RuntimeIocContainer.Instance.Resolve<IEntityRepository>())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CustomClaimsAuthorizationManager"/> class.</summary>
        /// <param name="userResourceAccessHandler">The user-based resource access handler.</param>
        /// <param name="entityRepository">The entity repository.</param>
        public CustomClaimsAuthorizationManager(IResourceAccessHandler userResourceAccessHandler, IEntityRepository entityRepository)
        {
            this.ResourceAccessHandler = userResourceAccessHandler;
            this.EntityRepository = entityRepository;
        }

        /// <summary>Gets user-based resource access handler.</summary>
        internal IResourceAccessHandler ResourceAccessHandler { get; private set; }

        /// <summary>Gets the entity repository.</summary>
        internal IEntityRepository EntityRepository { get; private set; }

        /// <summary>
        /// Checks if the principal specified in the authorization context is authorized
        /// to perform action specified in the authorization context on the specified resoure
        /// </summary>
        /// <param name="context">Authorization context</param>
        /// <returns>true if authorized, false otherwise</returns>
        public override bool CheckAccess(AuthorizationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var resource = context.Resource[0].Value;
            var action = context.Action[0].Value;

            // Verify that we can build canonical resources
            var canonicalResource = CanonicalResource.BuildCanonicalResource(resource, action);
            if (canonicalResource == null)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, "Canonical resource could not be constructed from Uri {0} and Action {1} is not valid.", resource, action);
                LogManager.Log(LogLevels.Error, msg);
                return false;
            }

            // Check if this is a global resource with global permissions
            if (this.ResourceAccessHandler.CheckGlobalAccess(canonicalResource))
            {
                return true;
            }

            // Determine if we can resolve a matching user in the system from the identity collection or the canonicalResource.
            var userEntityId = this.GetUserEntityId(context.Principal.Identities, canonicalResource);

            if (userEntityId == null)
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

            var isAllowed = this.ResourceAccessHandler.CheckAccess(canonicalResource, userEntityId);
            return isAllowed;
        }

        /// <summary>Get a user EntityId from a collection of Identities</summary>
        /// <param name="identities">The identities.</param>
        /// <param name="canonicalResource">Canonical resource for the user being requested.</param>
        /// <returns>The user entity id.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "Want it to be easy to extend with non-static behavior.")]
        internal EntityId GetUserEntityId(ClaimsIdentityCollection identities, CanonicalResource canonicalResource)
        {
            // Get the first one if there are more than one (not a scenario supported)
            var userClaim = identities.SelectMany(i => i.Claims)
                .FirstOrDefault(c => c.ClaimType == CustomClaimsAuthenticationManager.RcUserClaimType);

            if (userClaim != null)
            {
                return new EntityId(userClaim.Value);
            }

            // Try to get the entity id from the resource if it has a user namespace.
            var userEntityId = canonicalResource.ExtractEntityId("USER");
            if (userEntityId == null)
            {
                return null;
            }

            // User verification needs to be special-cased. Handle it as user access rather than global-access
            // so we can make the temporary access descriptor more robust.
            // TODO: Make the temporary access descriptor more robust with an additional secret
            // TODO: Would be nice to have a dictionary of URI names (like Verify)
            if (string.Compare(canonicalResource.Message, "VERIFY", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return userEntityId;
            }

            return null;
        }
    }
}
