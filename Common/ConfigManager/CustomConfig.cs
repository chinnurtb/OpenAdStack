//-----------------------------------------------------------------------
// <copyright file="CustomConfig.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace ConfigManager
{
    /// <summary>
    /// Provides access to a customizable version of the global configuration
    /// </summary>
    public class CustomConfig : ConfigBase
    {
        /// <summary>Global configuration overrides</summary>
        private readonly IDictionary<string, string> overrides;

        /// <summary>Initializes a new instance of the CustomConfig class.</summary>
        public CustomConfig() : this(null)
        {
        }

        /// <summary>Initializes a new instance of the CustomConfig class.</summary>
        /// <param name="overrides">Global configuration override</param>
        public CustomConfig(IDictionary<string, string> overrides)
        {            
            this.overrides = overrides ?? new Dictionary<string, string>(0);
        }

        /// <summary>Gets the dictionary of setting overrides</summary>
        public IDictionary<string, string> Overrides
        {
            get { return this.overrides; }
        }

        /// <summary>
        /// Get a string config value from either overrides or the global config
        /// </summary>
        /// <param name="configValueName">Config value name.</param>
        /// <returns>The config value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="configValueName"/> is null.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// No setting exists for <paramref name="configValueName"/>.
        /// </exception>
        protected override string GetConfigValue(string configValueName)
        {
            return this.overrides.ContainsKey(configValueName) ?
                this.overrides[configValueName] :
                Config.GetValue(configValueName);
        }
    }
}
