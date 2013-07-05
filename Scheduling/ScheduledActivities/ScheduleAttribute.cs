//-----------------------------------------------------------------------
// <copyright file="ScheduleAttribute.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using ScheduledWorkItems;

namespace ScheduledActivities
{
    /// <summary>
    /// Attribute specifying what values are required in the ActivityRequest
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ScheduleAttribute : Attribute
    {
        /// <summary>
        /// List of the arguments used to initialize the IWorkItemSourceSchedule implementation
        /// </summary>
        private object[] scheduleArgs;

        /// <summary>
        /// Initializes a new instance of the ScheduleAttribute class.
        /// </summary>
        /// <remarks>
        /// The properties of this attribute are used by ScheduledActivitySource to initialize
        /// the IWorkItemSourceSchedule instance returned by IScheduledWorkItemSource.Schedule.
        /// If the scheduleType does not have a public constructor matching the provided
        /// scheduleArgs then a System.MissingMethodException will be thrown when the
        /// ScheduledActivitySource constructor attempts to create the IWorkItemSourceSchedule
        /// instance using Activator.CreateInstance(Type, object[]).
        /// </remarks>
        /// <param name="scheduleType">
        /// IWorkItemSourceSchedule implementation to use
        /// </param>
        /// <param name="scheduleArgs">
        /// Arguments used to initialize the IWorkItemSourceSchedule implementation
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if scheduleType is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if scheduleType does not implement IWorkItemSourceSchedule.
        /// </exception>
        /// <see cref="ScheduledActivities.ScheduledActivitySource"/>
        /// <see cref="ScheduledActivities.IActivitySourceSchedule"/>
        /// <see cref="ScheduledWorkItems.IScheduledWorkItemSource"/>
        public ScheduleAttribute(Type scheduleType, params object[] scheduleArgs)
        {
            if (scheduleType == null)
            {
                throw new ArgumentNullException("scheduleType");
            }

            if (!typeof(IActivitySourceSchedule).IsAssignableFrom(scheduleType))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The type '{0}' does not implement IWorkItemSourceSchedule.",
                    scheduleType.FullName);
                throw new ArgumentException(message, "scheduleType");
            }

            this.ScheduleType = scheduleType;
            this.scheduleArgs = scheduleArgs;
        }

        /// <summary>Gets the type of the schedule</summary>
        public Type ScheduleType { get; private set; }

        /// <summary>Gets the arguments for the schedule</summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Okay to suppress for attributes: http://msdn.microsoft.com/en-us/library/0fss9skc.aspx")]
        public object[] ScheduleArgs
        {
            get
            {
                return this.scheduleArgs.ToArray();
            }
        }
    }
}
