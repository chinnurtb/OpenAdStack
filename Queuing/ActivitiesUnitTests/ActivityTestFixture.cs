//-----------------------------------------------------------------------
// <copyright file="ActivityTestFixture.cs" company="Rare Crowds Inc">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ActivityUnitTests
{
    /// <summary>
    /// Tests for Activity
    /// </summary>
    [TestClass]
    public class ActivityTestFixture
    {
        /// <summary>
        /// Whether or not SubmitActivityRequest has been called since the test began.
        /// </summary>
        private bool submitActivityRequestCalled;
        
        /// <summary>Initialization for the tests</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.submitActivityRequestCalled = false;
        }

        /// <summary>
        /// Test Activity.CreateActivity with a valid Activity type
        /// </summary>
        [TestMethod]
        public void CreateValidActivityTest()
        {
            Type testActivityType = typeof(TestHelpers.ValidRequiredValuesActivity);

            var activity = Activity.CreateActivity(testActivityType, null, TestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);
            Assert.IsInstanceOfType(activity, testActivityType);
        }

        /// <summary>
        /// Test Activity.CreateActivity with a valid Activity type
        /// </summary>
        [TestMethod]
        public void CreateValidActivityDefaultHandlerFactoryTest()
        {
            Type testActivityType = typeof(TestHelpers.ValidRequiredValuesActivity);

            var activity = Activity.CreateActivity(testActivityType, null, TestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);
            Assert.IsInstanceOfType(activity, testActivityType);
        }

        /// <summary>
        /// Test Activity.CreateActivity with default activity handler factory
        /// </summary>
        [TestMethod]
        public void CreateValidActivityWithDefaultHandlerFactoryTest()
        {
            Type testActivityType = typeof(TestHelpers.ValidActivity);

            var activity = Activity.CreateActivity(testActivityType, null, TestHelpers.SubmitActivityRequest) 
                as TestHelpers.ValidActivity;

            // Assert we have a default handler factory
            var handlerFactory = activity.ActivityHandlerFactoryTestProxy;
            Assert.IsInstanceOfType(handlerFactory, typeof(DefaultActivityHandlerFactory));

            // Assert it produces a default handler
            var handler = activity.ActivityHandlerFactoryTestProxy
                .CreateActivityHandler(new ActivityRequest(), new Dictionary<Type, object>());
            Assert.IsInstanceOfType(handler, typeof(DefaultActivityHandler));

            // Assert the default handler preduces and empty result value dictionary
            var defaultResult = handler.Execute();
            Assert.AreEqual(0, defaultResult.Count);
        }

        /// <summary>
        /// Test Activity.CreateActivity with a non-default activity handler factory
        /// </summary>
        [TestMethod]
        public void CreateValidActivityWithNonDefaultHandlerFactoryTest()
        {
            Type testActivityType = typeof(TestHelpers.ValidActivity);

            var activity = Activity.CreateActivity(
                new TestHelpers.NonDefaultActivityHandlerFactory(), testActivityType, null, TestHelpers.SubmitActivityRequest)
                    as TestHelpers.ValidActivity;

            // Assert we have a non-default handler factory
            var handlerFactory = activity.ActivityHandlerFactoryTestProxy;
            Assert.IsInstanceOfType(handlerFactory, typeof(TestHelpers.NonDefaultActivityHandlerFactory));
        }

        /// <summary>
        /// Test Activity.CreateActivity with an invalid Activity type
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateInvalidNamelessActivityTest()
        {
            Type testActivityType = typeof(TestHelpers.InvalidNamelessActivity);
            Activity.CreateActivity(testActivityType, null, TestHelpers.SubmitActivityRequest);
        }

        /// <summary>
        /// Test Activity.CreateActivity with an invalid Activity type
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateInvalidAbstractActivityTest()
        {
            Type testActivityType = typeof(TestHelpers.InvalidAbstractActivity);
            Activity.CreateActivity(testActivityType, null, TestHelpers.SubmitActivityRequest);
        }

        /// <summary>
        /// Test running an activity that generates another activity
        /// </summary>
        [TestMethod]
        public void SubmitActivityRequestActivityTest()
        {
            Type testActivityType = typeof(TestHelpers.CreateRequestActivity);
            var activity = Activity.CreateActivity(testActivityType, null, this.SubmitActivityRequest);
            Assert.IsNotNull(activity);
            Assert.IsInstanceOfType(activity, testActivityType);

            var request = new ActivityRequest { Task = "Test", Values = { } };
            activity.Run(request);
            Assert.IsTrue(this.submitActivityRequestCalled);
        }

        /// <summary>Submits an activity request from within an activity</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="sourceName">The source of the request</param>
        /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
        public bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            this.submitActivityRequestCalled = true;
            return true;
        }
    }
}
