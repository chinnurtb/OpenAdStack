// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>
    /// Test fixture for DataAccessLayer Extensions class
    /// </summary>
    [TestClass]
    public class ExtensionsFixture
    {
        /// <summary>IEntity repository stub for testing.</summary>
        private IEntityRepository repository;

        /// <summary>EntityId for testing.</summary>
        private EntityId partnerEntityId;

        /// <summary>PartnerEntity for testing.</summary>
        private PartnerEntity partnerEntity;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.partnerEntityId = new EntityId();
            this.partnerEntity = TestEntityBuilder.BuildPartnerEntity(this.partnerEntityId);

            // Setup default campaign and company stubs
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.SetupGetEntityStub(this.partnerEntity);
        }

        /// <summary>Test get entity with type param.</summary>
        [TestMethod]
        public void GetEntityGeneric()
        {
            var actualEntity = this.repository.GetEntity<PartnerEntity>(
                null, this.partnerEntityId);
            Assert.AreSame(actualEntity, this.partnerEntity);
            Assert.IsInstanceOfType(actualEntity, typeof(PartnerEntity));
        }

        /// <summary>Test get entity with a mismatched type param.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void GetEntityGenericTypeMismatch()
        {
            this.repository.GetEntity<CampaignEntity>(null, this.partnerEntityId);
        }

        /// <summary>Test TryGetEntity with type param.</summary>
        [TestMethod]
        public void TryGetEntity()
        {
            var actualEntity = this.repository.TryGetEntity(
                null, this.partnerEntityId);
            Assert.AreSame(actualEntity, this.partnerEntity);
            Assert.IsInstanceOfType(actualEntity, typeof(PartnerEntity));
        }

        /// <summary>Test TryGetEntity returns null on fail.</summary>
        [TestMethod]
        public void TryGetEntityFail()
        {
            var notFoundId = new EntityId();
            this.repository.Stub(f => f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(notFoundId)))
                    .Throw(new DataAccessEntityNotFoundException());

            var actualEntity = this.repository.TryGetEntity(null, notFoundId);
            Assert.IsNull(actualEntity);
        }

        /// <summary>Test TryGetEntity with type param.</summary>
        [TestMethod]
        public void TryGetEntityGeneric()
        {
            var actualEntity = this.repository.TryGetEntity<PartnerEntity>(
                null, this.partnerEntityId);
            Assert.AreSame(actualEntity, this.partnerEntity);
            Assert.IsInstanceOfType(actualEntity, typeof(PartnerEntity));
        }

        /// <summary>Test TryGetEntity with a mismatched type param.</summary>
        [TestMethod]
        public void TryGetEntityGenericTypeMismatch()
        {
            var actualEntity = this.repository.TryGetEntity<CampaignEntity>(null, this.partnerEntityId);
            Assert.IsNull(actualEntity);
        }

        /// <summary>Get all users in the system.</summary>
        [TestMethod]
        public void GetAllUsersEmpty()
        {
            IEntityFilter filter = null;
            Action<IEntityFilter> captureArgs = p => filter = p;
            this.repository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything))
                .WhenCalled(call => captureArgs(((RequestContext)call.Arguments[0]).EntityFilter))
                .Return(new List<EntityId>());

            var users = this.repository.GetAllUsers(new RequestContext());
            
            Assert.IsNotNull(users);
            Assert.AreEqual(0, users.Count);
            Assert.AreEqual(
                UserEntity.UserEntityCategory, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
        }

        /// <summary>Happy-path GetAllUsers</summary>
        [TestMethod]
        public void GetAllUsers()
        {
            // Set up data returned by stubs
            var user1 = TestEntityBuilder.BuildUserEntity();
            var user2 = TestEntityBuilder.BuildUserEntity();
            this.SetupGetEntityStub(user1);
            this.SetupGetEntityStub(user2);

            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, UserEntity.UserEntityCategory);

            // Set up the index to return the company1 and company2 entity ids.
            IEntityFilter filter = null;
            Action<IEntityFilter> captureArgs = p => filter = p;
            this.repository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything))
                .WhenCalled(call => captureArgs(((RequestContext)call.Arguments[0]).EntityFilter))
                .Return(new List<EntityId> { user1.ExternalEntityId, user2.ExternalEntityId });

            var entities = this.repository.GetAllUsers(new RequestContext());

            Assert.AreEqual(2, entities.Count);
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == user1.ExternalEntityId));
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == user2.ExternalEntityId));
            Assert.AreEqual(
                UserEntity.UserEntityCategory, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
        }

        /// <summary>Test that we can get all the Companies in the system.</summary>
        [TestMethod]
        public void GetAllCompanies()
        {
            // Set up data returned by stubs
            var company1 = TestEntityBuilder.BuildCompanyEntity();
            var company2 = TestEntityBuilder.BuildCompanyEntity();
            this.SetupGetEntityStub(company1);
            this.SetupGetEntityStub(company2);

            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, CompanyEntity.CompanyEntityCategory);

            // Set up the index to return the company1 and company2 entity ids.
            IEntityFilter filter = null;
            Action<IEntityFilter> captureArgs = p => filter = p;
            this.repository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything))
                .WhenCalled(call => captureArgs(((RequestContext)call.Arguments[0]).EntityFilter))
                .Return(new List<EntityId> { company1.ExternalEntityId, company2.ExternalEntityId });

            // Call the repository get method
            var entities = this.repository.GetAllCompanies(new RequestContext());

            Assert.AreEqual(2, entities.Count);
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == company1.ExternalEntityId));
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == company1.ExternalEntityId));
            Assert.AreEqual(
                CompanyEntity.CompanyEntityCategory, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
        }

        /// <summary>Test parallel TryGetEntities.</summary>
        [TestMethod]
        public void ParallelTryGetEntities()
        {
            // Set up data returned by stubs
            var company1 = TestEntityBuilder.BuildCompanyEntity();
            var company2 = TestEntityBuilder.BuildCompanyEntity();
            this.SetupGetEntityStub(company1);
            this.SetupGetEntityStub(company2);

            var entities = this.repository.TryGetEntities(
                new RequestContext(),
                new[] { (EntityId)company1.ExternalEntityId, (EntityId)company2.ExternalEntityId });

            Assert.AreEqual(2, entities.Count);
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == company1.ExternalEntityId));
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == company1.ExternalEntityId));
        }

        /// <summary>Test adding a version filter to an entity filter.</summary>
        [TestMethod]
        public void AddVersionToEntityFilter()
        {
            var filter = new RepositoryEntityFilter();
            filter.AddVersionToEntityFilter(2);
            Assert.AreEqual("2", filter.EntityQueries.QueryStringParams[EntityFilterNames.VersionFilter]);
            filter.AddVersionToEntityFilter(3);
            Assert.AreEqual("3", filter.EntityQueries.QueryStringParams[EntityFilterNames.VersionFilter]);
        }

        /// <summary>Test we can TrySaveEntity.</summary>
        [TestMethod]
        public void TrySaveEntity()
        {
            PartnerEntity savedEntity = null;

            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .WhenCalled(call => savedEntity = (PartnerEntity)call.Arguments[1]);

            var result = this.repository.TrySaveEntity(null, this.partnerEntity);
            Assert.IsTrue(result);
            Assert.AreSame(this.partnerEntity, savedEntity);
        }

        /// <summary>Test TrySaveEntity returns false on fail.</summary>
        [TestMethod]
        public void TrySaveEntityFail()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessException());

            var result = this.repository.TrySaveEntity(null, this.partnerEntity);
            Assert.IsFalse(result);
        }

        /// <summary>Test SetEntityStatus extension for single entity id.</summary>
        [TestMethod]
        public void SetEntityStatus()
        {
            HashSet<EntityId> entityIdArgs = null;
            bool activeArg = false;
            Action<HashSet<EntityId>, bool> captureArgs = (ids, active) => 
            { 
                entityIdArgs = ids;
                activeArg = active;
            };

            this.repository.Stub(f => f.SetEntityStatus(
                Arg<RequestContext>.Is.Anything, Arg<HashSet<EntityId>>.Is.Anything, Arg<bool>.Is.Anything))
                .WhenCalled(call => captureArgs((HashSet<EntityId>)call.Arguments[1], (bool)call.Arguments[2]));

            var entityId = new EntityId();
            this.repository.SetEntityStatus(null, entityId, true);
            Assert.IsTrue(activeArg);
            Assert.AreEqual(entityId, entityIdArgs.Single());

            this.repository.SetEntityStatus(null, entityId, false);
            Assert.IsFalse(activeArg);
        }

        /// <summary>Setup repository stub for get entity.</summary>
        /// <param name="entity">The entity to set up.</param>
        private void SetupGetEntityStub(IEntity entity)
        {
            this.repository.Stub(f => f.GetEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal((EntityId)entity.ExternalEntityId)))
                    .Return(entity);
        }
    }
}
