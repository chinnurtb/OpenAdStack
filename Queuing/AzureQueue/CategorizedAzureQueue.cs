//-----------------------------------------------------------------------
// <copyright file="CategorizedAzureQueue.cs" company="Rare Crowds Inc">
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
using ConfigManager;
using Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Utilities.Runtime;
using WorkItems;

namespace Queuing.Azure
{
    /// <summary>
    /// Implementation of ICategorizedQueue using AzureQueues
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
    public class CategorizedAzureQueue : ICategorizedQueue
    {
        /// <summary>Format string for queue addresses</summary>
        /// <remarks>Args will be the queue's deploymentId and category.</remarks>
        private const string QueueAddressFormat = "wiq-{0}-{1}";

        /// <summary>Dictionary containing queues keyed by their category</summary>
        private readonly IDictionary<string, AzureQueue> queues;

        /// <summary>Initializes a new instance of the CategorizedAzureQueue class.</summary>
        public CategorizedAzureQueue()
        {
            this.queues = new Dictionary<string, AzureQueue>();
        }

        /// <summary>Gets the StorageAccount to use for the queues</summary>
        private static CloudStorageAccount StorageAccount
        {
            get { return CloudStorageAccount.Parse(Config.GetValue("Queue.ConnectionString")); }
        }

        /// <summary>Put an entry on the queue</summary>
        /// <param name="entry">Entry to enqueue</param>
        public void Enqueue(WorkItemQueueEntry entry)
        {
            // Always enqueue to the "active" deployment if available
            this.DoQueueAction(
                DeploymentProperties.ActiveDeploymentId ?? DeploymentProperties.DeploymentId,
                entry.Category,
                q => q.Enqueue(entry));
        }

        /// <summary>Get up to the specified number of entries from the queue</summary>
        /// <param name="category">Category to get an entry from</param>
        /// <param name="maxEntries">Maximum number of entries to get</param>
        /// <returns>Array containing the dequeued entries (if any)</returns>
        public WorkItemQueueEntry[] Dequeue(string category, int maxEntries)
        {
            WorkItemQueueEntry[] entries = null;
            this.DoQueueAction(
                DeploymentProperties.DeploymentId,
                category,
                q => entries = q.Dequeue(maxEntries));
            return entries;
        }

        /// <summary>Deletes an entry from the queue so that it may not be dequeued again</summary>
        /// <param name="entry">Entry to delete</param>
        public void Delete(WorkItemQueueEntry entry)
        {
            // Always delete from the current deployment
            this.DoQueueAction(
                DeploymentProperties.DeploymentId,
                entry.Category,
                q => q.Delete(entry));
        }

        /// <summary>Performs an action on the queue for the specified deployment/category</summary>
        /// <remarks>Creates the AzureQueue client instance if it does not already exist</remarks>
        /// <param name="deploymentId">The deployment id</param>
        /// <param name="category">The category</param>
        /// <param name="action">The action to perform</param>
        private void DoQueueAction(string deploymentId, string category, Action<AzureQueue> action)
        {
            var queueAddress = QueueAddressFormat
                .FormatInvariant(category, deploymentId)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
            if (!this.queues.ContainsKey(queueAddress))
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Creating AzureQueue client for queue '{0}'\nDeploymentId: {1} / Active DeploymentId: {2}",
                    queueAddress,
                    DeploymentProperties.DeploymentId,
                    DeploymentProperties.ActiveDeploymentId);
                this.queues[queueAddress] = new AzureQueue(StorageAccount, queueAddress);
            }

            action(this.queues[queueAddress]);
        }
    }
}
