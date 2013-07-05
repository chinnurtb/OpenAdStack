//-----------------------------------------------------------------------
// <copyright file="GetAdvertisersActivityFixture.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using System.Xml;
using Activities;
using AppNexusActivities;
using AppNexusActivities.AppActivities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DataServiceUtilities;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using ResourceAccess;
using Rhino.Mocks;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>Tests for GetAdvertisersActivity</summary>
    [TestClass]
    public class GetAdvertisersActivityFixture
    {
        /// <summary>Random number generator</summary>
        private static readonly Random R = new Random();

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>Mock resources access handler for testing</summary>
        private IResourceAccessHandler mockAccessHandler;

        /// <summary>Mock AppNexus client for testing</summary>
        private IAppNexusApiClient mockAppNexusClient;

        /// <summary>
        /// The last request submitted via the test SubmitActivityRequestHandler
        /// </summary>
        private ActivityRequest submittedRequest;

        /// <summary>Companies for testing</summary>
        private CompanyEntity[] testCompanyEntities;

        /// <summary>Advertisers for testing</summary>
        private IDictionary<string, object>[] testAdvertisers;

        /// <summary>User entity for testing</summary>
        private UserEntity testUser;

        /// <summary>Logger for testing</summary>
        private TestLogger testLogger;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });

            this.CreateTestEntities();

            this.mockAppNexusClient = MockRepository.GenerateMock<IAppNexusApiClient>();
            this.mockAppNexusClient.Stub(f => f.GetMemberAdvertisers())
                .Return(this.testAdvertisers);
            
            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType)
                .Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything))
                .Return(this.mockAppNexusClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });

            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();
            this.mockRepository.Stub(f => f.GetFilteredEntityIds(Arg<RequestContext>.Is.Anything))
                .Return(this.testCompanyEntities.Select(e => (EntityId)e.ExternalEntityId).ToList());
            foreach (var testCompanyEntity in this.testCompanyEntities)
            {
                var entity = testCompanyEntity;
                this.mockRepository.Stub(f => f.GetEntity(
                    Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(entity.ExternalEntityId))).Return(entity);
            }

            RepositoryStubUtilities.SetupGetUserStub(
                this.mockRepository, this.testUser.UserId, this.testUser, false);

            this.mockAccessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.mockAccessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything))
                .Return(true);
        }

        /// <summary>Basic activity create test</summary>
        [TestMethod]
        public void Create()
        {
            var activity = this.CreateActivity();
            Assert.IsNotNull(activity);
        }

        /// <summary>Test exporting a creative</summary>
        [TestMethod]
        public void GetAppNexusAdvertisersJson()
        {
            var advertisersJson = this.GetAdvertisers(DataServiceResultsFormat.Json);
            Assert.IsFalse(string.IsNullOrWhiteSpace(advertisersJson));

            var advertisers = JsonConvert.DeserializeObject<IDictionary<string, object>[]>(advertisersJson);
            Assert.IsNotNull(advertisers);
            Assert.AreEqual(
                this.testAdvertisers.Length - (this.testAdvertisers.Length / 4),
                advertisers.Length);
        }

        /// <summary>Test exporting a creative</summary>
        [TestMethod]
        public void GetAppNexusAdvertisersXml()
        {
            var advertisersXml = this.GetAdvertisers(DataServiceResultsFormat.Xml);
            Assert.IsFalse(string.IsNullOrWhiteSpace(advertisersXml));

            var advertisers = new XmlDocument();
            advertisers.LoadXml(advertisersXml);
            var advertiserRows = advertisers.FirstChild;
            Assert.AreEqual("rows", advertiserRows.Name);
            Assert.AreEqual(
                this.testAdvertisers.Length - (this.testAdvertisers.Length / 4),
                advertiserRows.ChildNodes.Count);
        }

        /// <summary>Runs the activity and returns the results</summary>
        /// <param name="format">Format of results to request</param>
        /// <returns>The results</returns>
        /// <exception cref="AssertFailedException">
        /// Activity result failed to meet expectations
        /// </exception>
        private string GetAdvertisers(DataServiceResultsFormat format)
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.GetAdvertisers,
                Values =
                {
                    { "AuthUserId", this.testUser.UserId },
                    { DataServiceActivityValues.ResultsFormat, format.ToString() },
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey(DataServiceActivityValues.Results));

            return result.Values[DataServiceActivityValues.Results];
        }

        /// <summary>
        /// Creates an instance of the GetAdvertisersActivity activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private GetAdvertisersActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository },
                { typeof(IResourceAccessHandler), this.mockAccessHandler },
            };

            return Activity.CreateActivity(
                typeof(GetAdvertisersActivity),
                context,
                this.SubmitActivityRequest)
                as GetAdvertisersActivity;
        }

        /// <summary>Test submit activity request handler</summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if successful; otherwise, false.</returns>
        private bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            this.submittedRequest = request;
            return true;
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestEntities()
        {
            this.testUser = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(),
                R.Next().ToString(CultureInfo.InvariantCulture),
                "nobody@example.com");
            this.testUser.SetUserType(UserType.AppNexusApp);
            
            var apnxAdvertiserIds =
                Enumerable.Range(0, 700)
                .Select(i => R.Next())
                .Distinct()
                .ToArray();

            this.testCompanyEntities =
                apnxAdvertiserIds
                .Take(apnxAdvertiserIds.Length / 4)
                .Select(apnxid =>
                {
                    var company = EntityTestHelpers.CreateTestCompanyEntity(
                        new EntityId(), "Test AppNexus Company {0}".FormatInvariant(apnxid));
                    company.SetAppNexusAdvertiserId(apnxid);
                    return company;
                })
                .Concat(
                    Enumerable.Range(0, 20)
                    .Select(i =>
                        EntityTestHelpers.CreateTestCompanyEntity(
                        new EntityId(), "Test Non-AppNexus Company {0}".FormatInvariant(i))))
                .Concat(
                    Enumerable.Range(0, 5)
                    .Select(i =>
                    {
                        var company = EntityTestHelpers.CreateTestCompanyEntity(
                            new EntityId(), "Test Empty AppNexus AdvertiserId Company {0}".FormatInvariant(i));
                        company.SetPropertyValueByName(AppNexusEntityProperties.AdvertiserId, string.Empty);
                        return company;
                    }))
                .Concat(
                    Enumerable.Range(0, 2)
                    .Select(i =>
                    {
                        var company = EntityTestHelpers.CreateTestCompanyEntity(
                            new EntityId(), "Test Empty AppNexus AdvertiserId Company {0}".FormatInvariant(i));
                        company.SetPropertyValueByName(AppNexusEntityProperties.AdvertiserId, "undefined");
                        return company;
                    }))
                .ToArray();

            this.testAdvertisers = apnxAdvertiserIds
                .Select(apnxid => new Dictionary<string, object>
                {
                    { AppNexusValues.Id, apnxid },
                    { AppNexusValues.Name, "Test AppNexus Advertiser {0}".FormatInvariant(apnxid) },
                })
                .ToArray();
        }
    }
}
