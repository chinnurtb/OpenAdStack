// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRetryProviderFixture.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>
    /// Unit test fixture for DefaultRetryProvider
    /// </summary>
    [TestClass]
    public class DefaultRetryProviderFixture
    {
        /// <summary>Per test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new List<ILogger> { MockRepository.GenerateStub<ILogger>() });
        }

        /// <summary>Test default contruction.</summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            var retryProvider = new DefaultRetryProvider(5, 5001);
            Assert.AreEqual(4, retryProvider.MaxRetries);
            Assert.AreEqual(5001, retryProvider.WaitTime);
        }

        /// <summary>Test clone.</summary>
        [TestMethod]
        public void Clone()
        {
            var retryProvider = new DefaultRetryProvider(5, 5001);
            var clonedProvider = retryProvider.Clone();
            Assert.AreEqual(4, clonedProvider.MaxRetries);
            Assert.AreEqual(5001, clonedProvider.WaitTime);
        }

        /// <summary>Retry or throw when retries are left, no wait.</summary>
        [TestMethod]
        public void RetryOrThrowRetriesLeft()
        {
            var retryProvider = new DefaultRetryProvider(5, 5000);
            var remainingTries = 1;
            var timeNow = DateTime.Now;
            retryProvider.RetryOrThrow(new DataAccessException(), ref remainingTries, true, false);

            Assert.AreEqual(0, remainingTries);

            var duration = (DateTime.Now - timeNow).TotalMilliseconds;

            // 5000 minus some room for randomness
            Assert.IsTrue(duration < 4500);
        }

        /// <summary>Retry or throw when no retries are left.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void RetryOrThrowNoRetriesLeftNoWait()
        {
            var retryProvider = new DefaultRetryProvider(5, 5001);
            var remainingTries = 0;
            retryProvider.RetryOrThrow(new DataAccessException(), ref remainingTries, true, false);
        }

        /// <summary>Retry or throw when no retries are left.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void RetryOrThrowCanRetryFalse()
        {
            var retryProvider = new DefaultRetryProvider(5, 5001);
            var remainingTries = 1;
            retryProvider.RetryOrThrow(new DataAccessException(), ref remainingTries, false, false);
        }

        /// <summary>Retry or throw when retries are left and wait required.</summary>
        [TestMethod]
        public void RetryOrThrowWait()
        {
            var retryProvider = new DefaultRetryProvider(5, 100);
            var remainingTries = 1;
            var timeNow = DateTime.Now;
            try
            {
                retryProvider.RetryOrThrow(new DataAccessException(), ref remainingTries, true, true);
            }
            catch (DataAccessException)
            {
                var duration = (DateTime.Now - timeNow).TotalMilliseconds;

                // With three tries we should sleep twice
                Assert.IsTrue(duration >= 100);
            }
        }
    }
}
