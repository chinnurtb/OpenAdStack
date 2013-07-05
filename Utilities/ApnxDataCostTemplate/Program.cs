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
using System.IO;
using ConsoleAppUtilities;

namespace Utilities.AppNexus.DataCostTemplate
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {
        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return
                    "Imports and exports segments from AppNexus accounts.\n" +
                    @"Example: ApnxSegments.exe -api ""http://api.appnexus.com/"" -user ""UserName"" -pass ""p@s5w0rd"" -out ""datacosts.csv""\n" +
                    CommandLineArguments.GetDescriptions<AppNexusDataCostArgs>();
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
                var arguments = CommandLineArguments.Create<AppNexusDataCostArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    throw new ArgumentException("One or more arguments are invalid");
                }

                var generator = new AppNexusDataCostGenerator(
                    arguments.ApiEndpoint,
                    arguments.UserName,
                    arguments.Password);

                using (var output = arguments.OutFile.OpenWrite())
                {
                    generator.GenerateAndSaveDataCostCsvTemplate(output);
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
