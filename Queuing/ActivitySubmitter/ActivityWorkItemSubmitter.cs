//-----------------------------------------------------------------------
// <copyright file="ActivityWorkItemSubmitter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Activities;
using ConfigManager;
using Diagnostics;
using Queuing;
using Utilities.Runtime;
using WorkItems;

namespace ActivitySubmitter
{
    /// <summary>Callback for activity results</summary>
    /// <param name="result">The result</param>
    /// <param name="context">Additional pass-through context</param>
    public delegate void ActivityResultCallback(ActivityResult result, object[] context);

    /// <summary>Activity request submitter</summary>
    public class ActivityWorkItemSubmitter : IActivitySubmitter, IDisposable
    {
        /// <summary>Default maximum result poller idle wait</summary>
        private const int DefaultMaxIdleWait = 50;

        /// <summary>Default minimum result poller idle wait</summary>
        private const int DefaultMinIdleWait = 10;

        /// <summary>Factor by which idle wait increases relative to time spent idle</summary>
        private const double IdleBackoffFactor = 1.005;

        /// <summary>Pattern used to validate Name</summary>
        private static readonly Regex ValidNamePattern = new Regex("[a-zA-Z0-9]+");

        /// <summary>Activity result callbacks</summary>
        private readonly IDictionary<string, Action<ActivityResult>> Callbacks = new Dictionary<string, Action<ActivityResult>>();

        /// <summary>Thread which polls for completed work-items</summary>
        private readonly Thread PollerThread;

        /// <summary>Queuer for enqueuing activitiy work items</summary>
        private readonly IQueuer Queuer;

        /// <summary>Wait handle used by the polling thread and signaled on dispose/idle reset</summary>
        private readonly EventWaitHandle PollerWaitHandle = new AutoResetEvent(false);

        /// <summary>Backing field for IdleWait</summary>
        private int idleWait = MinIdleWait;

        /// <summary>Whether the poller should shutdown</summary>
        /// <remarks>Set when the instance is being disposed</remarks>
        private bool shutdown;

        /// <summary>Initializes a new instance of the ActivityWorkItemSubmitter class</summary>
        /// <param name="name">Name for the submitter. Alphanumeric characters only.</param>
        /// <param name="queuer">IQueuer implementation</param>
        public ActivityWorkItemSubmitter(string name, IQueuer queuer)
        {
            if (!ValidNamePattern.IsMatch(name))
            {
                throw new ArgumentException("Non-alphanumeric characters are not allowed ({0})".FormatInvariant(name), "name");
            }

            this.Name = name;
            this.Id = "{0}-{1}".FormatInvariant(
                this.Name.ToLowerInvariant(),
                Guid.NewGuid().ToString("n").Right(8),
                DeploymentProperties.RoleInstanceId.Right(4).Replace("_", string.Empty));
            this.Queuer = queuer;

            // Launch the result poller thread
            this.PollerThread = new Thread(this.PollForCompletedWorkItems);
            this.PollerThread.Start();
        }

        /// <summary>Gets the submitter name</summary>
        public string Name { get; private set; }

        /// <summary>Gets the submitter unique id</summary>
        public string Id { get; private set; }

        /// <summary>Gets the count of unhandled exceptions in the poller</summary>
        internal int PollerErrorCount { get; private set; }

        /// <summary>Gets the maximum idle wait</summary>
        private static int MaxIdleWait
        {
            get
            {
                try
                {
                    return Config.GetIntValue("ActivitySubmitter.MaxIdleWait");
                }
                catch (ArgumentException)
                {
                    return DefaultMaxIdleWait;
                }
            }
        }

        /// <summary>Gets the minimum idle wait</summary>
        private static int MinIdleWait
        {
            get
            {
                try
                {
                    return Config.GetIntValue("ActivitySubmitter.MinIdleWait");
                }
                catch (ArgumentException)
                {
                    return DefaultMinIdleWait;
                }
            }
        }

        /// <summary>Gets how long to wait when idle</summary>
        private int IdleWait
        {
            get
            {
                return this.idleWait = Math.Max(0, Math.Min((int)Math.Ceiling(this.idleWait * IdleBackoffFactor), MaxIdleWait));
            }
        }

        /// <summary>Submits a request and waits for the result</summary>
        /// <param name="request">Activity request</param>
        /// <param name="category">Activity runtime category</param>
        /// <param name="timeout">How long to wait for a result</param>
        /// <returns>The result, if successful; otherwise, false.</returns>
        public ActivityResult SubmitAndWait(ActivityRequest request, ActivityRuntimeCategory category, int timeout)
        {
            ActivityResult result = null;
            var waitHandle = new AutoResetEvent(false);
            if (!this.SubmitWithCallback(request, category, r => { result = r; waitHandle.Set(); }))
            {
                throw new QueueException("Unable to queue message");
            }

            if (!waitHandle.WaitOne(timeout))
            {
                LogManager.Log(LogLevels.Warning, "SubmitAndWait for request '{0}' timed out after {1}ms.", request.Id, timeout);
            }

            if (result == null)
            {
                LogManager.Log(LogLevels.Warning, "SubmitAndWait for request '{0}' did not get a result.", request.Id);
                lock (this.Callbacks)
                {
                    this.Callbacks.Remove(request.Id);
                }
            }

            return result;
        }

        /// <summary>Submits a request and invokes a callback for the result</summary>
        /// <param name="request">Activity request</param>
        /// <param name="category">Activity runtime category</param>
        /// <param name="callback">Result callback</param>
        /// <returns>True if the activity was successfully submitted; otherwise, false.</returns>
        public bool SubmitWithCallback(ActivityRequest request, ActivityRuntimeCategory category, Action<ActivityResult> callback)
        {
            var workItem = new WorkItem
            {
                Id = request.Id,
                Category = category.ToString(),
                ResultType = WorkItemResultType.PerSource,
                Source = this.Id,
                Content = request.SerializeToXml()
            };

            if (!this.Queuer.EnqueueWorkItem(ref workItem))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to enqueue activity request:\n{0}",
                    request.SerializeToXml());
                return false;
            }

            lock (this.Callbacks)
            {
                this.Callbacks.Add(request.Id, callback);
            }

            this.ResetIdleWait();
            return true;
        }

        /// <summary>Dispose of managed resources</summary>
        public void Dispose()
        {
            this.shutdown = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Dispose of managed resources such as the poller thread and wait handle</summary>
        /// <param name="disposing">Whether the object is being disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && this.PollerThread.ThreadState == ThreadState.Running)
            {
                this.PollerWaitHandle.Set();
                Thread.Sleep(100);
                if (this.PollerThread.ThreadState == ThreadState.Running)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Poller thread failed to exit within the time allowed and will be forcibly aborted.");
                    this.PollerThread.Abort();
                }

                this.PollerWaitHandle.Dispose();
            }
        }

        /// <summary>Attempts to deserialize the ActivityResult of a work item</summary>
        /// <param name="workItem">The work item</param>
        /// <returns>The result, if successful; otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern. Exception is logged.")]
        private static ActivityResult TryDeserializeWorkItemResult(WorkItem workItem)
        {
            try
            {
                return ActivityResult.DeserializeFromXml(workItem.Result);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Error deserializing result from work item '{0}': {1}\n\n{2}",
                    workItem.Id,
                    e,
                    workItem.Result);
                return null;
            }
        }

        /// <summary>Safely invokes the result callback and logs any exceptions thrown</summary>
        /// <param name="callback">Activity result callback to invoke</param>
        /// <param name="result">The activity result</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern. Exception is logged.")]
        private static void SafeInvokeActivityResultCallback(Action<ActivityResult> callback, ActivityResult result)
        {
            try
            {
                callback(result);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Error executing callback for work item '{0}': {1}\n\nActivity Result:\n{2}",
                    result.RequestId,
                    e,
                    result.SerializeToXml());
            }
        }

        /// <summary>Reset the fields used to calculate IdleWait</summary>
        private void ResetIdleWait()
        {
            this.idleWait = MinIdleWait;
            this.PollerWaitHandle.Set();
        }

        /// <summary>Polls for completed work items</summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Thread proc last chance handling. Exception is logged.")]
        private void PollForCompletedWorkItems()
        {
            while (!this.shutdown)
            {
                try
                {
                    // Retrieve processed work items
                    var workItems = this.Queuer.DequeueProcessedWorkItems(WorkItemResultType.PerSource, this.Id, 20);

                    // Wait before continuing if there aren't any processed work items
                    if (workItems == null || workItems.Length == 0)
                    {
                        var wait = this.IdleWait;
                        LogManager.Log(LogLevels.Trace, "Result Poller: No results. Waiting {0}ms", wait);
                        if (this.PollerWaitHandle.WaitOne(wait))
                        {
                            LogManager.Log(LogLevels.Trace, "Result Poller: Wait interrupted.");
                        }

                        continue;
                    }

                    this.ResetIdleWait();

                    // Get results from the work items and invoke their callbacks.
                    foreach (var workItem in workItems)
                    {
                        var result = TryDeserializeWorkItemResult(workItem);
                        if (result == null)
                        {
                            continue;
                        }

                        // Add processing times to the result for performance auditing
                        var processingTimes =
                            "in queue: {0}s; in processing: {1}s; results awaiting retrieval: {2}s"
                            .FormatInvariant(
                                workItem.TimeInQueue.TotalSeconds,
                                workItem.TimeInProcessing.TotalSeconds,
                                (DateTime.UtcNow - workItem.ProcessingCompleteTime).TotalSeconds);
                        result.Values.Add("ProcessingTimes", processingTimes);

                        lock (this.Callbacks)
                        {
                            Action<ActivityResult> callback;
                            if (!this.Callbacks.TryGetValue(workItem.Id, out callback))
                            {
                                LogManager.Log(
                                    LogLevels.Warning,
                                    "No callback found for received work item '{0}'",
                                    workItem.Id);
                                continue;
                            }

                            SafeInvokeActivityResultCallback(callback, result);

                            this.Queuer.RemoveFromQueue(workItem);
                            this.Callbacks.Remove(result.RequestId);
                        }
                    }
                }
                catch (ThreadAbortException tae)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Activity result poller thread aborted: {0}",
                        tae);
                    return;
                }
                catch (ThreadInterruptedException tie)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Activity result poller thread interrupted: {0}",
                        tie);
                    return;
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        true,
                        "[{0}] Unhandled exception while polling for completed work items. Poller will resume in 30 seconds.\n{1}",
                        ++this.PollerErrorCount,
                        e);
                    this.PollerWaitHandle.WaitOne(30000);
                }
            }
        }
    }
}
