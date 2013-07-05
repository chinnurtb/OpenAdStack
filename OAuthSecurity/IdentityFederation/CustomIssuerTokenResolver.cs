//-----------------------------------------------------------------------
// <copyright file="CustomIssuerTokenResolver.cs" company="Rare Crowds Inc">
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