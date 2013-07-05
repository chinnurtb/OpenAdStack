//-----------------------------------------------------------------------
// <copyright file="CampaignTestFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using EntityActivities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace EntityActivitiesUnitTests
{
    /// <summary>
    /// Test for campaign related activities
    /// </summary>
    [TestClass]
    public class CampaignTestFixture
    {
        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>
        /// Mock user access repository used for tests
        /// </summary>
        private IUserAccessRepository userAccessRepository;

        /// <summary>
        /// Mock access handler
        /// </summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>
        /// User ExternalEntityId used in the tests
        /// </summary>
        private string userEntityId;

        /// <summary>
        /// User UserId used in the tests
        /// </summary>
        private string userId;

        /// <summary>
        /// Company Id used in the tests
        /// </summary>
        private string companyId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string campaignId;

        /// <summary>
        /// Enum for a tri-state value
        /// </summary>
        private enum TriState
        {
            /// <summary>True state</summary>
            True,

            /// <summary>False state</summary>
            False,

            /// <summary>Initial state</summary>
            Initial
        }

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.companyId = EntityTestHelpers.NewEntityIdString();
            this.campaignId = EntityTestHelpers.NewEntityIdString();
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.userEntityId = EntityTestHelpers.NewEntityIdString();
            this.userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));

            ConfigurationManager.AppSettings["Activities.SubmitRequestRetries"] = "3";
            ConfigurationManager.AppSettings["Activities.SubmitRequestRetryWait"] = "10";
            ConfigurationManager.AppSettings["Delivery.DefaultNetwork"] = "AppNexus";
            ConfigurationManager.AppSettings["AppNexus.DefaultExporterVersion"] = "0";

            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);

            var contactEmail = "foo@example.com";
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.userId, contactEmail);
            RepositoryStubUtilities.SetupGetUserStub(this.repository, this.userId, expectedUser, false);

            var defaultCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyId, "Test Company " + Guid.NewGuid().ToString("N"));
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyId, defaultCompany, false);
        }

        /// <summary>Tests Creating a campaign</summary>
        [TestMethod]
        public void CreateCampaignTest()
        {
            var now = DateTime.UtcNow;

            RequestContext calledContext = null;
            CampaignEntity savedCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(
                this.repository, (c, e) => { calledContext = c; savedCampaign = e; }, false);

            // Setup company under which campaign is to be created under
            var company = EntityTestHelpers.CreateTestCompanyEntity(this.companyId, "Test Company");
            company.SetDeliveryNetwork(DynamicAllocation.DeliveryNetworkDesignation.AppNexus);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyId, company, false);

            // Setup campaign to be created from the request
            var updatedName = "New Test Campaign";
            var requestCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                updatedName,
                52000,
                now,
                now + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            var campaignJson = requestCampaign.SerializeToJson();

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(CreateCampaignActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));

            Assert.IsNotNull(savedCampaign);
            Assert.AreEqual<string>(this.campaignId, savedCampaign.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(updatedName, savedCampaign.ExternalName);
            Assert.AreEqual<string>(this.userId, savedCampaign.GetOwnerId());
        }

        /// <summary>Tests attempting to create a campaign that already exists</summary>
        [TestMethod]
        public void CreateCampaignAlreadyExistsTest()
        {
            var now = DateTime.UtcNow;

            // Setup mock to return original campaign
            var originalName = "Test Campaign Original";
            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                originalName,
                42000,
                now,
                now + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, originalCampaign, false);

            RequestContext calledContext = null;
            CampaignEntity savedCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(
                this.repository, (c, e) => { calledContext = c; savedCampaign = e; }, false);

            // Setup company under which campaign is to be created under
            var company = EntityTestHelpers.CreateTestCompanyEntity(this.companyId, "Test Company");
            company.SetDeliveryNetwork(DynamicAllocation.DeliveryNetworkDesignation.AppNexus);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyId, company, false);

            // Setup campaign to be created from the request
            var updatedName = "New Test Campaign";
            var requestCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                updatedName,
                52000,
                now,
                now + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            var campaignJson = requestCampaign.SerializeToJson();

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(CreateCampaignActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));

            Assert.IsNotNull(savedCampaign);
            Assert.AreEqual<string>(this.campaignId, savedCampaign.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(updatedName, savedCampaign.ExternalName);
            Assert.AreEqual<string>(this.userId, savedCampaign.GetOwnerId());
        }

        /// <summary>Tests Creating and saving campaign</summary>
        [TestMethod]
        public void SaveCampaignTest()
        {
            var now = DateTime.UtcNow;

            // Setup mock to return original campaign
            var originalName = "Test Campaign Original";
            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                originalName,
                42000,
                now,
                now + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, originalCampaign, false);

            RequestContext calledContext = null;
            CampaignEntity savedCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(
                this.repository, (c, e) => { calledContext = c; savedCampaign = e; }, false);

            // Setup campaign to be saved from the request
            var updatedName = "Test Campaign Edited";
            var requestCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                updatedName,
                52000,
                now,
                now + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            var campaignJson = requestCampaign.SerializeToJson();

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Save the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));

            Assert.IsNotNull(savedCampaign);
            Assert.AreEqual<string>(this.campaignId, savedCampaign.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(updatedName, savedCampaign.ExternalName);
        }

        /// <summary>
        /// Tests updating an existing campaign with system properties in the
        /// original and only properties in the request.
        /// </summary>
        [TestMethod]
        public void SaveCampaignTestWithSystemPropertiesFromOriginal()
        {
            var requestProperties = new Dictionary<string, Guid>
            {
                { "PropertyA", Guid.NewGuid() },
                { "PropertyB", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value));
            var originalSystemProperties = new Dictionary<string, Guid>
            {
                { "PropertyX", Guid.NewGuid() },
                { "PropertyY", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value, PropertyFilter.System));

            this.TestSavesWithOriginalProperties(
                null,
                originalSystemProperties,
                requestProperties,
                null);
        }

        /// <summary>
        /// Tests updating an existing campaign with properties in the original
        /// and only system properties in the request.
        /// </summary>
        [TestMethod]
        public void SaveCampaignTestWithSystemPropertiesFromRequest()
        {
            var originalProperties = new Dictionary<string, Guid>
            {
                { "PropertyA", Guid.NewGuid() },
                { "PropertyB", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value));
            var requestSystemProperties = new Dictionary<string, Guid>
            {
                { "PropertyX", Guid.NewGuid() },
                { "PropertyY", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value, PropertyFilter.System));

            this.TestSavesWithOriginalProperties(
                originalProperties,
                null,
                null,
                requestSystemProperties);
        }

        /// <summary>
        /// Tests updating an existing campaign where properties and system
        /// properties are in both the original and the request.
        /// </summary>
        [TestMethod]
        public void SaveCampaignTestWithPropertiesFromOriginalAndRequest()
        {
            var originalSystemProperties = new Dictionary<string, Guid>
            {
                { "PropertyX", Guid.NewGuid() },
                { "PropertyY", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value, PropertyFilter.System));
            var originalProperties = new Dictionary<string, Guid>
            {
                { "PropertyA", Guid.NewGuid() },
                { "PropertyB", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value));

            var requestProperties = new Dictionary<string, Guid>
            {
                { "PropertyA", Guid.NewGuid() },
                { "PropertyB", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value));
            var requestSystemProperties = new Dictionary<string, Guid>
            {
                { "PropertyX", Guid.NewGuid() },
                { "PropertyY", Guid.NewGuid() }
            }.Select(kvp => new EntityProperty(kvp.Key, kvp.Value, PropertyFilter.System));

            this.TestSavesWithOriginalProperties(
                originalProperties,
                originalSystemProperties,
                requestProperties,
                requestSystemProperties);
        }

        /// <summary>Tests Creating and saving campaign with updated budget</summary>
        [TestMethod]
        public void SaveCampaignTestUpdateBudget()
        {
            var submittedRequests = new Dictionary<string, ActivityRequest>();
            SubmitActivityRequestHandler submitActivityRequest = (submittedRequest, source) =>
            {
                submittedRequests.Add(source, submittedRequest);
                return true;
            };

            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                4200,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(48, 0, 0),
                "Mr. Perfect");

            var requestCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            var campaignJson = requestCampaign.SerializeToJson();

            // Set the repository mock to return the test campaign
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, originalCampaign, false);

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, submitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify a DAUpdateBudgetAllocations request was NOT submitted
            Assert.AreEqual(0, submittedRequests.Count);
        }

        /// <summary>Tests Creating and saving campaign without OwnerId gets OwnerId set to current user</summary>
        [TestMethod]
        public void SaveCampaignTestAddMissingOwnerId()
        {
            var submittedRequests = new Dictionary<string, ActivityRequest>();
            SubmitActivityRequestHandler submitActivityRequest = (submittedRequest, source) =>
            {
                submittedRequests.Add(source, submittedRequest);
                return true;
            };

            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                4200,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(48, 0, 0),
                "Mr. Perfect");

            var requestCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(48, 0, 0),
                "Mr. Perfect");
            var campaignJson = requestCampaign.SerializeToJson();

            // Set the repository mock to return the test campaign
            IEntity savedCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, (entity) => savedCampaign = entity, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, originalCampaign, false);

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, submitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify a DAUpdateBudgetAllocations request was NOT submitted
            Assert.AreEqual(0, submittedRequests.Count);

            // Verify OwnerId set to current userId
            Assert.IsNotNull(savedCampaign);
            Assert.AreEqual(this.userId, savedCampaign.TryGetPropertyByName<string>("OwnerId", null));
        }

        /// <summary>Tests Creating and saving campaign with valuation inputs approved</summary>
        [TestMethod]
        public void SaveCampaignTestApproveValuationInputs()
        {
            var submittedRequests = new Dictionary<string, ActivityRequest>();
            SubmitActivityRequestHandler submitActivityRequest = (submittedRequest, source) =>
            {
                submittedRequests.Add(source, submittedRequest);
                return true;
            };

            // Valuations stay same, approval status changes
            this.SetupTestCampaignValuations(TriState.False, TriState.False, true);
            var requestCampaignJson = this.SetupTestCampaignValuations(TriState.False, TriState.True, false);

            // Capture the saved campaign
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => { }, false);

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, submitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", requestCampaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            // Verify a DAApproveValuationInputs request was submitted
            Assert.AreEqual(1, submittedRequests.Count);
            Assert.AreEqual(activity.Name, submittedRequests.First().Key);
            var chainedRequest = submittedRequests.First().Value;
            Assert.AreEqual("DAApproveValuationInputs", chainedRequest.Task);
            ActivityTestHelpers.AssertRequestHasValues(chainedRequest, "AuthUserId", "CompanyEntityId", "CampaignEntityId");
        }

        /// <summary>Tests Creating and saving campaign with valuation inputs changed</summary>
        [TestMethod]
        public void SaveCampaignTestUpdateValuationInputs()
        {
            var submittedRequests = new Dictionary<string, ActivityRequest>();
            SubmitActivityRequestHandler submitActivityRequest = (submittedRequest, source) =>
            {
                submittedRequests.Add(source, submittedRequest);
                return true;
            };

            // Valuations change, approval status approved
            this.SetupTestCampaignValuations(TriState.False, TriState.True, true);
            var requestCampaignJson = this.SetupTestCampaignValuations(TriState.True, TriState.True, false);

            // Capture the saved campaign
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => { }, false);

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, submitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", requestCampaignJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
        }

        /// <summary>
        /// Tests getting campaigns associated with Company
        /// </summary>
        [TestMethod]
        public void GetCampaignsForCompanyTest()
        {
            var externalName = "Test Campaign";
            var expectedCampaign = EntityTestHelpers.CreateTestCampaignEntity(this.campaignId, externalName, 42000, DateTime.UtcNow, DateTime.UtcNow + new TimeSpan(48, 0, 0), "Mr. Perfect");
            expectedCampaign.Budget = 42000;
            expectedCampaign.Properties.Add(new EntityProperty("PropertyX", Guid.NewGuid(), PropertyFilter.System));
            var expectedCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyId, externalName);            

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyId, expectedCompany, false);
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { expectedCampaign });

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCampaignsForCompanyActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the campaign using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", expectedCampaign.SerializeToJson() },
                }
            };
            request.QueryValues.Add("Flags", "WithSystemProperties");

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Campaigns");

            var campaignJson = result.Values["Campaigns"];
            Assert.IsNotNull(campaignJson);

            // TODO: Parse list of campaign entities and verify
            Assert.IsTrue(campaignJson.Contains(((EntityId)expectedCampaign.ExternalEntityId).ToString()));
            Assert.IsTrue(campaignJson.Contains(expectedCampaign.Properties.Single(p => p.Name == "PropertyX").Value.SerializationValue));
        }

        /// <summary>
        /// Test for GetBlobByEntityId
        /// </summary>
        [TestMethod]
        public void GetBlobEntityIdTest()
        {
            // add a blob to the Repository
            string blobJsonValue = "{\"NodeValuationSet\" : [ {\"MeasureSet\" : [1],\"Value\" : 0.5 }]";
            var blobEntityId = new EntityId();
            var measureSetBlob = BlobEntity.BuildBlobEntity(blobEntityId, blobJsonValue);

            // Set the repository mock to return the test blob
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { measureSetBlob });

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetBlobByEntityIdActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },                    
                    { "EntityId", EntityTestHelpers.NewEntityIdString() },
                    { "BlobEntityId", blobEntityId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Blob");

            var returnedBlobJson = result.Values["Blob"];
            Assert.IsNotNull(returnedBlobJson);

            Assert.IsTrue(returnedBlobJson == blobJsonValue);
        }
        
        /// <summary>
        /// Test getting a nonexistent campaign by entity id
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Code being tested does not look at the exception properties")]
        public void GetNonexistentCampaignByEntityIdTest()
        {
            // Set the repository mock to throw an ArgumentException which is expected for non-existent entities
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Throw(new DataAccessEntityNotFoundException());

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCampaignByEntityIdActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the campaign using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.InvalidEntityId, this.campaignId);
        }

        /// <summary>
        /// Tests required values for SaveCampaignActivity
        /// </summary>
        [TestMethod]
        public void SaveCampaignRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(SaveCampaignActivity));
        }

        /// <summary>
        /// Tests required values for GetCampaignsForCompanyActivity
        /// </summary>
        [TestMethod]
        public void GetCampaignsForCompanyRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(GetCampaignsForCompanyActivity));
        }

        /// <summary>Test scenarios for approval status change.</summary>
        [TestMethod]
        public void CheckValuationsModified()
        {
            // Create the activity
            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                 DateTime.UtcNow,
                 DateTime.UtcNow,
                "Mr. Perfect");

            var updatedCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                 DateTime.UtcNow,
                 DateTime.UtcNow,
                "Mr. Perfect");

            // No status or valuation inputs present
            var inputsModified = SaveCampaignActivity.CheckValuationsModified(originalCampaign, updatedCampaign);
            Assert.IsFalse(inputsModified);

            // Valuations inputs present but empty
            originalCampaign.SetPropertyValueByName(daName.MeasureList, string.Empty);
            updatedCampaign.SetPropertyValueByName(daName.MeasureList, string.Empty);
            inputsModified = SaveCampaignActivity.CheckValuationsModified(originalCampaign, updatedCampaign);
            Assert.IsFalse(inputsModified);

            // Valuations inputs same
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.Initial, TriState.False, TriState.True);
            inputsModified = SaveCampaignActivity.CheckValuationsModified(originalCampaign, updatedCampaign);
            Assert.IsFalse(inputsModified);

            // Valuation inputs changed
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.Initial, TriState.True, TriState.True);
            inputsModified = SaveCampaignActivity.CheckValuationsModified(originalCampaign, updatedCampaign);
            Assert.IsTrue(inputsModified);
        }

        /// <summary>Test scenarios for approval status change.</summary>
        [TestMethod]
        public void SubmitIfApprovalStatusChanged()
        {
            // Create the activity
            var originalCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                 DateTime.UtcNow,
                 DateTime.UtcNow,
                "Mr. Perfect");

            var updatedCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                 DateTime.UtcNow,
                 DateTime.UtcNow,
                "Mr. Perfect");

            // No status or valuation inputs present, no billing approval
            var change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsFalse(change);

            // Add valuation inputs (no change) leave status unset, billing approved
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.Initial, TriState.False, TriState.True);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsFalse(change);

            // Change valuation inputs but not status, billing approved
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.Initial, TriState.True, TriState.True);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, true);
            Assert.IsFalse(change);

            // Set up status changed from draft to approved, no change in valuation inputs, billing approved
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.False, TriState.True);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsTrue(change);

            // Set up status changed from draft to approved, no change in valuation inputs, no billing approval
            SetupValuationInputs(originalCampaign, TriState.Initial, TriState.False, TriState.False);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.False, TriState.False);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsFalse(change);

            // Set up status stays approved with change in valuation inputs, billing approved
            SetupValuationInputs(originalCampaign, TriState.True, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.True, TriState.True);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, true);
            Assert.IsTrue(change);

            // Set up status stays approved with change in valuation inputs, no billing approval
            SetupValuationInputs(originalCampaign, TriState.True, TriState.False, TriState.False);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.True, TriState.False);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, true);
            Assert.IsFalse(change);

            // Set up status stays approved with no change in valuation inputs, billing approved
            SetupValuationInputs(originalCampaign, TriState.True, TriState.False, TriState.True);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.False, TriState.True);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsFalse(change);

            // Set up status stays approved with no change in valuation inputs, no billing approval
            SetupValuationInputs(originalCampaign, TriState.True, TriState.False, TriState.False);
            SetupValuationInputs(updatedCampaign, TriState.True, TriState.False, TriState.False);
            change = SaveCampaignActivity.CheckIfApprovedValuationsNeedUpdate(originalCampaign, updatedCampaign, false);
            Assert.IsFalse(change);
        }

        /// <summary>Set a property on an entity from a tristate.</summary>
        /// <typeparam name="T">Type of values being added.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="name">The property name.</param>
        /// <param name="state">The state.</param>
        /// <param name="map">Map of states to values.</param>
        private static void SetPropertyFromTriState<T>(IEntity entity, string name, TriState state, Dictionary<TriState, T> map)
        {
            if (state == TriState.Initial)
            {
                var ep = entity.TryGetEntityPropertyByName(name);
                if (ep != null)
                {
                    entity.Properties.Remove(ep);
                }

                return;
            }

            entity.SetPropertyByName(name, map[state]);
        }

        /// <summary>Test helper to set up valuations on campaigns.</summary>
        /// <param name="campaign">The request campaign.</param>
        /// <param name="goliveStatus">The updated status.</param>
        /// <param name="valuationsState">True for new valuations.</param>
        /// <param name="billingApproved">True if billing appoval present</param>
        private static void SetupValuationInputs(
            CampaignEntity campaign,
            TriState goliveStatus,
            TriState valuationsState,
            TriState billingApproved)
        {
            var billingStatusMap = new Dictionary<TriState, bool>
                {
                    { TriState.True, true }, { TriState.False, false }
                };
            var goliveStatusMap = new Dictionary<TriState, string>
                {
                    { TriState.True, daName.StatusApproved }, { TriState.False, daName.StatusDraft }
                };
            var valuationsMap = new Dictionary<TriState, string>
                {
                    { TriState.True, @"{""IdealValuation"":17.6, ""MaxValuation"":""3.29"", ""Measures"":[{""measureId"":""1155940"", ""group"":"""", ""valuation"":""56"", ""pinned"":false}, {""measureId"":""1155964"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1106030"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1345698"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200106"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200123"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1201053"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200852"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}]}" }, 
                    { TriState.False, @"{""IdealValuation"":18.7, ""MaxValuation"":""3.42"", ""Measures"":[{""measureId"":""1155940"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1155964"", ""group"":"""", ""valuation"":""42"", ""pinned"":false}, {""measureId"":""1106030"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1345698"", ""group"":"""", ""valuation"":""60"", ""pinned"":false}, {""measureId"":""1200106"", ""group"":"""", ""valuation"":""87"", ""pinned"":false}, {""measureId"":""1200123"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1201053"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200852"", ""group"":"""", ""valuation"":""32"", ""pinned"":false}]}" }
                };

            SetPropertyFromTriState(campaign, BillingActivityNames.IsBillingApproved, billingApproved, billingStatusMap);
            SetPropertyFromTriState(campaign, daName.Status, goliveStatus, goliveStatusMap);
            SetPropertyFromTriState(campaign, daName.MeasureList, valuationsState, valuationsMap);
        }

        /// <summary>
        /// Tests for updating already existing Campaign, using properties
        /// and/or system properties from the original when not provided
        /// in the request.
        /// </summary>
        /// <param name="originalProperties">The original properties</param>
        /// <param name="originalSystemProperties">The original system properties</param>
        /// <param name="requestProperties">The request properties</param>
        /// <param name="requestSystemProperties">The request system properties</param>
        private void TestSavesWithOriginalProperties(
            IEnumerable<EntityProperty> originalProperties,
            IEnumerable<EntityProperty> originalSystemProperties,
            IEnumerable<EntityProperty> requestProperties,
            IEnumerable<EntityProperty> requestSystemProperties)
        {
            // Setup GetCompanies mock to return original campaign
            var originalName = "Test Campaign Original";
            var originalCampaign = new CampaignEntity(
                new EntityId(this.campaignId),
                new Entity
                {
                    ExternalEntityId = this.campaignId,
                    ExternalName = originalName
                });
            if (originalProperties != null)
            {
                originalCampaign.Properties.Add(originalProperties);
            }

            if (originalSystemProperties != null)
            {
                originalCampaign.Properties.Add(originalSystemProperties);
            }

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, originalCampaign, false);

            // Setup campaign to be saved from the request
            var updatedName = "Test Company Edited";
            var requestCampaign = new CampaignEntity(
                new EntityId(this.campaignId),
                new Entity
                {
                    ExternalEntityId = this.campaignId,
                    ExternalName = updatedName
                }); 
            if (requestProperties != null)
            {
                requestCampaign.Properties.Add(requestProperties);
            }

            if (requestSystemProperties != null)
            {
                requestCampaign.Properties.Add(requestSystemProperties);
            }

            var campaignJson = requestCampaign.SerializeToJson(EntityActivityTestHelpers.BuildEntityFilter(true, false, false, null));

            // Mock SaveCampaign to catch and release the campaign
            CampaignEntity savedCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => savedCampaign = e, false);

            // Creating the activity (SaveCampaignActivity)
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCampaignActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the campaign using activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.userId },
                    { "ParentEntityId", this.companyId },
                    { "EntityId", this.campaignId },
                    { "Payload", campaignJson },
                    { "SystemProperties", "Include" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            Assert.IsNotNull(savedCampaign);
            Assert.AreEqual<string>(this.campaignId, savedCampaign.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(updatedName, savedCampaign.ExternalName);

            // Check the properties that were used
            var properties = savedCampaign.Properties
                .Where(p => p.IsDefaultProperty && p.Value.DynamicType == PropertyType.Guid)
                .ToDictionary(kvp => kvp.Name, kvp => kvp.Value);
            var expectedProperties = (requestProperties ?? originalProperties)
                .Where(p => p.Value.DynamicType == PropertyType.Guid).ToList();
            Assert.IsTrue(expectedProperties.All(p => properties.Any(kvp => p.Name == kvp.Key && (Guid)p.Value == (Guid)kvp.Value)));
            Assert.AreEqual(expectedProperties.Count(), properties.Count);

            // Check the system properties that were used
            var systemProperties = savedCampaign.Properties
                .Where(p => p.IsSystemProperty)
                .ToDictionary(kvp => kvp.Name, kvp => kvp.Value);
            var expectedSystemProperties = (requestSystemProperties ?? originalSystemProperties).ToList();
            Assert.IsTrue(expectedSystemProperties.All(p => systemProperties.Any(kvp => p.Name == kvp.Key && (Guid)p.Value == (Guid)kvp.Value)));
            Assert.AreEqual(expectedSystemProperties.Count(), systemProperties.Count);
        }

        /// <summary>Setup original campaign and new request campaign for testing.</summary>
        /// <param name="valuationsState">True for new valuations.</param>
        /// <param name="approvalStatus">Approval status for request campaign.</param>
        /// <param name="isOriginal">True if this is the original campaign.</param>
        /// <returns>campaign json.</returns>
        private string SetupTestCampaignValuations(TriState valuationsState, TriState approvalStatus, bool isOriginal)
        {
            DynamicAllocationTestUtilities.DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();

            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow + new TimeSpan(48, 0, 0);
            var campaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignId,
                "Test Campaign",
                42000,
                startDate,
                endDate,
                "Mr. Perfect");
            campaign.SetDeliveryNetwork(DynamicAllocation.DeliveryNetworkDesignation.AppNexus);
            SetupValuationInputs(campaign, approvalStatus, valuationsState, TriState.True);
            
            var campaignJson = campaign.SerializeToJson();

            if (isOriginal)
            {
                // Set the repository mock to return the test campaign
                RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, campaign, false);
            }

            return campaignJson;
        }
    }
}
