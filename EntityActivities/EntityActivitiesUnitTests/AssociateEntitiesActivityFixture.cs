//-----------------------------------------------------------------------
// <copyright file="AssociateEntitiesActivityFixture.cs" company="Rare Crowds Inc">
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
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using EntityActivities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;
using Utilities.Serialization;

namespace EntityActivitiesUnitTests
{
    /// <summary>
    /// Unit test fixture for AssociateEntitiesActivity
    /// </summary>
    [TestClass]
    public class AssociateEntitiesActivityFixture
    {
        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>
        /// Mock access handler
        /// </summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
        }

        /// <summary>Happy path test.</summary>
        [TestMethod]
        public void AssociateEntitiesSuccess()
        {
            var userEntity = EntityTestHelpers.CreateTestUserEntity(new EntityId(), "userfoo", "useremail");
            this.repository.Stub(f => f.GetUser(null, null)).IgnoreArguments().Return(userEntity);

            var parentEntityId = new EntityId();
            var parentEntity = EntityTestHelpers.CreateTestPartnerEntity(parentEntityId, "foo");
            var targetEntityId = new EntityId();
            var targetEntity = EntityTestHelpers.CreateTestPartnerEntity(targetEntityId, "foo");
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, targetEntityId, targetEntity, false);
            this.repository.Stub(f => f.AssociateEntities(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId>.Is.Equal(parentEntityId),
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<HashSet<IEntity>>.Is.Anything,
                    Arg<AssociationType>.Is.Equal(AssociationType.Child),
                    Arg<bool>.Is.Equal(false))).Return(parentEntity);

            // Create the activity
            var activity = Activity.CreateActivity(
                typeof(AssociateEntitiesActivity), 
                new Dictionary<Type, object>
                    {
                        { typeof(IEntityRepository), this.repository },
                        { typeof(IResourceAccessHandler), this.accessHandler }
                    }, 
                ActivityTestHelpers.SubmitActivityRequest);
            
            Assert.IsNotNull(activity);

            var payloadValues = new Dictionary<string, string>
                {
                    { "ParentEntity", parentEntityId.ToString() },
                    { "AssociationName", "association" },
                    { "ChildEntity", targetEntityId.ToString() },
                    { "AssociationType", string.Empty }, // child if key is present
                };
            var payLoadJson = AppsJsonSerializer.SerializeObject(payloadValues);
            
            // Create the Company using the activity
            var activityRequest = new ActivityRequest
            {
                Values = 
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", EntityTestHelpers.NewEntityIdString() },
                    { "Payload", payLoadJson }
                }
            };

            var result = activity.Run(activityRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
        }
    }
}
