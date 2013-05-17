//-----------------------------------------------------------------------
// <copyright file="IScheduledWorkItemSourceProvider.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace ScheduledWorkItems
{
    /// <summary>Interface for providers of scheduled work item sources</summary>
    /// <remarks>
    /// Generally there should be one for each assembly of scheduled work item sources.
    /// Implementations of this interface class are what should be mapped in Unity,
    /// not the individual work item sources.
    /// </remarks>
    public interface IScheduledWorkItemSourceProvider
    {
        /// <summary>Creates the provided scheduled work item sources</summary>
        /// <returns>The created scheduled work item sources</returns>
        IEnumerable<IScheduledWorkItemSource> CreateScheduledWorkItemSources();
    }
}
