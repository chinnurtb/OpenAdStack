//-----------------------------------------------------------------------
// <copyright file="IAuthorizationManager.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.IdentityFederation
{
    /// <summary>Interface for authorization managers</summary>
    public interface IAuthorizationManager
    {
        /// <summary>
        /// Checks if the authorization context is authorized to perform action
        /// specified in the authorization context on the specified resoure.
        /// </summary>
        /// <param name="action">Action to authorize</param>
        /// <param name="resource">Resource to authorize</param>
        /// <returns>True if authorized; otherwise, false.</returns>
        bool CheckAccess(string action, string resource);
    }
}
