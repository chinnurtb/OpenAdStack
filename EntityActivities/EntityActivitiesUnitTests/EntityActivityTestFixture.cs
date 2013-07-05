//-----------------------------------------------------------------------
// <copyright file="EntityActivityTestFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Text;
using Activities;
using DataAccessLayer;
using EntityActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityActivitiesUnitTests
{
    /// <summary>
    /// Test for campaign related activities
    /// </summary>
    [TestClass]
    public class EntityActivityTestFixture
    {
        /// <summary>Test auth user id</summary>
        private string authUserId;

        /// <summary>Test company entity id</summary>
        private EntityId companyEntityId;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.authUserId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));
            this.companyEntityId = new EntityId();
        }

        /// <summary>
        /// Test creating a context from a valid request with a company ID
        /// </summary>
        [TestMethod]
        public void CreateContextWithCompany()
        {
            var request = new ActivityRequest
            {
                Task = "Test",
                Values =
                {
                    { "AuthUserId", this.authUserId },
                    { "CompanyEntityId", this.companyEntityId.ToString() }
                }
            };

            var context = EntityActivity.CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request, "CompanyEntityId");

            Assert.IsNotNull(context);
            Assert.AreEqual(this.authUserId, context.UserId);
            Assert.AreEqual(this.companyEntityId, context.ExternalCompanyId);
        }

        /// <summary>
        /// Test creating a context from a valid request without a company ID
        /// </summary>
        [TestMethod]
        public void CreateContextWithoutCompany()
        {
            var request = new ActivityRequest
            {
                Task = "Test",
                Values =
                {
                    { "AuthUserId", this.authUserId },
                }
            };

            var context = EntityActivity.CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);

            Assert.IsNotNull(context);
            Assert.AreEqual(this.authUserId, context.UserId);
            Assert.IsNull(context.ExternalCompanyId);
        }

        /// <summary>
        /// The entity filter is populated on the context according to the type of operation
        /// </summary>
        [TestMethod]
        public void CreateContextEntityFilterIsMapped()
        {
            var request = new ActivityRequest
            {
                Task = "Test",
                Values =
                {
                    { "AuthUserId", this.authUserId },
                }
            };

            // Internal operations map to the DefaultInternalEntityFilter
            var context = EntityActivity.CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            this.AssertEntityFilter(EntityActivity.DefaultInternalGetEntityFilter, context.EntityFilter);
            context = EntityActivity.CreateRepositoryContext(RepositoryContextType.InternalEntitySave, request);
            this.AssertEntityFilter(EntityActivity.DefaultInternalSaveEntityFilter, context.EntityFilter);
            
            // External operations map to the external get/set filters respectively
            context = EntityActivity.CreateRepositoryContext(RepositoryContextType.ExternalEntityGet, request);
            this.AssertEntityFilter(EntityActivity.DefaultGetEntityFilter, context.EntityFilter);
            context = EntityActivity.CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request);
            this.AssertEntityFilter(EntityActivity.DefaultSaveEntityFilter, context.EntityFilter);
        }

        /// <summary>
        /// Test creating a context from an invalid request missing the this.authUserId
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void CreateContextMissingUserId()
        {
            var request = new ActivityRequest
            {
                Task = "Test",
                Values = { }
            };

            EntityActivity.CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
        }

        /// <summary>
        /// Test creating a context from an invalid request missing the company ID
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateContextMissingCompanyId()
        {
            var request = new ActivityRequest
            {
                Task = "Test",
                Values =
                {
                    { "AuthUserId", this.authUserId }
                }
            };

            EntityActivity.CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request, "CompanyEntityId");
        }

        /// <summary>
        /// Compare the filters in two IEntityFilters to see if they are the same
        /// </summary>
        /// <param name="expectedEntityFilter">expected filter</param>
        /// <param name="actualEntityFilter">actual filter</param>
        private void AssertEntityFilter(IEntityFilter expectedEntityFilter, IEntityFilter actualEntityFilter)
        {
            Assert.IsTrue(expectedEntityFilter.Filters.SequenceEqual(actualEntityFilter.Filters));
        }
    }
}
