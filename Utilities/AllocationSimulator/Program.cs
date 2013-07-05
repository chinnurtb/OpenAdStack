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
using ConsoleAppUtilities;
using Utilities.AllocationSimulator;

namespace AllocationSimulator
{
    /// <summary>Contains the program entry point</summary>
    public static class Program
    {      
        /// <summary>Separator for console output</summary>
        private const string Separator = "--------------------------------------------------------------------------------";

        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Simulates a campaign.
Usage: AllocationSimulator.exe {{-i ""InputFileName""}} {{-o ""OutputDirectory""}}\n{0}";

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<AllocationSimulatorArgs>());
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
                var arguments = CommandLineArguments.Create<AllocationSimulatorArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    Console.WriteLine("Invalid argument(s)");
                    return 1;
                }

                new Simulator(arguments).Run();
                return 0;
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine("Invalid argument(s)");
                Console.Error.WriteLine(ae);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }
    }
}
