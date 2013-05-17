// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteLogsEngine.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Utilities.DeleteLogs
{
    /// <summary>
    /// class for the engine of the DeleteLogs app
    /// </summary>
    public static class DeleteLogsEngine
    {
        /// <summary>
        /// Deletes logs older than hours specified in arguments
        /// </summary>
        /// <param name="arguments">The arguments</param>
        /// <returns>result indicator (0 for success)</returns>
        public static int DeleteLogs(DeleteLogsArgs arguments)
        {
            // set up storage access
            var storageAccount = CloudStorageAccount.Parse(arguments.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Specify a retry backoff of 10 seconds max instead of using default values. 
            blobClient.RetryPolicy = RetryPolicies.RetryExponential(
                3, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 3));

            // get the containers specified
            var containers = new List<CloudBlobContainer>();
            foreach (var name in arguments.LogContainersList)
            {
                containers.Add(blobClient.GetContainerReference(name));
            }

            // delete old logs
            var deleteDate = DateTime.Now.AddHours(-1 * arguments.HoursAgoThresholdForDeleting)
                .ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
            var options = new BlobRequestOptions();
            options.UseFlatBlobListing = true;
            foreach (var container in containers)
            {
                foreach (var item in container.ListBlobs(options))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        var blob = (CloudBlockBlob)item;
                        var blobDate = blob.Name.Split('/').Last().Split('.').First();

                        if (string.Compare(blobDate, deleteDate, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            blob.Delete();
                        }
                    }
                }
            }

            return 0;
        }
    }
}
