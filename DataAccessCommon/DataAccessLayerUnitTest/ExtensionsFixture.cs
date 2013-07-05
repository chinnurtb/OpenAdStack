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
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using Diagnostics;
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
            LogManager.Initialize(new List<ILogger> { MockRepository.GenerateStub<ILogger>() });
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
            Assert.AreSame(actualEntity.WrappedEntity, this.partnerEntity.WrappedEntity);
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
            Assert.AreSame(actualEntity.WrappedEntity, this.partnerEntity.WrappedEntity);
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
                UserEntity.CategoryName, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
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
                EntityFilterNames.EntityCategoryFilter, UserEntity.CategoryName);

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
                UserEntity.CategoryName, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
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
                EntityFilterNames.EntityCategoryFilter, CompanyEntity.CategoryName);

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
                CompanyEntity.CategoryName, filter.EntityQueries.QueryStringParams[EntityFilterNames.EntityCategoryFilter]);
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

        /// <summary>Test adding a property name filter to an entity filter.</summary>
        [TestMethod]
        public void AddPropertiesToEntityFilter()
        {
            var propertyNames = new List<string> { "prop1", "prop2" };
            var filter = new RepositoryEntityFilter();
            filter.AddPropertyNameFilter(propertyNames);
            Assert.AreEqual("prop1,prop2", filter.EntityQueries.QueryStringParams[EntityFilterNames.PropertyNameFilter]);
            var names = filter.GetPropertyNameFilter().ToList();
            Assert.IsTrue(names.SequenceEqual(propertyNames));
            Assert.IsTrue(names.Count() == 2);
        }

        /// <summary>Test adding an association name filter to an entity filter.</summary>
        [TestMethod]
        public void AddAssociationsToEntityFilter()
        {
            var associationNames = new List<string> { "assoc1", "assoc2" };
            var filter = new RepositoryEntityFilter();
            filter.AddAssociationNameFilter(associationNames);
            Assert.AreEqual("assoc1,assoc2", filter.EntityQueries.QueryStringParams[EntityFilterNames.AssociationNameFilter]);
            var names = filter.GetAssociationNameFilter().ToList();
            Assert.IsTrue(names.SequenceEqual(associationNames));
            Assert.IsTrue(names.Count() == 2);
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

        /// <summary>Test ForceUpdateEntity with property and association list.</summary>
        [TestMethod]
        public void ForceUpdateWithPropertyAndAssociationList()
        {
            RequestContext ctx = null;
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .WhenCalled(call => ctx = (RequestContext)call.Arguments[0]);

            var associationList = new List<string> { "associationName" };
            var propertyList = new List<string> { "propertyName" };
            var context = new RequestContext();

            this.repository.ForceUpdateEntity(context, this.partnerEntity, propertyList, associationList);

            Assert.AreEqual("propertyName", ctx.EntityFilter.GetPropertyNameFilter().Single());
            Assert.AreEqual("associationName", ctx.EntityFilter.GetAssociationNameFilter().Single());
        }

        /// <summary>Test ForceUpdateEntity happy path.</summary>
        [TestMethod]
        public void ForceUpdateEntitySuccess()
        {
            PartnerEntity savedEntity = null;
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .WhenCalled(call => savedEntity = (PartnerEntity)call.Arguments[1]);

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddAssociationNameFilter(new List<string> { "whatever" });
            entityFilter.AddPropertyNameFilter(new List<string> { "whatever" });
            var context = new RequestContext { EntityFilter = entityFilter };

            this.repository.ForceUpdateEntity(context, this.partnerEntity);
            Assert.AreSame(this.partnerEntity, savedEntity);
        }

        /// <summary>ForceUpdateEntity EntityFilter required.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void ForceUpdateEntityMissingEntityFilter()
        {
            this.repository.ForceUpdateEntity(new RequestContext(), this.partnerEntity);
        }

        /// <summary>ForceUpdateEntity Association names filter required.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void ForceUpdateEntityMissingMissingAssociationNamesFilter()
        {
            var entityFilter = new RepositoryEntityFilter(false, false, false, true);
            var context = new RequestContext { EntityFilter = entityFilter };
            this.repository.ForceUpdateEntity(context, this.partnerEntity);
        }

        /// <summary>ForceUpdateEntity Property names filter required.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void ForceUpdateEntityMissingMissingPropertyNamesFilter()
        {
            var entityFilter = new RepositoryEntityFilter(true, false, false, false);
            var context = new RequestContext { EntityFilter = entityFilter };
            this.repository.ForceUpdateEntity(context, this.partnerEntity);
        }

        /// <summary>ForceUpdateEntity max retries with stale entity throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void ForceUpdateEntityMaxRetriesWithStaleVersion()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessStaleEntityException());

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddAssociationNameFilter(new List<string> { "whatever" });
            entityFilter.AddPropertyNameFilter(new List<string> { "whatever" });
            var context = new RequestContext { EntityFilter = entityFilter };

            this.repository.ForceUpdateEntity(context, this.partnerEntity);
        }

        /// <summary>ForceUpdateEntity max retries with other exception.</summary>
        [TestMethod]
        public void ForceUpdateEntityMaxRetriesWithOtherException()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessException());

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddAssociationNameFilter(new List<string> { "whatever" });
            entityFilter.AddPropertyNameFilter(new List<string> { "whatever" });
            var context = new RequestContext { EntityFilter = entityFilter };

            var timeNow = DateTime.Now;
            try
            {
                this.repository.ForceUpdateEntity(context, this.partnerEntity, new DefaultRetryProvider(3, 100));
            }
            catch (DataAccessException)
            {
                var duration = (DateTime.Now - timeNow).TotalMilliseconds;

                // With three tries we should sleep twice
                Assert.IsTrue(duration >= 200);
            }
        }

        /// <summary>ForceUpdateEntity fails on entity not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void ForceUpdateEntityEntityNotFound()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessEntityNotFoundException());

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddAssociationNameFilter(new List<string> { "whatever" });
            entityFilter.AddPropertyNameFilter(new List<string> { "whatever" });
            var context = new RequestContext { EntityFilter = entityFilter };

            this.repository.ForceUpdateEntity(context, this.partnerEntity);
        }

        /// <summary>ForceUpdateEntity failse on type mismatch.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void ForceUpdateEntityTypeMismatch()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessTypeMismatchException());

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddAssociationNameFilter(new List<string> { "whatever" });
            entityFilter.AddPropertyNameFilter(new List<string> { "whatever" });
            var context = new RequestContext { EntityFilter = entityFilter };

            this.repository.ForceUpdateEntity(context, this.partnerEntity);
        }

        /// <summary>TryForceUpdateEntity return true on success.</summary>
        [TestMethod]
        public void TryForceUpdateWithFilterListsSuccess()
        {
            PartnerEntity savedEntity = null;
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .WhenCalled(call => savedEntity = (PartnerEntity)call.Arguments[1]);

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            var context = new RequestContext { EntityFilter = entityFilter };

            var result = this.repository.TryForceUpdateEntity(context, this.partnerEntity, new List<string> { "whatever" }, new List<string> { "whatever" });
            Assert.AreSame(this.partnerEntity, savedEntity);
            Assert.IsTrue(result);
        }

        /// <summary>TryForceUpdateEntity return false on fail.</summary>
        [TestMethod]
        public void TryForceUpdateWithFilterListsFail()
        {
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IEntity>.Is.Anything))
                .Throw(new DataAccessStaleEntityException());

            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            var context = new RequestContext { EntityFilter = entityFilter };

            var result = this.repository.TryForceUpdateEntity(context, this.partnerEntity, new List<string> { "whatever" }, new List<string> { "whatever" });
            Assert.IsFalse(result);
        }

        /// <summary>Build a request context success</summary>
        [TestMethod]
        public void BuildContextWithNameFilters()
        {
            var context = new RequestContext
                {
                    ExternalCompanyId = new EntityId(),
                    UserId = new EntityId(),
                    ForceOverwrite = true,
                    EntityFilter = new RepositoryEntityFilter(true, true, true, true)
                };

            // Both property and association names
            var newContext = context.BuildContextWithNameFilters(new List<string> { "foo" }, new List<string> { "boo" });
            Assert.AreEqual(context.ExternalCompanyId, newContext.ExternalCompanyId);
            Assert.AreEqual(context.UserId, newContext.UserId);
            Assert.IsFalse(newContext.ForceOverwrite);
            Assert.IsTrue(newContext.EntityFilter.GetPropertyNameFilter().Contains("foo"));
            Assert.IsTrue(newContext.EntityFilter.GetAssociationNameFilter().Contains("boo"));

            // No association names - make sure no associations are updated
            newContext = context.BuildContextWithNameFilters(new List<string> { "foo" }, new List<string>());
            Assert.IsTrue(newContext.EntityFilter.GetPropertyNameFilter().Contains("foo"));
            Assert.IsFalse(newContext.EntityFilter.GetAssociationNameFilter().Contains("boo"));
            Assert.IsFalse(newContext.EntityFilter.IncludeAssociations);

            // No property names - make sure no properties are updated
            newContext = context.BuildContextWithNameFilters(new List<string>(), new List<string> { "boo" });
            Assert.IsFalse(newContext.EntityFilter.GetPropertyNameFilter().Contains("foo"));
            Assert.IsFalse(newContext.EntityFilter.IncludeDefaultProperties);
            Assert.IsFalse(newContext.EntityFilter.IncludeExtendedProperties);
            Assert.IsFalse(newContext.EntityFilter.IncludeSystemProperties);
            Assert.IsTrue(newContext.EntityFilter.GetAssociationNameFilter().Contains("boo"));
            Assert.IsTrue(newContext.EntityFilter.IncludeAssociations);
        }

        /// <summary>Build a request context success with null filter lists.</summary>
        [TestMethod]
        public void BuildContextWithNameFiltersNullFilters()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = new EntityId(),
                UserId = new EntityId(),
                ForceOverwrite = true,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            var associationList = new List<string> { "associationName" };
            var propertyList = new List<string> { "propertyName" };

            var newContext = context.BuildContextWithNameFilters(null, associationList);
            Assert.IsFalse(newContext.EntityFilter.GetPropertyNameFilter().Any());
            Assert.AreEqual("associationName", newContext.EntityFilter.GetAssociationNameFilter().Single());

            newContext = context.BuildContextWithNameFilters(propertyList, null);
            Assert.AreEqual("propertyName", newContext.EntityFilter.GetPropertyNameFilter().Single());
            Assert.IsFalse(newContext.EntityFilter.GetAssociationNameFilter().Any());

            newContext = context.BuildContextWithNameFilters(null, null);
            Assert.IsFalse(newContext.EntityFilter.GetPropertyNameFilter().Any());
            Assert.IsFalse(newContext.EntityFilter.GetAssociationNameFilter().Any());
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
