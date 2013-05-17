// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ConsoleAppUtilities;
using SqlUtilities.Storage;
using Utilities.Runtime;
using Utilities.Storage;

namespace Utilities.DeploymentProps
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Separator between deployment and role instances</summary>
        private const char DeploymentInstanceSeparator = '-';

        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Views and downloads entries from persistent dictionaries.
Usage: DProps.exe {{-c Command}} [-d DeploymentID] [-i RoleInstanceID] [additional optional arguments]
{0}";

        /// <summary>Format for verbose index</summary>
        private const string VerboseIndexFormat =
@"Deployments and role instances:
{0}

({1} total, {3} role instances and {2} deployments)";

        /// <summary>Format for verbose list</summary>
        private const string VerboseListFormat =
@"Properties:
{0}

({1} properties in {2} role instances and {3} deployments)";

        /// <summary>Format for verbose get</summary>
        private const string VerboseGetFormat =
@"Values:
{0}

({1} values in {2} role instances and {3} deployments)";

        /// <summary>Format for verbose remove</summary>
        private const string VerboseRemoveFormat = @"Removed '{0}' from {1} deployments and/or role instances.";

        /// <summary>Format for debug information</summary>
        private const string DebugInformationFormat = @"
Execution time: {0}ms
Connection string: {1}";

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<DeploymentPropsArgs>());
            }
        }

        /// <summary>Gets or sets the SQL connection string</summary>
        private static string ConnectionString
        {
            get { return ConfigurationManager.AppSettings["ConnectionString"]; }
            set { ConfigurationManager.AppSettings["ConnectionString"] = value; }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        internal static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(Usage);
                return 0;
            }

            try
            {
                // Parse command-line arguments
                var arguments = CommandLineArguments.Create<DeploymentPropsArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    Console.WriteLine(Usage);
                    return 1;
                }

                // Initialize persistent storage (used by Utilities.Runtime.DeploymentProperties)
                ConnectionString = arguments.ConnectionString ?? ConnectionString;
                ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = "Sql";
                PersistentDictionaryFactory.Initialize(new[] { new SqlDictionaryFactory(ConnectionString) });

                // Get deployment ids matching argument filters
                var deploymentIds = GetDeploymentIds(
                    arguments.DeploymentId,
                    arguments.InstanceId,
                    arguments.PropertyName,
                    arguments.Recursive || arguments.Command == PropsCommand.Instances);

                // Run the specified command
                var commandStartTime = DateTime.UtcNow;
                switch (arguments.Command)
                {
                    case PropsCommand.Index:
                        // Display an index of the deployment ids
                        Index(deploymentIds, arguments.Verbose);
                        break;
                    case PropsCommand.Instances:
                        // Display an index of the filtered deployment ids
                        // Additionally filters for only instances
                        deploymentIds = deploymentIds
                            .Where(id => id.Contains(DeploymentInstanceSeparator))
                            .ToArray();
                        Index(deploymentIds, arguments.Verbose);
                        break;
                    case PropsCommand.List:
                        // List the names of all properties in the filtered deployments
                        List(deploymentIds, arguments.Verbose);
                        break;
                    case PropsCommand.Get:
                        // Get the specified property from the filtered deployments
                        Get(deploymentIds, arguments.PropertyName, arguments.PropertyValue, arguments.Verbose);
                        break;
                    case PropsCommand.Set:
                        // Set the specified property on the specified deployment/instance
                        Set(arguments.DeploymentId, arguments.InstanceId, arguments.PropertyName, arguments.PropertyValue);
                        break;
                    case PropsCommand.Remove:
                        // Remove the specified property from the filtered deployments
                        Remove(deploymentIds, arguments.PropertyName, arguments.Verbose);
                        break;
                }

                var commandExecuteTime = DateTime.UtcNow - commandStartTime;
                if (arguments.Verbose)
                {
                    DisplayDebugInformation(commandExecuteTime, ConnectionString);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
                return 2;
            }
        }

        /// <summary>
        /// Gets deployment/role instance ids meeting the specified criteria
        /// </summary>
        /// <param name="deploymentIdFilter">Deployment ID/prefix (optional)</param>
        /// <param name="instanceIdFilter">Instance ID (optional)</param>
        /// <param name="propertyName">Property name (optional)</param>
        /// <param name="recursive">Whether role instances should be included</param>
        /// <returns>List of deployment/role instance ids</returns>
        private static string[] GetDeploymentIds(string deploymentIdFilter, string instanceIdFilter, string propertyName, bool recursive)
        {
            return DeploymentProperties.Dictionary.Keys
                .OrderBy(id => id)
                .Where(id => recursive == id.Contains(DeploymentInstanceSeparator))
                .Where(id => string.IsNullOrWhiteSpace(deploymentIdFilter) ? true : id.StartsWith(deploymentIdFilter, StringComparison.OrdinalIgnoreCase))
                .Where(id => string.IsNullOrWhiteSpace(instanceIdFilter) ? true : id.Contains(instanceIdFilter))
                .Where(id => string.IsNullOrWhiteSpace(propertyName) ? true : DeploymentProperties.Dictionary[id].ContainsKey(propertyName))
                .ToArray();
        }

        /// <summary>Displays debug information</summary>
        /// <param name="executionTime">How long the command took to execute</param>
        /// <param name="connectionString">The connection string used</param>
        private static void DisplayDebugInformation(TimeSpan executionTime, string connectionString)
        {
            Console.WriteLine(
                DebugInformationFormat.FormatInvariant(
                    executionTime.TotalMilliseconds,
                    connectionString));
        }

        /// <summary>
        /// Displays an index of deployment/role instance ids
        /// </summary>
        /// <param name="deploymentIds">Pre-filtered Deployment IDs</param>
        /// <param name="verbose">Whether to display verbose information</param>
        private static void Index(string[] deploymentIds, bool verbose)
        {
            Console.WriteLine(
                verbose ?
                VerboseIndexFormat.FormatInvariant(
                    string.Join("\n", deploymentIds),
                    deploymentIds.Length,
                    deploymentIds.Where(id => id.Contains(DeploymentInstanceSeparator)).Count(),
                    deploymentIds.Where(id => !id.Contains(DeploymentInstanceSeparator)).Count()) :
                string.Join("\n", deploymentIds));
        }

        /// <summary>
        /// Displays a list of properties in the deployments/role instances
        /// </summary>
        /// <param name="deploymentIds">Pre-filtered Deployment IDs</param>
        /// <param name="verbose">Whether to display verbose information</param>
        private static void List(string[] deploymentIds, bool verbose)
        {
            var propertyNames = DeploymentProperties.Dictionary
                .Where(deployment => deploymentIds.Contains(deployment.Key))
                .SelectMany(properties => properties.Value.Keys)
                .Distinct();
            
            Console.WriteLine(
                verbose ?
                VerboseListFormat.FormatInvariant(
                    string.Join("\n", propertyNames),
                    propertyNames.Count(),
                    deploymentIds.Where(id => id.Contains(DeploymentInstanceSeparator)).Count(),
                    deploymentIds.Where(id => !id.Contains(DeploymentInstanceSeparator)).Count()) :
                string.Join("\n", propertyNames));
        }

        /// <summary>
        /// Displays a get of properties in the deployments/role instances
        /// </summary>
        /// <param name="deploymentIds">Pre-filtered Deployment IDs</param>
        /// <param name="name">Property name</param>
        /// <param name="value">Property value</param>
        /// <param name="verbose">Whether to display verbose information</param>
        private static void Get(string[] deploymentIds, string name, string value, bool verbose)
        {
            var properties = DeploymentProperties.Dictionary
                .Where(deployment => deploymentIds.Contains(deployment.Key))
                .Where(deployment => string.IsNullOrWhiteSpace(name) ? true : deployment.Value.ContainsKey(name))
                .Where(deployment => string.IsNullOrWhiteSpace(value) ? true : deployment.Value[name] == value)
                .Select(deployment =>
                    "{0}[{1}]={2}".FormatInvariant(
                        deployment.Key,
                        name,
                        deployment.Value[name]));
            
            Console.WriteLine(
                verbose ?
                VerboseGetFormat.FormatInvariant(
                    string.Join("\n", properties),
                    properties.Count(),
                    deploymentIds.Where(id => id.Contains(DeploymentInstanceSeparator)).Count(),
                    deploymentIds.Where(id => !id.Contains(DeploymentInstanceSeparator)).Count()) :
                string.Join("\n", properties));
        }

        /// <summary>
        /// Sets a property on a deployment/role instance
        /// </summary>
        /// <param name="deploymentId">Deployment ID</param>
        /// <param name="instanceId">Role Instance ID</param>
        /// <param name="name">Property name</param>
        /// <param name="value">Property value</param>
        private static void Set(string deploymentId, string instanceId, string name, string value)
        {
            var id =
                string.IsNullOrWhiteSpace(instanceId) ?
                deploymentId :
                "{0}{1}{2}".FormatInvariant(
                    deploymentId,
                    DeploymentInstanceSeparator,
                    instanceId);

            if (!DeploymentProperties.Dictionary.ContainsKey(id))
            {
                try
                {
                    DeploymentProperties.Dictionary.Add(id, new Dictionary<string, string>());
                }
                catch (ArgumentException)
                {
                    // Key conflict means it was just added by something else
                }
            }

            if (!DeploymentProperties.Dictionary.TryUpdateValue(
                id,
                deployment => deployment[name] = value))
            {
                throw new InvalidOperationException("Unable to update value");
            }
        }

        /// <summary>
        /// Sets a property on a deployment/role instance
        /// </summary>
        /// <param name="deploymentIds">Pre-filtered Deployment IDs</param>
        /// <param name="name">Property name</param>
        /// <param name="verbose">Whether to display verbose information</param>
        private static void Remove(string[] deploymentIds, string name, bool verbose)
        {
            var removed = 0;

            foreach (var id in deploymentIds)
            {
                if (!DeploymentProperties.Dictionary.TryUpdateValue(
                    id,
                    deployment =>
                        {
                            if (deployment.Remove(name))
                            {
                                removed++;
                            }
                        }))
                {
                    throw new InvalidOperationException("Unable to update value");
                }
            }

            if (verbose)
            {
                Console.WriteLine(VerboseRemoveFormat.FormatInvariant(name, removed));
            }
        }
    }
}
