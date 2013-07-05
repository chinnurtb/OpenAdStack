//-----------------------------------------------------------------------
// <copyright file="UserTestFixture.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using EntityActivities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace EntityActivityUnitTests
{
    /// <summary>
    /// Tests for user related activities
    /// </summary>
    [TestClass]
    public class UserTestFixture
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
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
        }

        /// <summary>
        /// Tests creating and saving a new user
        /// </summary>
        [TestMethod]
        public void SaveUserTest()
        {
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var userId = string.Empty;
            var contactEmail = "foo@example.com";
            var requestUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userId, contactEmail);
            var userJson = requestUser.SerializeToJson();

            // Set the userAccess mock to return true
            this.userAccessRepository.Stub(f => f.AddUserAccessList(Arg<EntityId>.Is.Anything, Arg<List<string>>.Is.Anything)).Return(true);

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(SaveUserActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create a the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", userJson }
                }
            };

            var result = activity.Run(request);

            var expectedAccessList = new List<string>
                    {
                        "USERVERIFICATION.HTML:#:GET:*",
                        "USER:*:#:GET:PROPERTIES",
                        string.Format(CultureInfo.InvariantCulture, "USER:{0}:#:POST:VERIFY", userEntityId),
                        string.Format(CultureInfo.InvariantCulture, "USER:{0}:#:GET:*", userEntityId),
                    };

            // Assert that user access repository was called to add the default permissions
            this.userAccessRepository.AssertWasCalled(
                f => f.AddUserAccessList(
                    Arg<EntityId>.Matches(a => a.ToString() == userEntityId),
                    Arg<List<string>>.Matches(a => a.Count == 4 && !a.Except(expectedAccessList).Any())));

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "User");

            var resultUser = EntityJsonSerializer.DeserializeUserEntity(new EntityId(request.Values["EntityId"]), result.Values["User"]);
            Assert.IsNotNull(resultUser);
            Assert.IsFalse(string.IsNullOrEmpty(resultUser.UserId));
            Assert.AreEqual(contactEmail, (string)resultUser.ContactEmail);
        }

        /// <summary>
        /// Tests updating an existing user
        /// </summary>
        [TestMethod]
        public void UpdateUserTest()
        {
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var userId = string.Empty;
            var contactEmail = "foo@example.com";
            var requestUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userId, contactEmail);
            var userJson = requestUser.SerializeToJson();

            // Set the userAccess mock to return true
            this.userAccessRepository.Stub(f => f.AddUserAccessList(Arg<EntityId>.Is.Anything, Arg<List<string>>.Is.Anything)).Return(true);

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(SaveUserActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create a the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", userJson }
                }
            };

            var result = activity.Run(request);

            // user has been created, now update it
            var newContactEmail = "foo-new@example.com";
            var requestUpdateUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userId, newContactEmail);
            var updateUserJson = requestUpdateUser.SerializeToJson();

            // Set the repository mock to return the created user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, requestUser, false);
            RequestContext calledContext = null;
            RepositoryStubUtilities.SetupSaveUserStub(this.repository, (c, e) => { calledContext = c; }, false);

            // Create the activity
            var updateActivity = Activity.CreateActivity(typeof(SaveUserActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(updateActivity);

            // Update the user using the activity
            var updateRequest = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", updateUserJson }
                }
            };

            var updateResult = activity.Run(updateRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(updateResult);
            ActivityTestHelpers.AssertResultHasValues(updateResult, "User");

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));

            var updateResultUser = EntityJsonSerializer.DeserializeUserEntity(new EntityId(request.Values["EntityId"]), updateResult.Values["User"]);
            Assert.IsNotNull(updateResultUser);
            Assert.AreEqual(userId, (string)updateResultUser.UserId);
            Assert.AreEqual(newContactEmail, (string)updateResultUser.ContactEmail);
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void GetUserTest()
        {
            // Create a test user
            var id1 = new EntityId();
            var id2 = new EntityId();
            var expectedUser1 = EntityTestHelpers.CreateTestUserEntity(id1, Guid.NewGuid().ToString(), "dontcare");
            var expectedUser2 = EntityTestHelpers.CreateTestUserEntity(id2, Guid.NewGuid().ToString(), "dontcare");
            var userIds = new List<EntityId> { id1, id2 };

            // Set the repository mock to return the test user
            this.repository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything)).Return(userIds);
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(id1))).Return(expectedUser1);
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(id2))).Return(expectedUser2);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetUsersActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, EntityActivityValues.Users);
            var usersJson = result.Values[EntityActivityValues.Users];
            Assert.IsTrue(usersJson.Contains(((EntityId)expectedUser1.ExternalEntityId).ToString()));
            Assert.IsTrue(usersJson.Contains(((EntityId)expectedUser2.ExternalEntityId).ToString()));
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void GetUsersTest()
        {
            // Create a test user
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var userId = Guid.NewGuid().ToString();
            var contactEmail = "foo@example.com";
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userId, contactEmail);

            // Set the repository mock to return the test user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, expectedUser, false);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetUserActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "User");

            var resultUser = EntityJsonSerializer.DeserializeUserEntity(new EntityId(request.Values["EntityId"]), result.Values["User"]);
            Assert.IsNotNull(resultUser);
            Assert.AreEqual(userId, (string)resultUser.UserId);
            Assert.AreEqual(contactEmail, (string)resultUser.ContactEmail);
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void VerifyUserSuccessTest()
        {
            // Create a test user
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var contactEmail = "foo@example.com";
            var lastModified = DateTime.UtcNow;
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userEntityId, contactEmail);
            expectedUser.LastModifiedDate = lastModified;

            // Set the repository mock to return the test user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, expectedUser, false);

            // Set the userAccess mock to return true
            this.userAccessRepository.Stub(f => f.RemoveUserAccessList(Arg<EntityId>.Is.Anything, Arg<List<string>>.Is.Anything)).Return(true);

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(UserMessageVerify), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", "{}" }
                }
            };

            var result = activity.Run(request);

            var expectedAccessList = new List<string>
                    {
                        string.Format(CultureInfo.InvariantCulture, "USER:{0}:#:POST:VERIFY", userEntityId),
                    };

            // Assert that user access repository was called to add the remove the verify permission
            this.userAccessRepository.AssertWasCalled(
                f => f.RemoveUserAccessList(
                    Arg<EntityId>.Matches(a => a.ToString() == userEntityId),
                    Arg<List<string>>.Matches(a => a.Count == 1 && !a.Except(expectedAccessList).Any())));

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void VerifyUserUserNotFoundTest()
        {
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, null, true);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(UserMessageVerify), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", "{}" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual((int)ActivityErrorId.InvalidEntityId, result.Error.ErrorId);
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void VerifyUserAlreadyValidated()
        {
            // Create a test user
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var contactEmail = "foo@example.com";
            var lastModified = DateTime.UtcNow;
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userEntityId, contactEmail);
            expectedUser.LastModifiedDate = lastModified;
            expectedUser.UserId = "anythingElseHereToSimulateWLID"; // already validated

            // Set the repository mock to return the test user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, expectedUser, false);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(UserMessageVerify), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", "{}" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Error.ErrorId, (int)ActivityErrorId.GenericError);
        }

        /// <summary>
        /// Tests getting a previously saved user
        /// </summary>
        [TestMethod]
        public void VerifyUserExpiredTest()
        {
            // Create a test user
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var contactEmail = "foo@example.com";
            var lastModified = DateTime.UtcNow - (new TimeSpan(100, 0, 0));
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userEntityId, contactEmail);
            expectedUser.LastModifiedDate = lastModified;
            expectedUser.UserId = userEntityId;

            // Set the repository mock to return the test user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, expectedUser, false);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(UserMessageVerify), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", "{}" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(result.Error.ErrorId, (int)ActivityErrorId.GenericError);
            Assert.AreEqual("User cannot be registered", result.Error.Message.Substring(0, 25));
        }

        /// <summary>
        /// Tests getting a nonexistent user
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Code being tested does not look at the exception properties")]
        public void GetNonexistentUserTest()
        {
            var userId = new EntityId();

            // Set the repository mock to return the test user
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userId, null, true);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetUserActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.InvalidEntityId, userId);
        }

        /// <summary>
        /// Test getting a nonexistent user by entity id
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Code being tested does not look at the exception properties")]
        public void GetNonexistentUserByEntityIdTest()
        {
            // Set the repository mock to return null which is expected for non-existent entities
            var userEntityId = new EntityId().ToString();
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, null, true);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetUserActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.InvalidEntityId, userEntityId);
        }

        /// <summary>
        /// Tests required values for SaveUserActivity
        /// </summary>
        [TestMethod]
        public void SaveUserRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(SaveUserActivity));
        }

        /// <summary>
        /// Tests required values for GetUserActivity
        /// </summary>
        [TestMethod]
        public void GetUserRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(GetUserActivity));
        }

        /// <summary>
        /// Tests creating and saving a new user
        /// </summary>
        [TestMethod]
        public void SaveAppNexusAppUserTest()
        {
            ConfigurationManager.AppSettings["Activities.SubmitRequestRetries"] = "3";
            
            var userEntityId = EntityTestHelpers.NewEntityIdString();
            var userId = string.Empty;
            var contactEmail = "foo@example.com";
            var requestUser = EntityTestHelpers.CreateTestUserEntity(userEntityId, userId, contactEmail);
            requestUser.UserId = "9999";
            requestUser.SetUserType(UserType.AppNexusApp);
            var userJson = requestUser.SerializeToJson();

            ActivityRequest chainedRequest = null;
            
            // Set the userAccess mock to return true
            this.userAccessRepository.Stub(f => f.AddUserAccessList(Arg<EntityId>.Is.Anything, Arg<List<string>>.Is.Anything)).Return(true);

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(SaveUserActivity), activityContext, (r, s) => { chainedRequest = r; return true; });
            Assert.IsNotNull(activity);

            // Create a the user using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId },
                    { "Payload", userJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);

            var resultUser = EntityJsonSerializer.DeserializeUserEntity(new EntityId(request.Values["EntityId"]), result.Values["User"]);
            Assert.IsNotNull(resultUser);
            Assert.IsFalse(string.IsNullOrEmpty(resultUser.UserId));
            Assert.AreEqual(contactEmail, (string)resultUser.ContactEmail);

            // Verify the chained request was submitted
            Assert.IsNotNull(chainedRequest);
            Assert.AreEqual(AppNexusUtilities.AppNexusActivityTasks.NewAppUser, chainedRequest.Task);
            Assert.AreEqual(userEntityId, chainedRequest.Values[EntityActivityValues.EntityId]);
        }
    }
}
