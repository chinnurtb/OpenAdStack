//-----------------------------------------------------------------------
// <copyright file="ConfigIntervalSchedule.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using ConfigManager;

namespace ScheduledActivities.Schedules
{
    /// <summary>
    /// Schedule that runs on a TimeSpan interval specified in a config value
    /// </summary>
    /// <seealso cref="System.TimeSpan"/>
    /// <seealso cref="ScheduledActivities.Schedules.IntervalSchedule"/>
    public class ConfigIntervalSchedule : IntervalSchedule, IActivitySourceSchedule
    {
        /// <summary>
        /// Initializes a new instance of the ConfigIntervalSchedule class.
        /// </summary>
        /// <param name="configValueName">Config value name</param>
        public ConfigIntervalSchedule(string configValueName)
            : base(Config.GetValue(configValueName))
        {
        }
    }
}
