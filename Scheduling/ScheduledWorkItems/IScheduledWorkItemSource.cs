//-----------------------------------------------------------------------
// <copyright file="IScheduledWorkItemSource.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using WorkItems;

namespace ScheduledWorkItems
{
    /// <summary>Interface for providers of scheduled work items</summary>
    public interface IScheduledWorkItemSource
    {
        /// <summary>Gets the name by which the provider is identified.</summary>
        /// <remarks>
        /// This is used to find this source to handle the scheduled, processed
        /// and failed events.
        /// </remarks>
        string Name { get; }

        /// <summary>Gets or sets the context from the IWorkItemSourceProvider</summary>
        object Context { get; set; }

        /// <summary>Creates new scheduled work items.</summary>
        void CreateNewWorkItems();

        /// <summary>Handle the result of a previously created work item.</summary>
        /// <param name="workItem">The work item that has been processed.</param>
        void OnWorkItemProcessed(WorkItem workItem);
    }
}
