//-----------------------------------------------------------------------
// <copyright file="IActivitySourceSchedule.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduledActivities
{
    /// <summary>Interface for activity request source schedules</summary>
    public interface IActivitySourceSchedule
    {
        /// <summary>
        /// Gets when the scheduled activity request source should next be checked
        /// for new work items.
        /// </summary>
        /// <param name="lastTime">The last time the source was checked</param>
        /// <returns>The next time the source should be checked</returns>
        DateTime GetNextTime(DateTime lastTime);
    }
}
