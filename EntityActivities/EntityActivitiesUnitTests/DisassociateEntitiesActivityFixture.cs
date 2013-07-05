//-----------------------------------------------------------------------
// <copyright file="DisassociateEntitiesActivityFixture.cs" company="Rare Crowds Inc">
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
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;
using Utilities.Serialization;

namespace EntityActivitiesUnitTests
{
    /// <summary>
    /// Unit test fixture for DisassociateEntitiesActivity
    /// </summary>
    [TestClass]
    public class DisassociateEntitiesActivityFixture
    {
        /// <summary>Mock entity repository</summary>
        private IEntityRepository repository;

        /// <summary>Mock access handler</summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>Test "parent" entity</summary>
        private IEntity parentEntity;

        /// <summary>Test target entity</summary>
        private IEntity targetEntity;

        /// <summary>Test association name</summary>
        private string associationName;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();

            this.associationName = "TestAssociation";
            this.targetEntity = (IEntity)EntityTestHelpers.CreateTestPartnerEntity(new EntityId(), "target");
            this.parentEntity = (IEntity)EntityTestHelpers.CreateTestPartnerEntity(new EntityId(), "parent");

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.parentEntity.ExternalEntityId, this.parentEntity, false);
            RepositoryStubUtilities.SetupSaveEntityStub<IEntity>(this.repository, e => this.parentEntity = e, false);
        }

        /// <summary>Remove single and only association.</summary>
        [TestMethod]
        public void RemoveOnlyAssociation()
        {
            this.parentEntity.Associations.Add(new Association
            {
                ExternalName = this.associationName,
                TargetEntityId = this.targetEntity.ExternalEntityId,
                TargetEntityCategory = this.targetEntity.EntityCategory,
                TargetExternalType = this.targetEntity.ExternalType,
            });

            // Build the disassociate request
            var payloadValues = new Dictionary<string, string>
                {
                    { "ParentEntity", this.parentEntity.ExternalEntityId.ToString() },
                    { "AssociationName", this.associationName },
                    { "ChildEntity", this.targetEntity.ExternalEntityId.ToString() },
                    { "AssociationType", string.Empty }, // child if key is present
                };
            var payLoadJson = AppsJsonSerializer.SerializeObject(payloadValues);
            var activityRequest = new ActivityRequest
            {
                Values = 
                {
                    { EntityActivityValues.AuthUserId, Guid.NewGuid().ToString() },
                    { EntityActivityValues.ParentEntityId, new EntityId().ToString() },
                    { EntityActivityValues.EntityId, this.parentEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.MessagePayload, payLoadJson }
                }
            };

            // Instantiate and run the activity
            var activity = this.CreateActivity();
            var result = activity.Run(activityRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            Assert.AreEqual(0, this.parentEntity.Associations.Count);
        }

        /// <summary>Remove one of two associations. Verify other is kept.</summary>
        [TestMethod]
        public void RemoveOneOfTwoAssociations()
        {
            // Generic association to the test target entity
            var associationToRemove = new Association
            {
                ExternalName = this.associationName,
                TargetEntityId = this.targetEntity.ExternalEntityId,
                TargetEntityCategory = this.targetEntity.EntityCategory,
                TargetExternalType = this.targetEntity.ExternalType,
            };
            this.parentEntity.Associations.Add(associationToRemove);

            // Association to keep. Identical except for the target entity id
            var associationToKeep = new Association
            {
                ExternalName = this.associationName,
                TargetEntityId = new EntityId().ToString(),
                TargetEntityCategory = this.targetEntity.EntityCategory,
                TargetExternalType = this.targetEntity.ExternalType,
            };
            this.parentEntity.Associations.Add(associationToKeep);

            // Build the disassociate request
            var payloadValues = new Dictionary<string, string>
                {
                    { "ParentEntity", this.parentEntity.ExternalEntityId.ToString() },
                    { "AssociationName", this.associationName },
                    { "ChildEntity", this.targetEntity.ExternalEntityId.ToString() },
                    { "AssociationType", string.Empty }, // child if key is present
                };
            var payLoadJson = AppsJsonSerializer.SerializeObject(payloadValues);
            var activityRequest = new ActivityRequest
            {
                Values = 
                {
                    { EntityActivityValues.AuthUserId, Guid.NewGuid().ToString() },
                    { EntityActivityValues.ParentEntityId, new EntityId().ToString() },
                    { EntityActivityValues.EntityId, this.parentEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.MessagePayload, payLoadJson },
                }
            };

            // Instantiate and run the activity
            var activity = this.CreateActivity();
            var result = activity.Run(activityRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            Assert.AreEqual(1, this.parentEntity.Associations.Count);
            Assert.IsTrue(this.parentEntity.Associations.Contains(associationToKeep));
        }

        /// <summary>Creates an activity instance for testing</summary>
        /// <returns>The activity instance</returns>
        private Activity CreateActivity()
        {
            var activity = Activity.CreateActivity(
                typeof(DisassociateEntitiesActivity),
                new Dictionary<Type, object>
                    {
                        { typeof(IEntityRepository), this.repository },
                        { typeof(IResourceAccessHandler), this.accessHandler }
                    },
                ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);
            return activity;
        }
    }
}
