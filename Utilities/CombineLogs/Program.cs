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
using System.Configuration;
using System.Globalization;
using System.IO;
using ConsoleAppUtilities;

namespace Utilities.CombineLogs
{
    /// <summary>Contains the program entry point</summary>    
    public static class Program
    {
        /// <summary>Parsed command-line arguments</summary>
        private static CombineLogsArgs arguments;

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return
                    "Time inteleaves logs.\n" +
                    "Usage: CombineLogs.exe -start \"start date of logs to combine\" [optional arguments]\n" + 
                    CommandLineArguments.GetDescriptions<CombineLogsArgs>();
            }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        public static int Main(string[] args)
        {
            try
            {
                arguments = CommandLineArguments.Create<CombineLogsArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    Console.WriteLine(Usage);
                    return 1;
                }
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae);
                Console.WriteLine();
                Console.WriteLine(Usage);
                return 1;
            }

            // use defaults if not command line specified
            arguments.ConnectionString = string.IsNullOrWhiteSpace(arguments.ConnectionString) ?
                ConfigurationManager.AppSettings["ConnectionString"] :
                arguments.ConnectionString;

            arguments.LogContainers = string.IsNullOrWhiteSpace(arguments.LogContainers) ?
                ConfigurationManager.AppSettings["LogContainers"] :
                arguments.LogContainers;

            arguments.EndDate = arguments.EndDate.Date == new DateTime().Date ?
                DateTime.Now.AddDays(1) :
                arguments.EndDate;

            // create FileInfo if not specified
            arguments.OutputFile = arguments.OutputFile ?? new FileInfo(arguments.StartDate.ToString("yyyyMMddHH", CultureInfo.InvariantCulture) + ".txt");

            Console.WriteLine(
                "Combining logs from " + 
                arguments.LogContainers + 
                " from date " + 
                arguments.StartDate + 
                " to date " + 
                arguments.EndDate + 
                " to file " + 
                arguments.OutputFile);
       
            var combinedLog = CombineLogsEngine.CombineLogs(arguments);

            // save the output file
            using (var writer = arguments.OutputFile.CreateText())
            {
                writer.Write(combinedLog.ToString());
            }

            return 0;
        }
    }
}
