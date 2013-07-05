//-----------------------------------------------------------------------
// <copyright file="TestHelpers.cs" company="Rare Crowds Inc">
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
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Activities;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ActivityUnitTests
{
    /// <summary>Helpers for the activity unit tests</summary>
    [TestClass]
    public static class TestHelpers
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
            TestHelpers.MockLogger = MockRepository.GenerateMock<ILogger>();
            LogManager.Initialize(new[] { TestHelpers.MockLogger });
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
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Error);
        }

        /// <summary>Assert the result has values for the specified keys</summary>
        /// <param name="result">the result</param>
        /// <param name="keys">the keys</param>
        public static void AssertResultHasValues(ActivityResult result, params string[] keys)
        {
            Assert.IsNotNull(result.Values);
            Assert.AreEqual(keys.Length, result.Values.Keys.Intersect(keys).Count());
        }
        
        /// <summary>Valid Activity without RequiredValues or ResultValues</summary>
        [Name("ValidActivity")]
        internal class ValidActivity : Activity
        {
            /// <summary>
            /// Gets the ActivityHandlerFactory (exposes for testing)
            /// </summary>
            internal IActivityHandlerFactory ActivityHandlerFactoryTestProxy
            {
                get { return this.ActivityHandlerFactory; }
            }

            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                return new ActivityResult
                {
                    Succeeded = true,
                    Values = request.Values
                };
            }
        }

        /// <summary>Valid Activity with RequiredValues</summary>
        [Name("ValidRequiredValuesActivity"), RequiredValues("Foo", "Bar")]
        internal class ValidRequiredValuesActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Valid Activity with RequiredValues and ResultValues</summary>
        [Name("ValidRequiredValuesAndResultValuesActivity"), RequiredValues("Foo", "Bar"), ResultValues("FooBar")]
        internal class ValidRequiredValuesAndResultValuesActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Invalid Activity missing its Name attribute</summary>
        internal class InvalidNamelessActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Invalid Abstract Activity for testing</summary>
        [Name("InvalidAbstractTestActivity")]
        internal abstract class InvalidAbstractActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Invalid duplicate of ValidActivity</summary>
        [Name("ValidActivity")]
        internal class InvalidDuplicateOfValidActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Valid Activity which creates an activity request</summary>
        [Name("CreateRequestActivity")]
        internal class CreateRequestActivity : Activity
        {
            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                var newRequest = new ActivityRequest
                {
                    Task = "Test",
                    Values = { }
                };
                this.SubmitRequest(request, false);
                return new ActivityResult { Succeeded = true };
            }
        }

        /// <summary>Valid activity which handles results</summary>
        [Name("HandlesResultActivity")]
        internal class HandlesResultActivity : Activity
        {
            /// <summary>Gets the last handled result</summary>
            public static ActivityResult LastHandledResult { get; private set; }

            /// <summary>Handles an activity result</summary>
            /// <param name="result">The result</param>
            public override void OnActivityResult(ActivityResult result)
            {
                LastHandledResult = result;
            }

            /// <summary>Process the request</summary>
            /// <param name="request">The request</param>
            /// <returns>The result</returns>
            [ExcludeFromCodeCoverage]
            protected override ActivityResult ProcessRequest(ActivityRequest request)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>Non-default activity handler factory for testing.</summary>
        internal class NonDefaultActivityHandlerFactory : IActivityHandlerFactory
        {
            /// <summary>Create the activity handler.</summary>
            /// <param name="request">The activity request.</param>
            /// <param name="context">The activity context.</param>
            /// <returns>An IActivityHandler instance.</returns>
            public IActivityHandler CreateActivityHandler(ActivityRequest request, IDictionary<Type, object> context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
