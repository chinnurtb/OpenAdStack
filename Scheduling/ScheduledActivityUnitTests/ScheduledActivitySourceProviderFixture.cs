//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceProviderFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
