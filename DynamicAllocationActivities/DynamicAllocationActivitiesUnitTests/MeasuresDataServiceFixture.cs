//-----------------------------------------------------------------------
// <copyright file="MeasuresDataServiceFixture.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using DataServiceUtilities;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Serialization;
using Utilities.Storage.Testing;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Tests for the measures data service activity</summary>
    [TestClass]
    public class MeasuresDataServiceFixture
    {
        /// <summary>Mock entity repository</summary>
        private IEntityRepository repository;

        /// <summary>Test user id</summary>
        private string userId;

        /// <summary>Test company entity id</summary>
        private string companyEntityId;

        /// <summary>Test campaign entity id</summary>
        private string campaignEntityId;

        /// <summary>Test company entity</summary>
        private CompanyEntity companyEntity;

        /// <summary>Test campaign entity</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Test measure sources</summary>
        private IMeasureSource[] measureSources;

        /// <summary>Gets the test measure source's measures</summary>
        private IDictionary<long, IDictionary<string, object>> Measures
        {
            get { return this.measureSources.SelectMany(source => source.Measures).ToDictionary(); }
        }

        /// <summary>
        /// Initialize the dynamic allocation service before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            AllocationParametersDefaults.Initialize();
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
            SimulatedPersistentDictionaryFactory.Initialize();

            this.InitializeMockRepository();
            this.InitializeMeasureSourceProvider();
        }

        /// <summary>Test creating an instance of MeasuresDataServiceActivity</summary>
        [TestMethod]
        public void CreateActivityTest()
        {
            var activity = this.CreateActivity();
            Assert.IsNotNull(activity);
            Assert.IsInstanceOfType(activity, typeof(MeasuresDataServiceActivity));
        }

        /// <summary>Test getting a subset of measures</summary>
        [TestMethod]
        public void GetMeasureSubset()
        {
            int offset = 10;
            int maxResults = 100;
            this.TestDataRangeRequest(offset, maxResults);
        }

        /// <summary>Test getting all of the measures</summary>
        [TestMethod]
        public void GetAllMeasures()
        {
            int offset = 0;
            int maxResults = this.Measures
                .Distinct(measure =>
                    measure.Value[MeasureValues.DisplayName])
                .Count();
            this.TestDataRangeRequest(offset, maxResults);
        }

        /// <summary>Test getting measures filtered by id</summary>
        [TestMethod]
        public void GetMeasuresById()
        {
            var ids = new long[] { 1106006, 1106008 };

            // Create a request for maximum results with a filter to only include DMA
            int maxResults = this.Measures
                .Distinct(measure => measure.Value[MeasureValues.DisplayName])
                .Count();
            var request = this.CreatePagedActivityRequest(0, maxResults);
            request.Values[DataServiceActivityValues.Ids] = string.Join(",", ids);

            // Run the activity and get the JSON results
            var results = this.RunJsonActivity(request);

            // Verify only results of the included id are returned
            Assert.IsTrue(results.Count == ids.Count());
            Assert.IsTrue(results.All(r => ids.Contains(r.Key)));
        }

        /// <summary>Test getting measures filtered by include types</summary>
        [TestMethod]
        public void GetIncludeFilteredMeasures()
        {
            var includeTypes = new[] { "DMA", "State" };

            // Create a request for maximum results with a filter to only include DMA
            int maxResults = this.Measures
                .Distinct(measure => measure.Value[MeasureValues.DisplayName])
                .Count();
            var request = this.CreatePagedActivityRequest(0, maxResults);
            request.Values[DataServiceActivityValues.Include] = string.Join(",", includeTypes);

            // Run the activity and get the JSON results
            var results = this.RunJsonActivity(request);

            // Verify only results of the included types are returned
            Assert.IsTrue(results.All(r => includeTypes.Contains((string)r.Value[MeasureValues.Type])));
        }

        /// <summary>Test getting measures filtered by exclude types</summary>
        [TestMethod]
        public void GetExcludeFilteredMeasures()
        {
            var excludeTypes = new[] { "Segment", "AgeRange" };

            // Create a request for maximum results with a filter to only include DMA
            int maxResults = this.Measures
                .Distinct(measure => measure.Value[MeasureValues.DisplayName])
                .Count();
            var request = this.CreatePagedActivityRequest(0, maxResults);
            request.Values[DataServiceActivityValues.Exclude] = string.Join(",", excludeTypes);

            // Run the activity and get the JSON results
            var results = this.RunJsonActivity(request);

            // Verify only results not of the excluded types are returned
            Assert.IsTrue(results.All(r => !excludeTypes.Contains((string)r.Value[MeasureValues.Type])));
        }

        /// <summary>Test getting measures filtered by include cost types</summary>
        [TestMethod]
        public void GetIncludeCostTypesFilteredMeasures()
        {
            var includeCostTypes = new[] { "NoCost", "Lotame" };

            // Create a request for maximum results with a filter to only include NoCost and Lotame
            int maxResults = this.Measures
                .Distinct(measure => measure.Value[MeasureValues.DisplayName])
                .Count();
            var request = this.CreatePagedActivityRequest(0, maxResults);
            request.Values[DynamicAllocationActivityValues.IncludeCostTypes] = string.Join(",", includeCostTypes);

            // Run the activity and get the JSON results
            var results = this.RunJsonActivity(request);

            // Verify only results of the included types are returned
            Assert.IsTrue(results.All(r => includeCostTypes.Contains((string)r.Value[MeasureValues.DataProvider])));
        }

        /// <summary>Test getting measures filtered by exclude cost types</summary>
        [TestMethod]
        public void GetExcludeCostTypesFilteredMeasures()
        {
            var excludeCostTypes = new[] { "Unknown", "NoCost" };

            // Create a request for maximum results with a filter to only include DMA
            int maxResults = this.Measures
                .Distinct(measure => measure.Value[MeasureValues.DisplayName])
                .Count();
            var request = this.CreatePagedActivityRequest(0, maxResults);
            request.Values[DynamicAllocationActivityValues.ExcludeCostTypes] = string.Join(",", excludeCostTypes);

            // Run the activity and get the JSON results
            var results = this.RunJsonActivity(request);

            // Verify only results not of the excluded types are returned
            Assert.IsTrue(results.All(r => !excludeCostTypes.Contains((string)r.Value[MeasureValues.DataProvider])));
        }

        /// <summary>Test formatting results as xml</summary>
        [TestMethod]
        public void FormatAsXmlPage()
        {
            var offset = 20;
            var maxResults = 75;

            var request = this.CreatePagedActivityRequest(offset, maxResults, DataServiceResultsFormat.Xml);
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsXml = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsXml));
        }

        /// <summary>Test getting top level results as xml</summary>
        [TestMethod]
        public void GetTopLevelCategoriesXmlSubtree()
        {
            string nodeId = null;
            var depth = 1;

            var request = this.CreateSubtreeActivityRequest(nodeId, depth);
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsXml = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsXml));

            // TODO: Assert row count and ids
        }

        /// <summary>Test getting a subtree of results as xml</summary>
        [TestMethod]
        public void GetAgeXmlSubtree()
        {
            var nodeId = "AppNexus:Age";
            var depth = 1;

            var request = this.CreateSubtreeActivityRequest(nodeId, depth);
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsXml = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsXml));

            // TODO: Assert row count and ids
        }

        /// <summary>Test getting partially loaded results as xml</summary>
        [TestMethod]
        public void GetPartiallyLoadedSubtree()
        {
            var embeddedMeasureSource = new EmbeddedJsonMeasureSource(
                Assembly.GetExecutingAssembly(),
                "DynamicAllocationActivitiesUnitTests.Resources.MeasureMap.js");
            var unloadedMeasureSource = MockRepository.GenerateMock<IMeasureSource>();
            unloadedMeasureSource.Stub(f => f.SourceId).Return("UnloadedSourceId");
            unloadedMeasureSource.Stub(f => f.Measures).Return(null);
            this.InitializeMeasureSourceProvider(
                embeddedMeasureSource,
                unloadedMeasureSource);
                
            var depth = 5;
            var request = this.CreateSubtreeActivityRequest(string.Empty, depth);
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsXml = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsXml));
        }

        /// <summary>Test filtering measures by cost type</summary>
        [TestMethod]
        public void IsMeasureOfCostType()
        {
            var measure = new Dictionary<string, object>
            {
                { "displayName", "AppNexus:Demographic:Age Ranges:Ages 18-24" },
                { "dataProvider", "NoCost" },
                { "network", "AppNexus" },
                { "type", "demographic" },
                { "subtype", "agerange" },
                { "APNXId", "18-24" },
            };

            Assert.IsFalse(MeasuresDataServiceActivity.IsMeasureOfCostType("UnKnown", measure));
            Assert.IsTrue(MeasuresDataServiceActivity.IsMeasureOfCostType("nocost", measure));
        }

        /// <summary>Delegate for submitting activity requests from within activities</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
        private static bool SubmitActivityRequestHandler(ActivityRequest request, string sourceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Test requesting a range of data</summary>
        /// <param name="offset">Request offset</param>
        /// <param name="maxResults">Request max results</param>
        private void TestDataRangeRequest(int offset, int maxResults)
        {
            var request = this.CreatePagedActivityRequest(offset, maxResults);
            var results = this.RunJsonActivity(request);
            Assert.AreEqual(maxResults, results.Count);

            // Assert that all the expected measures are present
            for (int i = 0; i < maxResults; i++)
            {
                results.Keys.Contains(this.Measures.Keys.ElementAt(offset + i));
            }
        }

        /// <summary>Creates and runs the activity with the provided request</summary>
        /// <param name="request">Request to run</param>
        /// <returns>Results for additional verification</returns>
        private IDictionary<long, IDictionary<string, object>> RunJsonActivity(ActivityRequest request)
        {
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsJson = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsJson));
            var results = AppsJsonSerializer.DeserializeObject<IDictionary<long, IDictionary<string, object>>>(resultsJson);
            Assert.IsNotNull(results);

            return results;
        }

        /// <summary>
        /// Creates a new instance of the MeasuresDataServiceActivity
        /// </summary>
        /// <returns>The activity instance</returns>
        private Activity CreateActivity()
        {
            var context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.repository }
            };
            return Activity.CreateActivity(
                typeof(MeasuresDataServiceActivity),
                context,
                SubmitActivityRequestHandler);
        }

        /// <summary>Creates a data service activity page request</summary>
        /// <remarks>Includes entities and range values</remarks>
        /// <param name="offset">Result offset to request</param>
        /// <param name="maxResults">Maximum number of results to request</param>
        /// <param name="format">Results format (default is JSON)</param>
        /// <returns>The activity request</returns>
        private ActivityRequest CreatePagedActivityRequest(
            int offset,
            int maxResults,
            DataServiceResultsFormat format = DataServiceResultsFormat.Json)
        {
            return new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetMeasures,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntityId },
                    { DataServiceActivityValues.ResultsFormat, format.ToString() },
                    { DataServiceActivityValues.Mode, DataServiceMode.Paged.ToString() },
                    { DataServiceActivityValues.Offset, offset.ToString(CultureInfo.InvariantCulture) },
                    { DataServiceActivityValues.MaxResults, maxResults.ToString(CultureInfo.InvariantCulture) },
                }
            };
        }

        /// <summary>Creates a data service activity subtree request</summary>
        /// <remarks>Includes nodeId and depth values</remarks>
        /// <param name="subtreePath">Path of the subtree</param>
        /// <param name="depth">Maximum depth of the subtree</param>
        /// <returns>The activity request</returns>
        private ActivityRequest CreateSubtreeActivityRequest(
            string subtreePath,
            int depth)
        {
            return new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetMeasures,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntityId },
                    { DataServiceActivityValues.SubtreePath, subtreePath },
                    { DataServiceActivityValues.Depth, depth.ToString(CultureInfo.InvariantCulture) },
                    { DataServiceActivityValues.Mode, DataServiceMode.Tree.ToString() },
                    { DataServiceActivityValues.ResultsFormat, DataServiceResultsFormat.Xml.ToString() },
                }
            };
        }

        /// <summary>Initialize a mock repository with test entities</summary>
        private void InitializeMockRepository()
        {
            // Create the test entities
            this.userId = Guid.NewGuid().ToString();

            this.companyEntityId = new EntityId();
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                this.companyEntityId.ToString(),
                "Test Company");

            this.campaignEntityId = new EntityId();
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId.ToString(),
                "Test Campaign",
                1000000,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(20),
                "???");
            this.campaignEntity.SetExporterVersion(1);

            // Initialize the mock repository
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.companyEntityId, this.companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.campaignEntityId, this.campaignEntity, false);
        }

        /// <summary>
        /// Initialize the MeasureSourceFactory with an embedded JSON measure source
        /// </summary>
        /// <param name="sources">
        /// Measure sources to provide. If not specified, a default measure source
        /// using the embedded MeasureMap.js will be provided.
        /// </param>
        private void InitializeMeasureSourceProvider(params IMeasureSource[] sources)
        {
            // Initialize an embedded JSON measure source
            this.measureSources =
                sources.Length > 0 ?
                sources :
                new IMeasureSource[]
                {
                    new EmbeddedJsonMeasureSource(
                        Assembly.GetExecutingAssembly(),
                        "DynamicAllocationActivitiesUnitTests.Resources.MeasureMap.js")
                };

            // Initialize the measure source factory with a mock measure source provider
            var measureSourceProvider = MockRepository.GenerateMock<IMeasureSourceProvider>();
            measureSourceProvider.Stub(f => f.DeliveryNetwork)
                .Return(DeliveryNetworkDesignation.AppNexus);
            measureSourceProvider.Stub(f => f.Version)
                .Return(1);
            measureSourceProvider.Stub(f => f.GetMeasureSources(Arg<object[]>.Is.Anything))
                .Return(this.measureSources);
            MeasureSourceFactory.Initialize(new[] { measureSourceProvider });
        }
    }
}
