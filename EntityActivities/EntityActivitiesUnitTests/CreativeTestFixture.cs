//-----------------------------------------------------------------------
// <copyright file="CreativeTestFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Text;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using EntityActivities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;
using ScheduledActivities;
using Utilities.Storage.Testing;

namespace EntityActivityUnitTests
{
    /// <summary>
    /// Test Creative activities 
    /// </summary>
    [TestClass]    
    public class CreativeTestFixture
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
        /// CreativeId used across tests
        /// </summary>
        private string creativeEntityId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string campaignId;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["Activities.SubmitRequestRetries"] = "3";
            this.campaignId = EntityTestHelpers.NewEntityIdString();
            this.userEntityId = EntityTestHelpers.NewEntityIdString();
            this.userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.creativeEntityId = EntityTestHelpers.NewEntityIdString();
            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();
            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);

            var contactEmail = "foo@example.com";
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.userId, contactEmail);

            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Anything)).Return(expectedUser);
        }

        /// <summary>
        /// Test for creating a new creative
        /// </summary>
        [TestMethod]
        public void CreateCreativeTest()
        {
            var creativeName = "Test Creative";
            var requestCreative = EntityTestHelpers.CreateTestCreativeEntity(this.creativeEntityId, creativeName, "Third party ad tag here");
            var creativeJson = requestCreative.SerializeToJson();

            var advertiserId = new EntityId();
            var advertiserEntity = EntityTestHelpers.CreateTestCompanyEntity(advertiserId, "Test Advertiser");
            advertiserEntity.SetPropertyValueByName("DeliveryNetwork", "AppNexus");

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, advertiserId, advertiserEntity, false);

            // Create the activity
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };

            var activity = Activity.CreateActivity(typeof(SaveCreativeActivity), activityContext, this.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var activityRequest = new ActivityRequest
            {
                Values = 
                {
                    { "AuthUserId", this.userId },                    
                    { "ParentEntityId", advertiserId },
                    { "EntityId", this.creativeEntityId },
                    { "Payload", creativeJson }
                }
            };

            var result = activity.Run(activityRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Creative");

            var resultCreative = EntityJsonSerializer.DeserializeCreativeEntity(new EntityId(this.creativeEntityId), result.Values["Creative"]);
            Assert.IsNotNull(resultCreative);
            Assert.AreEqual<string>(this.creativeEntityId, resultCreative.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(creativeName, resultCreative.ExternalName);
            Assert.AreEqual<string>(this.userId, resultCreative.GetOwnerId());
        }

       /// <summary>
        /// Tests getting creatives associated with campaign
        /// </summary>
        [TestMethod]
        public void GetCreativesForCampaignTest()
       {
           var externalName = "Test Creative";
           var expectedCampaign = EntityTestHelpers.CreateTestCampaignEntity(
               this.campaignId,
               externalName,
               42000,
               DateTime.UtcNow,
               DateTime.UtcNow + new TimeSpan(48, 0, 0),
               "Mr. Perfect");
           var expectedCreative = EntityTestHelpers.CreateTestCreativeEntity(
               this.creativeEntityId, externalName, "Third party ad tag");

           RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignId, expectedCampaign, false);
           this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { expectedCreative });

           // Create the activity
           var activity = Activity.CreateActivity(
               typeof(GetCreativesForCampaignActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
           Assert.IsNotNull(activity);

           // Get the campaign using the activity
           var request = new ActivityRequest
               { Values = { { "AuthUserId", Guid.NewGuid().ToString() }, { "EntityId", this.campaignId } } };

           var result = activity.Run(request);

           // Verify the result
           ActivityTestHelpers.AssertValidSuccessResult(result);
           ActivityTestHelpers.AssertResultHasValues(result, "Creatives");

           var creativeJson = result.Values["Creatives"];
           Assert.IsNotNull(creativeJson);

           // TODO: Parse list of campaign entities and verify
           Assert.IsTrue(creativeJson.Contains(((EntityId)expectedCreative.ExternalEntityId).ToString()));
       }

        /// <summary>
        /// Test getting a creative by its entity id
        /// </summary>
        [TestMethod]
        public void GetCreativeByEntityIdTest()
        {
            var creativeName = "Test Creative";
            var requestCreative = EntityTestHelpers.CreateTestCreativeEntity(this.creativeEntityId, creativeName, "Third party ad tag");
           
            // Set the repository mock to return the test Creative
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { requestCreative });

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCreativeByEntityIdActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },                    
                    { "ParentEntityId", EntityTestHelpers.NewEntityIdString() },
                    { "EntityId", EntityTestHelpers.NewEntityIdString() }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Creative");

            var creativeJson = result.Values["Creative"];
            Assert.IsNotNull(creativeJson);
            Assert.IsTrue(creativeJson.Contains(((EntityId)requestCreative.ExternalEntityId).ToString()));

            var creative = EntityJsonSerializer.DeserializeCreativeEntity(this.creativeEntityId, creativeJson);
            Assert.AreEqual((string)requestCreative.ExternalName, (string)creative.ExternalName);
        }

        /// <summary>
        /// Test for updating a creative
        /// </summary>
        [TestMethod]
        public void SaveCreativeTest()
        {
            var creativeName = "Test Creative";
            var requestCreative = EntityTestHelpers.CreateTestCreativeEntity(this.creativeEntityId, creativeName, "Third party ad tag");
            var creativeJson = requestCreative.SerializeToJson();
            
            var advertiserId = new EntityId();
            var advertiserEntity = EntityTestHelpers.CreateTestCompanyEntity(advertiserId, "Test Advertiser");
            advertiserEntity.SetPropertyValueByName("DeliveryNetwork", "AppNexus");

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.creativeEntityId, requestCreative, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, advertiserId, advertiserEntity, false);
            RequestContext calledContext = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CreativeEntity>(this.repository, (c, e) => { calledContext = c; }, false);

            // Create the activity
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCreativeActivity), activityContext, this.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },                    
                    { "ParentEntityId", advertiserId },
                    { "EntityId", this.creativeEntityId },
                    { "Payload", creativeJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Creative");

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));
            
            var resultCreative = EntityJsonSerializer.DeserializeCreativeEntity(new EntityId(this.creativeEntityId), result.Values["Creative"]);
            Assert.IsNotNull(resultCreative);
            Assert.AreEqual<string>(this.creativeEntityId, resultCreative.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(creativeName, resultCreative.ExternalName);
        }

        /// <summary>Delegate for submitting activity requests from within activities</summary>
        /// <param name="request">The activity request to submit</param>
        /// <param name="sourceName">Used to look up this activity when the result is ready</param>
        /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
        public bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            return true;
        }
    }
}
