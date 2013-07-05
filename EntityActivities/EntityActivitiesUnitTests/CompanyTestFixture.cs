//-----------------------------------------------------------------------
// <copyright file="CompanyTestFixture.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using EntityActivities;

using EntityActivitiesUnitTests;

using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using TestUtilities;

namespace EntityActivityUnitTests
{
    /// <summary>
    /// Test for Company related activities
    /// </summary>
    [TestClass]    
    public class CompanyTestFixture
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
        /// Company ExternalEntityId used in the tests
        /// </summary>
        private string companyEntityId;

        /// <summary>
        /// User ExternalEntityId used in the tests
        /// </summary>
        private string userEntityId;

        /// <summary>
        /// User UserId used in the tests
        /// </summary>
        private string userId;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.companyEntityId = EntityTestHelpers.NewEntityIdString();
            this.userEntityId = EntityTestHelpers.NewEntityIdString();
            this.userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);

            var contactEmail = "foo@example.com";
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.userId, contactEmail);

            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Anything)).Return(expectedUser);
        }
        
        /// <summary>
        /// Tests for creating new Company
        /// </summary>
        [TestMethod]
        public void CreateCompanyTest()
        {
            var externalName = "Test Company";
            var requestCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, externalName);
            var user = EntityTestHelpers.CreateTestUserEntity(new EntityId().ToString(), Guid.NewGuid().ToString(), "foo@example.com");
            var companyJson = requestCompany.SerializeToJson();

            // Set the repository mock to return the test Company
            this.repository.Stub(f => f.AddCompany(
                Arg<RequestContext>.Is.Anything,
                Arg<CompanyEntity>.Is.Anything));
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Anything)).Return(user);

            // Create the activity
            var activity = Activity.CreateActivity(typeof(CreateCompanyActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var activityRequest = new ActivityRequest
            {
                Values = 
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", EntityTestHelpers.NewEntityIdString() },
                    { "Payload", companyJson }
                }
            };

            var result = activity.Run(activityRequest);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Company");

            var resultCompany = EntityJsonSerializer.DeserializeCompanyEntity(new EntityId(this.companyEntityId), result.Values["Company"]);
            Assert.IsNotNull(resultCompany);
            Assert.AreEqual<string>(this.companyEntityId, resultCompany.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(externalName, resultCompany.ExternalName);       
        }

        /// <summary>
        /// Tests for updating already existing Company
        /// </summary>
        [TestMethod]
        public void SaveCompanyTest()
        {
            // Setup GetCompanies mock to return original company
            var originalName = "Test Company Original";
            var originalCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, originalName);

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, originalCompany, false);
            RequestContext calledContext = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CompanyEntity>(
                this.repository, (c, e) => { calledContext = c; }, false);

            // Setup company to be saved from the request
            var updatedName = "Test Company Edited";
            var requestCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, updatedName);
            var companyJson = requestCompany.SerializeToJson();

            // Create the activity
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };
            var activity = Activity.CreateActivity(typeof(SaveCompanyActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", this.companyEntityId },
                    { "Payload", companyJson }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Company");

            // Verify the extended properties and associations are not saved
            var expectedFilter = new RepositoryEntityFilter(true, true, false, false);
            Assert.IsTrue(expectedFilter.Filters.SequenceEqual(calledContext.EntityFilter.Filters));

            var resultCompany = EntityJsonSerializer.DeserializeCompanyEntity(
                new EntityId(request.Values["EntityId"]),
                result.Values["Company"]);
            Assert.IsNotNull(resultCompany);
            Assert.AreEqual<string>(this.companyEntityId, resultCompany.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(updatedName, resultCompany.ExternalName);
        }

        /// <summary>
        /// Tests updating an existing company with system properties in the
        /// original and only properties in the request.
        /// </summary>
        [TestMethod]
        public void SaveCompanyTestWithSystemPropertiesFromOriginal()
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
        /// Tests updating an existing company with properties in the original
        /// and only system properties in the request.
        /// </summary>
        [TestMethod]
        public void SaveCompanyTestWithSystemPropertiesFromRequest()
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
        /// Tests updating an existing company where properties and system
        /// properties are in both the original and the request.
        /// </summary>
        [TestMethod]
        public void SaveCompanyTestWithPropertiesFromOriginalAndRequest()
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

        /// <summary>
        /// Tests for getting existing Company associated with user
        /// </summary>
        [TestMethod]
        public void GetCompaniesForUserTest()
        {
            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            
            var externalName = "Test Company Edited";
            var contactEmail = "foo@example.com";
            var expectedCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, externalName);
            var expectedUser = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.userId, contactEmail);

            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Anything)).Return(expectedUser);
            
            // Set the repository mock to return companies
            this.repository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything))
                .Return(new List<EntityId> { expectedCompany.ExternalEntityId });
            this.repository.Stub(f => f.GetEntity(
                Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(expectedCompany.ExternalEntityId))).Return(expectedCompany);

            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCompaniesForUserActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "UserId", this.userId },
                    { "ExternalEntityEntityId", this.companyEntityId },
                    { "Company", expectedCompany.SerializeToJson() }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Companies");

            var companiesJson = result.Values["Companies"];
            Assert.IsNotNull(companiesJson);          

            // TODO: Parse list of company entities and better verify
            Assert.IsTrue(companiesJson.Contains(((EntityId)expectedCompany.ExternalEntityId).ToString()));
            Assert.IsTrue(companiesJson.Contains((string)expectedCompany.ExternalName));
        }

        /// <summary>
        /// Tests for getting existing Company associated with company
        /// </summary>
        [TestMethod]
        public void GetCompaniesForCompanyActivity()
        {
            var externalName = "Test Company Edited";
            var expectedCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, externalName);
            var parentCompany = EntityTestHelpers.CreateTestCompanyEntity(new EntityId(), "dontcare");

            // Set the repository mock to return the test Company
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, expectedCompany, false);
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { expectedCompany });

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCompaniesForCompanyActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "CompanyEntityId", this.companyEntityId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Companies");

            var companiesJson = result.Values["Companies"];
            Assert.IsNotNull(companiesJson);

            // TODO: Parse list of company entities and better verify
            Assert.IsTrue(companiesJson.Contains(((EntityId)expectedCompany.ExternalEntityId).ToString()));
            Assert.IsTrue(companiesJson.Contains((string)expectedCompany.ExternalName));
        }

        /// <summary>
        /// Test getting a company with associations
        /// </summary>
        [TestMethod]
        public void GetCompanyWithAssociations()
        {
            var advertiserEntityId = new EntityId();
            var advertiserExternalName = "Test Advertiser Company";
            var advertiserCompany = EntityTestHelpers.CreateTestCompanyEntity(advertiserEntityId, advertiserExternalName);

            var agencyEntityId = new EntityId();
            var agencyExternalName = "Test Agency Company";
            var agencyCompany = EntityTestHelpers.CreateTestCompanyEntity(agencyEntityId, agencyExternalName);

            agencyCompany.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = "Advertiser",
                TargetEntityCategory = CompanyEntity.CategoryName,
                TargetEntityId = advertiserEntityId,
                TargetExternalType = "???"
            });

            // Set the repository mock
            this.repository.Stub(f =>
                f.GetEntitiesById(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId[]>.Matches(ids => ids.Contains(advertiserEntityId))))
                .Return(new HashSet<IEntity> { advertiserCompany });

            this.repository.Stub(f =>
                f.GetEntitiesById(
                    Arg<RequestContext>.Is.Anything,
                    Arg<EntityId[]>.Matches(ids => ids.Contains(agencyEntityId))))
                .Return(new HashSet<IEntity> { agencyCompany });

            // Create the activity
            var activity = Activity.CreateActivity(
                typeof(GetCompanyByEntityIdActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", agencyEntityId }
                },
                QueryValues =
                {
                    { "Flags", "WithAssociations" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Company");

            var companyJson = result.Values["Company"];
            Assert.IsNotNull(companyJson);

            // TODO: Parse list of company entities and better verify
            Assert.IsTrue(companyJson.Contains(((EntityId)agencyCompany.ExternalEntityId).ToString()));
            Assert.IsTrue(companyJson.Contains((string)agencyCompany.ExternalName));
            Assert.IsTrue(companyJson.Contains(((EntityId)advertiserCompany.ExternalEntityId).ToString()));
        }

        /// <summary>
        /// Test getting a company by its entity id
        /// </summary>
        [TestMethod]
        public void GetCompanyByEntityIdTest()
        {
            var externalName = "Test Company Edited";
            var expectedCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, externalName);

            // Set the repository mock to return the test Company
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(new HashSet<IEntity> { expectedCompany });

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCompanyByEntityIdActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", this.companyEntityId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Company");

            var companyJson = result.Values["Company"];
            Assert.IsNotNull(companyJson);
            Assert.IsTrue(companyJson.Contains(((EntityId)expectedCompany.ExternalEntityId).ToString()));

            var company = EntityJsonSerializer.DeserializeCompanyEntity(this.companyEntityId, companyJson);
            Assert.AreEqual((string)expectedCompany.ExternalName, (string)company.ExternalName);
        }

        /// <summary>
        /// Test getting a nonexistent company by entity id
        /// </summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Code being tested does not look at the exception properties")]
        public void GetNonexistentCompanyByEntityIdTest()
        {
            // Set the repository mock to throw an ArgumentException which is expected for non-existent entities
            this.repository.Stub(f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Throw(new DataAccessEntityNotFoundException());

            // Create the activity
            var activity = Activity.CreateActivity(typeof(GetCompanyByEntityIdActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Get the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {   
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", this.companyEntityId }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.InvalidEntityId, this.companyEntityId);
        }

        /// <summary>
        /// Tests required values for CreateCompanyActivity
        /// </summary>
        [TestMethod]
        public void CreateCompanyRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(CreateCompanyActivity));              
        }

        /// <summary>
        /// Tests required values for SaveCompanyActivity
        /// </summary>
        [TestMethod]
        public void SaveCompanyRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(SaveCompanyActivity));
        }

        /// <summary>
        /// Tests required values for GetCompaniesForUserActivity
        /// </summary>
        [TestMethod]
        public void GetCompaniesForUserRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(GetCompaniesForUserActivity));
        }

        /// <summary>
        /// Tests required values for GetCompaniesForCompanyActivity
        /// </summary>
        [TestMethod]
        public void GetCompaniesForCompanyRequiredValuesTest()
        {
            ActivityTestHelpers.AssertErrorForMissingValues(typeof(GetCompaniesForCompanyActivity));
        }

        /// <summary>
        /// Tests for updating already existing Company, using properties
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
            // Setup the original company to be returned by GetCompanies
            var originalName = "Test Company Original";
            var originalCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, originalName);

            if (originalProperties != null)
            {
                originalCompany.Properties.Add(originalProperties);
            }

            if (originalSystemProperties != null)
            {
                originalCompany.Properties.Add(originalSystemProperties);
            }

            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, originalCompany, false);

            // Setup the company to be saved from the request
            var externalName = "Test Company Edited";
            var requestCompany = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, externalName);

            if (requestProperties != null)
            {
                requestCompany.Properties.Add(requestProperties);
            }

            if (requestSystemProperties != null)
            {
                requestCompany.Properties.Add(requestSystemProperties);
            }

            var companyJson = requestCompany.SerializeToJson(EntityActivityTestHelpers.BuildEntityFilter(true, false, false, null));

            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.userAccessRepository },
                    { typeof(IResourceAccessHandler), this.accessHandler }
                };

            // Create the activity
            var activity = Activity.CreateActivity(typeof(SaveCompanyActivity), activityContext, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Create the Company using the activity
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", this.companyEntityId },
                    { "Payload", companyJson },
                }
            };
            request.QueryValues.Add("Flags", "WithSystemProperties");

            var result = activity.Run(request);

            // Verify the result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, "Company");

            var resultCompany = EntityJsonSerializer.DeserializeCompanyEntity(new EntityId(request.Values["EntityId"]), result.Values["Company"]);
            Assert.IsNotNull(resultCompany);
            Assert.AreEqual<string>(this.companyEntityId, resultCompany.ExternalEntityId.Value.SerializationValue);
            Assert.AreEqual<string>(externalName, resultCompany.ExternalName);

            // Check the properties that were used
            var properties = resultCompany.Properties
                .Where(p => p.IsDefaultProperty)
                .ToDictionary(kvp => kvp.Name, kvp => kvp.Value);
            var expectedProperties = requestProperties ?? originalProperties;
            Assert.IsTrue(expectedProperties.All(p => properties.Any(kvp => p.Name == kvp.Key && (Guid)p.Value == (Guid)kvp.Value)));
            Assert.AreEqual(expectedProperties.Count(), properties.Count);

            // Check the system properties that were used
            var systemProperties = resultCompany.Properties
                .Where(p => p.IsSystemProperty)
                .ToDictionary(kvp => kvp.Name, kvp => kvp.Value);
            var expectedSystemProperties = requestSystemProperties ?? originalSystemProperties;
            Assert.IsTrue(expectedSystemProperties.All(p => systemProperties.Any(kvp => p.Name == kvp.Key && (Guid)p.Value == (Guid)kvp.Value)));
            Assert.AreEqual(expectedSystemProperties.Count(), systemProperties.Count);
        }
    }    
}
