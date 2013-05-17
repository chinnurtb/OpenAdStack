//-----------------------------------------------------------------------
// <copyright file="IntervalScheduleFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScheduledActivities;
using ScheduledActivities.Schedules;

namespace ScheduledActivityUnitTests
{
    /// <summary>Tests for IntervalSchedule</summary>
    [TestClass]
    public class IntervalScheduleFixture
    {
        /// <summary>
        /// Basic create test for IntervalSchedule(hours, minutes, seconds)
        /// </summary>
        [TestMethod]
        public void CreateSchedule()
        {
            var schedule = new IntervalSchedule(1, 2, 3);
            var now = DateTime.UtcNow;
            Assert.AreEqual(now + new TimeSpan(1, 2, 3), schedule.GetNextTime(now));
        }

        //// TODO: Test other constructors

        /// <summary>
        /// Basic create test for ConfigIntervalSchedule
        /// </summary>
        [TestMethod]
        public void CreateConfigSchedule()
        {
            var configValueName = Guid.NewGuid().ToString("N");
            var timeSpan = "12:34:56";
            ConfigurationManager.AppSettings[configValueName] = timeSpan;
            var schedule = new ConfigIntervalSchedule(configValueName);
            var now = DateTime.UtcNow;
            Assert.AreEqual(now + TimeSpan.Parse(timeSpan, CultureInfo.InvariantCulture), schedule.GetNextTime(now));
        }

        /// <summary>
        /// Test getting the next time from a schedule when the time will be the next day
        /// </summary>
        [TestMethod]
        public void ScheduleAcrossDays()
        {
            var schedule = new IntervalSchedule(0, 45, 0);
            var morning = DateTime.UtcNow.Date + new TimeSpan(11, 30, 0);
            var expectedMorningSchedule = morning + new TimeSpan(0, 45, 0);
            var evening = DateTime.UtcNow.Date + new TimeSpan(23, 30, 0);
            var expectedEveningSchedule = evening + new TimeSpan(0, 45, 0);
            
            var morningSchedule = schedule.GetNextTime(morning);
            Assert.AreEqual(expectedMorningSchedule, morningSchedule);

            var eveningSchedule = schedule.GetNextTime(evening);
            Assert.AreEqual(expectedEveningSchedule, eveningSchedule);
        }
    }
}
