//-----------------------------------------------------------------------
// <copyright file="ConfigTimeOfDaySchedule.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using ConfigManager;

namespace ScheduledActivities.Schedules
{
    /// <summary>Schedule that runs at a specific time of day specified in a config value</summary>
    /// <seealso cref="System.DateTime.TimeOfDay"/>
    /// <seealso cref="System.TimeSpan"/>
    /// <seealso cref="ScheduledActivities.Schedules.TimeOfDaySchedule"/>
    public class ConfigTimeOfDaySchedule : TimeOfDaySchedule, IActivitySourceSchedule
    {
        /// <summary>Initializes a new instance of the ConfigTimeOfDaySchedule class.</summary>
        /// <param name="configValueName">Config value name</param>
        public ConfigTimeOfDaySchedule(string configValueName)
            : base(Config.GetValue(configValueName))
        {
        }
    }
}
