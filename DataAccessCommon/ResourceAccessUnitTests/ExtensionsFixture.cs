// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using DataAccessLayerUnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;

namespace ResourceAccessUnitTests
{
    /// <summary>
    /// Test fixture for IResourceAccessHandler extensions class
    /// </summary>
    [TestClass]
    public class ExtensionsFixture
    {
        /// <summary>IEntityRepository stub for testing.</summary>
        private IEntityRepository repository;

        /// <summary>IResourceAccessHandler stub for testing.</summary>
        private IResourceAccessHandler resourceAccessHandler;

        /// <summary>EntityId for testing.</summary>
        private EntityId userEntityId;

        /// <summary>UserId for testing.</summary>
        private string userId;

        /// <summary>UserEntity for testing.</summary>
        private UserEntity userEntity;

        /// <summary>CanonicalResource for testing.</summary>
        private CanonicalResource canonicalResource;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.userEntityId = new EntityId();
            this.userId = "userfoo";
            this.userEntity = TestEntityBuilder.BuildUserEntity(this.userEntityId);
            this.userEntity.UserId = this.userId;
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.resourceAccessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            this.canonicalResource = new CanonicalResource(new Uri("http://foo"), "POST");
        }

        /// <summary>Test CheckAccessByUserId success.</summary>
        [TestMethod]
        public void CheckAccessByUserIdSuccess()
        {
            // Setup stubs
            this.repository.Stub(f => f.GetUser(
                    Arg<RequestContext>.Is.Anything,
                    Arg<string>.Is.Equal(this.userId)))
                    .Return(this.userEntity);

            this.resourceAccessHandler.Stub(f => f.CheckAccess(
                Arg<CanonicalResource>.Is.Equal(this.canonicalResource),
                Arg<EntityId>.Is.Equal(this.userEntityId))).IgnoreArguments().Return(true);

            var accessGranted = this.resourceAccessHandler.CheckAccessByUserId(
                this.repository, this.canonicalResource, this.userId);
            Assert.IsTrue(accessGranted);
        }

        /// <summary>Test CheckAccessByUserId user not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void CheckAccessByUserIdNotFound()
        {
            // Setup stubs
            this.repository.Stub(f => f.GetUser(
                    Arg<RequestContext>.Is.Anything,
                    Arg<string>.Is.Equal(this.userId)))
                    .Throw(new ArgumentException("foo"));

            this.resourceAccessHandler.CheckAccessByUserId(
                this.repository, this.canonicalResource, this.userId);
        }
    }
}
