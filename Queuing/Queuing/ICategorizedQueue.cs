//-----------------------------------------------------------------------
// <copyright file="ICategorizedQueue.cs" company="Rare Crowds Inc">
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
