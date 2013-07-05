//-----------------------------------------------------------------------
// <copyright file="RepositoryStubUtilitiesFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace EntityUtilitiesUnitTests
{
    /// <summary>
    /// Unit-test fixture for RepositoryStubUtilities
    /// </summary>
    [TestClass]
    public class RepositoryStubUtilitiesFixture
    {
        /// <summary>Repository instance for testing.</summary>
        private IEntityRepository repository;

        /// <summary>Entity id for testing.</summary>
        private EntityId partnerEntityId;

        /// <summary>Entity for testing.</summary>
        private PartnerEntity partnerEntity;

        /// <summary>RequestContext for testing.</summary>
        private RequestContext requestContext;

        /// <summary>UserEntity for testing.</summary>
        private UserEntity userEntity;

        /// <summary>
        /// Per-test initialization
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.partnerEntityId = new EntityId();
            this.partnerEntity = EntityTestHelpers.CreateTestPartnerEntity(this.partnerEntityId, "testentity");
            this.userEntity = EntityTestHelpers.CreateTestUserEntity(new EntityId(), new EntityId(), "foo@foo.com");
            this.requestContext = new RequestContext { EntityFilter = new RepositoryEntityFilter(true, false, false, false) };
        }

        /// <summary>Test GetEntity stub success.</summary>
        [TestMethod]
        public void SetupGetEntityStubSuccess()
        {
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.partnerEntityId, this.partnerEntity, false);
            var actualEntity = this.repository.GetEntity(null, this.partnerEntityId);
            Assert.AreEqual(actualEntity, this.partnerEntity);
        }

        /// <summary>Test GetEntity stub fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void SetupGetEntityStubFail()
        {
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.partnerEntityId, this.partnerEntity, true);
            this.repository.GetEntity(null, this.partnerEntityId);
        }

        /// <summary>
        /// Test GetEntity stub id mismatch returns null because the stub isn't actually set up.
        /// This is a Rhino thing.
        /// </summary>
        [TestMethod]
        public void SetupGetEntityStubIdMismatch()
        {
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.partnerEntityId, this.partnerEntity, true);
            var actualEntity = this.repository.GetEntity(null, new EntityId());
            Assert.IsNull(actualEntity);
        }

        /// <summary>Test GetEntity stub (with filter) success.</summary>
        [TestMethod]
        public void SetupGetEntityStubWithFilterSuccess()
        {
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.requestContext.EntityFilter, this.partnerEntityId, this.partnerEntity, false);
            var actualEntity = this.repository.GetEntity(this.requestContext, this.partnerEntityId);
            Assert.AreEqual(actualEntity, this.partnerEntity);
        }

        /// <summary>Test GetEntity stub (with version filter) success.</summary>
        [TestMethod]
        public void SetupGetEntityStubWithVersionFilterSuccess()
        {
            this.requestContext.EntityFilter.AddVersionToEntityFilter(1);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.requestContext.EntityFilter, this.partnerEntityId, this.partnerEntity, false);

            var partnerEntity2 = EntityTestHelpers.CreateTestPartnerEntity(this.partnerEntityId, "testentity");
            var ctx2 = new RequestContext(this.requestContext);
            ctx2.EntityFilter.AddVersionToEntityFilter(2);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, ctx2.EntityFilter, this.partnerEntityId, partnerEntity2, false);

            var actualEntity = this.repository.GetEntity(this.requestContext, this.partnerEntityId);
            Assert.AreEqual(actualEntity, this.partnerEntity);

            actualEntity = this.repository.GetEntity(ctx2, this.partnerEntityId);
            Assert.AreEqual(actualEntity, partnerEntity2);
        }

        /// <summary>Test GetEntity stub (with filter) fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void SetupGetEntityStubWithFilterFail()
        {
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.requestContext.EntityFilter, this.partnerEntityId, this.partnerEntity, true);
            this.repository.GetEntity(this.requestContext, this.partnerEntityId);
        }

        /// <summary>
        /// Test GetEntity stub with filter mismatch returns null because stub isn't actually set up.
        /// This is a Rhino thing.
        /// </summary>
        [TestMethod]
        public void SetupGetEntityStubWithFilterMismatch()
        {
            var mismatchFilter = new RepositoryEntityFilter(true, true, true, true);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, mismatchFilter, this.partnerEntityId, this.partnerEntity, true);
            var actualEntity = this.repository.GetEntity(this.requestContext, this.partnerEntityId);
            Assert.IsNull(actualEntity);
        }

        /// <summary>
        /// Test GetEntity stub with version filter mismatch returns null because stub isn't actually set up.
        /// This is a Rhino thing.
        /// </summary>
        [TestMethod]
        public void SetupGetEntityStubWithVersionFilterMismatch()
        {
            var mismatchFilter = new RepositoryEntityFilter(true, true, true, true);
            mismatchFilter.AddVersionToEntityFilter(2);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, mismatchFilter, this.partnerEntityId, this.partnerEntity, true);
            var actualEntity = this.repository.GetEntity(this.requestContext, this.partnerEntityId);
            Assert.IsNull(actualEntity);
        }

        /// <summary>Test SaveEntity stub success.</summary>
        [TestMethod]
        public void SetupSaveEntityStubSuccess()
        {
            PartnerEntity savedEntity = null;
            RepositoryStubUtilities.SetupSaveEntityStub<PartnerEntity>(
                this.repository, e => savedEntity = e, false);
            this.repository.SaveEntity(null, this.partnerEntity);
            Assert.AreSame(savedEntity, this.partnerEntity);
        }

        /// <summary>Test SaveEntity stub fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void SetupSaveEntityStubFail()
        {
            RepositoryStubUtilities.SetupSaveEntityStub<PartnerEntity>(
                this.repository, e => { }, true);
            this.repository.SaveEntity(null, this.partnerEntity);
        }

        /// <summary>Test SaveEntity stub with request context - success.</summary>
        [TestMethod]
        public void SetupSaveEntityStubWithContextSuccess()
        {
            PartnerEntity savedEntity = null;
            RequestContext calledContext = null;
            RepositoryStubUtilities.SetupSaveEntityStub<PartnerEntity>(
                this.repository, (c, e) => { calledContext = c; savedEntity = e; }, false);
            this.repository.SaveEntity(this.requestContext, this.partnerEntity);
            Assert.AreSame(savedEntity, this.partnerEntity);
            Assert.IsTrue(this.requestContext.EntityFilter.Filters.SequenceEqual(
                calledContext.EntityFilter.Filters));
        }

        /// <summary>Test SaveEntity stub with request context - fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void SetupSaveEntityStubWithContextFail()
        {
            RepositoryStubUtilities.SetupSaveEntityStub<PartnerEntity>(
                this.repository, (c, e) => { }, true);
            this.repository.SaveEntity(null, this.partnerEntity);
        }

        /// <summary>Test SaveUser stub with request context - success.</summary>
        [TestMethod]
        public void SetupSaveUserSuccess()
        {
            UserEntity savedEntity = null;
            RequestContext calledContext = null;
            RepositoryStubUtilities.SetupSaveUserStub(
                this.repository, (c, e) => { calledContext = c; savedEntity = e; }, false);
            this.repository.SaveUser(this.requestContext, this.userEntity);
            Assert.AreSame(savedEntity, this.userEntity);
            Assert.IsTrue(this.requestContext.EntityFilter.Filters.SequenceEqual(
                calledContext.EntityFilter.Filters));
        }

        /// <summary>Test SaveUser stub with request context - fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SetupSaveUserFail()
        {
            RepositoryStubUtilities.SetupSaveUserStub(
                this.repository, (c, e) => { }, true);
            this.repository.SaveUser(null, this.userEntity);
        }
        
        /// <summary>Test TryUpdateEntity stub success.</summary>
        [TestMethod]
        public void SetupTryUpdateEntityStubSuccess()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, this.partnerEntityId, p => savedProperties = p, false);
            var success = this.repository.TryUpdateEntity(
                null, this.partnerEntityId, new List<EntityProperty> { propertyToSave });
            Assert.IsTrue(success);
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }

        /// <summary>Test TryUpdateEntity stub fail.</summary>
        [TestMethod]
        public void SetupTryUpdateEntityStubFail()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, this.partnerEntityId, p => savedProperties = p, true);
            var success = this.repository.TryUpdateEntity(
                null, this.partnerEntityId, new List<EntityProperty> { propertyToSave });
            Assert.IsFalse(success);
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }

        /// <summary>
        /// Test TryUpdateEntity stub throws ArgumentNullException on id mismatch if you try to
        /// check the saved property. This is a Rhino thing - it doesn't get assigned.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetupTryUpdateEntityStubIdMismatch()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, this.partnerEntityId, p => savedProperties = p, false);
            this.repository.TryUpdateEntity(
                null, new EntityId(), new List<EntityProperty> { propertyToSave });
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }

        /// <summary>Test TryUpdateEntity stub (with entity filter) success.</summary>
        [TestMethod]
        public void SetupTryUpdateEntityStubWithFilterSuccess()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, this.requestContext.EntityFilter, this.partnerEntityId, p => savedProperties = p, false);
            var success = this.repository.TryUpdateEntity(
                this.requestContext, this.partnerEntityId, new List<EntityProperty> { propertyToSave });
            Assert.IsTrue(success);
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }

        /// <summary>Test TryUpdateEntity stub (with entity filter) fail.</summary>
        [TestMethod]
        public void SetupTryUpdateEntityStubWithFilterFail()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, this.requestContext.EntityFilter, this.partnerEntityId, p => savedProperties = p, true);
            var success = this.repository.TryUpdateEntity(
                this.requestContext, this.partnerEntityId, new List<EntityProperty> { propertyToSave });
            Assert.IsFalse(success);
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }

        /// <summary>
        /// Test TryUpdateEntity stub throws ArgumentNullException on filter mismatch if you try to
        /// check the saved property. This is a Rhino thing - it doesn't get assigned.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetupTryUpdateEntityStubWithFilterMismatch()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var propertyToSave = new EntityProperty("someprop", "somevalue");

            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repository, 
                new RepositoryEntityFilter(true, false, false, true), 
                this.partnerEntityId, 
                p => savedProperties = p, 
                false);
            this.repository.TryUpdateEntity(
                this.requestContext, new EntityId(), new List<EntityProperty> { propertyToSave });
            Assert.AreSame(propertyToSave.Name, savedProperties.Single().Name);
        }
    }
}
