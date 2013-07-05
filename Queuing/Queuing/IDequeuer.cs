//-----------------------------------------------------------------------
// <copyright file="IDequeuer.cs" company="Rare Crowds Inc">
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
