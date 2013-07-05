//-----------------------------------------------------------------------
// <copyright file="DataServiceActivityFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Activities;
using ActivityTestUtilities;
using DataServiceUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DataServiceUtilitiesUnitTests
{
    /// <summary>Unit tests for the data service activity base class</summary>
    [TestClass]
    public class DataServiceActivityFixture
    {
        /// <summary>
        /// Test creating a DataServiceActivityBase derived activity instance
        /// </summary>
        [TestMethod]
        public void CreateTest()
        {
            var activity = Activity.CreateActivity(
                typeof(TestDataServiceActivity),
                null,
                SubmitActivityRequestHandler);
            Assert.IsNotNull(activity);
            Assert.IsInstanceOfType(activity, typeof(TestDataServiceActivity));
        }

        /// <summary>
        /// Test requesting fewer results than are available
        /// </summary>
        [TestMethod]
        public void RequestResultSubset()
        {
            var offset = 0;
            var maxResults = 5;
            var availableResults = 10;
            var expectedResults = maxResults;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>
        /// Test requesting fewer results than are available starting from an offset
        /// </summary>
        [TestMethod]
        public void RequestResultSubsetWithOffset()
        {
            var offset = 3;
            var maxResults = 5;
            var availableResults = 10;
            var expectedResults = maxResults;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>
        /// Test requesting more results than are available
        /// </summary>
        [TestMethod]
        public void RequestResultSuperset()
        {
            var offset = 0;
            var maxResults = 10;
            var availableResults = 5;
            var expectedResults = 5;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>
        /// Test requesting more results than are available starting from an offset
        /// </summary>
        [TestMethod]
        public void RequestResultSupersetWithOffset()
        {
            var offset = 10;
            var maxResults = 50;
            var availableResults = 20;
            var expectedResults = availableResults - offset;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>
        /// Test requesting more results than are available
        /// </summary>
        [TestMethod]
        public void RequestNoResultsAvailable()
        {
            var offset = 0;
            var maxResults = 10;
            var availableResults = 0;
            var expectedResults = 0;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>
        /// Test requesting more results than are available starting from an offset
        /// </summary>
        [TestMethod]
        public void RequestNoResultsAvailableWithOffset()
        {
            var offset = 10;
            var maxResults = 50;
            var availableResults = 0;
            var expectedResults = 0;
            TestRequestPagedResults(availableResults, offset, maxResults, expectedResults);
        }

        /// <summary>Test requesting the top level nodes of a tree</summary>
        [TestMethod]
        public void RequestTopLevelSubtreeResults()
        {
            string subtreePath = null;
            var depth = 1;
            var graphBreadth = 3;
            var graphDepth = 5;
            var expectedResults = 4;

            TestRequestSubtreeResults(subtreePath, depth, graphBreadth, graphDepth, expectedResults);
            
            // TODO: Verify the node ids in the results match those expected
        }

        /// <summary>Test requesting a shallow subtree of one of the top level nodes</summary>
        [TestMethod]
        public void RequestShallowTopSubtreeResults()
        {
            string subtreePath = "1";
            var depth = 1;
            var graphBreadth = 4;
            var graphDepth = 5;
            var expectedResults = 4;

            TestRequestSubtreeResults(subtreePath, depth, graphBreadth, graphDepth, expectedResults);

            // TODO: Verify the node ids in the results match those expected
        }

        /// <summary>Test requesting a shallow subtree of one of the top level nodes</summary>
        [TestMethod]
        public void RequestDeepSubtreeResults()
        {
            string subtreePath = "1:2";
            var depth = 3;
            var graphBreadth = 4;
            var graphDepth = 5;
            var expectedResults = 84;

            TestRequestSubtreeResults(subtreePath, depth, graphBreadth, graphDepth, expectedResults);

            // TODO: Verify the node ids in the results match those expected
        }

        /// <summary>Test requesting a full tree</summary>
        [TestMethod]
        public void RequestFullTree()
        {
            string subtreePath = null;
            var depth = 4;
            var graphBreadth = 3;
            var graphDepth = 4;
            var expectedResults = 129;

            TestRequestSubtreeResults(subtreePath, depth, graphBreadth, graphDepth, expectedResults);

            // TODO: Verify the node ids in the results match those expected
        }

        /// <summary>Test requesting a subtree with unloaded results</summary>
        [TestMethod]
        public void RequestUnloadedResults()
        {
            string subtreePath = "100";
            var depth = 3;
            var graphBreadth = 2;
            var graphDepth = depth;
            var expectedResults = 1;

            TestRequestSubtreeResults(subtreePath, depth, graphBreadth, graphDepth, expectedResults);

            // TODO: Verify the node ids in the results match those expected
        }

        /// <summary>Test requesting results with a filter</summary>
        /// <remarks>TODO: Refactor into helper methods when/if variation tests are added</remarks>
        [TestMethod]
        public void TestRequestFilteredResults()
        {
            // Create the activity
            var activity = (TestDataServiceActivity)Activity.CreateActivity(
                typeof(TestDataServiceActivity),
                null,
                SubmitActivityRequestHandler);
            activity.ResultCount = 10;

            // Randomly select two results to be excluded
            var excludes = activity.Results.Take(2);
            
            // Create an activity request including the exclude filter
            var request = new ActivityRequest
            {
                Task = "TestDataServiceActivity",
                Values =
                {
                    { DataServiceActivityValues.Mode, DataServiceMode.Paged.ToString() },
                    { DataServiceActivityValues.ResultsFormat, DataServiceResultsFormat.Json.ToString() },
                    { DataServiceActivityValues.Offset, "0" },
                    { DataServiceActivityValues.MaxResults, "100" },
                    { DataServiceActivityValues.Exclude, string.Join(";", excludes) },
                }
            };

            // Run the activity and check the result
            var result = activity.Run(request);
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(
                result,
                DataServiceActivityValues.Results,
                DataServiceActivityValues.Total);

            // Verify two results were filtered out
            var resultTotal = Convert.ToInt32(result.Values[DataServiceActivityValues.Total]);
            Assert.AreEqual(activity.Results.Length - 2, resultTotal);

            // Verify results are valid JSON
            var resultsJson = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsJson));
            var results = JsonConvert.DeserializeObject<Guid[]>(resultsJson);
            Assert.IsNotNull(results);

            // Verify results do not include those to be excluded
            Assert.AreEqual(0, results.Intersect(excludes).Count());
        }

        /// <summary>Request results and do basic verification</summary>
        /// <param name="availableResults">Number of results available</param>
        /// <param name="offset">Request offset value</param>
        /// <param name="maxResults">Request maxResults value</param>
        /// <param name="expectedResults">Number of results expected</param>
        private static void TestRequestPagedResults(
            int availableResults,
            int offset,
            int maxResults,
            int expectedResults)
        {
            var request = CreatePagedRequest(offset, maxResults);
            var activity = CreateActivity(availableResults);

            var result = activity.Run(request);
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(
                result,
                DataServiceActivityValues.Results,
                DataServiceActivityValues.Total);

            var resultsJson = result.Values[DataServiceActivityValues.Results];
            Assert.IsFalse(string.IsNullOrWhiteSpace(resultsJson));

            var results = JsonConvert.DeserializeObject<Guid[]>(resultsJson);
            Assert.IsNotNull(results);
            Assert.AreEqual(expectedResults, results.Length);

            for (int i = 0; i < expectedResults; i++)
            {
                Assert.AreEqual(
                    activity.Results[offset + i],
                    results[i]);
            }

            var resultTotal = Convert.ToInt32(result.Values[DataServiceActivityValues.Total]);
            Assert.AreEqual(availableResults, resultTotal);
        }

        /// <summary>Test requesting subtree results</summary>
        /// <param name="subtreePath">Subtree path to request</param>
        /// <param name="depth">Subtree depth</param>
        /// <param name="graphBreadth">Available graph breadth</param>
        /// <param name="graphDepth">Available graph depth</param>
        /// <param name="expectedNodes">Number of nodes expected</param>
        /// <returns>Subtree xml for further validation</returns>
        private static string TestRequestSubtreeResults(string subtreePath, int depth, int graphBreadth, int graphDepth, int expectedNodes)
        {
            var request = new ActivityRequest
            {
                Task = "TestXmlDataService",
                Values =
                {
                    { DataServiceActivityValues.ResultsFormat, DataServiceResultsFormat.Xml.ToString() },
                    { DataServiceActivityValues.Mode, DataServiceMode.Tree.ToString() },
                    { DataServiceActivityValues.Depth, depth.ToString(CultureInfo.InvariantCulture) },
                    { DataServiceActivityValues.SubtreePath, subtreePath },
                }
            };

            var activity = (TestXmlDataServiceActivity)Activity.CreateActivity(
                typeof(TestXmlDataServiceActivity),
                null,
                SubmitActivityRequestHandler);
            activity.Breadth = graphBreadth;
            activity.Depth = graphDepth;

            var result = activity.Run(request);

            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(result, DataServiceActivityValues.Results);

            var resultsXml = result.Values[DataServiceActivityValues.Results];
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(resultsXml);

            var nav = xmlDoc.CreateNavigator();
            var nodeCount = nav.Select("//row").Count;
            Assert.AreEqual(expectedNodes, nodeCount);
            
            return xmlDoc.OuterXml;
        }
        
        /// <summary>
        /// Creates an instance of the TestDataServiceActivity and sets its resultSet count.
        /// </summary>
        /// <param name="initialResultCount">Initial resultSet count</param>
        /// <returns>The created activity instance</returns>
        private static TestDataServiceActivity CreateActivity(int initialResultCount)
        {
            var activity = (TestDataServiceActivity)Activity.CreateActivity(
                typeof(TestDataServiceActivity),
                null,
                SubmitActivityRequestHandler);
            activity.ResultCount = initialResultCount;
            return activity;
        }

        /// <summary>Creates a TestDataServiceActivity request</summary>
        /// <param name="offset">Request offset value</param>
        /// <param name="maxResults">Request maxResults value</param>
        /// <returns>The created request</returns>
        private static ActivityRequest CreatePagedRequest(int offset, int maxResults)
        {
            return CreatePagedRequest("TestDataServiceActivity", offset, maxResults);
        }

        /// <summary>Creates a data service activity request</summary>
        /// <param name="taskName">Request task name</param>
        /// <param name="offset">Request offset value</param>
        /// <param name="maxResults">Request maxResults value</param>
        /// <returns>The created request</returns>
        private static ActivityRequest CreatePagedRequest(string taskName, int offset, int maxResults)
        {
            return new ActivityRequest
            {
                Task = taskName,
                Values =
                {
                    { DataServiceActivityValues.Mode, DataServiceMode.Paged.ToString() },
                    { DataServiceActivityValues.ResultsFormat, DataServiceResultsFormat.Json.ToString() },
                    { DataServiceActivityValues.Offset, offset.ToString(CultureInfo.InvariantCulture) },
                    { DataServiceActivityValues.MaxResults, maxResults.ToString(CultureInfo.InvariantCulture) }
                }
            };
        }

        /// <summary>
        /// SubmitActivityRequestHandler delegate (required to create activity instances)
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source</param>
        /// <returns>Nothing (throws NotImplementedException)</returns>
        private static bool SubmitActivityRequestHandler(ActivityRequest request, string sourceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>DataServiceActivityBase derived class for testing</summary>
        [Name("TestDataServiceActivity")]
        private class TestDataServiceActivity : DataServiceActivityBase<Guid>
        {
            /// <summary>Backing field for Results</summary>
            private Guid[] results;

            /// <summary>Backing field for ResultCount</summary>
            private int resultCount;

            /// <summary>Gets the set of results to be returned</summary>
            /// <remarks>Note: setting ResultCount will reset Results</remarks>
            public Guid[] Results
            {
                get
                {
                    return this.results = this.results ??
                        Enumerable.Range(0, this.ResultCount)
                        .Select(i => Guid.NewGuid())
                        .OrderBy(r => this.GetResultPath(r))
                        .ToArray();
                }
            }

            /// <summary>Gets or sets the number of results GetResults should return</summary>
            /// <remarks>Note: setting ResultCount will reset Results</remarks>
            public int ResultCount
            {
                get
                {
                    return this.resultCount;
                }

                set
                {
                    this.results = null;
                    this.resultCount = value;
                }
            }

            /// <summary>Gets the result path separator</summary>
            protected override char ResultPathSeparator
            {
                get { return '-'; }
            }

            /// <summary>Gets the path for a result</summary>
            /// <param name="result">The result</param>
            /// <returns>The result's path</returns>
            protected override string GetResultPath(Guid result)
            {
                return result.ToString();
            }

            /// <summary>Gets whether the result is loaded</summary>
            /// <param name="result">The result</param>
            /// <returns>Whether the result is loaded</returns>
            protected override bool IsResultLoaded(Guid result)
            {
                return true;
            }

            /// <summary>Determine whether a result should be filtered</summary>
            /// <param name="result">Result to be filtered</param>
            /// <param name="requestValues">Request values</param>
            /// <returns>True if the result should be filtered out; otherwise, false</returns>
            protected override bool FilterResult(Guid result, IDictionary<string, string> requestValues)
            {
                if (requestValues.ContainsKey("Exclude"))
                {
                    var excludes = requestValues["Exclude"].Split(';').Select(exclude => new Guid(exclude));
                    return excludes.Contains(result);
                }
                else
                {
                    return false;
                }
            }

            /// <summary>Gets results based upon the request values</summary>
            /// <param name="requestValues">The request values</param>
            /// <returns>The results</returns>
            protected override Guid[] GetResults(IDictionary<string, string> requestValues)
            {
                return this.Results;
            }
        }
    }
}
