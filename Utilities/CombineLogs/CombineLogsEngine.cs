// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CombineLogsEngine.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Utilities.CombineLogs;

namespace Utilities.CombineLogs
{
    /// <summary>
    /// The engine for CombineLogs
    /// </summary>
    public static class CombineLogsEngine
    {
        /// <summary>
        /// Combines and time interleaves several log blobs
        /// </summary>
        /// <param name="arguments">the arguments</param>
        /// <returns>success indicator</returns>
        internal static XDocument CombineLogs(CombineLogsArgs arguments)
        {
            // set up storage access
            var storageAccount = CloudStorageAccount.Parse(arguments.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Specify a retry backoff of 10 seconds max instead of using default values. 
            blobClient.RetryPolicy = RetryPolicies.RetryExponential(
                3, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 3));

            var startDate = arguments.StartDate.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
            var endDate = arguments.EndDate.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);

            // Get an Enumerable of the relevant blobs as text
            // TODO: do this using streams instead
            var options = new BlobRequestOptions { UseFlatBlobListing = true };
            var logTexts = arguments.LogContainersList
                .Select(containerName => blobClient.GetContainerReference(containerName))
                .SelectMany(container => container.ListBlobs(options))
                .OfType<CloudBlob>()
                .Where(blob => 
                    string.CompareOrdinal(blob.Name.Split('/').Last().Split('.').First(), startDate) >= 0 && 
                    string.CompareOrdinal(blob.Name.Split('/').Last().Split('.').First(), endDate) <= 0)
                .Select(blob => blob.DownloadText());

            // TODO: parse the fragment as xml and combine properly
            var logMessages = new StringBuilder("<msgs>");
            foreach (var text in logTexts)
            {
                logMessages.Append(text);
            }

            logMessages.Append("</msgs>");

            // parse as xml and sort the elements by time
            var combinedLog = XDocument.Parse(logMessages.ToString());
            var elements = combinedLog.Root.Elements().OrderBy(msg => DateTime.Parse(msg.Attribute("time").Value, CultureInfo.InvariantCulture)).ToArray();
            combinedLog.Root.ReplaceAll(elements);

            return combinedLog;
        }
    }
}
