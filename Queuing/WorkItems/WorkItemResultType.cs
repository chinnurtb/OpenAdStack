//-----------------------------------------------------------------------
// <copyright file="WorkItemResultType.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace WorkItems
{
    /// <summary>Types of WorkItem results</summary>
    /// <seealso cref="WorkItems.WorkItem"/>
    public enum WorkItemResultType
    {
        /// <summary>Default, unset value</summary>
        Unknown,

        /// <summary>
        /// Results are immediately handled by the source upon
        /// completion and are not returned via result queues.
        /// </summary>
        /// <remarks>
        /// The source's Activity.OnActivityResult handler is
        /// called directly by the ActivityWorkItemProcessor.
        /// </remarks>
        Direct,

        /// <summary>
        /// Results are polled for the submitter using the
        /// CheckWorkItem method of IQueuer.
        /// </summary>
        /// <remarks>
        /// This should be done away with once ActivitySubmitter
        /// performance surpases polling using CheckWorkItem.
        /// </remarks>
        Polled,

        /// <summary>
        /// Results are submitted to a per-source result queue.
        /// </summary>
        /// <remarks>
        /// Each ActivityWorkItemSubmitter instance has its own
        /// result queue through which these are processed. This
        /// enables synchronous activity execution with results.
        /// </remarks>
        PerSource,

        /// <summary>
        /// Results are submitted to a shared result queue.
        /// </summary>
        /// <remarks>
        /// Any WorkItemScheduler instance is capable of handling
        /// activity results even if it is not the instance that
        /// submitted the original activity request.
        /// </remarks>
        Shared
    }
}
