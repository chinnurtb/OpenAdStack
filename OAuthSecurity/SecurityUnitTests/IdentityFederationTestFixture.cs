//-----------------------------------------------------------------------
// <copyright file="IdentityFederationTestFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens;
using System.Security;
using System.ServiceModel.Security;
using ConfigManager;
using IdentityFederation;
using Microsoft.IdentityModel.Claims;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAuthSecurity;

namespace SecurityUnitTests
{
    /// <summary>
    /// Class for testing IdentityFederation project
    /// </summary>
    [TestClass]
    public class IdentityFederationTestFixture
    {
        /// <summary>
        /// IssuerNameRegistry used in IdentityFederation
        /// </summary>
        private readonly CustomIssuerNameRegistry issuerNameRegistry = new CustomIssuerNameRegistry();

        /// <summary>
        /// claims to be used in SimpleWebToken
        /// </summary>
        private Collection<Claim> claims = null;

        /// <summary>
        /// SimpleWebToken used in tests
        /// </summary>
        private SimpleWebToken simpleWebToken = null;

        /// <summary>
        /// Initialize the claims
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.claims = new Collection<Claim>();
            var organizationClaim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/organization", "Rare Crowds Inc.", ClaimValueTypes.String, Config.GetValue("ACS.Issuer"));
            this.claims.Add(organizationClaim);
        }

        /// <summary>
        /// Positive test for CustomIssuerNameRegistry GetIssuerName()
        /// </summary>
        [TestMethod]
        public void CustomIssuerNameRegistryGetIssuerName()
        {
            this.simpleWebToken = new SimpleWebToken(new Uri("https://localhost"), Config.GetValue("ACS.Issuer"), DateTime.UtcNow.AddMinutes(10.0), this.claims, string.Empty, string.Empty, string.Empty);
            var issuer = this.issuerNameRegistry.GetIssuerName(this.simpleWebToken);
            Assert.IsNotNull(issuer);
            Assert.AreEqual(Config.GetValue("ACS.Issuer"), issuer);
        }

        /// <summary>
        ///  Negative test for CustomIssuerNameRegistry GetIssuerName()
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(SecurityException))]
        public void CustomIssuerNameRegistryGetIssuerNameFail()
        {
            this.issuerNameRegistry.GetIssuerName(this.simpleWebToken);
        }

        /// <summary>
        /// Positive test for CustomIssuerTokenResolver TryResolveSecurityKeyCore()
        /// </summary>
        [TestMethod]
        public void CustomIssuerTokenResolverTryResolveSecurityKeyCore()
        {
            string relyingPartyRealm = Config.GetValue("ACS.RelyingPartyRealm");
            SecurityKeyIdentifierClause keyIdentifierClause = new KeyNameIdentifierClause(relyingPartyRealm);
            SecurityKey key;
            var customIssuertokenResolver = new CustomIssuerTokenResolverTest();
            bool result = customIssuertokenResolver.TryResolveSecurityKeyCoreTest(keyIdentifierClause, out key);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Test for CustomIssuerTokenResolver TryResolveSecurityKeyCore() null key
        /// </summary>
        [TestMethod]
        public void CustomIssuerTokenResolverTryResolveSecurityKeyCoreNullKey()
        {
            SecurityKey key;
            var customIssuertokenResolver = new CustomIssuerTokenResolverTest();
            bool result = customIssuertokenResolver.TryResolveSecurityKeyCoreTest(null, out key);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test for CustomIssuerTokenResolver TryResolveSecurityKeyCore() bad key
        /// </summary>
        [TestMethod]
        public void CustomIssuerTokenResolverTryResolveSecurityKeyCoreBadKey()
        {
            SecurityKeyIdentifierClause keyIdentifierClause = new KeyNameIdentifierClause("FakeRelyingPartyRealm");
            SecurityKey key;
            var customIssuertokenResolver = new CustomIssuerTokenResolverTest();
            var result = customIssuertokenResolver.TryResolveSecurityKeyCoreTest(keyIdentifierClause, out key);
            Assert.IsFalse(result);
        }   
    }
}
