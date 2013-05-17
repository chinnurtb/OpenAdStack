//-----------------------------------------------------------------------
// <copyright file="IDequeuer.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using WorkItems;

namespace Queuing
{
    /// <summary>Defines an interface for dequeuing work items</summary>
    public interface IDequeuer
    {
        /// <summary>Dequeue the next n work items</summary>
        /// <param name="category">Category of the work items to dequeue</param>
        /// <param name="maxWorkItems">Maximum number of work items to dequeue</param>
        /// <returns>The next n work items</returns>
        WorkItem[] DequeueWorkItems(string category, int maxWorkItems);

        /// <summary>Updates the work item</summary>
        /// <param name="workItem">The work item</param>
        void UpdateWorkItem(WorkItem workItem);

        /// <summary>Re-enqueues the processed work item</summary>
        /// <param name="workItem">The work item</param>
        /// <returns>True if the work item was enqueued; otherwise, false.</returns>
        bool EnqueueProcessedWorkItem(WorkItem workItem);

        /// <summary>Removes the work item from the queue</summary>
        /// <param name="workItem">The work item</param>
        void RemoveFromQueue(WorkItem workItem);

        /// <summary>Cleanup processed/failed work items</summary>
        void CleanupWorkItems();
    }
}
