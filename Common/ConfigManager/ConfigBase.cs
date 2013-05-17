//-----------------------------------------------------------------------
// <copyright file="ConfigBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace ConfigManager
{
    /// <summary>Provides access to configuration</summary>
    public class ConfigBase : IConfig
    {
        /// <summary>Function used to get values</summary>
        private readonly Func<string, string> getValue;

        /// <summary>Initializes a new instance of the ConfigBase class.</summary>
        internal ConfigBase()
            : this(null)
        {
        }

        /// <summary>Initializes a new instance of the ConfigBase class.</summary>
        /// <param name="valueAccessor">Function used to get values</param>
        internal ConfigBase(Func<string, string> valueAccessor)
        {
            this.getValue = valueAccessor ?? this.GetConfigValue;
        }

        /// <summary>Get a string config value</summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="configValueName"/> is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        public string GetValue(string configValueName)
        {
            return this.getValue(configValueName);
        }

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
        public int GetIntValue(string configValueName)
        {
            var value = this.getValue(configValueName);
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

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
        public int[] GetIntValues(string configValueName)
        {
            return GetValues<int>(
                configValueName,
                value => int.Parse(value, CultureInfo.InvariantCulture));
        }

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
        public double GetDoubleValue(string configValueName)
        {
            var value = this.getValue(configValueName);
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

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
        public bool GetBoolValue(string configValueName)
        {
            var value = this.getValue(configValueName);
            return bool.Parse(value);
        }

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
        public TimeSpan GetTimeSpanValue(string configValueName)
        {
            var value = this.getValue(configValueName);
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

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
        public TimeSpan[] GetTimeSpanValues(string configValueName)
        {
            return GetValues<TimeSpan>(
                configValueName,
                value => TimeSpan.Parse(value, CultureInfo.InvariantCulture));
        }

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
        public TEnum GetEnumValue<TEnum>(string configValueName)
            where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enum", "TEnum");
            }

            var value = this.getValue(configValueName);
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }

        /// <summary>Overridable method for getting values from config.</summary>
        /// <remarks>
        /// Not Implemented. Either override this method in a base
        /// class or provide a valueAccessor at construction.
        /// </remarks>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        protected virtual string GetConfigValue(string configValueName)
        {
            throw new NotImplementedException("Either override ConfigBase.GetValue or provide a valueAccessor");
        }

        /// <summary>
        /// Gets an array of values parsed from a config value in the app.config or Azure config
        /// </summary>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="configValueName">Config value name.</param>
        /// <param name="parser">A function to parse each value.</param>
        /// <returns>The array of parsed values.</returns>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// One or more entries in the list are empty.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// One or more entries are not valid.
        /// </exception>
        private TValue[] GetValues<TValue>(string configValueName, Func<string, TValue> parser)
        {
            var value = this.getValue(configValueName);
            var values = value.Split(new[] { "|" }, StringSplitOptions.None);
            if (values.Contains(null) || values.Contains(string.Empty))
            {
                throw new ArgumentNullException("configValueName", "Value cannot contain empty elements");
            }

            try
            {
                return values.Select(parser).ToArray();
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} contains one or more invalid values",
                        configValueName),
                    e);
            }
        }
    }
}
