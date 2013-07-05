//-----------------------------------------------------------------------
// <copyright file="ActivityTestHelpers.cs" company="Emerging Media Group">
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
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Activities;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ActivityTestUtilities
{
    /// <summary>Helpers for the activity unit tests</summary>
    [TestClass]
    public static class ActivityTestHelpers
    {
        /// <summary>
        /// Gets the mock logger
        /// </summary>
        public static ILogger MockLogger { get; private set; }

        /// <summary>Creates a mock of ILogger and initializes the LogManager with it.</summary>
        /// <param name="context">Parameter is not used</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            MockLogger = MockRepository.GenerateMock<ILogger>();
            LogManager.Initialize(new[] { MockLogger });
        }

        /// <summary>Submits an activity request from within an activity</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="sourceName">The source of the request</param>
        /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
        [ExcludeFromCodeCoverage]
        public static bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Assert the result is a valid success response</summary>
        /// <param name="result">the result</param>
        public static void AssertValidSuccessResult(ActivityResult result)
        {
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);
        }

        /// <summary>Assert the result has values for the specified keys</summary>
        /// <param name="result">the result</param>
        /// <param name="keys">the keys</param>
        public static void AssertResultHasValues(ActivityResult result, params string[] keys)
        {
            Assert.IsNotNull(result.Values);
            Assert.AreEqual(keys.Length, result.Values.Keys.Intersect(keys).Count());
        }

        /// <summary>
        /// Assert the result is a valid error response
        /// </summary>
        /// <param name="result">The result to validate</param>
        /// <param name="errorId">Expected ErrorId</param>
        /// <param name="messageContains">Text expected to be contained by the message (optional)</param>
        public static void AssertValidErrorResult(ActivityResult result, ActivityErrorId errorId, string messageContains)
        {
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual((int)errorId, result.Error.ErrorId);

            if (messageContains != null)
            {
                Assert.IsNotNull(result.Error.Message);
                Assert.IsTrue(result.Error.Message.Contains(messageContains));
            }
        }

        /// <summary>
        /// Assert the values of the result are as expected
        /// </summary>
        /// <param name="result">the result</param>
        /// <param name="expectedValues">the expected values</param>
        public static void AssertResultValuesAreEqual(ActivityResult result, NameValueCollection expectedValues)
        {
            Assert.IsNotNull(result.Values);
            Assert.AreEqual(expectedValues.Count, result.Values.Count);
            foreach (var key in expectedValues.AllKeys)
            {
                Assert.AreEqual(expectedValues[key], result.Values[key]);
            }
        }

        /// <summary>
        /// Assert the request has values for the specified keys
        /// </summary>
        /// <param name="request">the request</param>
        /// <param name="keys">the keys</param>
        public static void AssertRequestHasValues(ActivityRequest request, params string[] keys)
        {
            Assert.IsNotNull(request.Values);
            Assert.AreEqual(keys.Length, request.Values.Keys.Intersect(keys).Count());
        }

        /// <summary>
        /// Asserts error for missing values for requested activity type
        /// </summary>
        /// <remarks>Calling Test Method should contains [ExpectedException(typeof(ArgumentException))] attribute.</remarks>
        /// <param name="activityType">Type of activity</param>   
        public static void AssertErrorForMissingValues(Type activityType)
        {
            // Create the activity
            var activity = Activity.CreateActivity(activityType, null, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Getting required values for requested activity
            var requiredValuesAttributes = activityType.GetCustomAttributes(typeof(RequiredValuesAttribute), true);
            var requiredValues = requiredValuesAttributes.Cast<RequiredValuesAttribute>().SelectMany(a => a.ValueNames).ToArray();

            // Creating activity request with missing required values
            var request = new ActivityRequest();

            // running the requested activity
            var result = activity.Run(request);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual((int)ActivityErrorId.MissingRequiredInput, result.Error.ErrorId);
            foreach (var value in requiredValues)
            {
                // Asserting exception message contains reqired values
                Assert.IsTrue(result.Error.Message.Contains(value));
            }
        }
    }
}
