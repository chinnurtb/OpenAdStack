//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySource.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Activities;
using ConfigManager;
using Diagnostics;
using Queuing;
using ScheduledWorkItems;
using Utilities.Storage;
using WorkItems;

namespace ScheduledActivities
{
    /// <summary>Abstract base class for sources of scheduled activities</summary>
    public abstract class ScheduledActivitySource : IScheduledWorkItemSource
    {
        /// <summary>
        /// Name of the store to which create scheduled requests start times are persisted.
        /// </summary>
        private const string CreateScheduledRequestsLastStartTimesStoreName = "scheduledactivities-createstarttimes";

        /// <summary>
        /// Name of the store to which create scheduled requests end times are persisted.
        /// </summary>
        private const string CreateScheduledRequestsLastEndTimesStoreName = "scheduledactivities-createendtimes";

        /// <summary>
        /// Shared persistent dictionary of when CreateScheduledRequests was last started.
        /// TODO: Make static
        /// </summary>
        /// <remarks>
        /// This is set before calling CreateScheduledRequests.
        /// </remarks>
        private IPersistentDictionary<DateTime> createScheduledRequestsLastStartTimes;

        /// <summary>
        /// Shared persistent dictionary of when CreateScheduledRequests was last run.
        /// TODO: Make static
        /// </summary>
        /// <remarks>
        /// This is set after the created requests have been persisted.
        /// </remarks>
        private IPersistentDictionary<DateTime> createScheduledRequestsLastEndTimes;

        /// <summary>
        /// Queuer used to enqueue the created activity requests.
        /// TODO: Make static
        /// </summary>
        private IQueuer queuer;

        /// <summary>
        /// Initializes a new instance of the ScheduledActivitySource class.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the derived class is missing either of the required
        /// SourceNameAttribute or ScheduleAttribute attributes.
        /// </exception>
        /// <exception cref="System.MissingMethodException">
        /// Thrown if no constructor for the IWorkItemSourceSchedule matching the
        /// arguments provided in the ScheduleAttribute.
        /// </exception>
        /// <seealso cref="ScheduledActivities.ScheduleAttribute"/>
        /// <seealso cref="ScheduledActivities.IActivitySourceSchedule"/>
        /// <seealso cref="ScheduledWorkItems.IScheduledWorkItemSource"/>
        protected ScheduledActivitySource()
        {
            var sourceNameAttribute = this.GetRequiredSingleAttribute<SourceNameAttribute>(false);
            this.Name = sourceNameAttribute.Value;

            var scheduleAttribute = this.GetRequiredSingleAttribute<ScheduleAttribute>(false);
            this.Schedule = (IActivitySourceSchedule)Activator.CreateInstance(scheduleAttribute.ScheduleType, scheduleAttribute.ScheduleArgs);
        }

        /// <summary>Gets the name by which the provider is identified.</summary>
        /// <remarks>
        /// This is used to look up this provider to update with the scheduled,
        /// processed and failed events.
        /// </remarks>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the schedule by which the source is to be checked for new
        /// work items.
        /// </summary>
        public IActivitySourceSchedule Schedule { get; internal set; }

        /// <summary>Gets or sets the context from the IWorkItemSourceProvider</summary>
        public object Context { get; set; }

        /// <summary>
        /// Gets or sets the last time this scheduled activity source started
        /// to create activity requests.
        /// </summary>
        protected DateTime LastCreateStartTime
        {
            get
            {
                if (!this.createScheduledRequestsLastStartTimes.ContainsKey(this.Name))
                {
                    this.createScheduledRequestsLastStartTimes[this.Name] = DateTime.MinValue;
                }
                
                return this.createScheduledRequestsLastStartTimes[this.Name];
            }

            set
            {
                this.createScheduledRequestsLastStartTimes[this.Name] = value;
            }
        }

        /// <summary>
        /// Gets or sets the last time this scheduled activity source finished
        /// creating activity requests.
        /// </summary>
        protected DateTime LastCreateEndTime
        {
            get
            {
                if (!this.createScheduledRequestsLastEndTimes.ContainsKey(this.Name))
                {
                    this.createScheduledRequestsLastEndTimes[this.Name] = DateTime.MinValue;
                }

                return this.createScheduledRequestsLastEndTimes[this.Name];
            }
            
            set
            {
                this.createScheduledRequestsLastEndTimes[this.Name] = value;
            }
        }

        /// <summary>Gets the maximum amount of time allowed to create new requests</summary>
        private static TimeSpan MaximumCreateScheduledRequestsRunTime
        {
            get { return Config.GetTimeSpanValue("ScheduledActivities.MaxCreateRequestRunTime"); }
        }

        /// <summary>Gets the number of time to retry submitting requests from within activities</summary>
        private static int SubmitRequestRetries
        {
            // TODO: Move to config?
            get { return 3; }
        }

        /// <summary>Gets the the time (in milliseconds) to wait before retrying to submit requests</summary>
        private static int SubmitRequestRetryWait
        {
            // TODO: Move to config?
            get { return 500; }
        }

        /// <summary>Factory method for scheduled activity sources</summary>
        /// <param name="activitySourceType">
        /// Type of the scheduled activity source to create
        /// </param>
        /// <param name="context">Context for the scheduled activity source to use</param>
        /// <param name="queuer">Queuer used to enqueue created activity requests</param>
        /// <returns>The created scheduled activity source</returns>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="activitySourceType"/> is not a ScheduledActivitySource or is abstract.
        /// </exception>
        public static ScheduledActivitySource Create(Type activitySourceType, object context, IQueuer queuer)
        {
            if (!typeof(ScheduledActivitySource).IsAssignableFrom(activitySourceType) || activitySourceType.IsAbstract)
            {
                throw new ArgumentException(
                    "The type '{0}' does not inherit from ScheduledActivitySource or is abstract"
                    .FormatInvariant(activitySourceType.FullName),
                    "activitySourceType");
            }

            var activitySource = (ScheduledActivitySource)Activator.CreateInstance(activitySourceType);
            activitySource.Context = context;
            activitySource.queuer = queuer;
            activitySource.createScheduledRequestsLastStartTimes = PersistentDictionaryFactory
                .CreateDictionary<DateTime>(CreateScheduledRequestsLastStartTimesStoreName);
            activitySource.createScheduledRequestsLastEndTimes = PersistentDictionaryFactory
                .CreateDictionary<DateTime>(CreateScheduledRequestsLastEndTimesStoreName);

            return activitySource;
        }

        /// <summary>
        /// Checks if it is time to get new requests and if so calls CreateScheduledRequests
        /// in the derived source to create new activity request work items.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Requires protection from derived class' implementation of CreateScheduledRequests")]
        public void CreateNewWorkItems()
        {
            var nextScheduledTime = this.Schedule.GetNextTime(this.LastCreateEndTime);
            if (nextScheduledTime > DateTime.UtcNow)
            {
                return;
            }

            // Check if another instance is already in the process of creating.
            // Note: Initial values for both are DateTime.MinValue, so it is important
            // not to do <= here. The next check would fail, of course, but should never
            // happen in the first place.
            if (this.LastCreateEndTime < this.LastCreateStartTime)
            {
                // Check if the other instance still has time or if it has timed out
                if ((DateTime.UtcNow - this.LastCreateStartTime) < MaximumCreateScheduledRequestsRunTime)
                {
                    // The other instance still has time
                    return;
                }
            }

            try
            {
                // Setting LastCreateStartTime to UtcNow makes it greater than LastCreateEndTime which
                // effectively takes a lock which will expire after MaximumCreateScheduledRequestsRunTime.
                this.LastCreateStartTime = DateTime.UtcNow;
                LogManager.Log(
                    LogLevels.Trace,
                    "Started creating scheduled requests for {0}",
                    this);
            }
            catch (InvalidETagException)
            {
                // Between the check and now another thread has already started updating
                return;
            }

            try
            {
                // Create any scheduled requests
                this.CreateScheduledRequests();
            }
            catch (Exception ex)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "ScheduledActivitySource.CreateNewWorkItems() - {0} - Unhandled exception while creating work items: {1}",
                    this,
                    ex);
            }

            try
            {
                // Setting LastCreateEndTime to UtcNow makes it greater than LastCreateEndTime which
                // releases the lock.
                LogManager.Log(
                    LogLevels.Trace,
                    "Finished creating scheduled requests for {0}",
                    this);
                this.LastCreateEndTime = DateTime.UtcNow;
            }
            catch (InvalidETagException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "LastCreateEndTime for {0} already updated by another process",
                    this);
                return;
            }
        }

        /// <summary>Handle the result of a previously created work item.</summary>
        /// <param name="workItem">The work item that has been processed.</param>
        public void OnWorkItemProcessed(WorkItem workItem)
        {
            LogManager.Log(
                LogLevels.Trace,
                "Processing result for work item '{0}'\n{1}",
                workItem.Id,
                workItem.Result);

            // Process the work item's result
            var request = ActivityRequest.DeserializeFromXml(workItem.Content);
            var result = ActivityResult.DeserializeFromXml(workItem.Result);
            this.OnActivityResult(request, result);
        }

        /// <summary>Returns a string representing the scheduled activity source</summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} - Name: {1}  Schedule Type: {2}  Next Scheduled Time: {3}",
                this.GetType().FullName,
                this.Name,
                this.Schedule.GetType().FullName,
                this.Schedule.GetNextTime(this.LastCreateEndTime));
        }

        /// <summary>Creates requests at the scheduled time</summary>
        public abstract void CreateScheduledRequests();

        /// <summary>Handler for activity results</summary>
        /// <param name="request">The request</param>
        /// <param name="result">The result</param>
        public abstract void OnActivityResult(ActivityRequest request, ActivityResult result);

        /// <summary>Submits a new activity request from within an activity</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="category">The category of the request</param>
        /// <param name="retry">Whether to retry failed submissions</param>
        /// <returns>True if the request was submitted successfully; otherwise, false</returns>
        protected bool SubmitRequest(ActivityRequest request, ActivityRuntimeCategory category, bool retry)
        {
            var retries = SubmitRequestRetries;
            var workItem = new WorkItem
            {
                Id = request.Id,
                Source = this.Name,
                ResultType = WorkItemResultType.Shared,
                Category = category.ToString(),
                Content = request.SerializeToXml()
            };

            do
            {
                if (!this.queuer.EnqueueWorkItem(ref workItem) ||
                    workItem.Status == WorkItemStatus.Failed)
                {
                    Thread.Sleep(SubmitRequestRetryWait);
                    continue;
                }

                return true;
            }
            while (retry && retries-- > 0);

            LogManager.Log(
                LogLevels.Error,
                true,
                "Failed to submit scheduled activity request after {0} attempts:\n{1}",
                SubmitRequestRetries,
                request.SerializeToXml());
            return false;
        }
        
        /// <summary>Gets an attribute that is required to be on the derived class</summary>
        /// <remarks>
        /// This check is for exactly one instance of the attribute. When/if needed
        /// a variation can be added for multiple attributes.
        /// </remarks>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="inherit">
        /// Specifies whether to search the inheritance chain to find the attribute.
        /// </param>
        /// <returns>The attribute, if found</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the attribute is not found or if there are multiple.
        /// </exception>
        private TAttribute GetRequiredSingleAttribute<TAttribute>(bool inherit)
            where TAttribute : Attribute
        {
            var attribute = this.GetType().GetCustomAttributes(inherit).OfType<TAttribute>().SingleOrDefault();
            if (attribute == null)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The type '{0}' does not have exactly one of the required {1} attribute.",
                    this.GetType().FullName,
                    typeof(TAttribute).FullName);
                throw new InvalidOperationException(message);
            }

            return attribute;
        }
    }
}
