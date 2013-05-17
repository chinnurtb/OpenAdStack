//-----------------------------------------------------------------------
// <copyright file="Queue.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using ConfigManager;
using Diagnostics;
using Utilities.Storage;
using WorkItems;

namespace Queuing
{
    /// <summary>
    /// Implmenets interfaces for queuing and dequeuing work items
    /// </summary>
    internal class Queue : IQueuer, IDequeuer
    {
        /// <summary>
        /// Format string for result WorkItem.Category
        /// </summary>
        internal const string ResultCategoryFormat = "R-{0}";

        /// <summary>
        /// Queue onto which queue entries for work items are enqueued
        /// </summary>
        private ICategorizedQueue queue;

        /// <summary>
        /// Backing field for WorkItems. DO NOT USE DIRECTLY.
        /// </summary>
        private IPersistentDictionary<WorkItem> workItems;

        /// <summary>
        /// Backing field for FailedWorkItems. DO NOT USE DIRECTLY.
        /// </summary>
        private IPersistentDictionary<WorkItem> failedWorkItems;

        /// <summary>
        /// Initializes a new instance of the Queue class.
        /// </summary>
        /// <param name="queue">The categorized queue</param>
        public Queue(ICategorizedQueue queue)
        {
            this.queue = queue;
        }

        /// <summary>
        /// Gets the name of the store for work items
        /// </summary>
        private static string WorkItemStoreName
        {
            get { return Config.GetValue("Queue.WorkItemStoreName"); }
        }

        /// <summary>
        /// Gets the name of the store for failed work items
        /// </summary>
        private static string FailedWorkItemStoreName
        {
            get { return Config.GetValue("Queue.FailedWorkItemStoreName"); }
        }

        /// <summary>
        /// Gets the number of retries allowed when enqueuing work items
        /// </summary>
        private static int EnqueueRetries
        {
            get { return Config.GetIntValue("Queue.EnqueueRetries"); }
        }

        /// <summary>
        /// Gets how long to wait between enqueuing retries
        /// </summary>
        private static TimeSpan EnqueueRetryWait
        {
            get { return Config.GetTimeSpanValue("Queue.EnqueueRetryWait"); }
        }

        /// <summary>
        /// Gets how long to retain processed/completed work items
        /// </summary>
        private static TimeSpan WorkItemRetentionPeriod
        {
            get { return Config.GetTimeSpanValue("Queue.WorkItemRetentionPeriod"); }
        }

        /// <summary>
        /// Gets the store into which work items are stored while they are in the queue
        /// </summary>
        private IPersistentDictionary<WorkItem> WorkItems
        {
            get
            {
                this.workItems =
                    this.workItems
                    ??
                    PersistentDictionaryFactory.CreateDictionary<WorkItem>(WorkItemStoreName);
                return this.workItems;
            }
        }

        /// <summary>
        /// Gets the store into which failed work items are stored after the retention period expires
        /// </summary>
        private IPersistentDictionary<WorkItem> FailedWorkItems
        {
            get
            {
                this.failedWorkItems =
                    this.failedWorkItems ??
                    PersistentDictionaryFactory.CreateDictionary<WorkItem>(FailedWorkItemStoreName, PersistentDictionaryType.Cloud);
                return this.failedWorkItems;
            }
        }

        #region Enqueuing
        /// <summary>Enqueue the specified work item.</summary>
        /// <param name="workItem">Work item to enqueue</param>
        /// <returns>True if the work item was enqueued; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">workItem is null.</exception>
        /// <exception cref="System.ArgumentException">workItem.Id or workItem.Content is missing.</exception>
        public bool EnqueueWorkItem(ref WorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException("workItem");
            }

            if (string.IsNullOrWhiteSpace(workItem.Id))
            {
                throw new ArgumentException("Missing WorkItem.Id", "workItem");
            }

            if (string.IsNullOrWhiteSpace(workItem.Content))
            {
                throw new ArgumentException("Missing WorkItem.Content", "workItem");
            }

            // Give the work item a unique identifier and set its status
            workItem.Status = WorkItemStatus.Pending;
            workItem.QueuedTime = DateTime.UtcNow;

            return this.AddWorkItemToQueue(ref workItem);
        }

        /// <summary>
        /// Retrieves the current state of the requested work item
        /// </summary>
        /// <param name="workItemId">Id of the work item to check</param>
        /// <returns>The work item</returns>
        public WorkItem CheckWorkItem(string workItemId)
        {
            if (!this.WorkItems.ContainsKey(workItemId))
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "No work item found with id {0}",
                    workItemId);
                return null;
            }

            return this.WorkItems[workItemId];
        }

        /// <summary>Dequeue processed work items for a specific source</summary>
        /// <param name="resultType">Type of source that enqueued the work items</param>
        /// <param name="source">Name of the source that enqueued the work items</param>
        /// <returns>Processed work items</returns>
        /// <param name="maxWorkItems">Maximum number of work items to dequeue</param>
        public WorkItem[] DequeueProcessedWorkItems(WorkItemResultType resultType, string source, int maxWorkItems)
        {
            var category = GetResultCategory(resultType, source);
            return this.DequeueWorkItems(category, WorkItemStatus.Processed, maxWorkItems);
        }
        #endregion

        #region Dequeuing
        /// <summary>Dequeue the next n work items</summary>
        /// <param name="category">Category of the work items to dequeue</param>
        /// <param name="maxWorkItems">Maximum number of work items to dequeue</param>
        /// <returns>The next n work items</returns>
        public WorkItem[] DequeueWorkItems(string category, int maxWorkItems)
        {
            return this.DequeueWorkItems(category, WorkItemStatus.Pending, maxWorkItems);
        }

        /// <summary>Updates the work item</summary>
        /// <param name="workItem">The work item</param>
        /// <exception cref="System.ArgumentNullException">
        /// The WorkItem is null
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// The WorkItem.Id is missing or invalid
        /// </exception>
        public void UpdateWorkItem(WorkItem workItem)
        {
            this.CheckValidWorkItem(workItem);
            this.WorkItems[workItem.Id] = workItem;
        }

        /// <summary>Renqueues the processed work item</summary>
        /// <param name="workItem">The work item</param>
        /// <returns>True if the work item was enqueued; otherwise, false.</returns>
        public bool EnqueueProcessedWorkItem(WorkItem workItem)
        {
            workItem.Category = GetResultCategory(workItem.ResultType, workItem.Source);
            return this.AddWorkItemToQueue(ref workItem);
        }
        
        /// <summary>Removes the work item from the queue</summary>
        /// <param name="workItem">The work item</param>
        /// <exception cref="System.ArgumentNullException">
        /// The WorkItem is null
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// The WorkItem.Id is missing or invalid
        /// </exception>
        public void RemoveFromQueue(WorkItem workItem)
        {
            this.CheckValidWorkItem(workItem);
            this.queue.Delete(workItem.QueueEntry);
        }

        /// <summary>Cleanup processed/failed work items</summary>
        /// <remarks>
        /// Failed work items are copied to the failed
        /// work item store before being deleted.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exceptions during cleanup are logged")]
        public void CleanupWorkItems()
        {
            // Get all the expired work items
            var expirationThreshold = DateTime.UtcNow - WorkItemRetentionPeriod;
            var expiredWorkItemsIds = this.WorkItems.Values
                .Where(wi =>
                    wi.ProcessingCompleteTime < expirationThreshold &&
                    (wi.Status == WorkItemStatus.Completed ||
                     wi.Status == WorkItemStatus.Processed ||
                     wi.Status == WorkItemStatus.Failed))
                .Select(wi => wi.Id)
                .ToArray();

            foreach (var workItemId in expiredWorkItemsIds)
            {
                try
                {
                    var workItem = this.WorkItems[workItemId];

                    // Archive failed work items before deleting
                    if (workItem.Status == WorkItemStatus.Failed)
                    {
                        this.FailedWorkItems[workItemId] = workItem;
                    }

                    // Remove the expired work items
                    this.WorkItems.Remove(workItemId);
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Error cleaning up work-item '{0}': {1}",
                        workItemId,
                        e);
                }
            }
        }

        /// <summary>Gets the result category for the type/source</summary>
        /// <param name="resultType">Type of result</param>
        /// <param name="source">Source name (optional)</param>
        /// <returns>The result category</returns>
        internal static string GetResultCategory(WorkItemResultType resultType, string source)
        {
            return ResultCategoryFormat.FormatInvariant(
                resultType == WorkItemResultType.Shared ? "shared" : source);
        }
        
        /// <summary>Adds a work item to the queue</summary>
        /// <param name="workItem">Work item to enqueue</param>
        /// <returns>True if the work item was enqueued; otherwise, false.</returns>
        private bool AddWorkItemToQueue(ref WorkItem workItem)
        {
            // Attempt to store the work item and put the entry on the queue
            var retries = EnqueueRetries;
            do
            {
                try
                {
                    this.WorkItems[workItem.Id] = workItem;
                    this.queue.Enqueue(workItem.QueueEntry);
                    return true;
                }
                catch (QueueException qe)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Error enqueuing work item '{0}': {1}",
                        workItem.Id,
                        qe);
                }

                Thread.Sleep(EnqueueRetryWait);
            }
            while (retries-- > 0);

            LogManager.Log(
                LogLevels.Error,
                "Failed to enqueue work item '{0}' after {1} attempts.",
                workItem.Id,
                EnqueueRetries);

            return false;
        }

        /// <summary>Dequeue the next n work items</summary>
        /// <param name="category">Category of the work items to dequeue</param>
        /// <param name="expectedStatus">Expected status of work items teing dequeued</param>
        /// <param name="maxWorkItems">Maximum number of work items to dequeue</param>
        /// <returns>The next n work items</returns>
        private WorkItem[] DequeueWorkItems(string category, WorkItemStatus expectedStatus, int maxWorkItems)
        {
            List<WorkItem> workItems = new List<WorkItem>();

            foreach (WorkItemQueueEntry entry in this.queue.Dequeue(category, maxWorkItems))
            {
                // Make sure the work item still exists
                if (!this.WorkItems.ContainsKey(entry.WorkItemId))
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "No work item exists for the queue entry '{0}'",
                        entry.WorkItemId);
                    this.queue.Delete(entry);
                    continue;
                }

                // Get the work item for the queue entry
                WorkItem workItem = this.WorkItems[entry.WorkItemId];

                // Delete work items with invalid status
                if (workItem.Status != expectedStatus)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "The dequeued work item '{0}' has unexpected status {1} (expected {2}). Deleting from queue.",
                        workItem.Id,
                        workItem.Status,
                        WorkItemStatus.Pending);
                    this.queue.Delete(workItem.QueueEntry);
                }

                // Only update dequeue time for pending work items
                if (workItem.Status == WorkItemStatus.Pending)
                {
                    workItem.DequeueTime = DateTime.UtcNow;
                }

                // Add the work item for the entry to the list
                workItems.Add(workItem);
            }

            return workItems.ToArray();
        }

        /// <summary>Checks if a work item is valid</summary>
        /// <param name="workItem">The work item</param>
        /// <exception cref="System.ArgumentNullException">
        /// The WorkItem is null
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// The WorkItem.Id is missing or invalid
        /// </exception>
        private void CheckValidWorkItem(WorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException("workItem");
            }

            if (string.IsNullOrWhiteSpace(workItem.Id))
            {
                throw new ArgumentException("Missing WorkItem.Id", "workItem");
            }

            if (!this.WorkItems.ContainsKey(workItem.Id))
            {
                throw new ArgumentException("Invalid workItem.Id", "workItem");
            }
        }
        #endregion
    }
}
