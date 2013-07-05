//-----------------------------------------------------------------------
// <copyright file="IConfig.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;

namespace ConfigManager
{
    /// <summary>Interface for classes providing access to configuration</summary>
    public interface IConfig
    {
        /// <summary>Get a string config value</summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="configValueName"/> is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        string GetValue(string configValueName);

        /// <summary>
        /// Get an int config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The int parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// Setting value is not a valid int.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting ints")]
        int GetIntValue(string configValueName);

        /// <summary>
        /// Get an array of int config values from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// One or more entries in the list are empty.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// One or more entries are not valid ints.
        /// </exception>
        int[] GetIntValues(string configValueName);

        /// <summary>
        /// Get a double config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The double parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// Setting value is not a valid double.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting doubles")]
        double GetDoubleValue(string configValueName);

        /// <summary>
        /// Get a bool config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// Setting value is not a valid bool.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Methods is for getting bool")]
        bool GetBoolValue(string configValueName);

        /// <summary>
        /// Get a TimeSpan config value from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// Setting value is not a valid TimeSpan.
        /// </exception>
        TimeSpan GetTimeSpanValue(string configValueName);

        /// <summary>
        /// Get an array of TimeSpan config values from the global configuration.
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The bool parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// One or more entries in the list are empty.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// One or more entries are not valid TimeSpans.
        /// </exception>
        TimeSpan[] GetTimeSpanValues(string configValueName);

        /// <summary>
        /// Get an enum config value from the global configuration.
        /// </summary>
        /// <typeparam name="TEnum">Enum type.</typeparam>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The enum value parsed from the config value.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>,
        /// or TEnum is not an enum,
        /// or the value is not valid for the enum.
        /// </exception>
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "TEnum is a generic parameter")]
        TEnum GetEnumValue<TEnum>(string configValueName)
            where TEnum : struct, IConvertible;
    }
}
