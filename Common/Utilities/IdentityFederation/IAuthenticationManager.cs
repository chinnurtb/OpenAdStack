//-----------------------------------------------------------------------
// <copyright file="IAuthenticationManager.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.IdentityFederation
{
    /// <summary>Interface for authentication managers</summary>
    public interface IAuthenticationManager
    {
        /// <summary>Checks if the authentication context is a known user.</summary>
        /// <returns>True if user exists; otherwise, false.</returns>
        bool CheckValidUser();
    }
}
