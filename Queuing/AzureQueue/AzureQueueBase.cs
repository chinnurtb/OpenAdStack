//-----------------------------------------------------------------------
// <copyright file="AzureQueueBase.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Queuing.Azure
{
    /// <summary>Base class for AzureQueue. Manages the actual CloudQueue instance.</summary>
    public abstract class AzureQueueBase
    {
        /// <summary>How many times to retry cloud queue actions</summary>
        private const int CloudQueueRetries = 3;

        /// <summary>How long (in milliseconds) to wait between retries</summary>
        private const int CloudQueueRetryWait = 100;

        /// <summary>The Azure cloud queue</summary>
        private readonly CloudQueue queue;

        /// <summary>Initializes a new instance of the AzureQueueBase class.</summary>
        /// <param name="storageAccount">Storage account used for the queue</param>
        /// <param name="queueAddress">Address used for getting the queue reference</param>
        [SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
        protected AzureQueueBase(CloudStorageAccount storageAccount, string queueAddress)
        {
            var queueClient = storageAccount.CreateCloudQueueClient();
            this.queue = queueClient.GetQueueReference(queueAddress);
            this.queue.CreateIfNotExist();
        }

        /// <summary>Perform an action using the Azure cloud queue</summary>
        /// <remarks>Automatically attempts to recreate missing queues and retries the action</remarks>
        /// <param name="action">Action to try using the cloud queue</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is logged (w/ alert on persistent failures)")]
        protected void TryCloudQueueAction(Action<CloudQueue> action)
        {
            var retries = CloudQueueRetries;
            while (true)
            {
                try
                {
                    try
                    {
                        action(this.queue);
                        break;
                    }
                    catch (StorageClientException sce)
                    {
                        if (sce.StatusCode == HttpStatusCode.NotFound)
                        {
                            // Automatically attempt to recreate missing queues
                            LogManager.Log(
                                LogLevels.Warning,
                                true,
                                "The queue '{0}' does not exist. Will recreate missing queue and retry.\n{1}",
                                this.queue.Name,
                                sce);
                            this.queue.CreateIfNotExist();
                        }
                    }
                }
                catch (Exception e)
                {
                    retries--;
                    LogManager.Log(
                        retries > 0 ? LogLevels.Warning : LogLevels.Error,
                        retries > 0,
                        "Error dequeuing work items (attempt {0} of {1}): {2}",
                        CloudQueueRetries - retries,
                        CloudQueueRetries,
                        e);
                    if (retries > 0)
                    {
                        Thread.Sleep(CloudQueueRetryWait);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
