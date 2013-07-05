//-----------------------------------------------------------------------
// <copyright file="CustomIssuerNameRegistry.cs" company="Rare Crowds Inc">
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