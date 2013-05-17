//-----------------------------------------------------------------------
// <copyright file="ConfigManagerFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using ConfigManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonUnitTests
{
    /// <summary>
    /// Tests for the ConfigManager
    /// </summary>
    [TestClass]
    public class ConfigManagerFixture
    {
        /// <summary>
        /// Setting name for this test
        /// </summary>
        private string settingName;

        /// <summary>
        /// Per-method initialization for the tests
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.settingName = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Test getting a string value
        /// </summary>
        [TestMethod]
        public void GetStringValue()
        {
            var expected = Guid.NewGuid().ToString();
            ConfigurationManager.AppSettings[this.settingName] = expected;
            var value = Config.GetValue(this.settingName);
            Assert.AreEqual<string>(expected, value);
        }

        /// <summary>
        /// Test getting a string value that doesn't exist
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMissingStringValue()
        {
            Config.GetValue(this.settingName);
        }

        /// <summary>
        /// Test getting a an int value
        /// </summary>
        [TestMethod]
        public void GetIntValue()
        {
            var expected = 42;
            ConfigurationManager.AppSettings[this.settingName] = expected.ToString();
            var value = Config.GetIntValue(this.settingName);
            Assert.AreEqual(expected, value);
        }

        /// <summary>
        /// Test getting a an int value that doesn't exist
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMissingIntValue()
        {
            Config.GetIntValue(this.settingName);
        }

        /// <summary>
        /// Test getting a an int value that isn't a valid int
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void GetInvalidIntValue()
        {
            ConfigurationManager.AppSettings[this.settingName] = "Invalid";
            Config.GetIntValue(this.settingName);
        }

        /// <summary>
        /// Test getting an array of int values
        /// </summary>
        [TestMethod]
        public void GetIntValues()
        {
            var expected = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23 };
            ConfigurationManager.AppSettings[this.settingName] = string.Join("|", expected);
            var values = Config.GetIntValues(this.settingName) as int[];
            Assert.IsNotNull(values);
            Assert.AreEqual(expected.Length, values.Length);
            foreach (var value in Enumerable.Zip(expected, values, (a, b) => new Tuple<int, int>(a, b)))
            {
                Assert.AreEqual(value.Item1, value.Item2);
            }
        }

        /// <summary>
        /// Test getting a an array of int values that don't exist
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMissingIntValues()
        {
            Config.GetIntValues(this.settingName);
        }

        /// <summary>
        /// Test getting an array of int values containing empties
        /// </summary>
        /// <remarks>
        /// Expected behavior is all or nothing. If one is invalid then
        /// an exception should be thrown. Partial success is not supported.
        /// </remarks>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetEmptyIntValues()
        {
            ConfigurationManager.AppSettings[this.settingName] = "2|3||5||7";
            Config.GetIntValues(this.settingName);
        }

        /// <summary>
        /// Test getting an array containing invalid int value(s)
        /// </summary>
        /// <remarks>
        /// Expected behavior is all or nothing. If one is invalid then
        /// an exception should be thrown. Partial success is not supported.
        /// </remarks>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetInvalidIntValues()
        {
            // Cannot mix doubles in with ints
            var values = new[] { 2, 3.14, 5, 7, 11, 13, 17, 19, 23 };
            ConfigurationManager.AppSettings[this.settingName] = string.Join("|", values);
            Config.GetIntValues(this.settingName);
        }

        /// <summary>
        /// Test getting a double value
        /// </summary>
        [TestMethod]
        public void GetDoubleValue()
        {
            var expected = 42.42;
            ConfigurationManager.AppSettings[this.settingName] = expected.ToString();
            var value = Config.GetDoubleValue(this.settingName);
            Assert.AreEqual(expected, value);
        }

        /// <summary>
        /// Test getting a bool value
        /// </summary>
        [TestMethod]
        public void GetBoolValue()
        {
            var expected = true;
            ConfigurationManager.AppSettings[this.settingName] = expected.ToString();
            var value = Config.GetBoolValue(this.settingName);
            Assert.AreEqual(expected, value);
        }

        /// <summary>
        /// Test getting a TimeSpan value
        /// </summary>
        [TestMethod]
        public void GetTimeSpanValue()
        {
            var expected = new TimeSpan(1, 2, 34, 56, 789);
            ConfigurationManager.AppSettings[this.settingName] = expected.ToString();
            var value = Config.GetTimeSpanValue(this.settingName);
            Assert.AreEqual(expected, value);
        }

        /// <summary>
        /// Test getting an array of TimeSpan values
        /// </summary>
        [TestMethod]
        public void GetTimeSpanValues()
        {
            var expected = new[] { "00:00:00", "00:00:00.100", "00:00:05", "01:02:03", "1.00:00:00", "5.04:03:02.001" };
            ConfigurationManager.AppSettings[this.settingName] = string.Join("|", expected);
            var values = Config.GetTimeSpanValues(this.settingName) as TimeSpan[];
            Assert.IsNotNull(values);
            Assert.AreEqual(expected.Length, values.Length);
            foreach (var value in
                Enumerable.Zip(
                    expected,
                    values,
                    (a, b) => new Tuple<TimeSpan, TimeSpan>(TimeSpan.Parse(a, CultureInfo.InvariantCulture), b)))
            {
                Assert.AreEqual(value.Item1, value.Item2);
            }
        }

        /// <summary>
        /// Test getting a an array of TimeSpan values that don't exist
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMissingTimeSpanValues()
        {
            Config.GetTimeSpanValues(this.settingName);
        }

        /// <summary>
        /// Test getting an array of TimeSpan values containing empties
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetEmptyTimeSpanValues()
        {
            var values = new[] { "00:00:00", string.Empty, "00:00:05", string.Empty, "1.00:00:00", "5.04:03:02.001" };
            ConfigurationManager.AppSettings[this.settingName] = string.Join("|", values);
            Config.GetTimeSpanValues(this.settingName);
        }

        /// <summary>
        /// Test getting an array containing invalid TimeSpan value(s)
        /// </summary>
        /// <remarks>
        /// Expected behavior is all or nothing. If one is invalid then
        /// an exception should be thrown. Partial success is not supported.
        /// </remarks>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetInvalidTimeSpanValues()
        {
            // 24:00:00.100 is invalid. Must use 1.00:00:00.100 instead
            var expected = new[] { "00:00:00", "24:00:00.100", "00:00:05.100", "01:02:03", "1.00:00:00", "5.04:03:02.001" };
            ConfigurationManager.AppSettings[this.settingName] = string.Join("|", expected);
            Config.GetTimeSpanValues(this.settingName);
        }

        /// <summary>
        /// Test getting values from a customized configuration
        /// </summary>
        [TestMethod]
        public void GetCustomizedConfigurationValues()
        {
            var globalValue = Guid.NewGuid().ToString();
            var overrideValue = Guid.NewGuid().ToString();

            ConfigurationManager.AppSettings[this.settingName] = globalValue;
            Assert.AreEqual(globalValue, Config.GetValue(this.settingName));

            var uncustomizedConfig = new CustomConfig(new Dictionary<string, string>());
            Assert.AreEqual(globalValue, uncustomizedConfig.GetValue(this.settingName));

            var customizedConfig = new CustomConfig(
                new Dictionary<string, string> { { this.settingName, overrideValue } });
            Assert.AreNotEqual(Config.GetValue(this.settingName), customizedConfig.GetValue(this.settingName));
            Assert.AreNotEqual(globalValue, customizedConfig.GetValue(this.settingName));
            Assert.AreEqual(overrideValue, customizedConfig.GetValue(this.settingName));
        }
    }
}
