//-----------------------------------------------------------------------
// <copyright file="IQueuer.cs" company="Rare Crowds Inc">
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
