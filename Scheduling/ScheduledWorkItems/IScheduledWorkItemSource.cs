//-----------------------------------------------------------------------
// <copyright file="IScheduledWorkItemSource.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
