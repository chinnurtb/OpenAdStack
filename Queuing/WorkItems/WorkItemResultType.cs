//-----------------------------------------------------------------------
// <copyright file="WorkItemResultType.cs" company="Rare Crowds Inc">
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
