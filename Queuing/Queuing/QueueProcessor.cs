//-----------------------------------------------------------------------
// <copyright file="QueueProcessor.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Threading;
using ConfigManager;
using Diagnostics;
//// using Doppler.TraceListeners;
using Microsoft.Practices.Unity;
using Utilities.Runtime;
using Utilities.Storage;
using WorkItems;

namespace Queuing
{
    /// <summary>
    /// Dequeues work items and sends them to be processed
    /// </summary>
    public class QueueProcessor : IRunner
    {
        /// <summary>
        /// Default maximum number of warnings before the worker role falls over
        /// </summary>
        private const int DefaultMaxWarnings = 10;

        /// <summary>
        /// Default for how long to pause after a warning (unhandled exception in run loop)
        /// </summary>
        private static readonly TimeSpan DefaultWarningWait = new TimeSpan(0, 0, 30);

        /// <summary>
        /// Dequeuer used to get work items for processing
        /// </summary>
        private readonly IDequeuer dequeuer;

        /// <summary>
        /// Implementation of IWorkItemProcessor used to process work items
        /// </summary>
        private readonly IWorkItemProcessor workItemProcessor;

        /// <summary>
        /// Container for processing statistics
        /// </summary>
        private readonly QueueProcessorStats stats;

        /// <summary>
        /// Persistent dictionary of shared queue times
        /// </summary>
        private readonly IPersistentDictionary<DateTime> queueTimes;

        /// <summary>
        /// The last time when statistics were logged
        /// </summary>
        private DateTime statsLastLoggedTime;

        /// <summary>
        /// Initializes a new instance of the QueueProcessor class.
        /// </summary>
        /// <param name="workItemProcessor">The work item processor</param>
        /// <param name="dequeuer">The dequeuer</param>
        public QueueProcessor(IWorkItemProcessor workItemProcessor, IDequeuer dequeuer)
        {
            this.dequeuer = dequeuer;
            this.workItemProcessor = workItemProcessor;
            this.stats = new QueueProcessorStats(this);
            this.statsLastLoggedTime = DateTime.UtcNow;
            this.queueTimes = PersistentDictionaryFactory.CreateDictionary<DateTime>("queueprocessortimes");
        }

        /// <summary>
        /// Gets or sets the categories to process in order of priority
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Array property is only public for dependency injection resolution")]
        public string[] Categories { get; set; }

        #region Configuration

        /// <summary>
        /// Gets the minimum time to wait between polling for workitems to process
        /// </summary>
        private static int MinQueuePollWait
        {
            get { return Config.GetIntValue("QueueProcessor.MinQueuePollWait"); }
        }

        /// <summary>
        /// Gets the maximum time to wait between polling for workitems to process
        /// </summary>
        private static int MaxQueuePollWait
        {
            get { return Config.GetIntValue("QueueProcessor.MaxQueuePollWait"); }
        }

        /// <summary>
        /// Gets the factor by which the time to wait between polling for work items multiplies each time there is nothing to do
        /// </summary>
        private static double QueuePollBackoff
        {
            get { return Config.GetDoubleValue("QueueProcessor.QueuePollBackoff"); }
        }

        /// <summary>
        /// Gets how long without work to go before entering the inactive state
        /// </summary>
        private static int InactiveQueuePollWait
        {
            get { return Config.GetIntValue("QueueProcessor.InactiveQueuePollWait"); }
        }

        /// <summary>
        /// Gets the time to go without work before entering the inactive state
        /// </summary>
        private static int InactiveQueueTime
        {
            get { return Config.GetIntValue("QueueProcessor.InactiveQueueTime"); }
        }

        /// <summary>
        /// Gets the maximum number of work items to poll for at one time
        /// </summary>
        private static int MaxPollBatchSize
        {
            get { return Config.GetIntValue("QueueProcessor.MaxPollBatchSize"); }
        }

        /// <summary>
        /// Gets how frequently stats are to be logged
        /// </summary>
        private static TimeSpan LogStatsFrequency
        {
            get { return Config.GetTimeSpanValue("QueueProcessor.LogStatsFrequency"); }
        }

        /// <summary>
        /// Gets how frequently work items are to be cleaned up
        /// </summary>
        private static TimeSpan WorkItemCleanupFrequency
        {
            get { return Config.GetTimeSpanValue("QueueProcessor.WorkItemCleanupFrequency"); }
        }

        /// <summary>
        /// Gets how long to wait after the queue is empty before exiting
        /// </summary>
        private static TimeSpan DrainStabilizationPeriod
        {
            get { return Config.GetTimeSpanValue("QueueProcessor.DrainStabilizationPeriod"); }
        }

        /// <summary>
        /// Gets the maximum number of warnings before the worker role falls over
        /// </summary>
        private static int MaxWarnings
        {
            get
            {
                try
                {
                    return Config.GetIntValue("QueueProcessor.MaxWarnings");
                }
                catch (ArgumentException)
                {
                    return DefaultMaxWarnings;
                }
            }
        }

        /// <summary>
        /// Gets how long to pause after a warning (unhandled exception in run loop)
        /// </summary>
        private static TimeSpan WarningWait
        {
            get
            {
                try
                {
                    return Config.GetTimeSpanValue("QueueProcessor.WarningWait");
                }
                catch (ArgumentException)
                {
                    return DefaultWarningWait;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the last time work items were cleaned up
        /// </summary>
        private DateTime LastWorkItemCleanupTime
        {
            get { return this.queueTimes.ContainsKey("LastCleanup") ? this.queueTimes["LastCleanup"] : DateTime.MinValue; }
            set { this.queueTimes["LastCleanup"] = value; }
        }

        /// <summary>
        /// Dequeue and process work items and their results
        /// created by the work item processor.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Thread proc requires global exception handler to protect worker role.")]
        public void Run()
        {
            try
            {
                LogManager.Log(
                    LogLevels.Information,
                    "Queue processing starting - Categories: [{0}]",
                    string.Join(", ", this.Categories));

                DateTime lastWorkItemTime = DateTime.MinValue;
                var pollWaitTime = MinQueuePollWait;
                var warnings = 0;

                while (true)
                {
                    try
                    {
                        // Periodically log statistics
                        if (DateTime.UtcNow - this.statsLastLoggedTime > LogStatsFrequency)
                        {
                            this.statsLastLoggedTime = DateTime.UtcNow;
                            LogManager.Log(LogLevels.Information, this.stats.ToString());
                        }

                        // Process any queued work items
                        try
                        {
                            if (this.ProcessQueuedWorkItems())
                            {
                                // Reset the wait time
                                pollWaitTime = MinQueuePollWait;
                                lastWorkItemTime = DateTime.UtcNow;
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            LogManager.Log(
                                LogLevels.Error,
                                "Unhandled exception while processing work item(s): {0}",
                                e);
                            throw;
                        }

                        // No work items to process, increase the wait time until the maximum is reached when there's nothing to do
                        // After having nothing to do for too long jump to the inactive poll time
                        pollWaitTime = (DateTime.UtcNow - lastWorkItemTime).TotalMilliseconds < InactiveQueueTime ?
                            Math.Min((int)Math.Ceiling(pollWaitTime * QueuePollBackoff), MaxQueuePollWait) :
                            InactiveQueuePollWait;

                        // Periodically cleanup work items if there is nothing else to do
                        var lastCleanupTime = this.LastWorkItemCleanupTime;
                        var cleanupStartTime = DateTime.UtcNow;
                        if (DateTime.UtcNow - lastCleanupTime > WorkItemCleanupFrequency)
                        {
                            try
                            {
                                LogManager.Log(
                                    LogLevels.Trace,
                                    "Cleaning up processed/failed work items. Last cleanup: {0}",
                                    this.LastWorkItemCleanupTime);
                                this.LastWorkItemCleanupTime = cleanupStartTime;
                                this.dequeuer.CleanupWorkItems();
                            }
                            catch (InvalidETagException)
                            {
                                LogManager.Log(
                                    LogLevels.Warning,
                                    "Not cleaning up work items: Another queue processor has already started cleanup.");
                            }
                        }

                        // Check if deployment status is landing, if so then exit when the queue has been
                        // empty for longer than the drain stabilization period
                        if (DeploymentProperties.DeploymentState == DeploymentState.Landing)
                        {
                            if (DateTime.UtcNow - lastWorkItemTime > DrainStabilizationPeriod)
                            {
                                // Drain stabilization period has elapsed, exit.
                                LogManager.Log(
                                    LogLevels.Information,
                                    "Deployment {0} landing. Queues [{1}] drained. QueueProcessor in thread {2} exiting.",
                                    DeploymentProperties.DeploymentId,
                                    string.Join(", ", this.Categories),
                                    Thread.CurrentThread.Name);
                                return;
                            }
                            else
                            {
                                // Reset the wait time to minimum while stabilizing.
                                pollWaitTime = MinQueuePollWait;
                            }
                        }

                        // Sleep for the remainder of the poll wait time if cleanup didn't run or finished in less time
                        var remainingWaitTime = (int)(pollWaitTime - (DateTime.UtcNow - cleanupStartTime).TotalMilliseconds);
                        if (remainingWaitTime > 0)
                        {
                            Thread.Sleep(remainingWaitTime);
                        }
                    }
                    catch (Exception e)
                    {
                        if (++warnings > MaxWarnings)
                        {
                            throw;
                        }

                        LogManager.Log(
                            LogLevels.Warning,
                            true,
                            "[{0}/{1}] Unhandled exception in QueueProcessor.Run: {2}. Pausing for {3} before continuing.\n{4}",
                            warnings,
                            MaxWarnings,
                            e.GetType().FullName,
                            WarningWait,
                            e);
                        Thread.Sleep(WarningWait);
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.Log(LogLevels.Error, "QueueProcessor exiting due to unhandled exception: {0}", e);
                return;
            }
        }

        /// <summary>Processes work items from the queue</summary>
        /// <returns>True if there were work items to process; otherwise, false</returns>
        private bool ProcessQueuedWorkItems()
        {
            var workItems = this.DequeueWorkItems();
            if (workItems.Length == 0)
            {
                return false;
            }

            foreach (var workItem in workItems)
            {
                this.ProcessWorkItem(workItem);
            }

            return true;
        }

        /// <summary>Dequeues work items for processing</summary>
        /// <returns>Work items to be processed (if any)</returns>
        private WorkItem[] DequeueWorkItems()
        {
            foreach (var category in this.Categories)
            {
                var workItems = this.dequeuer.DequeueWorkItems(category, MaxPollBatchSize);
                if (workItems.Length > 0)
                {
                    this.stats.AddDequeued(category, workItems.Length);
                    LogManager.Log(
                        LogLevels.Trace,
                        "Dequeued {0} work item(s) for processing from category {1}.",
                        workItems.Length,
                        category);
                    return workItems;
                }
            }

            return new WorkItem[0];
        }

        /// <summary>Processes a single work item</summary>
        /// <param name="workItem">The work item</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Catch-all exception handling required to protect the QueueProcessor from the WorkItemProcessor")]
        private void ProcessWorkItem(WorkItem workItem)
        {
            try
            {
////            CorrelationMapper.HandoffOperationStart(workItem.Id, workItem.Category ?? "<UNKNOWN>");

                LogManager.Log(
                    LogLevels.Trace,
                    "Processing work item {0} (source: '{1}')",
                    workItem.Id,
                    workItem.Source);

                // Process the work item and submit any resulting new work items to the queue
                workItem.Status = WorkItemStatus.InProgress;
                workItem.ProcessingStartTime = DateTime.UtcNow;
                this.dequeuer.UpdateWorkItem(workItem);
                this.workItemProcessor.ProcessWorkItem(ref workItem);
                workItem.ProcessingCompleteTime = DateTime.UtcNow;
                this.dequeuer.UpdateWorkItem(workItem);
                this.dequeuer.RemoveFromQueue(workItem);
                LogManager.Log(
                    LogLevels.Trace,
                    "Processed work item {0}\nSource: '{1}'\nTime in queue: {2}\nTime to process: {3}\nTotal time: {4})",
                    workItem.Id,
                    workItem.Source,
                    workItem.TimeInQueue,
                    workItem.TimeInProcessing,
                    DateTime.UtcNow - workItem.QueuedTime);
                this.stats.AddProcessed(workItem);

                if (workItem.ResultType != WorkItemResultType.Direct &&
                    workItem.ResultType != WorkItemResultType.Polled)
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "Re-enqueuing processed work item '{0}' result (ResultType: {1} Source: {2})",
                        workItem.Id,
                        workItem.ResultType,
                        workItem.Source);

                    this.dequeuer.EnqueueProcessedWorkItem(workItem);
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    this.stats.AddFailed(workItem);
                    LogManager.Log(
                        LogLevels.Error,
                        "Error processing workitem '{0}': {1}",
                        workItem.Id,
                        ex);
                    workItem.Status = WorkItemStatus.Failed;
                    workItem.Result = ex.ToString();
                    workItem.ProcessingCompleteTime = DateTime.UtcNow;
                    this.dequeuer.UpdateWorkItem(workItem);
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error failing work item: {0}\n\nOriginal failure: {1}",
                        e,
                        ex);
                }
            }
////        finally
////        {
////            CorrelationMapper.HandoffOperationEnd();
////        }
        }
    }
}
