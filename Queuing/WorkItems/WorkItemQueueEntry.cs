//-----------------------------------------------------------------------
// <copyright file="WorkItemQueueEntry.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;

namespace WorkItems
{
    /// <summary>
    /// Defines an interface for entries corresponding to work items that get put on a queue
    /// </summary>
    [DataContract]
    public class WorkItemQueueEntry
    {
        /// <summary>Serializer for WorkItemQueueEntry</summary>
        private static readonly DataContractSerializer Serializer =
            new DataContractSerializer(typeof(WorkItemQueueEntry));

        /// <summary>Gets the identifier of the work item for this queue entry</summary>
        [DataMember]
        public string WorkItemId { get; internal set; }

        /// <summary>Gets the category of the work item</summary>
        [DataMember]
        public string Category { get; internal set; }

        /// <summary>Gets the work item queue entry serialized to bytes</summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Returning a byte array is okay.")]
        public byte[] AsBytes
        {
            get
            {
                using (var buffer = new MemoryStream())
                {
                    Serializer.WriteObject(buffer, this);
                    var bytes = new byte[buffer.Length];
                    Array.Copy(buffer.GetBuffer(), bytes, buffer.Length);
                    return bytes;
                }
            }
        }

        /// <summary>Deserializes a work item queue entry from bytes</summary>
        /// <param name="bytes">The bytes to deserialize</param>
        /// <returns>The work item queue entry</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Bytes is bytes.")]
        public static WorkItemQueueEntry FromBytes(byte[] bytes)
        {
            using (var buffer = new MemoryStream(bytes))
            {
                return (WorkItemQueueEntry)Serializer.ReadObject(buffer);
            }
        }
    }
}
