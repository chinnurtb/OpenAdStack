//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceProviderFixture.cs" company="Rare Crowds Inc">
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
using System.Reflection;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using ScheduledActivities;

namespace ScheduledActivityUnitTests
{
    /// <summary>Tests for ScheduledActivitySourceProvider</summary>
    [TestClass]
    public class ScheduledActivitySourceProviderFixture
    {
        /// <summary>Test creating a valid activity source</summary>
        [TestMethod]
        public void ProvidedSourcesFromThisAssembly()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var provider = new TestActivitySourceProvider(null);

            foreach (var sourceType in provider.ScheduledActivitySourceTypes)
            {
                Assert.AreEqual(thisAssembly, sourceType.Assembly);
            }
        }

        /// <summary>
        /// Scheduled activity source provider for testing
        /// </summary>
        public class TestActivitySourceProvider : ScheduledActivitySourceProvider
        {
            /// <summary>
            /// Initializes a new instance of the TestActivitySourceProvider class.
            /// </summary>
            /// <param name="queuer">Queuer used to enqueue created activity request work items</param>
            public TestActivitySourceProvider(IQueuer queuer)
                : base(queuer)
            {
            }
            
            /// <summary>
            /// Gets the context for the scheduled activity source providers.
            /// </summary>
            protected override object Context
            {
                get { return "Context"; }
            }
        }
    }
}
