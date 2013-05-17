//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthenticationManager.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using DataAccessLayer;
using Utilities.IdentityFederation;

namespace AppNexusApp.AppNexusAuth
{
    /// <summary>Manages authentication for AppNexus App Single-Sign-On</summary>
    public class AppNexusAuthenticationManager : IAuthenticationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppNexusAuthenticationManager"/> class. 
        /// Injection constructor
        /// </summary>
        /// <param name="entityRepository">Entity repository for getting users</param>
        /// <param name="claimRetriever">Used to get claims for the current context</param>
        public AppNexusAuthenticationManager(
            IEntityRepository entityRepository,
            IClaimRetriever claimRetriever)
        {
            this.EntityRepository = entityRepository;
            this.ClaimRetriever = claimRetriever;
        }

        /// <summary>Gets or sets the repository for user access</summary>
        internal IEntityRepository EntityRepository { get; set; }

        /// <summary>Gets or sets the claim retriever</summary>
        internal IClaimRetriever ClaimRetriever { get; set; }

        /// <summary>Checks if the authentication context is a known user.</summary>
        /// <returns>True if user exists; otherwise, false.</returns>
        public bool CheckValidUser()
        {
            return this.CheckIfUserExists(this.ClaimRetriever.GetClaimValue(null)) != null;
        }

        /// <summary>Check if a user exists in the system corresponding to a name identifier.</summary>
        /// <param name="nameIdentifier">The name identifier.</param>
        /// <returns>If the user was found, the user entityId; otherwise, null.</returns>
        private string CheckIfUserExists(string nameIdentifier)
        {
            try
            {
                var userEntity = this.EntityRepository.GetUser(new RequestContext(), nameIdentifier);
                return userEntity.ExternalEntityId.ToString();
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
