//-----------------------------------------------------------------------
// <copyright file="CustomClaimsAuthorizationManagerFixture.cs" company="Rare Crowds Inc.">
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
using System.Security;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using IdentityFederation;
using Microsoft.IdentityModel.Claims;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ResourceAccess;

using Rhino.Mocks;

namespace SecurityUnitTests
{
    /// <summary>Test fixture for CustomClaimsAuthorizationManager</summary>
    [TestClass]
    public class CustomClaimsAuthorizationManagerFixture
    {
        /// <summary>IClaimsPrincipal for testing.</summary>
        private ClaimsPrincipal validPrincipal;

        /// <summary>IClaimsPrincipal without user claim for testing.</summary>
        private ClaimsPrincipal invalidPrincipal;

        /// <summary>AuthZ manager for testing.</summary>
        private CustomClaimsAuthorizationManager authZMgr;

        /// <summary>AuthZ context for testing.</summary>
        private AuthorizationContext validUserAuthContext;

        /// <summary>AuthZ invalid context for testing.</summary>
        private AuthorizationContext invalidUserAuthContext;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            var notFoundUserClaim = new Claim(
                ClaimTypes.NameIdentifier, 
                new EntityId().ToString(),
                ClaimValueTypes.String,
                new CustomClaimsAuthenticationManager(null).AcsIssuer);
            var userClaim = new Claim(
                CustomClaimsAuthenticationManager.RcUserClaimType, 
                new EntityId().ToString(), 
                ClaimValueTypes.String, 
                CustomClaimsAuthenticationManager.InternalIssuer);
            this.validPrincipal = new ClaimsPrincipal(new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { userClaim }) });
            this.invalidPrincipal = new ClaimsPrincipal(new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { notFoundUserClaim }) });
            this.validUserAuthContext = new AuthorizationContext(this.validPrincipal, "http://localhost/dontcare", "dontcare");
            this.invalidUserAuthContext = new AuthorizationContext(this.invalidPrincipal, "http://localhost/dontcare", "dontcare");

            this.SetupAuthZManager(true, false);
        }

        /// <summary>AuthorizationContext is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CheckAccessNullArgument()
        {
            this.authZMgr.CheckAccess(null);
        }

        /// <summary>Resource is not a valid absolute uri.</summary>
        [TestMethod]
        public void CheckAccessResourceNotValidUri()
        {
            var invalidResourceAuthContext = new AuthorizationContext(this.validPrincipal, "api/entity/dontcare", "dontcare");
            this.SetupAuthZManager(true, true);
            Assert.IsFalse(this.authZMgr.CheckAccess(invalidResourceAuthContext));
        }

        /// <summary>Resource is globally allowed.</summary>
        [TestMethod]
        public void CheckAccessGlobalAccessAllowed()
        {
            this.SetupAuthZManager(false, true);
            Assert.IsTrue(this.authZMgr.CheckAccess(this.validUserAuthContext));
        }
        
        /// <summary>No user claim on Website resource.</summary>
        [TestMethod]
        [ExpectedException(typeof(SecurityException))]
        public void CheckAccessNoUserClaimWeb()
        {
            this.SetupAuthZManager(false, false);
            this.authZMgr.CheckAccess(this.invalidUserAuthContext);
        }

        /// <summary>No user claim on API resource.</summary>
        [TestMethod]
        public void CheckAccessNoUserClaimApi()
        {
            var apiResourceInvalidUserAuthContext = new AuthorizationContext(this.invalidPrincipal, "http://localhost/api/entity/dontcare", "dontcare");
            this.SetupAuthZManager(false, false);
            Assert.IsFalse(this.authZMgr.CheckAccess(apiResourceInvalidUserAuthContext));
        }
        
        /// <summary>User access is denied.</summary>
        [TestMethod]
        public void CheckAccessFail()
        {
            // Use a web resource so this isn't ambiguous with no user claim on an api resource
            this.SetupAuthZManager(false, false);
            Assert.IsFalse(this.authZMgr.CheckAccess(this.validUserAuthContext));
        }

        /// <summary>User access is granted.</summary>
        [TestMethod]
        public void CheckAccessSuccess()
        {
            this.SetupAuthZManager(true, false);
            Assert.IsTrue(this.authZMgr.CheckAccess(this.validUserAuthContext));
        }

        /// <summary>User claim is present on principal.</summary>
        [TestMethod]
        public void GetUserEntityIdSuccess()
        {
            var userEntityId = this.authZMgr.GetUserEntityId(this.validPrincipal.Identities, null);
            Assert.IsNotNull(userEntityId);
        }

        /// <summary>
        /// User claim is retrieved on principal with multiple identities and claims.
        /// Even if there is more than one RCUser claim we only pick one.
        /// </summary>
        [TestMethod]
        public void GetUserEntityIdSuccessMultiple()
        {
            // Multiple identities with multiple claims
            var foreignClaim = new Claim("footype", "foovalue");
            var altUserClaim = new Claim(CustomClaimsAuthenticationManager.RcUserClaimType, new EntityId().ToString());
            this.validPrincipal.Identities[0].Claims.Add(foreignClaim);
            var foreignIdentity = new ClaimsIdentity(new List<Claim> { foreignClaim, altUserClaim });
            this.validPrincipal.Identities.Add(foreignIdentity);
            var userEntityId = this.authZMgr.GetUserEntityId(this.validPrincipal.Identities, null);
            Assert.IsNotNull(userEntityId);
        }

        /// <summary>User claim is not present on principal but in a user verify request.</summary>
        [TestMethod]
        public void GetUserEntityIdFromUserVerifyRequest()
        {
            var canonicalResource = new CanonicalResource(new Uri("https://localhost/user/00000000000000000000000000000001?message=verify"), "POST");
            var userEntityId = this.authZMgr.GetUserEntityId(this.invalidPrincipal.Identities, canonicalResource);
            Assert.IsNotNull(userEntityId);
        }

        /// <summary>User claim is not present on principal and user from request cannot be found.</summary>
        [TestMethod]
        public void GetUserEntityIdNotUserVerifyFail()
        {
            var canonicalResource = new CanonicalResource(new Uri("https://localhost/user/00000000000000000000000000000001"), "GET");
            var userEntityId = this.authZMgr.GetUserEntityId(this.invalidPrincipal.Identities, canonicalResource);
            Assert.IsNull(userEntityId);
        }

        /// <summary>Setup an authz manager with stubbed resource access handlers.</summary>
        /// <param name="grantForUser">allow user.</param>
        /// <param name="grantGlobal">allow global.</param>
        private void SetupAuthZManager(bool grantForUser, bool grantGlobal)
        {
            var userEntity = EntityJsonSerializer.DeserializeUserEntity(new EntityId("00000000000000000000000000000001"), string.Empty);
            var entityRepository = MockRepository.GenerateStub<IEntityRepository>();
            entityRepository.Stub(f => f.GetUser(null, null)).IgnoreArguments().Return(userEntity);

            var resourceAccessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            resourceAccessHandler.Stub(f => f.CheckAccess(null, null)).IgnoreArguments().Return(grantForUser);
            resourceAccessHandler.Stub(f => f.CheckGlobalAccess(null)).IgnoreArguments().Return(grantGlobal);
            this.authZMgr = new CustomClaimsAuthorizationManager(resourceAccessHandler, entityRepository);
        }
    }
}
