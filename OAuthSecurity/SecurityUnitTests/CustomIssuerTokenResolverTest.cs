//-----------------------------------------------------------------------
// <copyright file="CustomIssuerTokenResolverTest.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System.IdentityModel.Tokens;
using IdentityFederation;

namespace SecurityUnitTests
{
    /// <summary>
    /// Class to test protected methods of CustomIssuerTokenResolver
    /// </summary>
    public class CustomIssuerTokenResolverTest : CustomIssuerTokenResolver
    {
        /// <summary>
        /// Method to access TryResolveSecurityKeyCore
        /// </summary>
        /// <param name="keyIdentifierClause">the key associated with the SecurityKey</param>
        /// <param name="key">the SecurityKey associated with the first parameter</param>
        /// <returns>whether method succeeded</returns>
        public bool TryResolveSecurityKeyCoreTest(SecurityKeyIdentifierClause keyIdentifierClause, out SecurityKey key)
        {
            return TryResolveSecurityKeyCore(keyIdentifierClause, out key);
        }
    }
}
