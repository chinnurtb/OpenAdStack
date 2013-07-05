//-----------------------------------------------------------------------
// <copyright file="ConfigTimeOfDaySchedule.cs" company="Rare Crowds Inc">
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
