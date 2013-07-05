//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceProvider.cs" company="Rare Crowds Inc">
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

using Diagnostics;
using Queuing;
using Utilities.Storage;

namespace DynamicAllocationActivityDispatchers
{
    /// <summary>
    /// Scheduled activity source provider for Dynamic Allocation
    /// </summary>
    public class ScheduledActivitySourceProvider : ScheduledActivities.ScheduledActivitySourceProvider
    {
        /// <summary>
        /// Initializes a new instance of the ScheduledActivitySourceProvider class.
        /// </summary>
        /// <param name="queuer">Queuer used to enqueue created work items</param>
        public ScheduledActivitySourceProvider(IQueuer queuer)
            : base(queuer)
        {
        }

        /// <summary>
        /// Gets the context for the dynamnic allocation scheduled activity sources.
        /// </summary>
        protected override object Context
        {
            get { return null; }
        }
    }
}
