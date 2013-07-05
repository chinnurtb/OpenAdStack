// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandLineArguments.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleAppUtilities
{
    /// <summary>
    /// Base class for CommandLineArgumentContainers.
    /// Provides a factory method that parses the arguments from a string array.
    /// </summary>
    public abstract class CommandLineArguments
    {
        /// <summary>Built in conversions for arguments</summary>
        /// <remarks>Includes enums and types assignable from double, int, DateTime and string.</remarks>
        private static readonly ConversionDictionary BuiltInConversions = new ConversionDictionary
        {
            { t => t.IsEnum, (t, s) => Enum.Parse(t, s, true) },
            { t => t.IsAssignableFrom(typeof(DateTime)), (t, s) => DateTime.Parse(s, CultureInfo.InvariantCulture) },
            { t => t.IsAssignableFrom(typeof(FileInfo)), (t, s) => new FileInfo(s) },
            { t => t.IsAssignableFrom(typeof(double)), (t, s) => double.Parse(s, CultureInfo.InvariantCulture) },
            { t => t.IsAssignableFrom(typeof(int)), (t, s) => int.Parse(s, CultureInfo.InvariantCulture) },
            { t => t.IsAssignableFrom(typeof(string)), (t, s) => s }
        };

        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public abstract bool ArgumentsValid { get; }

        /// <summary>Gets conversions to be used in addition to the built-in ones</summary>
        /// <remarks>Overrides should Concat(base.Conversions) to include the built-in conversions.</remarks>
        protected virtual ConversionDictionary Conversions
        {
            get { return BuiltInConversions; }
        }

        /// <summary>Gets a value indicating whether the case of argument identifiers should be ignored</summary>
        /// <remarks>False by default</remarks>
        protected virtual bool IgnoreIdentifierCase
        {
            get { return false; }
        }

        /// <summary>Gets a value indicating whether unknown identifiers should be ignored</summary>
        /// <remarks>False by default</remarks>
        protected virtual bool IgnoreUnknownIdentifiers
        {
            get { return false; }
        }

        /// <summary>
        /// Gets all properties decorated with CommandLineArgumentAttribute
        /// </summary>
        private IEnumerable<PropertyInfo> ArgumentProperties
        {
            get
            {
                return this.GetType()
                    .GetProperties()
                    .Where(p => p.GetCustomAttributes(true)
                        .OfType<CommandLineArgumentAttribute>()
                        .Count() > 0);
            }
        }

        /// <summary>Gets all CommandLineArgumentAttributes decorating the properties</summary>
        private IEnumerable<CommandLineArgumentAttribute> ArgumentAttributes
        {
            get
            {
                return this.ArgumentProperties
                    .SelectMany(p => p.GetCustomAttributes(true))
                    .OfType<CommandLineArgumentAttribute>();
            }
        }

        /// <summary>Initializes a new instance of the CommandLineArguments derived class.</summary>
        /// <typeparam name="TArguments">CommandLineArguments derived class to create an instance of</typeparam>
        /// <param name="args">The arguments to be parsed</param>
        /// <returns>The initialized TArguments instance</returns>
        public static TArguments Create<TArguments>(string[] args)
            where TArguments : CommandLineArguments, new()
        {
            var arguments = new TArguments();
            arguments.SetDefaultValues();
            arguments.ParseCommandLineArguments(args);
            return arguments;
        }

        /// <summary>Gets the argument descriptions formatted for display in a usage message.</summary>
        /// <typeparam name="TArguments">CommandLineArguments derived class</typeparam>
        /// <returns>The descriptions of TArgument's argument properties</returns>
        [SuppressMessage("Microsoft.Design", "CA1004", Justification = "The alternative would be to replace TArguments with a Type parameter and lose the compile time type checking")]
        public static string GetDescriptions<TArguments>()
            where TArguments : CommandLineArguments, new()
        {
            StringBuilder descriptions = new StringBuilder();
            var arguments = new TArguments();
            foreach (var argument in arguments.ArgumentAttributes)
            {
                // TODO: Include type information
                descriptions.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}\t{1}",
                    string.Join(", ", argument.AllIdentifiers),
                    argument.Description));
            } 

            return descriptions.ToString();
        }

        /// <summary>Sets default values for the properties decorated with CommandLineArgumentAttribute</summary>
        private void SetDefaultValues()
        {
            // Get the argument attributes that have defaults
            var defaultArgumentAttributes = this.ArgumentAttributes
                .Where(arg => arg.DefaultValue != null || arg.DefaultAppSetting != null);

            foreach (var argumentAttribute in defaultArgumentAttributes)
            {
                // Get the default value
                var defaultValue = argumentAttribute.DefaultValue ??
                    ConfigurationManager.AppSettings[argumentAttribute.DefaultAppSetting];

                // Get the property corresponding to the defaultArgumentAttribute
                var property = this.ArgumentProperties
                    .Where(p => p.GetCustomAttributes(true)
                        .OfType<CommandLineArgumentAttribute>()
                        .Contains(argumentAttribute))
                    .Single();

                // Set the property to the default value
                this.SetPropertyValue(property, defaultValue);
            }
        }

        /// <summary>Parses the args and sets the properties decorated with CommandLineArgumentAttribute</summary>
        /// <param name="args">The arguments</param>
        private void ParseCommandLineArguments(string[] args)
        {
            // Parse the arguments
            for (int i = 0; i < args.Length; i++)
            {
                // Get the CommandLineArgumentAttribute for the identifier
                var identifier = this.IgnoreIdentifierCase ? args[i].ToUpperInvariant() : args[i];
                var argumentAttribute = this.ArgumentAttributes
                    .Where(a => a.AllIdentifiers
                        .Select(id => this.IgnoreIdentifierCase ? id.ToUpperInvariant() : id)
                        .Contains(identifier))
                    .SingleOrDefault();

                // No argumentAttribute matching the identifier were found
                if (argumentAttribute == null)
                {
                    if (!this.IgnoreUnknownIdentifiers)
                    {
                        var message = string.Format(CultureInfo.InvariantCulture, "Unknown argument identifier: '{0}'", identifier);
                        throw new ArgumentException(message);
                    }

                    continue;
                }

                // Get the property corresponding to the found argumentAttribute
                var property = this.ArgumentProperties
                    .Where(p => p.GetCustomAttributes(true)
                        .OfType<CommandLineArgumentAttribute>()
                        .Contains(argumentAttribute))
                    .Single();

                // Boolean arguments are set true if present
                if (property.PropertyType.IsAssignableFrom(typeof(bool)))
                {
                    property.SetValue(this, true, null);
                    continue;
                }

                // Set the property to the provided value
                this.SetPropertyValue(property, args[++i]);
            }
        }

        /// <summary>
        /// Sets the property using the provided string value via the conversion
        /// for the property's type.
        /// </summary>
        /// <param name="property">The property</param>
        /// <param name="value">The string value</param>
        /// <exception cref="ArgumentException">
        /// No conversion for the property's PropertyType was found.
        /// </exception>
        private void SetPropertyValue(PropertyInfo property, string value)
        {
            // Find a conversion for the property type
            var conversion = this.Conversions.FirstOrDefault(c => c.Key(property.PropertyType)).Value;
            if (conversion == null)
            {
                throw new ArgumentException(
                    "Unable to find a conversion for '{0}' ({1})"
                    .FormatInvariant(property.Name, property.PropertyType.FullName));
            }

            // Set the target's property to the converted value
            property.SetValue(this, conversion(property.PropertyType, value), null);
        }
    }
}
