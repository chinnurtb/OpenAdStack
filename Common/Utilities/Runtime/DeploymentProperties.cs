//-----------------------------------------------------------------------
// <copyright file="DeploymentProperties.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;
using Utilities.Storage;

namespace Utilities.Runtime
{
    /// <summary>Deployment runtime properties</summary>
    public class DeploymentProperties
    {
        /// <summary>Key for the "active" deployment property dictionary</summary>
        internal const string ActiveDeployment = "[ACTIVE]";

        /// <summary>Name of the store for deployment properties</summary>
        internal const string DeploymentPropertyStoreName = "deployment-props";

        /// <summary>Format for the role instance keys</summary>
        internal const string RoleInstanceKeyFormat = "{0}-{1}";

        /// <summary>Dictionary containing deployment properties</summary>
        private readonly IPersistentDictionary<IDictionary<string, string>> dictionary;

        /// <summary>Singleton instance</summary>
        private static DeploymentProperties instance;

        /// <summary>
        /// Initializes a new instance of the DeploymentProperties class
        /// </summary>
        protected DeploymentProperties()
        {
            this.dictionary = PersistentDictionaryFactory.CreateDictionary<IDictionary<string, string>>(DeploymentPropertyStoreName);
        }

        /// <summary>Gets or sets the "active" deployment id</summary>
        public static string ActiveDeploymentId
        {
            get { return Instance.GetDeploymentPropertyImpl(ActiveDeployment, "DeploymentId"); }
            set { Instance.SetDeploymentPropertyImpl(ActiveDeployment, "DeploymentId", value); }
        }

        /// <summary>Gets the current deployment id</summary>
        public static string DeploymentId
        {
            get { return Instance.GetDeploymentId(); }
        }

        /// <summary>Gets the current role instance id</summary>
        public static string RoleInstanceId
        {
            get { return Instance.GetRoleInstanceId(); }
        }

        /// <summary>Gets the state of the current deployment</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806", Justification = "Return value of Enum.TryParse is irrelevant")]
        public static DeploymentState DeploymentState
        {
            get
            {
                var deploymentState = DeploymentProperties.GetDeploymentProperty("State");
                if (string.IsNullOrWhiteSpace(deploymentState))
                {
                    return DeploymentState.Unknown;
                }

                DeploymentState state;
                Enum.TryParse<DeploymentState>(deploymentState, true, out state);
                return state;
            }
        }

        /// <summary>Gets or sets the state of the current role instance</summary>
        [SuppressMessage("Microsoft.Usage", "CA1806", Justification = "Return value of Enum.TryParse is irrelevant")]
        public static RoleInstanceState RoleInstanceState
        {
            get
            {
                var roleInstanceState = DeploymentProperties.GetRoleInstanceProperty("RoleState");
                if (string.IsNullOrWhiteSpace(roleInstanceState))
                {
                    return RoleInstanceState.Unknown;
                }

                RoleInstanceState state;
                Enum.TryParse<RoleInstanceState>(roleInstanceState, true, out state);
                return state;
            }

            set
            {
                DeploymentProperties.SetRoleInstanceProperty("RoleState", value.ToString());
            }
        }

        /// <summary>Gets the dictionary containing deployment properties</summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic type is appropriate in this case")]
        public static IPersistentDictionary<IDictionary<string, string>> Dictionary
        {
            get { return Instance.dictionary; }
        }

        /// <summary>Gets or sets the singleton instance</summary>
        internal static DeploymentProperties Instance
        {
            get { return instance = instance ?? new DeploymentProperties(); }
            set { instance = value; }
        }

        /// <summary>Gets the property dictionary key for the role instance</summary>
        private static string RoleInstanceKey
        {
            get
            {
                return RoleInstanceKeyFormat
                    .FormatInvariant(DeploymentId, RoleInstanceId);
            }
        }

        /// <summary>Gets a property value for the current deployment</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property value, or null if it has not been set</returns>
        public static string GetDeploymentProperty(string propertyName)
        {
            return Instance.GetDeploymentPropertyImpl(DeploymentId, propertyName);
        }

        /// <summary>Sets a property value for the current deployment</summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public static void SetDeploymentProperty(string propertyName, string propertyValue)
        {
            Instance.SetDeploymentPropertyImpl(DeploymentId, propertyName, propertyValue);
        }

        /// <summary>Gets a property value for the current role instance</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property value, or null if it has not been set</returns>
        public static string GetRoleInstanceProperty(string propertyName)
        {
            return Instance.GetRoleInstancePropertyImpl(propertyName);
        }

        /// <summary>Sets a property value for the current role instance</summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public static void SetRoleInstanceProperty(string propertyName, string propertyValue)
        {
            Instance.SetRoleInstancePropertyImpl(propertyName, propertyValue);
        }

        /// <summary>Gets property values for all role instances</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property values</returns>
        public static string[] GetRoleInstanceProperties(string propertyName)
        {
            return Instance.GetRoleInstancePropertiesImpl(propertyName);
        }

        /// <summary>Gets the deployment Id</summary>
        /// <returns>The deployment id</returns>
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Using a Get method to avoid conflict with public static property")]
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will only be called in Azure context")]
        protected virtual string GetDeploymentId()
        {
            return RoleEnvironment.IsAvailable ?
                RoleEnvironment.DeploymentId :
                Guid.Empty.ToString("N");
        }

        /// <summary>Gets the current role's instance id</summary>
        /// <returns>The role instance id</returns>
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Using a Get method to avoid conflict with public static property")]
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will only be called in Azure context")]
        protected virtual string GetRoleInstanceId()
        {
            return RoleEnvironment.IsAvailable ?
                RoleEnvironment.CurrentRoleInstance.Id :
                Guid.Empty.ToString("N");
        }

        /// <summary>Gets a deployment property value</summary>
        /// <param name="deploymentId">Deployment Id</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property value, or null if it has not been set</returns>
        private string GetDeploymentPropertyImpl(string deploymentId, string propertyName)
        {
            if (!this.dictionary.ContainsKey(deploymentId) ||
                !this.dictionary[deploymentId].ContainsKey(propertyName))
            {
                return null;
            }

            return this.dictionary[deploymentId][propertyName];
        }

        /// <summary>Sets a deployment property value</summary>
        /// <param name="deploymentId">Deployment Id</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        private void SetDeploymentPropertyImpl(string deploymentId, string propertyName, string propertyValue)
        {
            this.CreatePropertiesIfNotExist(deploymentId);
            this.dictionary.TryUpdateValue(
                deploymentId,
                values => values[propertyName] = propertyValue);
        }

        /// <summary>Gets a role instance property value</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property value, or null if it has not been set</returns>
        private string GetRoleInstancePropertyImpl(string propertyName)
        {
            if (!this.dictionary.ContainsKey(RoleInstanceKey) ||
                !this.dictionary[RoleInstanceKey].ContainsKey(propertyName))
            {
                return null;
            }

            return this.dictionary[RoleInstanceKey][propertyName];
        }

        /// <summary>Sets a role instance property value</summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        private void SetRoleInstancePropertyImpl(string propertyName, string propertyValue)
        {
            this.CreatePropertiesIfNotExist(RoleInstanceKey);
            this.dictionary.TryUpdateValue(
                RoleInstanceKey,
                values => values[propertyName] = propertyValue);
        }

        /// <summary>Gets property values for all role instances</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property values</returns>
        private string[] GetRoleInstancePropertiesImpl(string propertyName)
        {
            return this.dictionary
                .Where(kvp => kvp.Key.StartsWith(DeploymentId, StringComparison.Ordinal))
                .Select(kvp => kvp.Value)
                .Where(props => props.ContainsKey(propertyName))
                .Select(props => props[propertyName])
                .ToArray();
        }

        /// <summary>
        /// Creates a properties dictionary in the store if one does not exist.
        /// </summary>
        /// <param name="propertiesKey">
        /// Key in the persistent dictionary to store the properties
        /// </param>
        private void CreatePropertiesIfNotExist(string propertiesKey)
        {
            if (!this.dictionary.ContainsKey(propertiesKey))
            {
                try
                {
                    this.dictionary.Add(propertiesKey, new Dictionary<string, string>());
                }
                catch (InvalidETagException)
                {
                    // Ignore invalid eTag exceptions, these will happen if there's
                    // a race with another thread between ContainsKey and Add.
                    // All that matters here is that it exists.
                }
                catch (ArgumentException ae)
                {
                    // Ignore this race condition as well for the same reasons
                    if (!ae.Message.Contains("value already exists"))
                    {
                        throw;
                    }
                }
            }
        }
    }
}
