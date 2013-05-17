// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConcreteEntityRepositoryFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ConcreteDataStore;
using DataAccessLayer;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.StorageClient;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for ConcreteEntityRepository</summary>
    [TestClass]
    public class ConcreteEntityRepositoryFixture
    {
        /// <summary>minimum entity store</summary>
        private IEntityStore entityStore;

        /// <summary>minimum index store</summary>
        private IIndexStore indexStore;

        /// <summary>index factory stub returning minimum index</summary>
        private IIndexStoreFactory indexFactory;

        /// <summary>xml entity store factory for testing</summary>
        private IEntityStoreFactory entityFactory;

        /// <summary>storeage key factory for testing</summary>
        private IStorageKeyFactory storageKeyFactory;

        /// <summary>entity repository for testing</summary>
        private IEntityRepository entityRepository;

        /// <summary>RequestContext for testing.</summary>
        private RequestContext requestContext;

        /// <summary>IBlobStoreFactory for testing.</summary>
        private IBlobStoreFactory blobStoreFactory;

        /// <summary>IBlobStore for testing.</summary>
        private IBlobStore blobStore;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            // Create the index store stub and set up it's factory stub to return it
            this.indexStore = MockRepository.GenerateStub<IIndexStore>(); 
            this.indexFactory = MockRepository.GenerateStub<IIndexStoreFactory>();
            this.indexFactory.Stub(f => f.GetIndexStore()).Return(this.indexStore);
            
            // Create EntityStore stub and set up it's factory stub to return it
            this.entityStore = MockRepository.GenerateStub<IEntityStore>();
            this.entityFactory = MockRepository.GenerateStub<IEntityStoreFactory>();
            this.entityFactory.Stub(f => f.GetEntityStore()).Return(this.entityStore);

            // Create BlobStore stub and set up it's factory stub to return it
            this.blobStore = MockRepository.GenerateStub<IBlobStore>();
            this.blobStore.Stub(f => f.GetStorageKeyFactory()).Return(new AzureBlobStorageKeyFactory());
            this.blobStoreFactory = MockRepository.GenerateStub<IBlobStoreFactory>();
            this.blobStoreFactory.Stub(f => f.GetBlobStore()).Return(this.blobStore);

            // Create StorageKeyFactory stub
            this.storageKeyFactory = MockRepository.GenerateStub<IStorageKeyFactory>();
            
            // Use the index and entity stores to build an Entity Repository
            this.entityRepository = new ConcreteEntityRepository(
                this.indexFactory, this.entityFactory, this.storageKeyFactory, this.blobStoreFactory);

            this.requestContext = new RequestContext { ExternalCompanyId = new EntityId(), UserId = "abc123" };
        }

        /// <summary>Test injector construction of entity repository</summary>
        [TestMethod]
        public void InjectorConstruction()
        {
            var newEntityRepository = new ConcreteEntityRepository(this.indexFactory, this.entityFactory, this.storageKeyFactory, this.blobStoreFactory);
            Assert.IsNotNull(newEntityRepository.IndexStoreFactory);
            Assert.AreSame(this.indexFactory, newEntityRepository.IndexStoreFactory);
            Assert.IsNotNull(newEntityRepository.EntityStoreFactory);
            Assert.AreSame(this.entityFactory, newEntityRepository.EntityStoreFactory);
            Assert.IsNotNull(newEntityRepository.StorageKeyFactory);
            Assert.AreSame(this.storageKeyFactory, newEntityRepository.StorageKeyFactory);
            Assert.IsNotNull(newEntityRepository.BlobStoreFactory);
            Assert.AreSame(this.blobStoreFactory, newEntityRepository.BlobStoreFactory);
        }
        
        /// <summary>Test single-entity save</summary>
        [TestMethod]
        public void TrySaveEntity()
        {
            // Build entity and set up stubs
            var entity = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entityFail = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity(), true);

            Assert.IsTrue(this.entityRepository.TrySaveEntity(this.requestContext, entity));
            Assert.IsFalse(this.entityRepository.TrySaveEntity(this.requestContext, entityFail));
        }

        /// <summary>Test single-entity save</summary>
        [TestMethod]
        public void SaveEntity()
        {
            // Build entity and set up stubs
            var entity = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());

            this.entityRepository.SaveEntity(this.requestContext, entity);

            // Entity and index should be updated for both of them but only entity1 should have isUpdate=true
            this.AssertSaveNewEntity(entity);
            Assert.AreEqual(this.requestContext.UserId, (string)entity.LastModifiedUser);
        }

        /// <summary>Test that read-only interface properties are preserved and/or updated correctly.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void SaveEntityFailGetWhenKeyExistsThrows()
        {
            // Build entity and set up stubs but with the get entity set to fail
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), true, true, e => { }, false);
            this.entityRepository.SaveEntity(this.requestContext, entity);
        }

        /// <summary>Test that read-only interface properties are preserved and/or updated correctly.</summary>
        [TestMethod]
        public void SaveEntityReadOnlyProperties()
        {
            // Build entity and set up stubs
            IRawEntity savedEntity = null;
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), true, e => savedEntity = e);

            // Capture the original create date and current last modified date
            var createDate = (DateTime)entity.CreateDate;
            var lastModifiedDate = (DateTime)entity.LastModifiedDate;

            // Setup missing create date on the incoming entity, (like it was not provided on the incoming entity json)
            var createDateProperty = entity.CreateDate;
            entity.InterfaceProperties.Remove(createDateProperty);

            // Set up a unique user for this test
            this.requestContext.UserId = new EntityId();

            this.entityRepository.SaveEntity(this.requestContext, entity);

            // CreateDate is preserved
            Assert.AreEqual(createDate, (DateTime)savedEntity.CreateDate);

            // LastModifiedX is updated
            Assert.IsTrue(lastModifiedDate < savedEntity.LastModifiedDate);
            Assert.AreNotEqual(this.requestContext.UserId, savedEntity.LastModifiedUser);

            // Version must be in the json and is incremented if not stale
            Assert.AreEqual(entity.LocalVersion++, savedEntity.LocalVersion);
        }

        /// <summary>Test Api to save a collection of entities.</summary>
        [TestMethod]
        public void SaveEntities()
        {
            // Build entities and set up stubs
            var entity1 = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entity2 = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entities = new HashSet<IEntity> { entity1, entity2 };

            this.entityRepository.SaveEntities(this.requestContext, entities);

            // Entity and index should be updated for both of them but only entity1 should have isUpdate=true
            this.AssertSaveNewEntity(entity1);
            this.AssertSaveNewEntity(entity2);
            Assert.AreEqual(this.requestContext.UserId, (string)entity1.LastModifiedUser);
            Assert.AreEqual(this.requestContext.UserId, (string)entity2.LastModifiedUser);
        }

        /// <summary>Test Api to save a collection of entities.</summary>
        [TestMethod]
        public void SaveEntitiesUnwrapsWrappedEntities()
        {
            var entity = (PartnerEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entities = new HashSet<IEntity> { entity };

            this.entityRepository.SaveEntities(this.requestContext, entities);

            // Assert that the entity passed to data store is derived from the wrapped entity
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId 
                    && !ReferenceEquals(entity, e)),
                Arg<bool>.Is.Equal(false)));
        }

        /// <summary>Test Api to update or save entities as appropriate.</summary>
        [TestMethod]
        public void MixedSaveAndUpdateEntities()
        {
            // Build entities and set up stubs
            var entity1 = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entity2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var entities = new HashSet<IEntity> { entity1, entity2 };

            this.entityRepository.SaveEntities(this.requestContext, entities);
            
            // BuildNewStorageKey should not be called for entity2
            this.storageKeyFactory.AssertWasNotCalled(f => f.BuildNewStorageKey(
                Arg<string>.Is.Anything, Arg<EntityId>.Is.Anything, Arg<IRawEntity>.Is.Equal(entity2.ExternalEntityId)));
            
            // Entity and index should be updated for both of them but only entity1 should have isUpdate=true
            this.AssertSaveNewEntity(entity1);
            this.AssertUpdateEntity(entity2);
        }

        /// <summary>Failure to save index on update should result in orphaned entity being removed.</summary>
        [TestMethod]
        public void UpdateFailCleanup()
        {
            // Setup a saved entity that will fail index save on update.
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), false);

            try
            {
                this.entityRepository.SaveEntity(this.requestContext, entity);
                Assert.Fail("DataAccessException should be thrown.");
            }
            catch (DataAccessException)
            {
                // The orphaned entity should get cleaned up.
                this.entityStore.AssertWasCalled(f => f.RemoveEntity(Arg<IStorageKey>.Is.Anything));
            }
        }

        /// <summary>Failure to save entity in table store should result in exception.</summary>
        [TestMethod]
        public void SaveEntityFailEntitySave()
        {
            // Setup a saved entity that will fail entity save.
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), false, true, x => { }, true);

            try
            {
                this.entityRepository.SaveEntity(this.requestContext, entity);
                Assert.Fail("DataAccessException should be thrown.");
            }
            catch (DataAccessException)
            {
                this.entityStore.AssertWasCalled(
                    f => f.SaveEntity(Arg<RequestContext>.Is.Anything, Arg<IRawEntity>.Is.Anything, Arg<bool>.Is.Anything));
                this.indexStore.AssertWasNotCalled(
                    f => f.SaveEntity(Arg<IRawEntity>.Is.Anything, Arg<bool>.Is.Anything));
            }
        }

        /// <summary>Test that we can get a single entity by external id.</summary>
        [TestMethod]
        public void GetEntity()
        {
            // Set up data returned by stubs
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity());

            // Call the repository get single method
            var resultEntity = (EntityWrapperBase)this.entityRepository.GetEntity(this.requestContext, entity.ExternalEntityId);

            Assert.AreEqual(entity.ExternalEntityId, resultEntity.ExternalEntityId);
        }

        /// <summary>Test that we can get a single entity by external id honoring the IEntityFilter.</summary>
        [TestMethod]
        public void GetEntityWithEntityFilter()
        {
            // Set up data returned by stubs
            var existingEntity = TestEntityBuilder.BuildPartnerEntity();
            existingEntity.Properties.Add(new EntityProperty("foo", "foovalue", PropertyFilter.Extended));
            this.SetupExistingEntityStubs(existingEntity);

            // Call the repository get single method
            var context = new RequestContext { EntityFilter = new RepositoryEntityFilter(true, true, false, true) };
            var resultEntity = (EntityWrapperBase)this.entityRepository.GetEntity(context, existingEntity.ExternalEntityId);

            Assert.AreEqual(existingEntity.ExternalEntityId, resultEntity.ExternalEntityId);
            Assert.AreEqual(0, resultEntity.Properties.Count(p => p.Name == "foo"));
        }

        /// <summary>Test that we fail in the correct way if an entity key is not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void GetEntitiesByIdKeyNotFound()
        {
            // Setup for an entity that does not exist
            var entity = this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());

            this.entityRepository.GetEntitiesById(this.requestContext, new EntityId[] { entity.ExternalEntityId });
        }

        /// <summary>Test that we fail in the correct way if an entity is not found (but the key is).</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void GetEntitiesByIdNotFound()
        {
            // Setup for an entity that has a key but not entity
            var id = new EntityId();
            var existingKey = new AzureStorageKey("acc", "tab", "par", new EntityId());
            var indexEntity = new Entity { Key = existingKey };
            this.indexStore.Stub(f => f.GetEntity(null, null, null)).IgnoreArguments().Return(indexEntity);
            this.entityStore.Stub(f => f.GetEntityByKey(null, null)).IgnoreArguments().Return(null);

            this.entityRepository.GetEntitiesById(this.requestContext, new[] { id });
        }

        /// <summary>Test we can merge and update properties.</summary>
        [TestMethod]
        public void TryUpdateEntityProperties()
        {
            IRawEntity rawEntity = null;
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), true, x => rawEntity = x);
            var originalKeyCopy = new AzureStorageKey((AzureStorageKey)entity.Key);
            var properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.Default),
                    new EntityProperty("SomeProperty2", "SomeValue2", PropertyFilter.Extended),
                    new EntityProperty("SomeProperty3", "SomeValue3", PropertyFilter.System),
                };
            this.requestContext.EntityFilter = new RepositoryEntityFilter(true, true, true, true);
            var result = this.entityRepository.TryUpdateEntity(this.requestContext, entity.ExternalEntityId, properties);
            Assert.IsTrue(result);

            // Assert Index and Entity stores are called
            this.AssertUpdateEntity(rawEntity, originalKeyCopy, properties[0]);
            this.AssertUpdateEntity(rawEntity, originalKeyCopy, properties[1]);
            this.AssertUpdateEntity(rawEntity, originalKeyCopy, properties[2]);
        }

        /// <summary>Test we can merge and update properties and that entity filter in context is ignored.</summary>
        [TestMethod]
        public void TryUpdateEntityPropertiesIgnoreEntity()
        {
            IRawEntity rawEntity = null;
            var existingEntity = TestEntityBuilder.BuildPartnerEntity();
            var existingFilteredProperty = new EntityProperty("foo", "foovalue", PropertyFilter.Extended);
            existingEntity.Properties.Add(existingFilteredProperty);
            var updatedEntity = this.SetupExistingEntityStubs(existingEntity, true, x => rawEntity = x);
            var originalKeyCopy = new AzureStorageKey((AzureStorageKey)updatedEntity.Key);
            var properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.Default),
                    new EntityProperty("foo", "updatedfoovalue", PropertyFilter.Extended)
                };

            // The filter should be ignored for a forced update
            this.requestContext.EntityFilter = new RepositoryEntityFilter(true, true, false, true);
            var result = this.entityRepository.TryUpdateEntity(this.requestContext, updatedEntity.ExternalEntityId, properties);
            Assert.IsTrue(result);

            // Assert Index and Entity stores are called
            this.AssertUpdateEntity(rawEntity, originalKeyCopy, properties[0]);
            this.AssertUpdateEntity(rawEntity, originalKeyCopy, properties[1]);

            // Assert that the updated filtered property was sent down regardless of the entity filter
            // in the context
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.Properties.Count(p => p.Value == "updatedfoovalue") == 1),
                Arg<bool>.Is.Equal(true)));
        }

        /// <summary>We should fail when the entity does not exist.</summary>
        [TestMethod]
        public void TryUpdateEntityPropertiesDoesNotExist()
        {
            var properties = new List<EntityProperty>();
            var result = this.entityRepository.TryUpdateEntity(this.requestContext, new EntityId(), properties);
            Assert.IsFalse(result);
        }

        /// <summary>We should fail when the entity exists but save fails.</summary>
        [TestMethod]
        public void TryUpdateEntityPropertiesSaveFails()
        {
            var entity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity(), false, x => { });
            var properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.Default),
                    new EntityProperty("SomeProperty2", "SomeValue2", PropertyFilter.Extended),
                    new EntityProperty("SomeProperty3", "SomeValue3", PropertyFilter.System),
                };
            this.requestContext.EntityFilter = new RepositoryEntityFilter(true, true, true, true);
            var result = this.entityRepository.TryUpdateEntity(this.requestContext, entity.ExternalEntityId, properties);
            Assert.IsFalse(result);
        }

        /// <summary>We should fail when properties cannot be updated legitmately.</summary>
        [TestMethod]
        public void TryUpdateEntityPropertiesInvalidUpdate()
        {
            var existingEntity = TestEntityBuilder.BuildPartnerEntity();

            // Set up one existing property with a different filter so update will fail
            existingEntity.TrySetEntityProperty(
                new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.System));
            var entity = this.SetupExistingEntityStubs(existingEntity, false, x => { });

            var properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.Default),
                    new EntityProperty("SomeProperty2", "SomeValue2", PropertyFilter.Extended),
                    new EntityProperty("SomeProperty3", "SomeValue3", PropertyFilter.System),
                };
            this.requestContext.EntityFilter = new RepositoryEntityFilter(true, true, true, true);
            var result = this.entityRepository.TryUpdateEntity(this.requestContext, entity.ExternalEntityId, properties);
            Assert.IsFalse(result);
        }

        /// <summary>Verify entity associations are added to source entity.</summary>
        [TestMethod]
        public void AssociationEntities()
        {
            // Setup an existing source entity to be updated
            var sourceEntity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());

            // Setup a set of target entities to associate to the source
            var targetEntity1 = TestEntityBuilder.BuildCompanyEntity();
            targetEntity1.ExternalType = "Foo";
            var targetEntity2 = TestEntityBuilder.BuildCompanyEntity();
            targetEntity2.ExternalType = "Foo";
            var targetEntities = new HashSet<IEntity> { targetEntity1, targetEntity2 };

            var resultEntity = this.entityRepository.AssociateEntities(
                this.requestContext, sourceEntity.ExternalEntityId, "Foos", "Details", targetEntities, AssociationType.Child, false);

            this.AssertUpdateEntity(sourceEntity);
            Assert.AreEqual(2, resultEntity.Associations.Count);
            var actualAssociation =
                resultEntity.Associations.Single(a => a.TargetEntityId == (EntityId)targetEntity1.ExternalEntityId);
            Assert.AreEqual((string)targetEntity1.EntityCategory, actualAssociation.TargetEntityCategory);
            Assert.AreEqual("Foos", actualAssociation.ExternalName);
            Assert.AreEqual("Foo", actualAssociation.TargetExternalType);
            Assert.AreEqual("Details", actualAssociation.Details);
            Assert.AreEqual(AssociationType.Child, actualAssociation.AssociationType);
        }

        /// <summary>Test that we can create a new user in the system.</summary>
        [TestMethod]
        public void SaveNewUserDefaultCompanyExists()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build user and call repository
            var userEntity = (UserEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildUserEntity());
            this.entityRepository.SaveUser(this.requestContext, userEntity);
            
            // Assert Index and Entity stores are called
            this.AssertSaveNewEntity(userEntity);
        }

        /// <summary>Test that trying to save a new user with the same userid but different entity id fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SaveNewUserWithDuplicateUserIdFails()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build existing user entity and setup stub to return it.
            var existingUserEntity = TestEntityBuilder.BuildUserEntity();
            existingUserEntity.Key = new AzureStorageKey("acc", "tab", "par", new EntityId(), 0, DateTime.UtcNow);
            this.entityStore.Stub(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Anything, Arg<IStorageKey>.Is.Anything)).Return(
                new HashSet<IRawEntity> { existingUserEntity.WrappedEntity });

            // Set up the index to return the correct current versions for the existing user entity
            this.indexStore.Stub(f => f.GetStorageKey(
                existingUserEntity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount)).Return(existingUserEntity.Key);
            
            // Build new user with same UserId but different entity id (and set up stubs for it)
            var newUserEntity = (UserEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildUserEntity());
            
            // Call repository
            this.entityRepository.SaveUser(this.requestContext, newUserEntity);
        }
        
        /// <summary>Test that we can update an existing user in the system.</summary>
        [TestMethod]
        public void UpdateUser()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build user and call repository save
            var entity = (UserEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildUserEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)entity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            entity.Properties.Add(newProperty);
            this.entityRepository.SaveUser(this.requestContext, entity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(entity, originalKey, newProperty);
        }

        /// <summary>Test that we can update an existing user and use the IEntityFilter in the context.</summary>
        [TestMethod]
        public void UpdateUserWithEntityFilter()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build user and call repository save
            var user = TestEntityBuilder.BuildUserEntity();
            var existingFilteredProperty = new EntityProperty("foo", "foovalue", PropertyFilter.Extended);
            user.Properties.Add(existingFilteredProperty);
            var updatedUser = (UserEntity)this.SetupExistingEntityStubs(user);
            var originalKey = new AzureStorageKey((AzureStorageKey)updatedUser.Key);
            var newProperty = new EntityProperty("stuff", "stuffvalue");
            updatedUser.Properties.Add(newProperty);

            // Because of the filter this value should not get saved
            updatedUser.Properties.Single(p => p.Name == "foo").Value = "updatedfoovalue";

            var context = new RequestContext { EntityFilter = new RepositoryEntityFilter(true, true, false, false) };
            this.entityRepository.SaveUser(context, updatedUser);

            // Assert Index and Entity stores are called for each entity and new property is present
            this.AssertUpdateEntity(updatedUser, originalKey, newProperty);

            // Assert the original value of the filtered property was retained
            this.AssertUpdateEntity(updatedUser, originalKey, existingFilteredProperty);

            // Assert that the updated filtered property value was not sent down
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.Properties.Count(p => p.Value == "updatedfoovalue") == 0),
                Arg<bool>.Is.Equal(true)));
        }

        /// <summary>Test that we can get a user in the system by their user id.</summary>
        [TestMethod]
        public void GetUserById()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build user entity and setup stub to return it.
            var userEntity = TestEntityBuilder.BuildUserEntity();
            userEntity.Key = new AzureStorageKey("acc", "tab", "par", new EntityId(), 0, DateTime.UtcNow);
            this.entityStore.Stub(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Anything, Arg<IStorageKey>.Is.Anything)).Return(
                new HashSet<IRawEntity> { userEntity.WrappedEntity });

            // Set up the index to return the correct current versions
            this.indexStore.Stub(f => f.GetStorageKey(
                userEntity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount)).Return(userEntity.Key);

            var entity = this.entityRepository.GetUser(this.requestContext, userEntity.UserId);

            Assert.AreSame(userEntity.WrappedEntity, entity.WrappedEntity);
            this.entityStore.AssertWasCalled(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Equal((string)userEntity.UserId), Arg<IStorageKey>.Is.Anything));
        }

        /// <summary>Test that we can get a user in the system by their user id when there is an orphaned version.</summary>
        [TestMethod]
        public void GetUserByIdHandlesOrphanedEntity()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Set up a single entity id with two entities - one orphaned
            var entityId = new EntityId();
            var key1 = new AzureStorageKey("acc", "tab", "par", new EntityId(), 0, DateTime.UtcNow);
            var key1Orphan = new AzureStorageKey(key1) { RowId = new EntityId() };
            var userEntity = TestEntityBuilder.BuildUserEntity(entityId);
            userEntity.UserId = "foo";
            userEntity.Key = key1;
            var userEntityOrphan = TestEntityBuilder.BuildUserEntity(entityId);
            userEntityOrphan.Key = key1Orphan;

            // Set up the stub for the entity store call to return these entities
            var entities = new HashSet<IRawEntity> { userEntity, userEntityOrphan };
            this.entityStore.Stub(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Anything, Arg<IStorageKey>.Is.Anything)).Return(entities);

            // Set up the index to return the correct current versions
            this.indexStore.Stub(f => f.GetStorageKey(
                userEntity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount)).Return(userEntity.Key);

            var entity = this.entityRepository.GetUser(this.requestContext, userEntity.UserId);

            Assert.AreSame(userEntity.WrappedEntity, entity.WrappedEntity);
            this.entityStore.AssertWasCalled(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Equal((string)userEntity.UserId), Arg<IStorageKey>.Is.Anything));
        }
        
        /// <summary>Test that getting a user that was not found fails correctly.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetUserNotFound()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Setup stub to return null.
            this.entityStore.Stub(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Anything, Arg<IStorageKey>.Is.Anything)).Return(new HashSet<IRawEntity>());

            // Call repository with bogus user id
            this.entityRepository.GetUser(this.requestContext, "bogususerid");
        }

        /// <summary>Test that we can get a user in the system by their ExternalEntityId.</summary>
        [TestMethod]
        public void GetUserByEntityId()
        {
            // Set up data returned by stubs
            var userEntity = this.SetupExistingEntityStubs(TestEntityBuilder.BuildUserEntity());

            // Call the repository get method
            this.entityRepository.GetEntity(this.requestContext, userEntity.ExternalEntityId);

            // Assert the datastore was called correctly
            this.indexStore.AssertWasCalled(f => f.GetEntity(userEntity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount, null));
            this.entityStore.AssertWasCalled(f => f.GetEntityByKey(Arg<RequestContext>.Is.Anything, Arg<IStorageKey>.Is.Equal(userEntity.Key)));
        }

        /// <summary>
        /// Test that we can get a user with properties filtered by the
        /// IEntityFilter in the context.
        /// </summary>
        [TestMethod]
        public void GetUserByIdWithEntityFiltered()
        {
            // Setup stub to return default company key
            this.SetupDefaultCompanyStub();

            // Build user entity and setup stub to return it.
            var user = TestEntityBuilder.BuildUserEntity();
            user.Properties.Add(new EntityProperty("stuff", "stuffvalue"));
            user.Properties.Add(new EntityProperty("foo", "foovalue", PropertyFilter.Extended));
            user.Key = new AzureStorageKey("acc", "tab", "par", new EntityId(), 0, DateTime.UtcNow);

            this.entityStore.Stub(f => f.GetUserEntitiesByUserId(Arg<string>.Is.Anything, Arg<IStorageKey>.Is.Anything)).Return(
                new HashSet<IRawEntity> { user.WrappedEntity });

            // Set up the index to return the correct current versions
            this.indexStore.Stub(f => f.GetStorageKey(
                user.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount)).Return(user.Key);

            var context = new RequestContext { EntityFilter = new RepositoryEntityFilter(true, true, false, true) };
            var resultUser = this.entityRepository.GetUser(context, user.UserId);

            // Assert the datastore was called correctly
            Assert.AreEqual(0, resultUser.Properties.Count(p => p.IsExtendedProperty));
            Assert.AreEqual(1, resultUser.Properties.Count(p => p.Name == "stuff"));
        }

        /// <summary>Test that we can create a new Company in the system.</summary>
        [TestMethod]
        public void AddNewCompany()
        {
            // Setup a Company Entity
            var companyEntity = TestEntityBuilder.BuildCompanyEntity();
                
            // Setup entity store to return a partial key with table name on SetupNewCompany
            var partialKey = new AzureStorageKey(null, "tableFoo", null, null);
            this.entityStore.Stub(f => f.SetupNewCompany(Arg<string>.Is.Anything)).Return(partialKey);

            // Setup key factory to return full key
            var fullKey = new AzureStorageKey("acc", "tableFoo", "par", new EntityId());
            this.storageKeyFactory.Stub(f => f.BuildNewStorageKey(
                Arg<string>.Is.Anything, Arg<EntityId>.Is.Anything, Arg<IRawEntity>.Is.Anything)).Return(fullKey);

            // Set up stubs
            this.entityStore.Stub(f => f.GetEntityByKey(null, null)).IgnoreArguments().Return(null);
            this.indexStore.Stub(f => f.SaveEntity(null, false)).IgnoreArguments();
            this.entityStore.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything, 
                Arg<IRawEntity>.Is.Anything, 
                Arg<bool>.Is.Equal(false)))
                .Return(true);

            var context = new RequestContext();
            this.entityRepository.AddCompany(context, companyEntity);

            // Assert entity and index store are called with correct values
            this.entityStore.AssertWasCalled(f => f.SetupNewCompany(Arg<string>.Is.Anything));
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == companyEntity.ExternalEntityId),
                Arg<bool>.Is.Equal(false)));
            this.indexStore.AssertWasCalled(f => f.SaveEntity(
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == companyEntity.ExternalEntityId),
                Arg<bool>.Is.Equal(false)));
        }

        /// <summary>Test that we can get Companies in the system by their external entity id.</summary>
        [TestMethod]
        public void GetCompanies()
        {
            // Set up data returned by stubs
            var company1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());
            var company2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());

            // Call the repository get method
            var entityIds = new EntityId[] { company1.ExternalEntityId, company2.ExternalEntityId };
            var entities = this.entityRepository.GetEntitiesById(this.requestContext, entityIds);

            // Assert the returned entity collection is populated
            this.AssertGetEntities(company1, company2, entities);
        }

        /// <summary>Test that we can get all the active entities of a category in the system.</summary>
        [TestMethod]
        public void GetFilteredEntityIds()
        {
            // Set up data returned by stubs
            var company1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());
            var company2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());
            var externalType = "Agency";

            // Set up the index to return the company1 and company2 entity ids.
            this.indexStore.Stub(f => f.GetEntityInfoByCategory(CompanyEntity.CompanyEntityCategory)).Return(
                new List<IRawEntity> 
                {
                    new Entity { ExternalEntityId = company1.ExternalEntityId, ExternalType = externalType },
                    new Entity { ExternalEntityId = company2.ExternalEntityId, ExternalType = externalType } 
                });

            // Call the repository get method
            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, CompanyEntity.CompanyEntityCategory);
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.ExternalTypeFilter, externalType);
            var entities = this.entityRepository.GetFilteredEntityIds(filterContext).ToList();

            Assert.AreEqual(2, entities.Count());
            Assert.AreEqual(1, entities.Count(e => e == (EntityId)company1.ExternalEntityId));
            Assert.AreEqual(1, entities.Count(e => e == (EntityId)company2.ExternalEntityId));
        }

        /// <summary>GetFilteredEntityIds throws if no category is provided.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void GetFilteredEntityIdsNoCategory()
        {
            // Call the repository get method
            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            this.entityRepository.GetFilteredEntityIds(filterContext);
        }

        /// <summary>Test that we can update an existing Company in the system.</summary>
        [TestMethod]
        public void UpdateCompany()
        {
            // Build company and call repository save
            var companyEntity = (CompanyEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildCompanyEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)companyEntity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            companyEntity.Properties.Add(newProperty);
            this.entityRepository.SaveEntity(this.requestContext, companyEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(companyEntity, originalKey, newProperty);
        }
        
        /// <summary>Test that we can get Campaigns by external id.</summary>
        [TestMethod]
        public void GetCampaigns()
        {
            // Set up data returned by stubs
            var campaign1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCampaignEntity());
            var campaign2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCampaignEntity());

            // Call the repository get method
            var entityIds = new EntityId[] { campaign1.ExternalEntityId, campaign2.ExternalEntityId };
            var entities = this.entityRepository.GetEntitiesById(this.requestContext, entityIds);

            // Assert the returned entity collection is populated
            this.AssertGetEntities(campaign1, campaign2, entities);
        }

        /// <summary>Test that we can create a new Campaign in the system.</summary>
        [TestMethod]
        public void SaveNewCampaign()
        {
            // Build entity and call repository save
            var campaignEntity = (CampaignEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildCampaignEntity());
            this.entityRepository.SaveEntity(this.requestContext, campaignEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertSaveNewEntity(campaignEntity);
        }

        /// <summary>Test that we can update an existing Campaign in the system.</summary>
        [TestMethod]
        public void UpdateCampaign()
        {
            // Build creative and call repository save
            var campaignEntity = (CampaignEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildCampaignEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)campaignEntity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            campaignEntity.Properties.Add(newProperty);

            this.entityRepository.SaveEntity(this.requestContext, campaignEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(campaignEntity, originalKey, newProperty);
        }

        /// <summary>Test that we can get Creatives by external id.</summary>
        [TestMethod]
        public void GetCreatives()
        {
            // Set up data returned by stubs
            var creative1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCreativeEntity());
            var creative2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildCreativeEntity());
            
            // Call the repository get method
            var entityIds = new EntityId[] { creative1.ExternalEntityId, creative2.ExternalEntityId };
            var entities = this.entityRepository.GetEntitiesById(this.requestContext, entityIds);

            // Assert the returned entity collection is populated
            this.AssertGetEntities(creative1, creative2, entities);
        }

        /// <summary>Test that we can save a new Creative.</summary>
        [TestMethod]
        public void SaveNewCreative()
        {
            // Build creative and call repository save
            var creative = (CreativeEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildCreativeEntity());
            this.entityRepository.SaveEntity(this.requestContext, creative);

            // Assert Index and Entity stores are called for each entity
            this.AssertSaveNewEntity(creative);
        }
        
        /// <summary>Test that we can update an existing Creative.</summary>
        [TestMethod]
        public void UpdateCreative()
        {
            // Build creative and call repository save
            var creative = (CreativeEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildCreativeEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)creative.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            creative.Properties.Add(newProperty);
            this.entityRepository.SaveEntity(this.requestContext, creative);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(creative, originalKey, newProperty);
        }

        /// <summary>Test that we can get Partner entities by external id.</summary>
        [TestMethod]
        public void GetPartnerEntities()
        {
            // Set up data returned by stubs
            var partner1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var partner2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity());

            // Call the repository get method
            var entityIds = new EntityId[] { partner1.ExternalEntityId, partner2.ExternalEntityId };
            var entities = this.entityRepository.GetEntitiesById(this.requestContext, entityIds);

            // Assert the returned entity collection is populated
            this.AssertGetEntities(partner1, partner2, entities);
        }

        /// <summary>Test that we can save a new Partner Entity.</summary>
        [TestMethod]
        public void SaveNewPartnerEntities()
        {
            // Build partner entity and call repository save
            var partnerEntity = (PartnerEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            this.entityRepository.SaveEntity(this.requestContext, partnerEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertSaveNewEntity(partnerEntity);
        }

        /// <summary>Test that we can update an existing Partner Entity.</summary>
        [TestMethod]
        public void UpdatePartnerEntities()
        {
            // Build PartnerEntity and call repository save
            var partnerEntity = (PartnerEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildPartnerEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)partnerEntity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            partnerEntity.Properties.Add(newProperty);
            this.entityRepository.SaveEntity(this.requestContext, partnerEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(partnerEntity, originalKey, newProperty);
        }

        /// <summary>
        /// Test that we can save an update to an existing Entity honoring
        /// the EntityFilter in the context.
        /// </summary>
        [TestMethod]
        public void UpdateEntityWithEntityFilter()
        {
            // Build PartnerEntity and call repository save
            var entity = TestEntityBuilder.BuildPartnerEntity();
            var existingFilteredProperty = new EntityProperty("foo", "foovalue", PropertyFilter.Extended);
            entity.Properties.Add(existingFilteredProperty);

            var updatedEntity = (PartnerEntity)this.SetupExistingEntityStubs(entity);
            var originalKey = new AzureStorageKey((AzureStorageKey)updatedEntity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            updatedEntity.Properties.Add(newProperty);
            
            // Because of the filter this value should not get saved
            updatedEntity.Properties.Single(p => p.Name == "foo").Value = "updatedfoovalue";

            var context = new RequestContext { EntityFilter = new RepositoryEntityFilter(true, true, false, false) };
            this.entityRepository.SaveEntity(context, updatedEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(updatedEntity, originalKey, newProperty);

            // Assert the original value of the filtered property was retained
            this.AssertUpdateEntity(updatedEntity, originalKey, existingFilteredProperty);

            // Assert that the updated filtered property value was not sent down
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.Properties.Count(p => p.Value == "updatedfoovalue") == 0),
                Arg<bool>.Is.Equal(true)));
        }

        /// <summary>Test that we can get Blob entities by external id.</summary>
        [TestMethod]
        public void GetBlobEntities()
        {
            // Set up data returned by stubs
            var blob1 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildBlobEntity());
            var blob2 = this.SetupExistingEntityStubs(TestEntityBuilder.BuildBlobEntity());

            // Call the repository get method
            var entityIds = new EntityId[] { blob1.ExternalEntityId, blob2.ExternalEntityId };
            var entities = this.entityRepository.GetEntitiesById(this.requestContext, entityIds);

            // Assert the returned entity collection is populated
            this.AssertGetEntities(blob1, blob2, entities);
        }

        /// <summary>Test that we can save a new Blob Entity.</summary>
        [TestMethod]
        public void SaveNewBlobEntities()
        {
            // Build blob entity and call repository save
            var blobEntity = (BlobEntity)this.SetupNewEntityStubs(TestEntityBuilder.BuildBlobEntity());
            this.entityRepository.SaveEntity(this.requestContext, blobEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertSaveNewEntity(blobEntity);
        }

        /// <summary>Test that we can update an existing Blob Entity.</summary>
        [TestMethod]
        public void UpdateBlobEntities()
        {
            // Build BlobEntity and call repository save
            var blobEntity = (BlobEntity)this.SetupExistingEntityStubs(TestEntityBuilder.BuildBlobEntity());
            var originalKey = new AzureStorageKey((AzureStorageKey)blobEntity.Key);
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, 1));
            blobEntity.Properties.Add(newProperty);
            this.entityRepository.SaveEntity(this.requestContext, blobEntity);

            // Assert Index and Entity stores are called for each entity
            this.AssertUpdateEntity(blobEntity, originalKey, newProperty);
        }

        /// <summary>Saving a legacy BlobEntity through IEntityRepository is not allowed.</summary>
        [TestMethod]
        public void SaveLegacyBlobEntityNotAllowed()
        {
            // Save the blob entity
            var blobPropertyEntity = new BlobPropertyEntity(new EntityId()) as IEntity;
            var saveSucceeded = this.entityRepository.TrySaveEntity(this.requestContext, blobPropertyEntity);
            Assert.IsFalse(saveSucceeded);
        }
        
        /// <summary>Test that SaveEntities fails for blob entities with no bytes.</summary>
        [TestMethod]
        public void SaveLegacyBlobEntityWithNoBytes()
        {
            var blobEntity = new BlobPropertyEntity(new EntityId()) as IEntity;
            var saveSucceeded = this.entityRepository.TrySaveEntity(this.requestContext, blobEntity);
            Assert.IsFalse(saveSucceeded);
        }

        /// <summary>An incoming property that may already be a blob ref should be validated.</summary>
        [TestMethod]
        public void VirtualizePropertyValidatesIncomingBlobRefProperty()
        {
            var context = this.requestContext;
            context.EntityFilter = new RepositoryEntityFilter(true, true, true, true);

            var incomingProperty = new EntityProperty
            {
                Name = "someProperty",
                Filter = PropertyFilter.Default,
                IsBlobRef = true,
                Value = "smallstring"
            };

            // property should not end up being a blob ref
            var virtualizedProperty = ConcreteEntityRepository.VirtualizeProperty(context, incomingProperty, this.blobStore);
            Assert.IsFalse(virtualizedProperty.IsBlobRef);
            Assert.AreEqual((string)incomingProperty.Value, (string)virtualizedProperty.Value);

            // reset for a valid blob ref
            incomingProperty.IsBlobRef = true;
            incomingProperty.Value = "{\"ContainerName\":\"container\",\"BlobId\":\"123\",\"StorageAccountName\":\"account\",\"VersionTimestamp\":null,\"LocalVersion\":0}";
            virtualizedProperty = ConcreteEntityRepository.VirtualizeProperty(context, incomingProperty, this.blobStore);
            Assert.IsTrue(virtualizedProperty.IsBlobRef);
            Assert.AreEqual((string)incomingProperty.Value, (string)virtualizedProperty.Value);
        }

        /// <summary>An outgoing heavy property that has been de-referenced should not be marked as a blob ref.</summary>
        [TestMethod]
        public void RealizedPropertyClearsBlobRef()
        {
            var context = this.requestContext;
            context.EntityFilter = new RepositoryEntityFilter(true, true, true, true);

            var outgoingProperty = new EntityProperty
            {
                Name = "someProperty",
                Filter = PropertyFilter.Default,
                IsBlobRef = true,
                Value = "{\"ContainerName\":\"container\",\"BlobId\":\"123\",\"StorageAccountName\":\"account\",\"VersionTimestamp\":null,\"LocalVersion\":0}"
            };

            var blobBytes = new byte[] { 0x01 };
            var blobEntity = new BlobPropertyEntity(new EntityId(), blobBytes);
            this.blobStore.Stub(f => f.GetBlobByKey(Arg<IStorageKey>.Is.Anything)).Return(blobEntity);

            var realizedProperty = ConcreteEntityRepository.RealizeProperty(context, outgoingProperty, this.blobStore);
            Assert.IsFalse(realizedProperty.IsBlobRef);
            Assert.AreEqual(blobBytes[0], ((byte[])realizedProperty.Value)[0]);
        }

        /// <summary>
        /// An outgoing property that was persisted with the blob ref marker but is not will
        /// be optimistically treated as a normal property.
        /// </summary>
        [TestMethod]
        public void InvalidOutgoingBlobRefsTreated()
        {
            var context = this.requestContext;
            context.EntityFilter = new RepositoryEntityFilter(true, true, true, true);

            var outgoingProperty = new EntityProperty
                {
                    Name = "someProperty", 
                    Filter = PropertyFilter.Default, 
                    IsBlobRef = true,
                    Value = "Not a blob reference."
                };

            var realizedProperty = ConcreteEntityRepository.RealizeProperty(context, outgoingProperty, this.blobStore);
            Assert.IsFalse(realizedProperty.IsBlobRef);
            Assert.AreEqual((string)outgoingProperty.Value, (string)realizedProperty.Value);
        }

        /// <summary>Set entity to active/inactive.</summary>
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

            this.indexStore.Stub(f => f.SetEntityStatus(
                Arg<HashSet<EntityId>>.Is.Anything, Arg<bool>.Is.Anything))
                .WhenCalled(call => captureArgs((HashSet<EntityId>)call.Arguments[0], (bool)call.Arguments[1]));

            var entityIds = new HashSet<EntityId> { new EntityId(), new EntityId() };
            this.entityRepository.SetEntityStatus(this.requestContext, entityIds, true);
            Assert.IsTrue(activeArg);
            Assert.IsTrue(entityIdArgs.SequenceEqual(entityIds));

            this.entityRepository.SetEntityStatus(this.requestContext, entityIds, false);
            Assert.IsFalse(activeArg);
        }

        /// <summary>Happy-path get entity at version.</summary>
        [TestMethod]
        public void GetEntityAtVersion()
        {
            // Set up a Partner Entity
            var entityVersion = 1;
            var entity = TestEntityBuilder.BuildPartnerEntity(new EntityId());
            entity.LocalVersion = entityVersion;
            this.SetupExistingEntityStubs(entity, true, true, x => { }, true, entityVersion);

            PropertyValue version = entityVersion;
            this.requestContext.EntityFilter = new RepositoryEntityFilter();
            this.requestContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.VersionFilter, version.SerializationValue);
            var entityVer1 = this.entityRepository.GetEntity(this.requestContext, entity.ExternalEntityId);

            Assert.AreEqual(entityVersion, entityVer1.Key.LocalVersion);
        }

        /// <summary>Copy the properties of a source entity to a target entity.</summary>
        /// <param name="sourceEntity">The source entity.</param>
        /// <returns>The new entity.</returns>
        private static IRawEntity CopyEntity(IRawEntity sourceEntity)
        {
            var newEntity = new Entity();
            newEntity.Key = sourceEntity.Key;

            foreach (var property in sourceEntity.InterfaceProperties)
            {
                newEntity.InterfaceProperties.Add(new EntityProperty(property));
            }

            foreach (var property in sourceEntity.Properties)
            {
                newEntity.Properties.Add(new EntityProperty(property));
            }

            foreach (var association in sourceEntity.Associations)
            {
                newEntity.Associations.Add(new Association(association));
            }

            return newEntity;
        }

        /// <summary>Helper method to build the DAL stubs for an existing entity.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="indexSaveSuccess">True if index save should return success.</param>
        /// <returns>The entity passback.</returns>
        private IEntity SetupExistingEntityStubs(EntityWrapperBase entity, bool indexSaveSuccess = true)
        {
            return this.SetupExistingEntityStubs(entity, indexSaveSuccess, x => { });
        }

        /// <summary>Helper method to build the DAL stubs for an existing entity.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="indexSaveSuccess">True if index save should return success.</param>
        /// <param name="captureEntity">Action to capture the saved entity.</param>
        /// <returns>The entity passback.</returns>
        private IEntity SetupExistingEntityStubs(EntityWrapperBase entity, bool indexSaveSuccess, Action<IRawEntity> captureEntity)
        {
            return this.SetupExistingEntityStubs(entity, true, indexSaveSuccess, captureEntity, true);
        }

        /// <summary>Helper method to build the DAL stubs for an existing entity.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entitySaveSuccess">True if entity save should return success.</param>
        /// <param name="indexSaveSuccess">True if index save should return success.</param>
        /// <param name="captureEntity">Action to capture the saved entity.</param>
        /// <param name="entityGetSuccess">True if entity get should succeed.</param>
        /// <returns>The entity passback.</returns>
        private IEntity SetupExistingEntityStubs(
            EntityWrapperBase entity, bool entitySaveSuccess, bool indexSaveSuccess, Action<IRawEntity> captureEntity, bool entityGetSuccess)
        {
            return this.SetupExistingEntityStubs(
                entity, entitySaveSuccess, indexSaveSuccess, captureEntity, entityGetSuccess, null);
        }

        /// <summary>Helper method to build the DAL stubs for an existing entity.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entitySaveSuccess">True if entity save should return success.</param>
        /// <param name="indexSaveSuccess">True if index save should return success.</param>
        /// <param name="captureEntity">Action to capture the saved entity.</param>
        /// <param name="entityGetSuccess">True if entity get should succeed.</param>
        /// <param name="version">Optional version (null for default behavior)</param>
        /// <returns>The entity passback.</returns>
        private IEntity SetupExistingEntityStubs(
            EntityWrapperBase entity, bool entitySaveSuccess, bool indexSaveSuccess, Action<IRawEntity> captureEntity, bool entityGetSuccess, int? version)
        {
            var localVersion = version.HasValue ? version.Value : 0;
            var existingKey = new AzureStorageKey("acc", "tab", "par", entity.ExternalEntityId);
            existingKey.LocalVersion = localVersion;
            entity.Key = existingKey;
            entity.LocalVersion = localVersion;
            entity.LastModifiedDate = DateTime.Now;
            entity.CreateDate = DateTime.Now;
            entity.LastModifiedUser = "123abc";

            this.entityStore.Stub(
                f => f.GetEntityByKey(
                    Arg<RequestContext>.Is.Anything, 
                    Arg<IStorageKey>.Is.Equal(existingKey)))
                    .WhenCalled(a => a.ReturnValue = entityGetSuccess 
                        ? CopyEntity(entity) : null) // return a new copy every call
                        .Return(null); // return ignored

            // Save should just return the entity passed in
            this.entityStore.Stub(
                f =>
                f.SaveEntity(
                    Arg<RequestContext>.Is.Anything, 
                    Arg<IRawEntity>.Is.Anything, 
                    Arg<bool>.Is.Equal(true)))
                    .Return(true)
                    .WhenCalled(a =>
                        {
                            a.ReturnValue = entitySaveSuccess;
                            captureEntity((IRawEntity)a.Arguments[1]);
                        });

            // For updating entities set the factory to return an updated key
            var updatedKey = new AzureStorageKey(existingKey);
            updatedKey.RowId = new EntityId();
            this.storageKeyFactory.Stub(f => f.BuildUpdatedStorageKey(null, null)).IgnoreArguments().Return(updatedKey);

            if (indexSaveSuccess)
            {
                this.indexStore.Stub(f => f.SaveEntity(null, true)).IgnoreArguments();
            }
            else
            {
                this.indexStore.Stub(f => f.SaveEntity(null, true)).IgnoreArguments().Throw(new DataAccessException());
            }

            // Setup default index gets based on entity passed in
            this.indexStore.Stub(f => f.GetEntity(entity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount, version))
                .Return(entity.WrappedEntity);
            this.indexStore.Stub(f => f.GetStorageKey(entity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount))
                .Return(entity.Key);

            // Return a copy of the existing entity to be used by the caller for updating
            return EntityWrapperBase.BuildWrappedEntity(CopyEntity(entity));
        }

        /// <summary>Helper method to build the DAL stubs for a new entity.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="saveFail">True if save should fail (throw).</param>
        /// <returns>The entity passback.</returns>
        private IEntity SetupNewEntityStubs(IEntity entity, bool saveFail = false)
        {
            var key = new AzureStorageKey("acc", "tab", "par", new EntityId());
            entity.Key = key;
            entity.LocalVersion = 0;
            entity.LastModifiedDate = DateTime.Now;
            entity.CreateDate = DateTime.Now;

            // For new entities set the stubs to return a null key and build a new key
            this.indexStore.Stub(f => f.GetStorageKey(
                entity.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount)).Return(null);

            this.storageKeyFactory.Stub(f => f.BuildNewStorageKey(
                Arg<string>.Is.Anything, 
                Arg<EntityId>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId))).Return(key);

            // Save should just return the entity passed in
            if (!saveFail)
            {
                this.entityStore.Stub(f => f.SaveEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId),
                    Arg<bool>.Is.Equal(false)))
                    .Return(true);
            }
            else
            {
                this.entityStore.Stub(f => f.SaveEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId),
                    Arg<bool>.Is.Equal(false)))
                    .Throw(new StorageClientException());
            }

            this.indexStore.Stub(f => f.SaveEntity(null)).IgnoreArguments();

            return entity;
        }

        /// <summary>Set up a stub to return the default company key.</summary>
        private void SetupDefaultCompanyStub()
        {
            // Setup key factory stub for default company key
            var defaultCompanyKey = new AzureStorageKey("acc", "tab", "par", ConcreteEntityRepository.DefaultCompanyId);
            this.indexStore.Stub(f => f.GetStorageKey(
                ConcreteEntityRepository.DefaultCompanyId, ConcreteEntityRepository.DefaultStorageAccount))
                .Return(defaultCompanyKey);
        }
        
        /// <summary>Helper method to assert that saving a new entity correctly calls the datastore methods.</summary>
        /// <param name="entity">The entity.</param>
        private void AssertSaveNewEntity(IRawEntity entity)
        {
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId),
                Arg<bool>.Is.Equal(false)));
            
            this.indexStore.AssertWasCalled(f =>
                f.SaveEntity(Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId), Arg<bool>.Is.Equal(false)));
            Assert.IsNotNull(entity.Key);
        }

        /// <summary>
        /// Helper method to assert that updating an existing entity correctly calls the datastore methods.
        /// Verifies the new property is present.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="originalKey">Original key of the entity that was updated.</param>
        /// <param name="newProperty">A property that is being added to the entity.</param>
        private void AssertUpdateEntity(IRawEntity entity, AzureStorageKey originalKey, EntityProperty newProperty)
        {
            Assert.AreNotEqual(entity.LocalVersion, originalKey.LocalVersion);
            Assert.AreNotEqual(((AzureStorageKey)entity.Key).RowId, originalKey.RowId);

            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.Properties.Count(p => p.Name == newProperty.Name) == 1), 
                Arg<bool>.Is.Equal(true)));
            this.indexStore.AssertWasCalled(f =>
                f.SaveEntity(Arg<IRawEntity>.Matches(e => e.Properties.Count(p => p.Name == newProperty.Name) == 1), Arg<bool>.Is.Equal(true)));
            Assert.IsNotNull(entity.Key);
        }
        
        /// <summary>Helper method to assert that updating an existing entity correctly calls the datastore methods.</summary>
        /// <param name="entity">The entity.</param>
        private void AssertUpdateEntity(IRawEntity entity)
        {
            this.entityStore.AssertWasCalled(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId),
                Arg<bool>.Is.Equal(true)));
            this.indexStore.AssertWasCalled(f => f.SaveEntity(
                Arg<IRawEntity>.Matches(e => e.ExternalEntityId == entity.ExternalEntityId),
                Arg<bool>.Is.Equal(true)));
        }

        /// <summary>Helper method to assert that a list of multiple requested entities (two for the test) is correctly returned.</summary>
        /// <typeparam name="T">Type of entity we are getting.</typeparam>
        /// <param name="expectedEntity1">The expected entity 1.</param>
        /// <param name="expectedEntity2">The expected entity 2.</param>
        /// <param name="actualEntities">The actual entities.</param>
        private void AssertGetEntities<T>(IRawEntity expectedEntity1, IRawEntity expectedEntity2, HashSet<T> actualEntities)
        {
            this.indexStore.AssertWasCalled(f => f.GetEntity(
                expectedEntity1.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount, null));
            this.indexStore.AssertWasCalled(f => f.GetEntity(
                expectedEntity2.ExternalEntityId, ConcreteEntityRepository.DefaultStorageAccount, null));
            this.entityStore.AssertWasCalled(f => f.GetEntityByKey(
                Arg<RequestContext>.Is.Anything,
                Arg<IStorageKey>.Is.Equal(expectedEntity1.Key)));
            this.entityStore.AssertWasCalled(f => f.GetEntityByKey(
                Arg<RequestContext>.Is.Anything,
                Arg<IStorageKey>.Is.Equal(expectedEntity2.Key)));
            Assert.AreEqual(2, actualEntities.Count);
        }

        /// <summary>Simple blob interface testing type...not trying to test PersistentDictionary here.</summary>
        [DataContract]
        internal class TestBlobType
        {
            /// <summary>Gets or sets Foo.</summary>
            [DataMember]
            public int Foo { get; set; }

            /// <summary>Gets or sets Bar.</summary>
            [DataMember]
            public string Bar { get; set; }
        }
    }
}
