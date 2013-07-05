//-----------------------------------------------------------------------
// <copyright file="CustomClaimsAuthenticationManagerFixture.cs" company="Rare Crowds Inc.">
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using IdentityFederation;
using Microsoft.IdentityModel.Claims;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace SecurityUnitTests
{
    /// <summary>Test fixture for CustomClaimsAuthenticationManager</summary>
    [TestClass]
    public class CustomClaimsAuthenticationManagerFixture
    {
        /// <summary>Valid incoming claim for testing</summary>
        private Claim validClaim;

        /// <summary>Valid incoming claim with no user for testing</summary>
        private Claim validClaimNoUser;

        /// <summary>Claim with invalid issuer for testing</summary>
        private Claim invalidIssuerClaim;

        /// <summary>Claim with invalid type for testing</summary>
        private Claim invalidClaimTypeClaim;

        /// <summary>Mock entity repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>An identity with valid claims for testing.</summary>
        private IEnumerable<IClaimsIdentity> validIdentities;

        /// <summary>An identity with valid claims but no user for testing.</summary>
        private IEnumerable<IClaimsIdentity> validIdentitiesNoUser;

        /// <summary>Valid user for testing.</summary>
        private string validUser = "gooduser";

        /// <summary>Invalid user for testing.</summary>
        private string invalidUser = "baduser";

        /// <summary>CustomClaimsAuthenticationManager for testing.</summary>
        private CustomClaimsAuthenticationManager authNManager;

        /// <summary>
        /// Initialize the claims
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
            
            // Set up repository stub to succeed on this.validUser and fail on this.invalidUser
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Matches(a => a == this.validUser))).Return(EntityJsonSerializer.DeserializeUserEntity(new EntityId(), string.Empty)).Repeat.Once();
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Matches(a => a == this.invalidUser))).Throw(new ArgumentException("message"));
            this.authNManager = new CustomClaimsAuthenticationManager(this.repository);

            this.validClaim = new Claim(
                ClaimTypes.NameIdentifier, this.validUser, ClaimValueTypes.String, this.authNManager.AcsIssuer);
            this.validIdentities = new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { this.validClaim }) };

            this.validClaimNoUser = new Claim(
                ClaimTypes.NameIdentifier, this.invalidUser, ClaimValueTypes.String, this.authNManager.AcsIssuer);
            this.validIdentitiesNoUser = new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { this.validClaimNoUser }) };

            this.invalidIssuerClaim = new Claim(
                ClaimTypes.NameIdentifier, "Id", ClaimValueTypes.String, "fooIssuer");

            this.invalidClaimTypeClaim = new Claim(
                "fooType", "fooValue", ClaimValueTypes.String, this.authNManager.AcsIssuer);
        }

        /// <summary>
        /// Test for Authenticate with null IClaimsPrincipal parameter
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AuthenticateNullClaimsPrincipal()
        {
            var customClaimsAuthenticationManager = new CustomClaimsAuthenticationManager(this.repository);
            customClaimsAuthenticationManager.Authenticate(string.Empty, null);
        }

        /// <summary>
        /// Test for Authenticate IClaimsPrincipal unauthenticated
        /// </summary>
        [TestMethod]
        public void AuthenticateUnauthenticated()
        {
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(new List<ClaimsIdentity> { identity });
            var outgoingPrincipal = this.authNManager.Authenticate(string.Empty, principal);
            Assert.AreSame(principal, outgoingPrincipal);
        }
        
        /// <summary>
        /// Test for Authenticate with no valid claims
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(SecurityException))]
        public void AuthenticateNoValidClaims()
        {
            var claims = new Collection<Claim> { this.invalidClaimTypeClaim };
            var claimIdentity = new ClaimsIdentity(claims, "Federated");
            var claimsIdentities = new List<IClaimsIdentity> { claimIdentity };
            var incomingPrincipal = new ClaimsPrincipal(claimsIdentities);
            this.authNManager.Authenticate(string.Empty, incomingPrincipal);
        }

        /// <summary>
        /// Test for Authenticate with no user found
        /// </summary>
        [TestMethod]
        public void AuthenticateNoUser()
        {
            var incomingPrincipal = new ClaimsPrincipal(this.validIdentitiesNoUser);
            var orginalClaims = incomingPrincipal.Identities.SelectMany(i => i.Claims).Count();

            var outgoingPrincipal = this.authNManager.Authenticate(string.Empty, incomingPrincipal);
            Assert.IsNotNull(outgoingPrincipal);
            Assert.AreSame(incomingPrincipal, outgoingPrincipal);
            Assert.IsTrue(outgoingPrincipal.Identities.SelectMany(i => i.Claims).Count() == orginalClaims);
        }

        /// <summary>
        /// Test for Authenticate user found
        /// </summary>
        [TestMethod]
        public void AuthenticateSuccess()
        {
            var incomingPrincipal = new ClaimsPrincipal(this.validIdentities.ToList());
            var orginalClaims = incomingPrincipal.Identities.SelectMany(i => i.Claims).Count();

            var outgoingPrincipal = this.authNManager.Authenticate(string.Empty, incomingPrincipal);
            Assert.IsNotNull(outgoingPrincipal);
            Assert.AreSame(incomingPrincipal, outgoingPrincipal);
            Assert.IsTrue(outgoingPrincipal.Identities.SelectMany(i => i.Claims).Count() > orginalClaims);
        }

        /// <summary>
        /// No claims
        /// </summary>
        [TestMethod]
        public void GetValidIdentitiesEmptyClaims()
        {
            var identities = new List<IClaimsIdentity>
                {
                    new ClaimsIdentity(new List<Claim>())
                };
            var filteredIdentities = this.authNManager.GetValidIdentities(identities);
            Assert.IsFalse(filteredIdentities.Any());
        }

        /// <summary>
        /// No claims from a recognized issuer
        /// </summary>
        [TestMethod]
        public void GetValidIdentitiesNoClaimsFromRecognizedIssuer()
        {
            var identities = new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { this.invalidIssuerClaim }) };
            var filteredIdentities = this.authNManager.GetValidIdentities(identities);
            Assert.IsFalse(filteredIdentities.Any());
        }

        /// <summary>
        /// No claims of recognized type
        /// </summary>
        [TestMethod]
        public void GetValidIdentitiesNoRecognizedClaimType()
        {
            var identities = new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { this.invalidClaimTypeClaim }) };
            var filteredIdentities = this.authNManager.GetValidIdentities(identities);
            Assert.IsFalse(filteredIdentities.Any());
        }
        
        /// <summary>
        /// Find identities with valid claims
        /// </summary>
        [TestMethod]
        public void GetValidIdentitiesSuccess()
        {
            var identities = new List<IClaimsIdentity>
                {
                    new ClaimsIdentity(new List<Claim> { this.validClaim, this.invalidIssuerClaim }),
                    new ClaimsIdentity(new List<Claim>()),
                    new ClaimsIdentity(new List<Claim> { this.invalidClaimTypeClaim })
                };
            var filteredIdentities = this.authNManager.GetValidIdentities(identities);
            Assert.AreEqual(1, filteredIdentities.Count());
        }

        /// <summary>
        /// Check if a user exists for a given NameIdentifier
        /// </summary>
        [TestMethod]
        public void CheckIfUserExistsSuccess()
        {
            var userEntityId = this.authNManager.CheckIfUserExists(this.validUser);
            Assert.IsFalse(string.IsNullOrEmpty(userEntityId));
        }

        /// <summary>
        /// Check if a user exists for a given NameIdentifier (fail)
        /// </summary>
        [TestMethod]
        public void CheckIfUserExistsFail()
        {
            var userEntityId = this.authNManager.CheckIfUserExists(this.invalidUser);
            Assert.IsTrue(string.IsNullOrEmpty(userEntityId));
        }

        /// <summary>Update claims - no users found</summary>
        [TestMethod]
        public void UpdateIdentitiesWithClaimsNoUserFound()
        {
            var orginalClaims = this.validIdentitiesNoUser.SelectMany(i => i.Claims).Count();
            var userFound = this.authNManager.UpdateIdentitiesWithClaims(ref this.validIdentitiesNoUser);

            // Should only find the original claim
            Assert.AreEqual(orginalClaims, this.validIdentitiesNoUser.SelectMany(i => i.Claims).Count());
            Assert.IsFalse(userFound);
        }

        /// <summary>Update claims - user found</summary>
        [TestMethod]
        public void UpdateIdentitiesWithClaimsUserFound()
        {
            var orginalClaims = this.validIdentities.SelectMany(i => i.Claims).Count();
            var userFound = this.authNManager.UpdateIdentitiesWithClaims(ref this.validIdentities);

            // Should find one additional claim of the organization claim type
            var updatedClaims = this.validIdentities.ToList();
            Assert.AreEqual(orginalClaims + 1, updatedClaims.SelectMany(i => i.Claims).Count());
            Assert.AreEqual(1, updatedClaims.SelectMany(i => i.Claims).Count(c => c.ClaimType == CustomClaimsAuthenticationManager.RcUserClaimType));
            Assert.IsTrue(userFound);
        }

        /// <summary>Update claims - multiple identities, only one user found</summary>
        [TestMethod]
        public void UpdateIdentitiesWithClaimsUserMixed()
        {
            var identities = this.validIdentities.ToList();
            identities.Add(new ClaimsIdentity(new List<Claim> { this.validClaimNoUser }));
            this.validIdentities = identities;

            var orginalClaims = this.validIdentities.SelectMany(i => i.Claims).Count();
            var userFound = this.authNManager.UpdateIdentitiesWithClaims(ref this.validIdentities);

            // Should find one additional claim
            Assert.AreEqual(orginalClaims + 1, this.validIdentities.SelectMany(i => i.Claims).Count());
            Assert.IsTrue(userFound);
        }
    }
}
