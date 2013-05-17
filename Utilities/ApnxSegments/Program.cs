// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds">
//   Copyright Rare Crowds. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using AppNexusClient;
using ConfigManager;
using ConsoleAppUtilities;
using Diagnostics;
using Newtonsoft.Json;
using Utilities;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace Utilities.AppNexusSegments
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Log file</summary>
        private static FileInfo log;

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return
                    "Imports and exports segments from AppNexus accounts.\n" +
                    @"Example: ApnxSegments.exe -api ""http://api.appnexus.com/"" -user ""UserName"" -pass ""p@s5w0rd"" -import -file ""MySegments.json""" +
                    CommandLineArguments.GetDescriptions<AppNexusSegmentsArgs>();
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
                var arguments = CommandLineArguments.Create<AppNexusSegmentsArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    throw new ArgumentException("One or more arguments were invalid");
                }

                if (!string.IsNullOrWhiteSpace(arguments.LogFile))
                {
                    log = new FileInfo(arguments.LogFile);
                }

                ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
                PersistentDictionaryFactory.Initialize(new[] { new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Cloud) });
                LogManager.Initialize(new[] { new TraceLogger() });

                var config = new CustomConfig(
                    new Dictionary<string, string>
                    {
                        { "AppNexus.Endpoint", arguments.ApiEndpoint },
                        { "AppNexus.Username", arguments.UserName },
                        { "AppNexus.Password", arguments.Password },
                    });

                using (var client = new AppNexusApiClient { Config = config })
                {
                    if (arguments.Import)
                    {
                        ImportSegmentsToMember(client, arguments.File);
                    }
                    else
                    {
                        ExportSegmentsFromMember(client, arguments.File, arguments.OutputXml);
                    }
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

        /// <summary>Imports segments from a file to the AppNexus account</summary>
        /// <param name="client">AppNexus API Client</param>
        /// <param name="file">Segments input file</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Log all exceptions")]
        private static void ImportSegmentsToMember(IAppNexusApiClient client, string file)
        {
            if (!new FileInfo(file).Exists)
            {
                throw new ArgumentException("The file '{0}' does not exist".FormatInvariant(file), "file");
            }

            var memberId = client.GetMember()["id"];
            var createSegmentUri = "segment?advertiser_id=null".FormatInvariant(memberId);

            var segmentsJson = File.ReadAllText(file);
            var segments = JsonConvert.DeserializeObject<IDictionary<string, object>[]>(segmentsJson);

            var totalSegments = segments.Count();
            var imported = 0;
            var oldId = 0L;
            Log("Importing {0} segments from '{0}'...", totalSegments);

            foreach (var segment in segments)
            {
                imported++;
                try
                {
                    // Remove old ID
                    oldId = (long)segment[AppNexusValues.Id];
                    segment.Remove(AppNexusValues.Id);

                    // Remove null values:
                    var nullKeys = segment
                        .Where(kvp => kvp.Value == null)
                        .Select(kvp => kvp.Key)
                        .ToArray();
                    foreach (var key in nullKeys)
                    {
                        segment.Remove(key);
                    }

                    var segmentJson = @"{{""segment"": {0}}}".FormatInvariant(JsonConvert.SerializeObject(segment));
                    var segmentId = ((AppNexusApiClient)client).CreateObject(segmentJson, createSegmentUri);

                    Log(
                        "Imported segment {0} of {1} (name: {2}, code: {3}, old id: {4}, new id: {5})",
                        imported,
                        totalSegments,
                        segment.ContainsKey("short_name") ? (string)segment["short_name"] : string.Empty,
                        segment.ContainsKey("code") ? (string)segment["code"] : string.Empty,
                        oldId,
                        segmentId);
                }
                catch (Exception e)
                {
                    Log(
                        "Error importing segment {0} of {1} (name: {2}, code: {3}, old id: {4}) - {5}",
                        imported,
                        totalSegments,
                        segment.ContainsKey("short_name") ? (string)segment["short_name"] : string.Empty,
                        segment.ContainsKey("code") ? (string)segment["code"] : string.Empty,
                        oldId,
                        e);
                }
            }
        }

        /// <summary>Exports segments from the AppNexus account to a file</summary>
        /// <param name="client">AppNexus API Client</param>
        /// <param name="file">Segments output file</param>
        /// <param name="outputXml">Whether or not to output the segments as XML</param>
        private static void ExportSegmentsFromMember(IAppNexusApiClient client, string file, bool outputXml)
        {
            Log("Exporting segments to '{0}'...", file);
            var segments = client.GetMemberSegments();
            if (!outputXml)
            {
                File.WriteAllText(file, JsonConvert.SerializeObject(segments));
            }
            else
            {
                using (var output = XmlTextWriter.Create(file))
                {
                    output.WriteStartDocument(true);
                    output.WriteStartElement("segments");

                    foreach (var segment in segments)
                    {
                        output.WriteStartElement("segment");
                        foreach (var value in segment)
                        {
                            var valueString = value.Value != null ?
                                    value.Value.ToString() :
                                    string.Empty;
                            output.WriteAttributeString(value.Key, valueString);
                        }

                        output.WriteEndElement();
                    }

                    output.WriteEndDocument();
                }
            }

            Log("Exported {0} segments to '{0}'.", segments.Length);
        }

        /// <summary>Log a formatted message</summary>
        /// <param name="format">Message format</param>
        /// <param name="args">Message args</param>
        private static void Log(string format, params object[] args)
        {
            var message = "[{0}] {1}\n".FormatInvariant(
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                format.FormatInvariant(args));
            Console.Write(message);
            if (log != null)
            {
                File.AppendAllText(log.FullName, message);
            }
        }
    }
}
