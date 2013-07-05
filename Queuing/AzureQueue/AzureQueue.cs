//-----------------------------------------------------------------------
// <copyright file="AzureQueue.cs" company="Rare Crowds Inc">
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
using System.Runtime.Serialization;
using ConfigManager;
using Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Utilities.Storage;
using WorkItems;

namespace Queuing.Azure
{
    /// <summary>Azure queue wrapper using WorkItemQueueEntry</summary>
    [SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
    public class AzureQueue : AzureQueueBase
    {
        /// <summary>Name of the store to which message receipts are persisted</summary>
        private const string MessageReceiptStoreName = "messagereceipts";

        /// <summary>Maximum visibility timeout (4 hours)</summary>
        private static readonly TimeSpan MaxVisibilityTimeout = new TimeSpan(4, 0, 0);

        /// <summary>Default visiblity timeout (maximum)</summary>
        private static readonly TimeSpan DefaultVisibilityTimeout = MaxVisibilityTimeout;

        /// <summary>Dictionary of queue entries and their corresponding messages</summary>
        private readonly IDictionary<string, MessageReceipt> EntryMessageReceipts = new Dictionary<string, MessageReceipt>();

        /// <summary>Visibility timeout for when getting messages</summary>
        private readonly TimeSpan VisibilityTimeout;

        /// <summary>Initializes a new instance of the AzureQueue class.</summary>
        /// <param name="storageAccount">Storage account used for the queue</param>
        /// <param name="queueAddress">Address used for getting the queue reference</param>
        [SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
        public AzureQueue(CloudStorageAccount storageAccount, string queueAddress)
            : base(storageAccount, queueAddress)
        {
            try
            {
                this.VisibilityTimeout = Config.GetTimeSpanValue("Queue.VisibilityTimeout");
                if (this.VisibilityTimeout > MaxVisibilityTimeout)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Invalid 'Queue.VisibilityTimeout' value: {0}. Maximum timeout is {1}.",
                        this.VisibilityTimeout,
                        MaxVisibilityTimeout);
                    this.VisibilityTimeout = MaxVisibilityTimeout;
                }
            }
            catch (ArgumentException)
            {
                this.VisibilityTimeout = DefaultVisibilityTimeout;
            }
        }

        /// <summary>Initializes a new instance of the AzureQueue class.</summary>
        /// <param name="storageAccount">Storage account used for the queue</param>
        /// <param name="queueAddress">Address used for getting the queue reference</param>
        /// <param name="visibilityTimeout">Queue message visibility timeout</param>
        [SuppressMessage("Microsoft.Naming", "CA1711", Justification = "This is a queue even though it doesn't inherit from System.Collections.Queue")]
        public AzureQueue(CloudStorageAccount storageAccount, string queueAddress, TimeSpan visibilityTimeout)
            : base(storageAccount, queueAddress)
        {
            this.VisibilityTimeout = visibilityTimeout;
        }

        /// <summary>Put an entry on the queue</summary>
        /// <param name="entry">Entry to enqueue</param>
        /// <exception cref="QueueException">Thrown if the entry cannot be queued</exception>
        public void Enqueue(WorkItemQueueEntry entry)
        {
            try
            {
                var message = new CloudQueueMessage(entry.AsBytes);
                this.TryCloudQueueAction(q => q.AddMessage(message));
            }
            catch (Exception e)
            {
                throw new QueueException(
                    "Error enqueuing work item entry {0}".FormatInvariant(entry.WorkItemId), e);
            }
        }

        /// <summary>
        /// Get up to the specified number of entries from the queue
        /// </summary>
        /// <param name="maxEntries">Maximum number of entries to get</param>
        /// <returns>Array containing the dequeued entries (if any)</returns>
        public WorkItemQueueEntry[] Dequeue(int maxEntries)
        {
            var entries = new List<WorkItemQueueEntry>();
            this.TryCloudQueueAction(q =>
                {
                    foreach (var message in q.GetMessages(maxEntries, this.VisibilityTimeout))
                    {
                        var entry = WorkItemQueueEntry.FromBytes(message.AsBytes);
                        if (entry != null)
                        {
                            var receipt = new MessageReceipt
                            {
                                MessageId = message.Id,
                                PopReceipt = message.PopReceipt
                            };
                            this.EntryMessageReceipts[entry.WorkItemId] = receipt;
                            entries.Add(entry);
                        }
                    }
                });
            return entries.ToArray();
        }

        /// <summary>
        /// Delete an entry from the queue so that it cannot be dequeued again
        /// </summary>
        /// <param name="entry">entry to be deleted</param>
        public void Delete(WorkItemQueueEntry entry)
        {
            if (!this.EntryMessageReceipts.ContainsKey(entry.WorkItemId))
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Unable to delete queue entry '{0}' (category '{1}'): No corresponding message receipt found.",
                    entry.WorkItemId,
                    entry.Category);
                return;
            }

            var receipt = this.EntryMessageReceipts[entry.WorkItemId];
            this.TryCloudQueueAction(q => q.DeleteMessage(receipt.MessageId, receipt.PopReceipt));
            this.EntryMessageReceipts.Remove(entry.WorkItemId);
        }

        /// <summary>
        /// Struct for keeping track of message id and pop receipt pairs
        /// </summary>
        [DataContract]
        private struct MessageReceipt
        {
            /// <summary>Gets or sets the id of the message</summary>
            [DataMember]
            public string MessageId { get; set; }

            /// <summary>Gets or sets the pop receipt for the message</summary>
            [DataMember]
            public string PopReceipt { get; set; }
        }
    }
}
