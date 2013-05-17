// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OAuthError.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OAuthSecurity
{
    /// <summary>
    /// Class to manage OAuth errors
    /// </summary>
    public class OAuthError
    {
        /// <summary>
        /// Gets or sets the error
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the error description
        /// </summary>
        public string ErrorDescription { get; set; }
    }
}
