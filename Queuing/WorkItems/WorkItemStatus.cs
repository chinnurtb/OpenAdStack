//-----------------------------------------------------------------------
// <copyright file="WorkItemStatus.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace WorkItems
{
    /// <summary>
    /// Status values for QueueWorkItems
    /// </summary>
    public enum WorkItemStatus
    {
        /// <summary>The work item has not been assigned a status.</summary>
        None,

        /// <summary>The work item is in the queue, waiting to be processed.</summary>
        Pending,

        /// <summary>The work item has been dequeued but not yet marked as processed.</summary>
        InProgress,

        /// <summary>The work item has been processed and results should be available.</summary>
        Processed,

        /// <summary>The work item's results have been processed.</summary>
        /// <remarks>
        /// If the work item has no source then it is considered completed as
        /// soon as it has been processed.
        /// </remarks>
        Completed,

        /// <summary>An unrecoverable error occured while processing the work item</summary>
        Failed
    }
}
