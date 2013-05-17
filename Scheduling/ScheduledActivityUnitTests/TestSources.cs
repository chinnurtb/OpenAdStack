//-----------------------------------------------------------------------
// <copyright file="TestSources.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using ScheduledActivities;
using ScheduledActivities.Schedules;
using ScheduledWorkItems;

namespace ScheduledActivityUnitTests
{
    /// <summary>Contains ScheduledActivitySource implementations for testing</summary>
    internal static class TestSources
    {
        /// <summary>Base class for test ScheduledActivitySource implementations</summary>
        /// <remarks>
        /// Bare implementation which returns null for both abstract members of
        /// ScheduledActivitySource. Used primarily as a base for testing required attributes.
        /// </remarks>
        public abstract class NullScheduledActivitySourceBase : ScheduledActivitySource
        {
            /// <summary>Creates new scheduled activity requests</summary>
            public override void CreateScheduledRequests()
            {
            }

            /// <summary>Handler for activity results</summary>
            /// <param name="request">The activity request</param>
            /// <param name="result">The activity result</param>
            public override void OnActivityResult(ActivityRequest request, ActivityResult result)
            {
            }
        }

        /// <summary>Valid ScheduledActivitySource</summary>
        /// <remarks>Scheduled to run every 10 milliseconds.</remarks>
        [SourceName("ValidActivitySource"), Schedule(typeof(IntervalSchedule), 0, 0, 0, 0, 50)]
        public class ValidActivitySource : NullScheduledActivitySourceBase
        {
            /// <summary>Gets the last activity result handled</summary>
            public ActivityResult LastResult { get; private set; }

            /// <summary>Creates a scheduled activity request</summary>
            public override void CreateScheduledRequests()
            {
                var request = new ActivityRequest
                {
                    Task = "Test",
                    Values = { }
                };
                this.SubmitRequest(request, ActivityRuntimeCategory.Interactive, false);
            }

            /// <summary>Handler for activity results</summary>
            /// <param name="request">The activity request</param>
            /// <param name="result">The activity result</param>
            public override void OnActivityResult(ActivityRequest request, ActivityResult result)
            {
                this.LastResult = result;
            }
        }

        /// <summary>Invalid ScheduledActivitySource missing the SourceName and Schedule attributes</summary>
        public class MissingSourceNameAndScheduleActivitySource : NullScheduledActivitySourceBase
        {
        }

        /// <summary>Invalid ScheduledActivitySource missing the SourceName attribute</summary>
        [Schedule(typeof(IntervalSchedule), 0, 0, 1)]
        public class MissingSourceNameActivitySource : NullScheduledActivitySourceBase
        {
        }

        /// <summary>Invalid ScheduledActivitySource missing the SourceName attribute</summary>
        [SourceName("MissingSchedule")]
        public class MissingScheduleActivitySource : NullScheduledActivitySourceBase
        {
        }

        /// <summary>Invalid ScheduledActivitySource with missing schedule args</summary>
        [SourceName("MissingScheduleArgs"), Schedule(typeof(IntervalSchedule))]
        public class MissingScheduleArgsActivitySource : NullScheduledActivitySourceBase
        {
        }

        /// <summary>Invalid ScheduledActivitySource with invalid schedule args</summary>
        [SourceName("InvalidScheduleArgs"), Schedule(typeof(IntervalSchedule), 327.46)]
        public class InvalidScheduleArgsActivitySource : NullScheduledActivitySourceBase
        {
        }

        /// <summary>Invalid ScheduledActivitySource with invalid schedule args</summary>
        [SourceName("InvalidScheduleArgValues"), Schedule(typeof(IntervalSchedule), "Not A Valid TimeSpan String")]
        public class InvalidScheduleArgValueActivitySource : NullScheduledActivitySourceBase
        {
        }
    }
}
