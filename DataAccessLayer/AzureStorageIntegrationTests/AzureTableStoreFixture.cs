// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureTableStoreFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using AzureUtilities.Storage;
using ConcreteDataStore;
using ConcreteDataStoreUnitTests;
using DataAccessLayer;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Storage;

using IEntityFilter = DataAccessLayer.IEntityFilter;

namespace AzureStorageIntegrationTests
{
    /// <summary>
    /// Integration tests for Azure Table Storage DAL
    /// </summary>
    [TestClass]
    public class AzureTableStoreFixture
    {
        /// <summary>Default company for testing (created once for the test run).</summary>
        private static EntityId defaultTestCompanyId = null;

        /// <summary>Default association target for testing (created once for the test run).</summary>
        private static EntityId defaultAssocTargetId1 = null;

        /// <summary>Default request context for testing.</summary>
        private RequestContext requestContext;

        /// <summary>IndexStoreFactory for testing.</summary>
        private IIndexStoreFactory indexStoreFactory;

        /// <summary>EntityStoreFactory for testing.</summary>
        private IEntityStoreFactory entityStoreFactory;

        /// <summary>IBlobStoreFactory for testing.</summary>
        private IBlobStoreFactory blobStoreFactory;

        /// <summary>StorageKeyFactory for testing.</summary>
        private AzureStorageKeyFactory storageKeyFactory;

        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>Initialize Azure storage emulator.</summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialization(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            // Force Azure emulated storage to start. DSService can still be running
            // but the emulated storage not available. The most reliable way to make sure
            // it's running and available is to stop it then start again.
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);
        }

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            PersistentDictionaryFactory.Initialize(new[]
                {
                    new CloudBlobDictionaryFactory(ConfigurationManager.AppSettings["Blob.ConnectionString"])
                });

            this.indexStoreFactory = new SqlIndexStoreFactory(ConfigurationManager.AppSettings["Index.ConnectionString"]);
            this.entityStoreFactory = new AzureEntityStoreFactory(ConfigurationManager.AppSettings["Entity.ConnectionString"]);
            this.storageKeyFactory = new AzureStorageKeyFactory(this.indexStoreFactory, new KeyRuleFactory());
            this.blobStoreFactory = new AzureBlobStoreFactory(ConfigurationManager.AppSettings["Entity.ConnectionString"]);
            this.repository = new ConcreteEntityRepository(this.indexStoreFactory, this.entityStoreFactory, this.storageKeyFactory, this.blobStoreFactory);

            this.requestContext = new RequestContext 
            { 
                UserId = "abc123", 
                EntityFilter = BuildEntityFilter(true, true, true) 
            };

            // Create a default company to save entites against
            if (defaultTestCompanyId == null)
            {
                var company = new CompanyEntity(
                    new EntityId(),
                    new Entity
                    {
                        ExternalName = "TestCompany",
                        ExternalType = "Company.Agency"
                    });
                this.repository.AddCompany(new RequestContext(), company);
                defaultTestCompanyId = company.ExternalEntityId;
            }

            this.requestContext.ExternalCompanyId = defaultTestCompanyId;

            // Create first default association target
            if (defaultAssocTargetId1 == null)
            {
                var partner = new PartnerEntity(
                    new EntityId(),
                    new Entity
                        {
                            ExternalName = "TestTarget",
                            ExternalType = "AssocTarget"
                        });
                this.repository.SaveEntity(this.requestContext, partner);
                defaultAssocTargetId1 = partner.ExternalEntityId;
            }
        }

        /// <summary>Happy-path GetEntitiesByType</summary>
        [TestMethod]
        public void GetEntitiesByType()
        {
            // Setup unique external type so we don't have a growing set
            var externalType = new EntityId().ToString();
            var entity1 = new PartnerEntity(new EntityId(), new Entity { EntityCategory = PartnerEntity.PartnerEntityCategory, ExternalType = externalType });
            var entity2 = new PartnerEntity(new EntityId(), new Entity { EntityCategory = PartnerEntity.PartnerEntityCategory, ExternalType = externalType });

            this.repository.SaveEntities(this.requestContext, new HashSet<IEntity> { entity1, entity2 });

            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, PartnerEntity.PartnerEntityCategory);
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.ExternalTypeFilter, externalType);

            var entityIds = this.repository.GetFilteredEntityIds(filterContext).ToList();
            var entities = this.repository.TryGetEntities(this.requestContext, entityIds);

            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == entity1.ExternalEntityId));
            Assert.AreEqual(1, entities.Count(e => e.ExternalEntityId == entity2.ExternalEntityId));

            this.repository.SetEntityStatus(this.requestContext, entity1.ExternalEntityId, false);

            entityIds = this.repository.GetFilteredEntityIds(filterContext).ToList();
            
            // Should only be one
            Assert.AreEqual(entity2.ExternalEntityId, entityIds.Single());
        }

        /// <summary>Test we can save and get all property types from Azure Table storage</summary>
        [TestMethod]
        public void RoundtripPropertiesToAzureTable()
        {
            var entityCategory = "Campaign";
            var entity = new CampaignEntity(new Entity { EntityCategory = entityCategory, ExternalEntityId = new EntityId(), });

            // Add a property of each basic supported property type to make sure we can round trip them through
            // Azure table storage.
            var stringPropertyName = "TheStringStuff";
            var stringPropertyValue = "stringValue";
            entity.Properties.Add(new EntityProperty { Name = stringPropertyName, Value = stringPropertyValue });

            var int32PropertyName = "TheInt32Stuff";
            var int32PropertyValue = int.MaxValue;
            entity.Properties.Add(new EntityProperty { Name = int32PropertyName, Value = int32PropertyValue });

            var int64PropertyName = "TheInt64Stuff";
            var int64PropertyValue = long.MaxValue;
            entity.Properties.Add(new EntityProperty { Name = int64PropertyName, Value = int64PropertyValue });

            var doublePropertyName = "TheDoubleStuff";
            var doublePropertyValue = double.MaxValue;
            entity.Properties.Add(new EntityProperty { Name = doublePropertyName, Value = doublePropertyValue });

            var boolPropertyName = "TheBoolStuff";
            var boolPropertyValue = false;
            entity.Properties.Add(new EntityProperty { Name = boolPropertyName, Value = boolPropertyValue });

            // Emulated storage does not have the same precision as the real cloud implementation so this will
            // work but DateTime.MaxValue would not.
            var datePropertyName = "TheDateStuff";
            var datePropertyValue = new DateTime(2000, 01, 01, 01, 01, 01, 999);
            entity.Properties.Add(new EntityProperty { Name = datePropertyName, Value = datePropertyValue });

            var guidPropertyName = "TheGuidStuff";
            var guidPropertyValue = Guid.NewGuid();
            entity.Properties.Add(new EntityProperty { Name = guidPropertyName, Value = guidPropertyValue });

            var binaryPropertyName = "TheBinaryStuff";
            var binaryPropertyValue = new byte[] { 0x0, 0x1, 0xF, 0xFF };
            var binaryEnityProperty = new EntityProperty { Name = binaryPropertyName, Value = binaryPropertyValue };
            entity.Properties.Add(binaryEnityProperty);

            // Include extended properties
            var extendedPropertyName = "SomeExtendedStuff";
            var extendedPropertyValue = "someExtendedValue";
            entity.Properties.Add(new EntityProperty(extendedPropertyName, extendedPropertyValue, PropertyFilter.Extended));

            // Include a couple system properties
            var systemStringPropertyName = "TheAdminStringStuff";
            var systemStringPropertyValue = "systemValue";
            entity.Properties.Add(new EntityProperty(systemStringPropertyName, systemStringPropertyValue, PropertyFilter.System));

            var systemInt32PropertyName = "TheAdminInt32Stuff";
            var systemInt32PropertyValue = int.MinValue;
            entity.Properties.Add(new EntityProperty(systemInt32PropertyName, systemInt32PropertyValue, PropertyFilter.System));

            var propertyWithUnderscoreName = "The_Spaced_Stuff";
            var propertyWithUnderscoreValue = Guid.NewGuid();
            entity.Properties.Add(new EntityProperty { Name = propertyWithUnderscoreName, Value = propertyWithUnderscoreValue });

            var entities = new HashSet<IEntity> { entity };
            this.repository.SaveEntities(this.requestContext, entities);
            var savedEntity = entities.First();
            var roundtripEntity = this.repository.GetEntitiesById(this.requestContext, new EntityId[] { savedEntity.ExternalEntityId }).First();

            // Assert that the round-trip values equal the original values
            Assert.AreEqual(savedEntity.ExternalEntityId, roundtripEntity.ExternalEntityId);
            Assert.AreEqual(stringPropertyValue, GetPropertyValue<string>(stringPropertyName, roundtripEntity));
            Assert.AreEqual(int32PropertyValue, GetPropertyValue<int>(int32PropertyName, roundtripEntity));
            Assert.AreEqual(int64PropertyValue, GetPropertyValue<long>(int64PropertyName, roundtripEntity));
            Assert.AreEqual(doublePropertyValue, GetPropertyValue<double>(doublePropertyName, roundtripEntity));
            Assert.AreEqual(boolPropertyValue, GetPropertyValue<bool>(boolPropertyName, roundtripEntity));
            Assert.AreEqual(datePropertyValue.ToUniversalTime(), GetPropertyValue<DateTime>(datePropertyName, roundtripEntity));
            Assert.AreEqual(guidPropertyValue, GetPropertyValue<Guid>(guidPropertyName, roundtripEntity));
            Assert.AreEqual(propertyWithUnderscoreValue, GetPropertyValue<Guid>(propertyWithUnderscoreName, roundtripEntity));

            // Assert that System and Extended properties have the correct filter
            Assert.AreEqual(PropertyFilter.System, roundtripEntity.GetEntityPropertyByName(systemStringPropertyName).Filter);
            Assert.AreEqual(PropertyFilter.System, roundtripEntity.GetEntityPropertyByName(systemInt32PropertyName).Filter);
            Assert.AreEqual(PropertyFilter.Extended, roundtripEntity.GetEntityPropertyByName(extendedPropertyName).Filter);

            // Binary data needs to compare off the serialized value because AreEqual doesn't work for this as a dynamic type
            Assert.AreEqual(binaryEnityProperty.Value.SerializationValue, roundtripEntity.Properties.Single(p => p.Name == binaryPropertyName).Value.SerializationValue);
        }

        /// <summary>Test entity not found fails correctly.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void GetEntitiesByIdNotFound()
        {
            this.repository.GetEntitiesById(this.requestContext, new[] { new EntityId() });
        }

        /// <summary>Test we throw if index update fails because of stale version.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessStaleEntityException))]
        public void SaveFailsOnStaleVersion()
        {
            var campaignEntity = TestEntityBuilder.BuildCampaignEntity();

            var context = new RequestContext { ExternalCompanyId = defaultTestCompanyId };
            this.repository.SaveEntity(context, campaignEntity);

            // Get the campaign then save it again to roll the version
            var staleCampaign = this.repository.TryGetEntity(context, campaignEntity.ExternalEntityId);
            this.repository.SaveEntity(context, campaignEntity);

            // Update the campaign
            this.repository.SaveEntity(context, staleCampaign);
        }

        /// <summary>Test we throw if index update fails because of out of sequence version.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SaveFailsOnOutOfSequenceVersion()
        {
            var campaignEntity = TestEntityBuilder.BuildCampaignEntity();

            var context = new RequestContext { ExternalCompanyId = defaultTestCompanyId };
            this.repository.SaveEntity(context, campaignEntity);

            // Update the campaign with an out of sequence version
            var campaign = this.repository.TryGetEntity(context, campaignEntity.ExternalEntityId);
            campaign.LocalVersion = 10;
            this.repository.SaveEntity(context, campaign);
        }

        /// <summary>Test we can update IEntity members of an existing entity (including property bag and association collections).</summary>
        [TestMethod]
        public void UpdateEntity()
        {
            var sourceEntity = TestEntityBuilder.BuildPartnerEntity();

            // Set up two properties in the property bag
            var propToChange = new EntityProperty("Foo", 10);
            var propToReplace = new EntityProperty("ReplaceMe", 10);
            var propToLeave = new EntityProperty("Bar", 100);
            var propToAdd = new EntityProperty("Added", "stuff");
            var propToRemove = new EntityProperty("DeleteMe", 1000);
            sourceEntity.Properties.Add(propToChange);
            sourceEntity.Properties.Add(propToReplace);
            sourceEntity.Properties.Add(propToLeave);
            sourceEntity.Properties.Add(propToRemove);

            // Set up an association collection of three and a standalone
            var targetEntity1 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "TestTarget", ExternalType = "AssocTarget" });
            var targetEntity2 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "TestTarget", ExternalType = "AssocTarget" });
            var targetEntity3 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "TestTarget", ExternalType = "AssocTarget" });
            var targetEntity4 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "TestTarget", ExternalType = "AssocTarget" });
            var targetEntities = new HashSet<IEntity> { targetEntity1, targetEntity2, targetEntity3, targetEntity4 };
            this.repository.SaveEntities(this.requestContext, targetEntities);

            var assocToChangeId = defaultAssocTargetId1;
            var assocIdChanged = (EntityId)targetEntity1.ExternalEntityId;
            var assocToLeaveId = (EntityId)targetEntity2.ExternalEntityId;
            var assocToReplaceId = (EntityId)targetEntity3.ExternalEntityId;
            var assocToAddId = (EntityId)targetEntity4.ExternalEntityId;
            var assocToChange = new Association
                {
                    TargetEntityId = assocToChangeId,
                    ExternalName = "Parent",
                    AssociationType = AssociationType.Relationship
                };
            var assocToLeave = new Association
                {
                    TargetEntityId = assocToLeaveId,
                    ExternalName = "Campaigns",
                    AssociationType = AssociationType.Relationship
                };
            var assocToReplace = new Association(assocToLeave) { TargetEntityId = assocToReplaceId };
            var assocToAdd = new Association(assocToLeave) { TargetEntityId = assocToAddId };
            sourceEntity.Associations.Add(assocToChange);
            sourceEntity.Associations.Add(assocToLeave);
            sourceEntity.Associations.Add(assocToReplace);

            // Save the entity
            var entities = new HashSet<IEntity> { sourceEntity };
            this.repository.SaveEntities(this.requestContext, entities);

            // Get a roundtrip copy of the saved entity for baseline compare and another copy to update
            var savedSourceEntity = this.repository.GetEntitiesById(this.requestContext, new[] { (EntityId)sourceEntity.ExternalEntityId }).Single();
            var updatedSourceEntity = this.repository.GetEntitiesById(this.requestContext, new[] { (EntityId)sourceEntity.ExternalEntityId }).Single();

            // Assert the initial state
            Assert.AreEqual(0, (int)updatedSourceEntity.LocalVersion);
            Assert.AreEqual(4, updatedSourceEntity.Properties.Count());
            Assert.AreEqual(1, updatedSourceEntity.Properties.Count(p => p.Name == "DeleteMe"));
            Assert.AreEqual(3, updatedSourceEntity.Associations.Count());
            Assert.AreEqual(2, updatedSourceEntity.Associations.Select(a => a.ExternalName).Distinct().Count());

            // Update an interface property
            updatedSourceEntity.ExternalName = "NewName";

            // Update the property bag
            updatedSourceEntity.Properties.Single(p => p.Name == "Foo").Value = 11;
            updatedSourceEntity.Properties.Single(p => p.Name == "ReplaceMe").Name = "NewBar";
            updatedSourceEntity.Properties.Add(propToAdd);
            updatedSourceEntity.Properties.Remove(propToRemove);

            // Update the assocations
            updatedSourceEntity.Associations.Single(a => a.ExternalName == "Parent").TargetEntityId = assocIdChanged;
            updatedSourceEntity.Associations.Single(a => (string)a.TargetEntityId == assocToReplaceId).ExternalName = "NewStandAlone";
            //// Add an association to the collection
            updatedSourceEntity.Associations.Add(assocToAdd);

            // Save it again and retrieve it
            var savedEntities = new HashSet<IEntity> { updatedSourceEntity };
            this.repository.SaveEntities(this.requestContext, savedEntities);

            // Get a roundtrip copy of the updated entity.
            updatedSourceEntity = this.repository.GetEntitiesById(this.requestContext, new[] { (EntityId)sourceEntity.ExternalEntityId }).Single();

            // First assert that the members we didn't update stayed the same
            Assert.AreEqual(savedSourceEntity.EntityCategory, updatedSourceEntity.EntityCategory);
            Assert.AreEqual(savedSourceEntity.ExternalEntityId, updatedSourceEntity.ExternalEntityId);
            Assert.AreEqual(savedSourceEntity.ExternalType, updatedSourceEntity.ExternalType);
            Assert.AreEqual(savedSourceEntity.CreateDate, updatedSourceEntity.CreateDate);
            Assert.AreEqual(100, (int)updatedSourceEntity.Properties.Single(p => p.Name == "Bar").Value);
            Assert.AreEqual("Campaigns", updatedSourceEntity.Associations.Single(a => a.TargetEntityId == assocToLeaveId).ExternalName);

            // Now assert the updated members changed.
            Assert.AreEqual(1, (int)updatedSourceEntity.LocalVersion);
            Assert.AreNotEqual(savedSourceEntity.LastModifiedDate, updatedSourceEntity.LastModifiedDate);
            Assert.AreEqual("NewName", (string)updatedSourceEntity.ExternalName);
            Assert.AreEqual(4, updatedSourceEntity.Properties.Count());
            Assert.AreEqual(4, updatedSourceEntity.Associations.Count());
            Assert.AreEqual(11, (int)updatedSourceEntity.Properties.Single(p => p.Name == "Foo").Value);
            Assert.AreEqual(1, updatedSourceEntity.Properties.Count(p => p.Name == "NewBar"));
            Assert.AreEqual(0, updatedSourceEntity.Properties.Count(p => p.Name == "DeleteMe"));
            Assert.AreEqual(1, updatedSourceEntity.Associations.Count(p => p.ExternalName == "NewStandAlone"));
            Assert.AreEqual(2, updatedSourceEntity.Associations.Count(a => a.ExternalName == "Campaigns"));
            Assert.AreEqual(3, updatedSourceEntity.Associations.Select(a => a.ExternalName).Distinct().Count());
            Assert.AreEqual(assocIdChanged, updatedSourceEntity.Associations.Single(a => a.ExternalName == "Parent").TargetEntityId);
        }

        /// <summary>
        /// Associate multiple entities to an entity (both stand-alone an collections of associations).
        /// Deactivate targets and check associations.
        /// </summary>
        [TestMethod]
        public void AssociateEntitiesAndSetStatus()
        {
            var sourceEntity = TestEntityBuilder.BuildPartnerEntity();

            // Set this up to include a collection of associations to Foo's and a stand-alone association to Bar
            var targetEntity1 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "MyFoo1", ExternalType = "Foo" });
            var targetEntity2 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "MyFoo2", ExternalType = "Foo" });
            var targetEntity3 = new PartnerEntity(new EntityId(), new Entity { ExternalName = "MyBar", ExternalType = "Bar" });

            var sourceEntities = new HashSet<IEntity> { sourceEntity };
            this.repository.SaveEntities(this.requestContext, sourceEntities);
            var savedTargetEntities = new HashSet<IEntity> { targetEntity1, targetEntity2, targetEntity3 };
            this.repository.SaveEntities(this.requestContext, savedTargetEntities);

            // AssociateEntities should override the filter if it is set to ignore associations
            this.requestContext.EntityFilter = BuildEntityFilter(true, false, false);

            // Associate the Foo entities as a collection called PileOFoos
            this.repository.AssociateEntities(
                this.requestContext,
                sourceEntity.ExternalEntityId,
                "PileOFoos",
                new HashSet<IEntity>(savedTargetEntities.Where(e => (string)e.ExternalType == "Foo")));

            // Associate the Bar entity as a stand-alone association called OneBar
            this.repository.AssociateEntities(
                this.requestContext,
                sourceEntity.ExternalEntityId,
                "OneBar",
                new HashSet<IEntity>(savedTargetEntities.Where(e => (string)e.ExternalType == "Bar")));

            // reset filter to get associations
            this.requestContext.EntityFilter = BuildEntityFilter(false, false, true);
            var associatedSourceEntity = this.repository.GetEntity(this.requestContext, sourceEntity.ExternalEntityId);

            Assert.AreEqual(3, associatedSourceEntity.Associations.Count);
            Assert.AreEqual(2, associatedSourceEntity.Associations.Count(a => a.ExternalName == "PileOFoos"));
            Assert.AreEqual(1, associatedSourceEntity.Associations.Count(a => a.ExternalName == "OneBar"));
            Assert.AreEqual(3, associatedSourceEntity.Associations.Select(a => a.TargetEntityId).Distinct().Count());

            var entityIds = new HashSet<EntityId> { targetEntity1.ExternalEntityId, targetEntity2.ExternalEntityId };
            this.repository.SetEntityStatus(this.requestContext, entityIds, false);
            associatedSourceEntity = this.repository.GetEntity(this.requestContext, sourceEntity.ExternalEntityId);
            
            Assert.AreEqual(1, associatedSourceEntity.Associations.Count);
            Assert.AreEqual(0, associatedSourceEntity.Associations.Count(a => a.ExternalName == "PileOFoos"));

            this.repository.SetEntityStatus(this.requestContext, entityIds, true);
            associatedSourceEntity = this.repository.GetEntity(this.requestContext, sourceEntity.ExternalEntityId);

            Assert.AreEqual(3, associatedSourceEntity.Associations.Count);
            Assert.AreEqual(2, associatedSourceEntity.Associations.Count(a => a.ExternalName == "PileOFoos"));
        }

        /// <summary>Test that we can Create new Users, retrieve and update them.</summary>
        [TestMethod]
        public void RoundtripUser()
        {
            // Build a new user entity - randomize UserId
            var userEntity = BuildUserWithUniqueUserId();

            // Currently this requires a well known ExternalCompanyId for a company that will get created
            // if it doesn't exist already. This represents a 'DefaultCompany' where users will live for now.
            var context = this.requestContext;
            
            // Save the user entity
            this.repository.SaveUser(context, userEntity);

            // Roundtrip the user entity both by entity id and user id
            var savedUserEntity = this.repository.GetEntity(context, userEntity.ExternalEntityId) as UserEntity;
            var savedUserByUserId = this.repository.GetUser(context, userEntity.UserId);

            // Assert returned saved user entities
            Assert.IsInstanceOfType(savedUserEntity, typeof(UserEntity));
            Assert.IsInstanceOfType(savedUserByUserId, typeof(UserEntity));
            Assert.AreEqual(userEntity.UserId, savedUserEntity.UserId);
            Assert.AreEqual(userEntity.UserId, savedUserByUserId.UserId);
            Assert.AreEqual(userEntity.ContactEmail, savedUserEntity.ContactEmail);
            Assert.AreEqual(userEntity.ContactEmail, savedUserByUserId.ContactEmail);

            // Update the user
            savedUserEntity.LastName = "Smith";
            this.repository.SaveUser(context, savedUserEntity);
            var updatedUser = this.repository.GetUser(context, savedUserEntity.UserId);

            // Assert roundtrip updated value
            Assert.AreEqual(savedUserEntity.UserId, updatedUser.UserId);
        }

        /// <summary>Test that getting a user id that is not found fails correctly.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetUserNotFound()
        {
            // Attempt to get a bogus user
            this.repository.GetUser(new RequestContext(), Guid.NewGuid().ToString("N"));
        }

        /// <summary>Test saving a user entity with a duplicate user id fails correctly.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void SaveDuplicateUserIdFails()
        {
            // Build save a user
            var userEntity = BuildUserWithUniqueUserId();
            var context = this.requestContext;
            this.repository.SaveUser(context, userEntity);

            // Now change the entityId and try to save it.
            userEntity.ExternalEntityId = new EntityId();
            this.repository.SaveUser(context, userEntity);
        }

        /// <summary>Get all users in the system.</summary>
        [TestMethod]
        public void GetAllUsers()
        {
            // Build a new user entity - randomize UserId
            var userEntity = BuildUserWithUniqueUserId();

            // Currently this requires a well known ExternalCompanyId for a company that will get created
            // if it doesn't exist already. This represents a 'DefaultCompany' where users will live for now.
            var context = this.requestContext;

            // Save the user entity and make sure it has multiple versions
            this.repository.SaveUser(context, userEntity);
            userEntity.LastName = "Smith";
            this.repository.SaveUser(context, userEntity);

            var users = this.repository.GetAllUsers(new RequestContext());

            // We can assert that we get exactly one user corresponding to the id we created,
            Assert.AreEqual(1, users.Count(u => u.ExternalEntityId == userEntity.ExternalEntityId));

            // We can assert each entity id appears exactly once.
            Assert.AreEqual(users.Count(), users.Select(u => u.ExternalEntityId).Distinct().Count());

            // We can assert there is at least one other user (sysadmin). Beyond that would be brittle.
            Assert.IsTrue(users.Count > 1);
        }

        /// <summary>Test that we can create new Companies, retrieve and update them.</summary>
        [TestMethod]
        public void RoundtripCompanies()
        {
            var companyId1 = new EntityId();
            var companyId2 = new EntityId();
            var companyName1 = "CompanyFoo1";
            var companyName2 = "CompanyFoo2";

            var companyEntity1 = TestEntityBuilder.BuildCompanyEntity(companyId1);
            companyEntity1.ExternalName = companyName1;
            var companyEntity2 = TestEntityBuilder.BuildCompanyEntity(companyId2);
            companyEntity2.ExternalName = companyName2;

            this.repository.AddCompany(new RequestContext(), companyEntity1);
            this.repository.AddCompany(new RequestContext(), companyEntity2);

            var addedCompanies = this.repository.GetEntitiesById(this.requestContext, new[] { companyId1, companyId2 });
            
            // Assert roundtrip values
            Assert.AreEqual(2, addedCompanies.Count);
            Assert.AreEqual(1, addedCompanies.Count(c => (string)c.ExternalName == companyName1));
            Assert.AreEqual(1, addedCompanies.Count(c => (string)c.ExternalName == companyName2));

            // Update one of the companies
            var addedCompany = addedCompanies.First();
            addedCompany.ExternalName = "NewCompany";
            this.repository.SaveEntity(new RequestContext(), addedCompany);
            var updatedCompany = this.repository.GetEntitiesById(this.requestContext, new[] { (EntityId)addedCompany.ExternalEntityId }).Single();

            // Assert roundtrip updated value
            Assert.AreEqual(addedCompany.ExternalName, updatedCompany.ExternalName);
        }

        /// <summary>Test that we can create new Campaigns, retrieve and update them.</summary>
        [TestMethod]
        public void RoundtripCampaigns()
        {
            var campaignEntity = TestEntityBuilder.BuildCampaignEntity();

            // Save the campaign entity
            ////var context = this.requestContext;
            var context = new RequestContext { ExternalCompanyId = defaultTestCompanyId };
            this.repository.SaveEntity(context, campaignEntity);

            // Roundtrip the campaign entity
            var entityIds = new[] { (EntityId)campaignEntity.ExternalEntityId };
            var savedCampaignEntity = this.repository.GetEntitiesById(context, entityIds).Single() as CampaignEntity;

            // Assert returned saved campaign entities
            Assert.IsInstanceOfType(savedCampaignEntity, typeof(CampaignEntity));
            Assert.AreEqual(CampaignEntity.CampaignEntityCategory, (string)savedCampaignEntity.EntityCategory);
            Assert.AreEqual(campaignEntity.ExternalEntityId, savedCampaignEntity.ExternalEntityId);
            Assert.AreEqual(campaignEntity.ExternalName, savedCampaignEntity.ExternalName);
            Assert.AreEqual(campaignEntity.Budget, savedCampaignEntity.Budget);
            Assert.AreEqual(campaignEntity.StartDate, savedCampaignEntity.StartDate);
            Assert.AreEqual(campaignEntity.EndDate, savedCampaignEntity.EndDate);
            Assert.AreEqual(campaignEntity.PersonaName, savedCampaignEntity.PersonaName);

            // Update the campaign
            savedCampaignEntity.Budget = 100000;
            this.repository.SaveEntity(context, savedCampaignEntity);
            var updatedCampaign = this.repository.GetEntitiesById(context, entityIds).Single() as CampaignEntity;

            // Assert roundtrip updated value
            Assert.AreEqual(savedCampaignEntity.Budget, updatedCampaign.Budget);
        }

        /// <summary>Test that we can create new Creative entities, retrieve and update them.</summary>
        [TestMethod]
        public void RoundtripCreatives()
        {
            var creativeEntity = TestEntityBuilder.BuildCreativeEntity();

            // Save the creative entity
            var context = this.requestContext;
            this.repository.SaveEntity(context, creativeEntity);

            // Roundtrip the creative entity
            var entityIds = new[] { (EntityId)creativeEntity.ExternalEntityId };
            var savedCreativeEntity = this.repository.GetEntitiesById(context, entityIds).Single();

            // Assert returned saved creative entities
            Assert.IsInstanceOfType(savedCreativeEntity, typeof(CreativeEntity));
            Assert.AreEqual(CreativeEntity.CreativeEntityCategory, (string)savedCreativeEntity.EntityCategory);
            Assert.AreEqual(creativeEntity.ExternalEntityId, savedCreativeEntity.ExternalEntityId);
            Assert.AreEqual(creativeEntity.ExternalName, savedCreativeEntity.ExternalName);

            // Update the creative
            var newPropertyValue = 1;
            var newProperty = new EntityProperty("newProperty", new PropertyValue(PropertyType.Int32, newPropertyValue));
            savedCreativeEntity.Properties.Add(newProperty);
            this.repository.SaveEntity(context, savedCreativeEntity);
            var updatedCreative = this.repository.GetEntitiesById(context, entityIds).Single();

            // Assert roundtrip updated value
            Assert.AreEqual(newPropertyValue, (int)updatedCreative.Properties.Single(p => p.Name == "newProperty"));
        }

        /// <summary>
        /// Save an entity including an explicit blob reference.
        /// This cooresponds to scenarios like creative upload, 
        /// or internal activities that generate large data. In
        /// both cases the activity has explicitly determined that
        /// the association in question should be a blob reference and
        /// set the entity accordingly.
        /// </summary>
        [TestMethod]
        public void RoundtripEntityWithBlobReference()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            // Set up a Blob Entity
            var blobEntityId = new EntityId();
            var objToBlob = new TestBlobType { Foo = 1, Bar = "two" };
            var blobEntity = BlobEntity.BuildBlobEntity(blobEntityId, objToBlob);

            // Save the partner entity
            var context = this.requestContext;
            var entities = new HashSet<IEntity> { entity };
            this.repository.SaveEntities(context, entities);

            // Save the blob entity
            this.repository.SaveEntity(context, blobEntity);

            // Associate blob entity to partner entity
            var roundTripEntity = this.repository.AssociateEntities(
                context,
                entity.ExternalEntityId,
                "BlobOnAStick",
                "BlobDetails",
                new HashSet<IEntity> { blobEntity },
                true);

            // Assert blob association exists
            var association = roundTripEntity.Associations.Single();
            Assert.AreEqual((EntityId)blobEntity.ExternalEntityId, association.TargetEntityId);
            Assert.AreEqual("BlobDetails", association.Details);
            Assert.AreEqual(BlobEntity.BlobEntityCategory, association.TargetEntityCategory);

            // Get the blob data
            var returnedBlob = this.repository.GetEntitiesById(context, new[] { blobEntityId }).Single() as BlobEntity;
            var blobToObj = returnedBlob.DeserializeBlob<TestBlobType>();
            Assert.AreEqual(objToBlob.Foo, blobToObj.Foo);
        }

        /// <summary>Entity update is an immutable operation and result in a new table entity.</summary>
        [TestMethod]
        public void UpdateEntityImmutable()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);

            // Update the partner entity
            entity.SetEntityProperty(new EntityProperty("foo", "stuff"));
            this.repository.TrySaveEntity(context, entity);
            var updatedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);

            // Underlying storage keys should not refer to the same table record
            var savedKey = (AzureStorageKey)savedEntity.Key;
            var updatedkey = (AzureStorageKey)updatedEntity.Key;
            Assert.AreNotEqual(savedKey.RowId, updatedkey.RowId);
            Assert.AreNotEqual(savedKey.LocalVersion, updatedkey.LocalVersion);
        }

        /// <summary>Test removing a table entity.</summary>
        [TestMethod]
        public void RemoveEntity()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);

            // Get the underlying storage key and remove the entity
            var savedKey = (AzureStorageKey)savedEntity.Key;
            this.entityStoreFactory.GetEntityStore().RemoveEntity(savedKey);

            savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            Assert.IsNull(savedEntity);
        }

        /// <summary>Roundtrip an entity with a large string property that gets backed by a blob.</summary>
        [TestMethod]
        public void RoundtripHeavyEntityString()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            var bigString = new string('*', 5000);
            entity.SetPropertyValueByName("BigString", new PropertyValue(PropertyType.String, bigString));

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            var actualBigString = savedEntity.GetPropertyValueByName("BigString").DynamicValue;
            Assert.AreEqual(bigString, actualBigString);

            // Update the partner entity
            var newBigString = new string(':', 5000);
            entity.SetPropertyValueByName("BigString", new PropertyValue(PropertyType.String, newBigString));
            this.repository.TrySaveEntity(context, entity);
            var updatedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            actualBigString = updatedEntity.GetPropertyValueByName("BigString").DynamicValue;
            Assert.AreEqual(newBigString, actualBigString);
        }

        /// <summary>A heavy property that transitions back and forth across the 'heavy' threshold is be correctly handled.</summary>
        [TestMethod]
        public void HeavyEntityHandlesSizeChangesAroundThreshold()
        {
            // Set up a Partner Entity
            var context = this.requestContext;
            context.EntityFilter = new RepositoryEntityFilter(true, true, true, true);

            // Make sure we return blob references
            context.ReturnBlobReferences = true;

            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);
            var smallString = new string('*', 2000);
            var bigString = new string('*', 2500);
            
            // Save property with small string - should not be a blob ref
            entity.SetPropertyByName("someString", smallString, PropertyFilter.Extended);
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            var someStringProperty = savedEntity.GetEntityPropertyByName("someString");
            Assert.IsFalse(someStringProperty.IsBlobRef);

            // Update property to big string - should be a blob ref
            entity.SetPropertyByName("someString", bigString, PropertyFilter.Extended);
            this.repository.TrySaveEntity(context, entity);
            savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            someStringProperty = savedEntity.GetEntityPropertyByName("someString");
            Assert.IsTrue(someStringProperty.IsBlobRef);

            // Update property back to small string - should not be a blob ref
            savedEntity.SetPropertyByName("someString", smallString, PropertyFilter.Extended);
            this.repository.TrySaveEntity(context, savedEntity);
            savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            someStringProperty = savedEntity.GetEntityPropertyByName("someString");
            Assert.IsFalse(someStringProperty.IsBlobRef);
        }

        /// <summary>Roundtrip an entity with a large string property that gets backed by a blob.</summary>
        [TestMethod]
        public void RoundtripHeavyEntityBinary()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            var bigBytes = System.Text.Encoding.Unicode.GetBytes(new string('*', 5000));
            entity.SetPropertyValueByName("BigBytes", new PropertyValue(PropertyType.Binary, bigBytes));

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            var actualBigBytes = (byte[])savedEntity.GetPropertyValueByName("BigBytes").DynamicValue;
            Assert.IsTrue(CompareByteArrays(bigBytes, actualBigBytes));
            
            // Update the partner entity
            var newBigBytes = System.Text.Encoding.Unicode.GetBytes(new string(':', 5000));
            entity.SetPropertyValueByName("BigBytes", new PropertyValue(PropertyType.Binary, newBigBytes));
            this.repository.TrySaveEntity(context, entity);
            var updatedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            actualBigBytes = (byte[])updatedEntity.GetPropertyValueByName("BigBytes").DynamicValue;
            Assert.IsTrue(CompareByteArrays(newBigBytes, actualBigBytes));
        }

        /// <summary>Roundtrip an entity with a large string extended property that gets backed by a blob.</summary>
        [TestMethod]
        public void RoundtripHeavyEntityExtendedProperty()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            var bigString = new string('*', 5000);
            entity.TrySetPropertyByName("BigString", bigString, PropertyFilter.Extended);

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            var actualBigString = savedEntity.TryGetPropertyByName<string>("BigString", null);
            Assert.AreEqual(bigString, actualBigString);
            Assert.AreEqual(PropertyFilter.Extended, savedEntity.GetEntityPropertyByName("BigString").Filter);
        }

        /// <summary>Roundtrip an entity with a large string system property that gets backed by a blob.</summary>
        [TestMethod]
        public void RoundtripHeavyEntitySystemProperty()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            var bigString = new string('*', 5000);
            entity.TrySetPropertyByName("BigString", bigString, PropertyFilter.System);

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var savedEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);
            var actualBigString = savedEntity.TryGetPropertyByName<string>("BigString", null);
            Assert.AreEqual(bigString, actualBigString);
            Assert.AreEqual(PropertyFilter.System, savedEntity.GetEntityPropertyByName("BigString").Filter);
        }

        /// <summary>GetEntity returns null if table missing</summary>
        [TestMethod]
        public void EntityStoreMissingTable()
        {
            var entityStore = this.entityStoreFactory.GetEntityStore();
            var key = new AzureStorageKey(null, "tablefoo", "partitionfoo", new EntityId());
            var entity = entityStore.GetEntityByKey(new RequestContext(), key);
            Assert.IsNull(entity);
        }

        /// <summary>GetEntity returns null if table empty</summary>
        [TestMethod]
        public void EntityStoreEmptyTable()
        {
            var entityStore = this.entityStoreFactory.GetEntityStore();
            var tableKey = (AzureStorageKey)entityStore.SetupNewCompany("tablefoo");
            var key = new AzureStorageKey(null, tableKey.TableName, "partitionfoo", new EntityId());
            var entity = entityStore.GetEntityByKey(new RequestContext(), key);
            Assert.IsNull(entity);
        }

        /// <summary>Generate a valid table name even when input name is not a valid table name.</summary>
        [TestMethod]
        public void BuildTableNameGeneratesValidTableName()
        {
            var entityStore = (AzureEntityDataStore)this.entityStoreFactory.GetEntityStore();
            
            // Generated table names unique even if input the same
            var tableName1 = AzureEntityDataStore.BuildTableName("0\u0b83123");
            var tableName2 = AzureEntityDataStore.BuildTableName("0\u0b83123");
            
            // Generated table names are truncated and still unique if too long
            var tableName3 = AzureEntityDataStore.BuildTableName(new string('a', 100));
            var tableName4 = AzureEntityDataStore.BuildTableName(new string('a', 100));

            Assert.IsFalse(entityStore.CreateTable("123"));
            Assert.IsFalse(entityStore.CreateTable("\u0b83123"));
            Assert.IsTrue(entityStore.CreateTable(tableName1));
            Assert.IsTrue(entityStore.CreateTable(tableName2));
            Assert.IsTrue(entityStore.CreateTable(tableName3));
            Assert.IsTrue(entityStore.CreateTable(tableName4));
        }

        /// <summary>
        /// Merge a previously saved entity.
        /// </summary>
        [TestMethod]
        public void MergeEntity()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);

            // Set up a heavy extended property
            var modifiedblobPropertyValue = new string('$', 5000);
            var blobPropertyValue = new string('*', 5000);
            var blobPropertyName = "BigString";

            // Set up a normal extended property
            var propertyValue = "somevalue";
            var modifiedPropertyValue = "somenewvalue";
            var extPropertyName = "ExtSomeName";

            // Set up a normal property
            var propertyName = "SomeName";

            // Save the entity with no filters
            entity.TrySetPropertyByName(blobPropertyName, blobPropertyValue, PropertyFilter.Extended);
            entity.TrySetPropertyByName(extPropertyName, propertyValue, PropertyFilter.Extended);
            entity.TrySetPropertyByName(propertyName, propertyValue);
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);

            // Filtered get excludes extended properties
            context.EntityFilter = BuildEntityFilter(false, false, false);
            var retrievedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(propertyValue, retrievedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(extPropertyName, null));

            // Filtered get includes extended properties
            context.EntityFilter = BuildEntityFilter(true, true, true);
            retrievedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(propertyValue, retrievedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(blobPropertyValue, retrievedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(propertyValue, retrievedEntity.TryGetPropertyByName<string>(extPropertyName, null));

            // Change properties but filter prevents saving extended properties
            retrievedEntity.TrySetPropertyByName(blobPropertyName, modifiedblobPropertyValue, PropertyFilter.Extended);
            retrievedEntity.TrySetPropertyByName(extPropertyName, modifiedPropertyValue, PropertyFilter.Extended);
            retrievedEntity.TrySetPropertyByName(propertyName, modifiedPropertyValue);
            context.EntityFilter = BuildEntityFilter(false, false, false);
            this.repository.TrySaveEntity(context, retrievedEntity);
            context.EntityFilter = BuildEntityFilter(true, true, true);
            var modifiedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, modifiedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(blobPropertyValue, modifiedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(propertyValue, modifiedEntity.TryGetPropertyByName<string>(extPropertyName, null));

            // Change properties with no filter merges all properties and gets all properties
            context.EntityFilter = null;
            this.repository.TrySaveEntity(context, retrievedEntity);
            context.EntityFilter = BuildEntityFilter(true, true, true);
            modifiedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, modifiedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(modifiedblobPropertyValue, modifiedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(modifiedPropertyValue, modifiedEntity.TryGetPropertyByName<string>(extPropertyName, null));

            // Remove extended property but filtering blocks modifications
            context.EntityFilter = BuildEntityFilter(false, false, false);
            retrievedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, retrievedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(extPropertyName, null));
            this.repository.TrySaveEntity(context, retrievedEntity);
            context.EntityFilter = BuildEntityFilter(true, true, true);
            retrievedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, retrievedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(modifiedblobPropertyValue, retrievedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(modifiedPropertyValue, retrievedEntity.TryGetPropertyByName<string>(extPropertyName, null));

            // ForceOverwrite ignores filter and writes whatever you give it
            context.EntityFilter = BuildEntityFilter(false, false, false);
            retrievedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, retrievedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(null, retrievedEntity.TryGetPropertyByName<string>(extPropertyName, null));
            context.ForceOverwrite = true;
            this.repository.TrySaveEntity(context, retrievedEntity);
            context.EntityFilter = BuildEntityFilter(true, true, true);
            modifiedEntity = this.repository.TryGetEntity(context, partnerEntityId);
            Assert.AreEqual(modifiedPropertyValue, modifiedEntity.TryGetPropertyByName<string>(propertyName, null));
            Assert.AreEqual(null, modifiedEntity.TryGetPropertyByName<string>(blobPropertyName, null));
            Assert.AreEqual(null, modifiedEntity.TryGetPropertyByName<string>(extPropertyName, null));
        }

        /// <summary>Test we can merge and update properties.</summary>
        [TestMethod]
        public void UpdateEntityProperties()
        {
            // Set up a Partner Entity
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(new EntityId());
            this.repository.TrySaveEntity(this.requestContext, entity);

            var properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeValue1", PropertyFilter.Default),
                    new EntityProperty("SomeProperty2", "SomeValue2", PropertyFilter.Extended),
                    new EntityProperty("SomeProperty3", "SomeValue3", PropertyFilter.System),
                };

            this.requestContext.EntityFilter = new RepositoryEntityFilter(true, true, true, true);
            var result = this.repository.TryUpdateEntity(this.requestContext, entity.ExternalEntityId, properties);
            Assert.IsTrue(result);

            var roundTripEntity = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);
            Assert.AreEqual("SomeValue1", roundTripEntity.TryGetPropertyByName<string>("SomeProperty1", null));
            Assert.AreEqual("SomeValue2", roundTripEntity.TryGetPropertyByName<string>("SomeProperty2", null));
            Assert.AreEqual("SomeValue3", roundTripEntity.TryGetPropertyByName<string>("SomeProperty3", null));

            // Change type of property (default to extended)
            properties = new List<EntityProperty>
                {
                    new EntityProperty("SomeProperty1", "SomeOtherValue1", PropertyFilter.Extended)
                };

            this.repository.TryUpdateEntity(this.requestContext, entity.ExternalEntityId, properties);
            roundTripEntity = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);
            var ep = roundTripEntity.GetEntityPropertyByName("SomeProperty1");
            Assert.AreEqual(3, roundTripEntity.Properties.Count);
            Assert.AreEqual("SomeOtherValue1", (string)ep);
            Assert.IsTrue(ep.IsExtendedProperty);
        }

        /// <summary>
        /// Non-interface property names and association names are encoded and decoded according to the latest schema version.
        /// </summary>
        [TestMethod]
        public void RoundtripEncodedNames()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);
            entity.TrySetPropertyByName("$ome_Property\u4e00\u16a0", "someValue");

            entity.Associations.Add(
                new Association
                    {
                        TargetEntityId = defaultAssocTargetId1,
                        ExternalName = "$ome_Association\u4e00\u16a0",
                        AssociationType = AssociationType.Relationship
                    });

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var roundtripEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);

            Assert.AreEqual("someValue", roundtripEntity.GetPropertyByName<string>("$ome_Property\u4e00\u16a0"));
            Assert.AreEqual("AssocTarget", roundtripEntity.GetAssociationByName("$ome_Association\u4e00\u16a0").TargetExternalType);
        }

        /// <summary>
        /// Non-interface property names and association names are encoded and decoded according to the latest schema version.
        /// </summary>
        [TestMethod]
        public void RoundtripEncodedValues()
        {
            // Set up a Partner Entity
            var partnerEntityId = new EntityId();
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(partnerEntityId);
            entity.TrySetPropertyByName("someName", "someValue<>");

            // Round-trip the partner entity
            var context = this.requestContext;
            this.repository.TrySaveEntity(context, entity);
            var roundtripEntity = this.repository.TryGetEntity(context, entity.ExternalEntityId);

            Assert.AreEqual("someValue<>", roundtripEntity.GetPropertyByName<string>("someName"));
        }

        /// <summary>Happy-path get entity at version.</summary>
        [TestMethod]
        public void GetEntityAtVersion()
        {
            // Set up a Partner Entity
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(new EntityId());
            entity.SetPropertyByName("SomeProperty", "SomeValue");
            this.repository.SaveEntity(this.requestContext, entity);

            entity.SetPropertyByName("SomeProperty", "SomeNewValue");
            this.repository.SaveEntity(this.requestContext, entity);
            var entityVer1 = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);
            
            Assert.AreEqual("SomeNewValue", entityVer1.GetPropertyByName<string>("SomeProperty"));

            this.requestContext.EntityFilter.AddVersionToEntityFilter(0);
            var entityVer0 = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);

            Assert.AreEqual("SomeValue", entityVer0.GetPropertyByName<string>("SomeProperty"));
        }

        /// <summary>Get entity at version when it has no associations but current version does.</summary>
        [TestMethod]
        public void GetEntityAtVersionNoAssociations()
        {
            // Set up a Partner Entity
            var entity = (IEntity)TestEntityBuilder.BuildPartnerEntity(new EntityId());
            entity.SetPropertyByName("SomeProperty", "SomeValue");
            this.repository.SaveEntity(this.requestContext, entity);

            // Set up a target association
            var targetEntity = (IEntity)TestEntityBuilder.BuildPartnerEntity(new EntityId());
            this.repository.SaveEntity(this.requestContext, targetEntity);

            // Associate the entity
            entity.SetPropertyByName("SomeProperty", "SomeNewValue");
            entity.AssociateEntities("foo", "none", new HashSet<IEntity> { targetEntity }, AssociationType.Relationship, true);
            this.repository.SaveEntity(this.requestContext, entity);
            
            // Assert the current version
            var entityCurr = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);
            Assert.AreEqual("SomeNewValue", entityCurr.GetPropertyByName<string>("SomeProperty"));
            Assert.AreEqual(1, entityCurr.Associations.Count);

            // Assert version 0
            var ver0Context = new RequestContext(this.requestContext);
            ver0Context.EntityFilter.AddVersionToEntityFilter(0);
            var entityVer0 = this.repository.GetEntity(ver0Context, entity.ExternalEntityId);
            Assert.AreEqual("SomeValue", entityVer0.GetPropertyByName<string>("SomeProperty"));
            Assert.AreEqual(0, entityVer0.Associations.Count);

            // Clear associations and save
            entityCurr.Associations.Clear();
            this.repository.SaveEntity(this.requestContext, entityCurr);

            // Assert the current version
            entityCurr = this.repository.GetEntity(this.requestContext, entity.ExternalEntityId);
            Assert.AreEqual("SomeNewValue", entityCurr.GetPropertyByName<string>("SomeProperty"));
            Assert.AreEqual(0, entityCurr.Associations.Count);

            // Assert version 1 still has associations
            var ver1Context = new RequestContext(this.requestContext);
            ver1Context.EntityFilter.AddVersionToEntityFilter(1);
            var entityVer1 = this.repository.GetEntity(ver1Context, entity.ExternalEntityId);
            Assert.AreEqual("SomeNewValue", entityVer1.GetPropertyByName<string>("SomeProperty"));
            Assert.AreEqual(1, entityVer1.Associations.Count);
        }

        /// <summary>Helper to extract a named PropertyValue from an entity.</summary>
        /// <typeparam name="T">The native type of the property value.</typeparam>
        /// <typeparamref name="T">The native type of the property value.</typeparamref>
        /// <param name="stringPropertyName">The string property name.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The value as native type.</returns>
        private static T GetPropertyValue<T>(string stringPropertyName, IEntity entity)
        {
            return (T)entity.Properties.Single(p => p.Name == stringPropertyName).Value.DynamicValue;
        }

        /// <summary>Build a user entity as with a unique user id</summary>
        /// <returns>new UserEntity</returns>
        private static UserEntity BuildUserWithUniqueUserId()
        {
            var userEntity = TestEntityBuilder.BuildUserEntity();
            userEntity.UserId = Guid.NewGuid().ToString("N");
            return userEntity;
        }

        /// <summary>Compare two byte arrays</summary>
        /// <param name="expectedBytes">The expected bytes.</param>
        /// <param name="actualBytes">The actual bytes.</param>
        /// <returns>true if byte arrays are equal.</returns>
        private static bool CompareByteArrays(byte[] expectedBytes, byte[] actualBytes)
        {
            if (expectedBytes.Count() != actualBytes.Count())
            {
                return false;
            }

            for (int i = 0; i < expectedBytes.Count(); i++)
            {
                if (expectedBytes[i] != actualBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Build an DataAccessLayer.IEntityFilter stub
        /// </summary>
        /// <param name="includeSystemProperties">True to include system properties.</param>
        /// <param name="includeExtendedProperties">True to include extended properties.</param>
        /// <param name="includeAssociations">True to include associations.</param>
        /// <returns>IEntityFilter stub.</returns>
        private static IEntityFilter BuildEntityFilter(bool includeSystemProperties, bool includeExtendedProperties, bool includeAssociations)
        {
            return new RepositoryEntityFilter(true, includeSystemProperties, includeExtendedProperties, includeAssociations);
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
