//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ConfigManager
{
    /// <summary>
    /// Provides access to configuration regardless of the runtime environment
    /// from either the app.config or Azure runtime role settings.
    /// </summary>
    public static class Config
    {
        /// <summary>Global configuration used by static accessors</summary>
        private static readonly ConfigBase Global = new ConfigBase(GetGlobalValue);

        /// <summary>
        /// Gets a value indicating whether settings should
        /// be retrieved from the role environment.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1806", Justification = "If bool.TryParse fails then default ignoreRoleEnvironment value of false is desired")]
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Runtime dependency check cannot bubble up.")]
        private static bool UseRoleEnvironment
        {
            get
            {
                var roleEnvironmentAvailable = false;

                try
                {
                    roleEnvironmentAvailable = RoleEnvironment.IsAvailable;
                }
                catch
                {
                }

                bool ignoreRoleEnvironment = false;
                bool.TryParse(
                    ConfigurationManager.AppSettings["Config.IgnoreRoleEnvironment"],
                    out ignoreRoleEnvironment);
                return !ignoreRoleEnvironment && roleEnvironmentAvailable;
            }
        }

        /// <summary>Get a string config value</summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        public static string GetValue(string configValueName)
        {
            return Global.GetValue(configValueName);
        }

        /// <summary>
        /// Get an int config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The int parsed from the config value.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting ints")]
        public static int GetIntValue(string configValueName)
        {
            return Global.GetIntValue(configValueName);
        }

        /// <summary>
        /// Get an array of int config values from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting ints")]
        public static int[] GetIntValues(string configValueName)
        {
            return Global.GetIntValues(configValueName);
        }

        /// <summary>
        /// Get a double config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The double parsed from the config value.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting doubles")]
        public static double GetDoubleValue(string configValueName)
        {
            return Global.GetDoubleValue(configValueName);
        }

        /// <summary>
        /// Get a bool config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting bool")]
        public static bool GetBoolValue(string configValueName)
        {
            return Global.GetBoolValue(configValueName);
        }

        /// <summary>
        /// Get a TimeSpan config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        public static TimeSpan GetTimeSpanValue(string configValueName)
        {
            return Global.GetTimeSpanValue(configValueName);
        }

        /// <summary>
        /// Get an array of TimeSpan config values from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        public static TimeSpan[] GetTimeSpanValues(string configValueName)
        {
            return Global.GetTimeSpanValues(configValueName);
        }

        /// <summary>
        /// Get an enum config value from the global configuration.
        /// </summary>
        /// <typeparam name="TEnum">Enum type.</typeparam>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The enum value parsed from the config value.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "TEnum is a generic parameter")]
        public static TEnum GetEnumValue<TEnum>(string configValueName)
            where TEnum : struct, IConvertible
        {
            return Global.GetEnumValue<TEnum>(configValueName);
        }

        /// <summary>
        /// Get a string config value from app.config or Azure config.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="configValueName"/> is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        private static string GetGlobalValue(string configValueName)
        {
            if (configValueName == null)
            {
                throw new ArgumentNullException("configValueName");
            }

            if (UseRoleEnvironment)
            {
                try
                {
                    return RoleEnvironment.GetConfigurationSettingValue(configValueName);
                }
                catch (RoleEnvironmentException ree)
                {
                    throw new ArgumentException("Setting does not exist: " + configValueName, "configValueName", ree);
                }
            }

            var value = ConfigurationManager.AppSettings[configValueName];
            if (value == null)
            {
                throw new ArgumentException("Setting does not exist: " + configValueName, "configValueName");
            }

            return value;
        }
    }
}
