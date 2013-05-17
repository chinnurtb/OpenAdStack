// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds">
//   Copyright Rare Crowds. All rights reserved.
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
