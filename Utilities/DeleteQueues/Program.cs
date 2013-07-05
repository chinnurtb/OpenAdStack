// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ConsoleAppUtilities;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Utilities.DeleteQueues
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Deletes Azure Queues
Usage: DelQueues.exe [-cs ""ConnectionString""] [-x ""ExcludeList""] [-i ""IncludeList""] [additional optional arguments]
{0}";

        /// <summary>Format for verbose deleted results</summary>
        private const string VerboseDeletedResultsFormat = @"
Deleted {0} queues from account ""{1}""
{2}";

        /// <summary>Format for verbose preview results</summary>
        private const string VerbosePreviewResultsFormat =
@"Would have deleted {0} queues from account ""{1}""
{2}";

        /// <summary>Parsed command-line arguments</summary>
        private static DeleteQueuesArgs arguments;

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<DeleteQueuesArgs>());
            }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(Usage);
                return 0;
            }

            try
            {
                arguments = CommandLineArguments.Create<DeleteQueuesArgs>(args);

                // Get an Azure queue client
                var connectionString =
                    string.IsNullOrWhiteSpace(arguments.ConnectionString) ?
                    ConfigurationManager.AppSettings["ConnectionString"] :
                    arguments.ConnectionString;
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var queueClient = storageAccount.CreateCloudQueueClient();

                // Get the list of queues to delete
                var queues =
                    (arguments.IncludeList.Length > 0 ?
                        arguments.IncludeList.SelectMany(prefix => queueClient.ListQueues(prefix)) :
                        queueClient.ListQueues())
                    .Where(queue =>
                        !arguments.ExcludeList.Any(substring => queue.Name.Contains(substring)));

                if (!arguments.Quiet && !arguments.Preview)
                {
                    Console.WriteLine(@"Deleting queues from ""{0}""...", connectionString);
                }

                // Delete (if not in preview mode) the queues and add them to the list
                var deletedQueueNames = new List<string>();
                foreach (var queue in queues)
                {
                    if (!arguments.Quiet && !arguments.Preview)
                    {
                        Console.WriteLine(@"Deleting queue ""{0}""...", queue.Name);
                        if (!arguments.Force && queue.PeekMessage() != null)
                        {
                            Console.Error.WriteLine(@"Not deleting queue ""{0}"". The queue is not empty", queue.Name);
                        }
                        else
                        {
                            queue.Delete();
                        }
                    }

                    deletedQueueNames.Add(queue.Name);
                }

                // Output the queues that were/would have been deleted
                if (arguments.Quiet)
                {
                    Console.WriteLine(string.Join("\n", deletedQueueNames));
                }
                else
                {
                    Console.WriteLine(
                        arguments.Preview ? VerbosePreviewResultsFormat : VerboseDeletedResultsFormat,
                        deletedQueueNames.Count,
                        connectionString,
                        string.Join("\n", deletedQueueNames));
                }

                return 0;
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae);
                Console.WriteLine();
                Console.WriteLine(Usage);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
                return 2;
            }
        }
    }
}
