//-----------------------------------------------------------------------
// <copyright file="WorkItemStatus.cs" company="Rare Crowds Inc">
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
