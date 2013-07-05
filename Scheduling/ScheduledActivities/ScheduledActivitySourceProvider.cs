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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diagnostics;
using Microsoft.Practices.Unity;
using Queuing;
using ScheduledActivities;
using ScheduledWorkItems;
using Utilities.Storage;

namespace ScheduledActivities
{
    /// <summary>
    /// Provides scheduled work item sources for Dynamic Allocation related activities
    /// </summary>
    public class ScheduledActivitySourceProvider : IScheduledWorkItemSourceProvider
    {
        /// <summary>Used to enqueue activity request work items.</summary>
        private readonly IQueuer Queuer;

        /// <summary>
        /// Initializes a new instance of the ScheduledActivitySourceProvider class.
        /// </summary>
        /// <param name="queuer">Queue used by sources to enqueue created activity requests</param>
        protected ScheduledActivitySourceProvider(IQueuer queuer)
        {
            this.Queuer = queuer;
        }

        /// <summary>Gets the scheduled activity source types</summary>
        internal Type[] ScheduledActivitySourceTypes
        {
            get
            {
                return this.GetType().Assembly.GetTypes()
                    .Where(t => typeof(ScheduledActivitySource).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToArray();
            }
        }

        /// <summary>Gets the context provided to the scheduled activity sources.</summary>
        protected virtual object Context
        {
            get { return null; }
        }

        /// <summary>Creates the provided scheduled work item sources</summary>
        /// <returns>The created scheduled work item sources</returns>
        public IEnumerable<IScheduledWorkItemSource> CreateScheduledWorkItemSources()
        {
            List<IScheduledWorkItemSource> sources = new List<IScheduledWorkItemSource>();
            foreach (var type in this.ScheduledActivitySourceTypes)
            {
                var source = ScheduledActivitySource.Create(type, this.Context, this.Queuer);
                sources.Add(source);
            }

            return sources;
        }
    }
}
