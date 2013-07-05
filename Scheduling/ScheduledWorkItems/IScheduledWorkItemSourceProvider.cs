//-----------------------------------------------------------------------
// <copyright file="IScheduledWorkItemSourceProvider.cs" company="Rare Crowds Inc">
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
