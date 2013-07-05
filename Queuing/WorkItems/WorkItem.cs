//-----------------------------------------------------------------------
// <copyright file="WorkItem.cs" company="Rare Crowds Inc">
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
using System.Runtime.Serialization;

namespace WorkItems
{
    /// <summary>Represents a unit of work to be processed</summary>
    /// <remarks>
    /// The actual work to be done is represented by the Content string and the
    /// outcome of processing it in the Result string. These properties are used
    /// by the IWorkItemProcessor implementation. Outside the IWorkItemProcessor
    /// they are to be treated as opaque, arbitrary data.
    /// </remarks>
    [DataContract]
    public class WorkItem
    {
        /// <summary>Gets or sets the work item identifier</summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>Gets or sets the work item's category</summary>
        [DataMember]
        public string Category { get; set; }

        /// <summary>Gets or sets the source of the work item</summary>
        [DataMember]
        public string Source { get; set; }

        /// <summary>Gets or sets the work item's result type</summary>
        [DataMember]
        public WorkItemResultType ResultType { get; set; }

        /// <summary>Gets or sets the work item status</summary>
        [DataMember]
        public WorkItemStatus Status { get; set; }

        /// <summary>Gets or sets a string containing the content of the work item</summary>
        [DataMember]
        public string Content { get; set; }

        /// <summary>Gets or sets a string containing the result of the work item</summary>
        [DataMember]
        public string Result { get; set; }

        /// <summary>Gets or sets when the work item was queued</summary>
        [DataMember]
        public DateTime QueuedTime { get; set; }

        /// <summary>Gets or sets when the work item was dequeued</summary>
        [DataMember]
        public DateTime DequeueTime { get; set; }

        /// <summary>Gets or sets when the work item started being processed</summary>
        [DataMember]
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>Gets or sets when the work item completed being processed</summary>
        [DataMember]
        public DateTime ProcessingCompleteTime { get; set; }

        /// <summary>Gets how long the work item was in the queue</summary>
        [IgnoreDataMember]
        public TimeSpan TimeInQueue
        {
            get { return this.DequeueTime - this.QueuedTime; }
        }

        /// <summary>Gets how long the work item was in processing</summary>
        [IgnoreDataMember]
        public TimeSpan TimeInProcessing
        {
            get { return this.ProcessingCompleteTime - this.ProcessingStartTime; }
        }

        /// <summary>Gets the queue entry for the work item</summary>
        [IgnoreDataMember]
        public WorkItemQueueEntry QueueEntry
        {
            get
            {
                return new WorkItemQueueEntry
                {
                    WorkItemId = this.Id,
                    Category = this.Category
                };
            }
        }
    }
}
