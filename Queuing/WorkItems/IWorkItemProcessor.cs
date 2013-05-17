//-----------------------------------------------------------------------
// <copyright file="IWorkItemProcessor.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace WorkItems
{
    /// <summary>
    /// Defines an interface for a processor of work items
    /// </summary>
    public interface IWorkItemProcessor
    {
        /// <summary>Processes a work item</summary>
        /// <param name="workItem">Work item to process</param>
        void ProcessWorkItem(ref WorkItem workItem);
    }
}
