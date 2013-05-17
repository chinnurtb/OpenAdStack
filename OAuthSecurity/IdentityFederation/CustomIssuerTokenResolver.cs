//-----------------------------------------------------------------------
// <copyright file="CustomIssuerTokenResolver.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security;
using ConfigManager;
using Microsoft.IdentityModel.Tokens;

namespace IdentityFederation
{
    /// <summary>
    /// Class to extend issuer token resover
    /// </summary>
    public class CustomIssuerTokenResolver : IssuerTokenResolver
    {
        /// <summary>
        /// private dicationary of issuers
        /// </summary>
        private IDictionary<string, SecurityKey> keyMap;

        /// <summary>
        /// Initializes a new instance of the CustomIssuerTokenResolver class
        /// </summary>
        public CustomIssuerTokenResolver()
        {
            this.keyMap = new Dictionary<string, SecurityKey> 
            { 
                { Config.GetValue("ACS.RelyingPartyRealm"), new InMemorySymmetricSecurityKey(Convert.FromBase64String(Config.GetValue("ACS.RelyingPartySecurityKey"))) }
            };
        }

        /// <summary>
        /// Method to resolve security key
        /// </summary>
        /// <param name="keyIdentifierClause">key identifier</param>
        /// <param name="key">security key</param>
        /// <returns>whether security key is resolved</returns>
        protected override bool TryResolveSecurityKeyCore(SecurityKeyIdentifierClause keyIdentifierClause, out SecurityKey key)
        {
            key = null;
            var nameClause = keyIdentifierClause as KeyNameIdentifierClause;

            if (nameClause != null)
            {
                return this.keyMap.TryGetValue(nameClause.KeyName, out key);
            }

            return false;
        }
    }
}