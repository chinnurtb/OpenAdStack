﻿//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
