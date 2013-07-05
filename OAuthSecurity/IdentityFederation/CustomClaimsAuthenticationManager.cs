// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomClaimsAuthenticationManager.cs" company="Rare Crowds Inc.">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using Microsoft.IdentityModel.Claims;
using Microsoft.Practices.Unity;
using RuntimeIoc.WebRole;

namespace IdentityFederation
{
    /// <summary>
    /// Class for extending the ClaimsAuthenticationManager. This class adds required clais to the IClaimsPrincipal
    /// </summary>
    public class CustomClaimsAuthenticationManager : ClaimsAuthenticationManager
    {
        /// <summary>User claim type.</summary>
        internal static readonly string RcUserClaimType = "http://schemas.rarecrowds.com/claims/user";

        /// <summary>Internal claims issuer.</summary>
        internal static readonly string InternalIssuer = "RareCrowds Issuer";

        /// <summary>ACS Issuer value</summary>
        internal readonly string AcsIssuer = Config.GetValue("ACS.Issuer");
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomClaimsAuthenticationManager"/> class. 
        /// Because this is invoked by WIF we need explicity default construction rather than
        /// using the injection constructor.
        /// </summary>
        public CustomClaimsAuthenticationManager() : this(RuntimeIocContainer.Instance.Resolve<IEntityRepository>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomClaimsAuthenticationManager"/> class. 
        /// Injection constructor
        /// </summary>
        /// <param name="entityRepository">The entity Repository.</param>
        public CustomClaimsAuthenticationManager(IEntityRepository entityRepository)
        {
            this.EntityRepository = entityRepository;
        }

        /// <summary>
        /// Gets or sets the repository for user access
        /// </summary>
        internal IEntityRepository EntityRepository { get; set; }

        /// <summary>
        /// Method to add claims to the incoming IClaimsPrincipal
        /// </summary>
        /// <param name="resourceName">The endpoint URI the Client is trying to access.</param>
        /// <param name="incomingPrincipal">The Principal generated from validating the incoming token.</param>
        /// <returns>Returns a principal with augmented claims.</returns>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        public override IClaimsPrincipal Authenticate(string resourceName, IClaimsPrincipal incomingPrincipal)
        {
            if (incomingPrincipal == null)
            {
                var msg = "IClaimsPrincipal is null.";
                LogManager.Log(LogLevels.Trace, msg);
                throw new ArgumentNullException("incomingPrincipal", msg);
            }

            // If the identity not authenticated yet, keep the current principal and redirect the incomingPrincipal to ACS
            if (!incomingPrincipal.Identity.IsAuthenticated)
            {
                return incomingPrincipal;
            }

            // Get claims that have the correct issuer and a NameIdentifier
            IEnumerable<IClaimsIdentity> validIdentities = this.GetValidIdentities(incomingPrincipal.Identities).ToList();

            if (!validIdentities.Any())
            {
                var msg = "No recognized identities or claims were found to authenticate.";
                LogManager.Log(LogLevels.Information, msg);
                throw new SecurityException(msg);
            }

            // Update identities with claims needed for our application.
            var usersFound = this.UpdateIdentitiesWithClaims(ref validIdentities);
            if (!usersFound)
            {
                // Log the event but let the AuthZ handler deal with the violation.
                var msg = "No valid user was found in system for any identities on the supplied principal.";
                LogManager.Log(LogLevels.Information, msg);
            }

            // Return the principal with the new claims (if any).
            return incomingPrincipal;
        }
        
        /// <summary>Check the list of identities for claims we can authenticate.</summary>
        /// <param name="identities">The identities to check.</param>
        /// <returns>The collection of valid identities.</returns>
        internal IEnumerable<IClaimsIdentity> GetValidIdentities(IEnumerable<IClaimsIdentity> identities)
        {
            // TODO: Validate the identity provider.

            // TODO: Deal with multiple issuers if it becomes relevant
            // We currently check NameIdentifier claims from the ACS issuer
            return identities.Where(i =>
                    i.Claims.Count > 0
                    && i.Claims.Any(c =>
                        c.ClaimType == ClaimTypes.NameIdentifier
                        && StringComparer.OrdinalIgnoreCase.Equals(c.Issuer, this.AcsIssuer)));
        }

        /// <summary>Update identities that have a cooresponding user with the required claims.</summary>
        /// <param name="validIdentities">Identities that have valid claim information.</param>
        /// <returns>True of a user was found.</returns>
        internal bool UpdateIdentitiesWithClaims(ref IEnumerable<IClaimsIdentity> validIdentities)
        {
            foreach (IClaimsIdentity identity in validIdentities)
            {
                // WLID will have provider and nameidentifier
                string nameIdentifier = identity.Claims.First(c => c.ClaimType == ClaimTypes.NameIdentifier).Value;

                // Verify that nameIdentifier exists in the system
                var userEntityId = this.CheckIfUserExists(nameIdentifier);
                if (userEntityId == null)
                {
                    var msg = string.Format(CultureInfo.InvariantCulture, "User does not exist in system: {0}", nameIdentifier);
                    LogManager.Log(LogLevels.Trace, msg);
                    continue;
                }

                // Add organization claim
                var organizationClaim = new Claim(
                    RcUserClaimType, userEntityId, ClaimValueTypes.String, InternalIssuer);

                // Add the claim to the principal
                identity.Claims.Add(organizationClaim);

                // Once we find a indentity that exists in our system we stop. We do not allow
                // more than one authentication context to be active at once. First one wins.
                return true;
            }

            return false;
        }

        /// <summary>Check if a user exists in the system cooresponding to a name identifier.</summary>
        /// <param name="nameIdentifier">The name identifier.</param>
        /// <returns>True if the user was found.</returns>
        internal string CheckIfUserExists(string nameIdentifier)
        {
            try
            {
                var userEntity = this.EntityRepository.GetUser(new RequestContext(), nameIdentifier);
                return userEntity.ExternalEntityId.ToString();
            }
            catch (ArgumentException)
            {
            }

            return null;
        }
    }
}
