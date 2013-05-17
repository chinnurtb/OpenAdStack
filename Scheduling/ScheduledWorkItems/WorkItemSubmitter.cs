//-----------------------------------------------------------------------
// <copyright file="WorkItemSubmitter.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConfigManager;
using Diagnostics;
using Microsoft.Practices.Unity;
using Queuing;
using Utilities.Runtime;
using Utilities.Storage;
using WorkItems;

namespace ScheduledWorkItems
{
    /// <summary>Submits scheduled work items to the queue</summary>
    public class WorkItemSubmitter : IRunner
    {
        /// <summary>Providers of work items to be scheduled</summary>
        private readonly IDictionary<string, IScheduledWorkItemSource> WorkItemSources;

        /// <summary>Queuer used to get processed work items</summary>
        private IQueuer queuer;

        /// <summary>Initializes a new instance of the WorkItemSubmitter class.</summary>
        /// <remarks>Loads scheduled work item sources from the providers</remarks>
        /// <param name="providers">Providers of work item sources</param>
        /// <param name="queuer">Queuer used to get processed work items</param>
        public WorkItemSubmitter(IScheduledWorkItemSourceProvider[] providers, IQueuer queuer)
        {
            this.WorkItemSources = GetScheduledWorkItemSources(providers);
            this.queuer = queuer;
        }

        /// <summary>
        /// Gets the interval at which to update
        /// </summary>
        private static TimeSpan UpdateInterval
        {
            get { return Config.GetTimeSpanValue("Scheduler.UpdateInterval"); }
        }

        /// <summary>Gets scheduled items from providers, enqueues them when scheduled and checks the status of those previously enqueued.</summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Thread proc requires global exception handler to protect worker role.")]
        public void Run()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        this.CreateNewWorkItems();
                    }
                    catch (Exception e)
                    {
                        LogManager.Log(LogLevels.Error, "Unhandled exception while creating work item(s): {0}", e);
                        throw;
                    }

                    try
                    {
                        this.HandleProcessedWorkItems();
                    }
                    catch (Exception e)
                    {
                        LogManager.Log(LogLevels.Error, "Unhandled exception while processing work item result(s): {0}", e);
                        throw;
                    }

                    if (DeploymentProperties.DeploymentState == DeploymentState.Landing)
                    {
                        LogManager.Log(
                            LogLevels.Information,
                            "Deployment {0} landing. WorkItemSubmitter in thread {1} exiting.",
                            DeploymentProperties.DeploymentId,
                            Thread.CurrentThread.Name);
                        break;
                    }

                    Thread.Sleep(UpdateInterval);
                }
            }
            catch (Exception e)
            {
                LogManager.Log(LogLevels.Error, "Scheduled WorkItemSubmitter exiting due to unhandled exception: {0}", e);
                return;
            }
        }

        /// <summary>Let the sources create new work items</summary>
        internal void CreateNewWorkItems()
        {
            foreach (var source in this.WorkItemSources.Values)
            {
                source.CreateNewWorkItems();
            }
        }

        /// <summary>Let the sources check their submitted work items</summary>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "OnWorkItemProcessed handers need to be isolated")]
        internal void HandleProcessedWorkItems()
        {
            var processedWorkItems = this.queuer.DequeueProcessedWorkItems(WorkItemResultType.Shared, null, 10);
            foreach (var workItem in processedWorkItems)
            {
                try
                {
                    if (!this.WorkItemSources.ContainsKey(workItem.Source))
                    {
                        LogManager.Log(
                            LogLevels.Error,
                            "Unknown scheduler source '{0}' for work item '{1}'",
                            workItem.Source,
                            workItem.Id);
                    }
                    else
                    {
                        var source = this.WorkItemSources[workItem.Source];
                        source.OnWorkItemProcessed(workItem);
                    }
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "An error occurred handling processed work item '{0}' from scheduler '{1}':\n{2}",
                        workItem.Id,
                        workItem.Source,
                        e);
                }
                finally
                {
                    try
                    {
                        this.queuer.RemoveFromQueue(workItem);
                    }
                    catch (Exception e)
                    {
                        LogManager.Log(
                            LogLevels.Error,
                            "Unable to remove processed work item '{0}' (source: {1}):\n{2}",
                            workItem.Id,
                            workItem.Source,
                            e);
                    }
                }
            }
        }
        
        /// <summary>Gets the scheduled work item sources from the providers</summary>
        /// <param name="providers">Providers to get work item sources from</param>
        /// <returns>The scheduled work item sources</returns>
        private static IDictionary<string, IScheduledWorkItemSource> GetScheduledWorkItemSources(IScheduledWorkItemSourceProvider[] providers)
        {
            // Create the scheduled work item sources
            LogManager.Log(
                LogLevels.Information,
                "Creating scheduled work item sources from {0} providers: {1}",
                providers.Length,
                string.Join(", ", providers.Select(p => p.GetType().FullName)));

            var providedWorkItemSources = new List<IScheduledWorkItemSource>(providers.SelectMany(p => p.CreateScheduledWorkItemSources()));
            var workItemSources = new Dictionary<string, IScheduledWorkItemSource>(providedWorkItemSources.Count);
            foreach (var workItemSource in providedWorkItemSources)
            {
                if (workItemSources.ContainsKey(workItemSource.Name))
                {
                    var message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Duplicate work item source. A work item source with the name '{0}' already exists.",
                        workItemSource.Name);
                    throw new InvalidOperationException(message);
                }

                workItemSources.Add(workItemSource.Name, workItemSource);
            }

            var providerNames = workItemSources.Values
                .Select(s => string.Format(CultureInfo.InvariantCulture, "{0} - \"{1}\"", s.GetType().FullName, s.Name));
            LogManager.Log(
                LogLevels.Information,
                "Created scheduled work item sources from {0} providers:\n{1}",
                providers.Length,
                string.Join("\n\t", providerNames));

            return workItemSources;
        }
    }
}
