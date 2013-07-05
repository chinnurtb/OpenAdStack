//-----------------------------------------------------------------------
// <copyright file="CustomConfig.cs" company="Rare Crowds Inc">
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
