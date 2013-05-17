//-----------------------------------------------------------------------
// <copyright file="CustomIssuerNameRegistry.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security;
using ConfigManager;
using IdentityFederation.Properties;
using Microsoft.IdentityModel.Tokens;
using OAuthSecurity;

namespace IdentityFederation
{
    /// <summary>
    /// Class to extend IssuerNameRegistry
    /// </summary>
    public class CustomIssuerNameRegistry : IssuerNameRegistry
    {
        /// <summary>
        /// Method to et the issuer name
        /// </summary>
        /// <param name="securityToken">security token associated with the issuer</param>
        /// <returns>issuer name</returns>
        public override string GetIssuerName(SecurityToken securityToken)
        {
            var swt = securityToken as SimpleWebToken;
            if (swt == null)
            {
                throw new SecurityException(Resources.SwtExpected);
            }

            // Check for known issuer.
            if (!StringComparer.Ordinal.Equals(swt.Issuer, Config.GetValue("ACS.Issuer")))
            {
                throw new SecurityException(string.Format(CultureInfo.CurrentCulture, Resources.UnknownIssuer, swt.Issuer));
            }

            return swt.Issuer;
        }
    }
}