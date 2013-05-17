//-----------------------------------------------------------------------
// <copyright file="ICategorizedQueue.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using WorkItems;

namespace Queuing
{
    /// <summary>
    /// Defines an interface for generic queues that handle IQueueEntries
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
    public interface ICategorizedQueue
    {
        /// <summary>
        /// Put an entry on the queue
        /// </summary>
        /// <param name="entry">Entry to enqueue</param>
        void Enqueue(WorkItemQueueEntry entry);

        /// <summary>
        /// Get up to the specified number of entries from the queue
        /// </summary>
        /// <param name="category">Category to get an entry from</param>
        /// <param name="maxEntries">Maximum number of entries to get</param>
        /// <returns>Array containing the dequeued entries (if any)</returns>
        WorkItemQueueEntry[] Dequeue(string category, int maxEntries);

        /// <summary>
        /// Delete an entry from the queue so that it cannot be dequeued again
        /// </summary>
        /// <param name="entry">entry to be deleted</param>
        void Delete(WorkItemQueueEntry entry);
    }
}
