//-----------------------------------------------------------------------
// <copyright file="AzureWorkerRole.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.ServiceRuntime;
using Queuing;
using RuntimeIoc.WorkerRole;
using ScheduledWorkItems;
using Utilities.Runtime;
using Utilities.Storage;

namespace WorkerRole
{
    /// <summary>Host for all workers</summary>
    public class AzureWorkerRole : RoleEntryPoint
    {
        /// <summary>Offset between each worker thread start</summary>
        private const int DefaultThreadStartOffsetSeconds = 5;

        /// <summary>
        /// States to monitor the threads for which require the
        /// role be restarted (unless the deployment is landing)
        /// </summary>
        private static readonly ThreadState[] RestartThreadStates = new[] { ThreadState.Aborted, ThreadState.Stopped };
        
        /// <summary>The Runners</summary>
        private IEnumerable<IRunner> runners;

        /// <summary>The Threads</summary>
        private Thread[] threads;

        /// <summary>Gets the offset between each worker thread start</summary>
        private static TimeSpan ThreadStartOffset
        {
            get
            {
                try
                {
                    return Config.GetTimeSpanValue("WorkerRole.ThreadStartOffset");
                }
                catch (ArgumentException)
                {
                    return new TimeSpan(0, 0, DefaultThreadStartOffsetSeconds);
                }
            }
        }

        /// <summary>Gets the interval at which to check if the threads are still running.</summary>
        private static TimeSpan ThreadCheckInterval
        {
            get { return Config.GetTimeSpanValue("WorkerRole.ThreadCheckInterval"); }
        }

        /// <summary>Gets a value indicating whether all threads have stopped</summary>
        private bool AllThreadsStopped
        {
            get
            {
                // Check if the runners have finished running yet
                if (this.threads.All(t => t.ThreadState == ThreadState.Stopped))
                {
                    LogManager.Log(
                        LogLevels.Information,
                        "Deployment {0} landing. All worker role {1} runners have stopped. Role instance has landed.",
                        DeploymentProperties.DeploymentId,
                        DeploymentProperties.RoleInstanceId);
                    DeploymentProperties.RoleInstanceState = RoleInstanceState.Landed;
                    return true;
                }

                var runningThreads = this.threads.Where(t => t.ThreadState != ThreadState.Stopped);
                LogManager.Log(
                    LogLevels.Trace,
                    "Deployment {0} landing... (waiting on {1} of {2} threads for role instance {3})\n{4}",
                    DeploymentProperties.DeploymentId,
                    runningThreads.Count(),
                    this.threads.Length,
                    DeploymentProperties.RoleInstanceId,
                    string.Join("\n", runningThreads.Select(t => t.Name)));
                return false;
            }
        }

        /// <summary>Initializes the worker role</summary>
        /// <returns>True if initialization succeeds, False if it fails.</returns>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "This for the Windows Azure SDK.")]
        public override bool OnStart()
        {
            // Subscribe to the unhandled exception event so they can be logged
            AppDomain.CurrentDomain.UnhandledException += this.UnhandledExceptionLogger;

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += delegate(object sender, RoleEnvironmentChangingEventArgs e)
            {
                // If a configuration setting is changing
                if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                {
                    // Set e.Cancel to true to restart this role instance
                    e.Cancel = true;
                }
            };

            // Resolve and initialize the persistent dictionary factory and loggers
            PersistentDictionaryFactory.Initialize(RuntimeIocContainer.Instance.ResolveAll<IPersistentDictionaryFactory>());
            QuotaLogger.InitializeDiagnostics();
            LogManager.Initialize(RuntimeIocContainer.Instance.ResolveAll<ILogger>());
            LogManager.Log(
                LogLevels.Information,
                true,
                "Starting WorkerRole instance {0}",
                DeploymentProperties.RoleInstanceId,
                DeploymentProperties.DeploymentId);

            // Set the role instance state to "launching"
            DeploymentProperties.RoleInstanceState = RoleInstanceState.Launching;

            // Resolve runners
            this.runners = RuntimeIocContainer.Instance.ResolveAll<IRunner>();
            LogManager.Log(LogLevels.Information, "Resolved {0} runners", this.runners.Count());

            // Resolve and initialize delivery network and measure source factories
            DeliveryNetworkClientFactory.Initialize(RuntimeIocContainer.Instance.ResolveAll<IDeliveryNetworkClientFactory>());
            MeasureSourceFactory.Initialize(RuntimeIocContainer.Instance.ResolveAll<IMeasureSourceProvider>());

            return base.OnStart();
        }

        /// <summary>Starts threads for each runner and monitors them</summary>
        public override void Run()
        {
            // Spawn the runner threads
            this.SpawnRunnerThreads();

            // Set the "active" deployment (if needed)
            SetActiveDeployment();

            // Set the role instance state to "running"
            DeploymentProperties.RoleInstanceState = RoleInstanceState.Running;
            LogManager.Log(
                LogLevels.Information,
                true,
                "WorkerRole instance {0} is running",
                DeploymentProperties.RoleInstanceId,
                DeploymentProperties.DeploymentId);

            // Main run loop
            while (true)
            {
                if (DeploymentProperties.DeploymentState == DeploymentState.Landing)
                {
                    // Check if all threads have stopped
                    if (this.AllThreadsStopped)
                    {
                        // Go to sleep awaiting role deletion
                        Thread.Sleep(Timeout.Infinite);
                    }
                }
                else if (this.threads.Any(t => RestartThreadStates.Contains(t.ThreadState)))
                {
                    // One or more threads have stopped running. Exit so that Azure can restart this role.
                    var restartThreads = this.threads
                        .Where(t => RestartThreadStates.Contains(t.ThreadState))
                        .Select(t => t.Name);
                    LogManager.Log(
                        LogLevels.Error,
                        "WorkerRole exiting. The following thread(s) need to be restarted: {0}",
                        string.Join(", ", restartThreads));

                    // Exit run loop
                    return;
                }

                Thread.Sleep(ThreadCheckInterval);
            }
        }

        /// <summary>Cleans up the worker role</summary>
        public override void OnStop()
        {
            LogManager.Log(
                LogLevels.Information,
                true,
                "WorkerRole instance {0} is stopping",
                DeploymentProperties.RoleInstanceId,
                DeploymentProperties.DeploymentId);
            AppDomain.CurrentDomain.UnhandledException -= this.UnhandledExceptionLogger;
            base.OnStop();
        }

        /// <summary>
        /// Sets the DeploymentProperties.ActiveDeploymentId to the current deployment
        /// if either none has been set yet or if running in the emulator.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Always running in Azure context")]
        private static void SetActiveDeployment()
        {
            if (DeploymentProperties.ActiveDeploymentId == null || RoleEnvironment.IsEmulated)
            {
                var reason = RoleEnvironment.IsEmulated ?
                        "Emulator always sets itself active" :
                        "DeploymentProperties.ActiveDeploymentId not set";
                LogManager.Log(
                    LogLevels.Information,
                    @"Setting current deployment as active: '{0}'\n({1})",
                    DeploymentProperties.DeploymentId,
                    reason);
                DeploymentProperties.ActiveDeploymentId = DeploymentProperties.DeploymentId;
            }
        }

        /// <summary>Logs unhandled exceptions using the LogManager (if available)</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void UnhandledExceptionLogger(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.Log(LogLevels.Error, true, "!UNHANDLED EXCEPTION! - {0}", e.ExceptionObject);
        }

        /// <summary>Spawn threads for the runners resolved in OnStart</summary>
        private void SpawnRunnerThreads()
        {
            var runnerThreads = new List<Thread>();
            foreach (var runner in this.runners)
            {
                LogManager.Log(LogLevels.Information, "Spawning {0} runner thread...", runner.GetType().FullName);
                var thread = new Thread(runner.Run)
                {
                    Name = "{0} ({1})".FormatInvariant(runner.GetType().FullName, Guid.NewGuid())
                };
                thread.Start();
                runnerThreads.Add(thread);

                Thread.Sleep(ThreadStartOffset);
                LogManager.Log(ThreadStartOffset.ToString());
            }

            LogManager.Log(
                LogLevels.Information,
                "Spawned {0} runner threads: {1}",
                runnerThreads.Count,
                string.Join(", ", runnerThreads.Select(t => t.Name)));

            this.threads = runnerThreads.ToArray();
        }
    }
}
