//-----------------------------------------------------------------------
// <copyright file="IQueuer.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using WorkItems;

namespace Queuing
{
    /// <summary>
    /// Defines an interface for enqueuing work items
    /// </summary>
    public interface IQueuer
    {
        /// <summary>Enqueue the specified work item</summary>
        /// <param name="workItem">Work item to enqueue</param>
        /// <returns>True if the work item was enqueued; otherwise, false.</returns>
        bool EnqueueWorkItem(ref WorkItem workItem);

        /// <summary>Retrieve the current state of the requested work item</summary>
        /// <param name="workItemId">Id of the work item to check</param>
        /// <returns>The work item</returns>
        WorkItem CheckWorkItem(string workItemId);

        /// <summary>Dequeue processed work items for a specific source</summary>
        /// <param name="resultType">Type of source that enqueued the work items</param>
        /// <param name="source">Name of the source that enqueued the work items</param>
        /// <returns>Processed work items</returns>
        /// <param name="maxWorkItems">Maximum number of work items to dequeue</param>
        WorkItem[] DequeueProcessedWorkItems(WorkItemResultType resultType, string source, int maxWorkItems);

        /// <summary>Removes the work item from the queue</summary>
        /// <param name="workItem">The work item</param>
        void RemoveFromQueue(WorkItem workItem);
    }
}
