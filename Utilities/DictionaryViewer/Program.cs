// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using AzureUtilities.Storage;
using ConsoleAppUtilities;
using SqlUtilities.Storage;
using Utilities.Storage;

namespace Utilities.DictionaryViewer
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Views and downloads entries from persistent dictionaries.
Usage: DView.exe {{-s ""Dictionary Store Name""}} {{-c Command}} [optional arguments]
{0}";
        
        /// <summary>Format for verbose index</summary>
        private const string VerboseIndexFormat =
@"Stores:
{0}
({1} entries)";

        /// <summary>Format for verbose list</summary>
        private const string VerboseListFormat =
@"Contents of ""{0}"":
{1}
({2} entries)";

        /// <summary>Format for verbose view</summary>
        private const string VerboseViewFormat =
@"Dictionary: {0}
Key: {1}
Size: {2} bytes
Content:
--------------------------------------------------------------------------------
{3}";

        /// <summary>Format for verbose remove</summary>
        private const string VerboseRemoveFormat = @"Removed '{0}' from '{1}'";

        /// <summary>Format for verbose delete</summary>
        private const string VerboseDeleteFormat = @"Deleting store '{0}'";

        /// <summary>Format for debug information</summary>
        private const string DebugInformationFormat =
@"Execution time: {0}ms
Connection string: {1}";

        /// <summary>Default dictionary type</summary>
        private const PersistentDictionaryType DefaultDictionaryType = PersistentDictionaryType.Sql;

        /// <summary>The connection strings</summary>
        private static readonly IDictionary<PersistentDictionaryType, string> ConnectionStrings =
            new Dictionary<PersistentDictionaryType, string>
            {
                { PersistentDictionaryType.Cloud, ConfigurationManager.AppSettings["Dictionary.Cloud.ConnectionString"] },
                { PersistentDictionaryType.Sql, ConfigurationManager.AppSettings["Dictionary.Sql.ConnectionString"] }
            };

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<DictionaryViewerArgs>());
            }
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
                var arguments = CommandLineArguments.Create<DictionaryViewerArgs>(args);

                var dictionaryType = arguments.DictionaryType != PersistentDictionaryType.Unknown ?
                    arguments.DictionaryType :
                    DefaultDictionaryType;
                var connectionString = arguments.ConnectionString ?? ConnectionStrings[dictionaryType];
                var factory =
                    dictionaryType == PersistentDictionaryType.Cloud ?
                    (IPersistentDictionaryFactory)new CloudBlobDictionaryFactory(connectionString) :
                    (IPersistentDictionaryFactory)new SqlDictionaryFactory(connectionString);
                
                PersistentDictionaryFactory.Initialize(new[] { factory });

                var dictionary =
                    arguments.Command != ViewerCommand.Index ?
                    PersistentDictionaryFactory.CreateDictionary<byte[]>(arguments.StoreName, dictionaryType, true) :
                    null;

                var commandStartTime = DateTime.UtcNow;
                switch (arguments.Command)
                {
                    case ViewerCommand.Index:
                        Index(dictionaryType, !arguments.Quiet);
                        break;
                    case ViewerCommand.List:
                        List(dictionary, !arguments.Quiet);
                        break;
                    case ViewerCommand.View:
                        View(dictionary, arguments.Key, !arguments.Quiet);
                        break;
                    case ViewerCommand.Get:
                        Get(dictionary, arguments.Key, arguments.OutFile);
                        break;
                    case ViewerCommand.Set:
                        Set(dictionary, arguments.Key, arguments.InFile);
                        break;
                    case ViewerCommand.Remove:
                        Remove(dictionary, arguments.Key, !arguments.Quiet);
                        break;
                    case ViewerCommand.Delete:
                        Delete(dictionary, !arguments.Quiet);
                        break;
                }

                var commandExecuteTime = DateTime.UtcNow - commandStartTime;
                if (arguments.Debug)
                {
                    DisplayDebugInformation(commandExecuteTime, connectionString);
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

        /// <summary>Displays an index of all dictionaries</summary>
        /// <param name="dictionaryType">Type of dictionaries to index</param>
        /// <param name="verbose">Whether to display additional information</param>
        private static void Index(PersistentDictionaryType dictionaryType, bool verbose)
        {
            var storeNames = PersistentDictionaryFactory.GetStoreIndex(dictionaryType);
            var stores = string.Join("\n", storeNames);
            if (!verbose)
            {
                Console.WriteLine(stores);
                return;
            }

            Console.WriteLine(
                VerboseIndexFormat.FormatInvariant(
                stores, storeNames.Length));
        }

        /// <summary>Display the keys of the dictionary</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="verbose">Whether to display additional information</param>
        private static void List(IPersistentDictionary<byte[]> dictionary, bool verbose)
        {
            var entries = string.Join("\n", dictionary.Keys);
            if (!verbose)
            {
                Console.WriteLine(entries);
                return;
            }

            Console.WriteLine(
                VerboseListFormat.FormatInvariant(
                dictionary.StoreName,
                entries,
                dictionary.Count));
        }

        /// <summary>Display the contents of an entry in a dictionary</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="key">The entry key</param>
        /// <param name="verbose">Whether to display additional information</param>
        private static void View(IPersistentDictionary<byte[]> dictionary, string key, bool verbose)
        {
            var bytes = dictionary[key];
            string content;
            using (var reader = new StreamReader(new MemoryStream(bytes)))
            {
                content = reader.ReadToEnd();
            }

            if (!verbose)
            {
                Console.WriteLine(content);
                return;
            }

            Console.WriteLine(
                VerboseViewFormat.FormatInvariant(
                    dictionary.StoreName,
                    key,
                    bytes.Length,
                    content));
        }

        /// <summary>Downloads the contents of an entry to a file</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="key">The entry key</param>
        /// <param name="file">The file</param>
        private static void Get(IPersistentDictionary<byte[]> dictionary, string key, FileInfo file)
        {
            var bytes = dictionary[key];
            using (var stream = file.OpenWrite())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>Uploads the contents of an entry from a file</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="key">The entry key</param>
        /// <param name="file">The file</param>
        private static void Set(IPersistentDictionary<byte[]> dictionary, string key, FileInfo file)
        {
            if (!file.Exists)
            {
                throw new ArgumentException(
                    "The file '{0}' cannot be found."
                    .FormatInvariant(file.FullName),
                    "file");
            }

            var bytes = new byte[file.Length];
            using (var stream = file.OpenRead())
            {
                stream.Read(bytes, 0, bytes.Length);
            }

            dictionary[key] = bytes;
        }

        /// <summary>Deletes an entry from a dictionary</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="key">The entry key</param>
        /// <param name="verbose">Whether to display additional information</param>
        private static void Remove(IPersistentDictionary<byte[]> dictionary, string key, bool verbose)
        {
            if (dictionary.Remove(key) && verbose)
            {
                Console.WriteLine(VerboseRemoveFormat.FormatInvariant(dictionary.StoreName, key));
            }
        }

        /// <summary>Deletes a dictionary</summary>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="verbose">Whether to display additional information</param>
        private static void Delete(IPersistentDictionary<byte[]> dictionary, bool verbose)
        {
            if (verbose)
            {
                Console.WriteLine(VerboseDeleteFormat.FormatInvariant(dictionary.StoreName));
            }

            dictionary.Delete();
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
    }
}
