// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using ConsoleAppUtilities;

namespace RunReport
{
    /// <summary>RunReports main class</summary>
    public static class Program
    {
        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Run a campaign report.
Usage: RunReport.exe -o ""OutputDirectory"" -company ""CompanyId""  -campaign ""CampaignId"" [-log ""LogFileDirectory""] [-cr|-dr|-all] [-v]
{0}";
        
        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return UsageFormat
                    .FormatInvariant(CommandLineArguments.GetDescriptions<RunReportArgs>());
            }
        }

        /// <summary>RunReports entry point</summary>
        /// <param name="args">The args.</param>
        /// <returns>0 if successful</returns>
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(Usage);
                return 0;
            }

            try
            {
                var arguments = CommandLineArguments.Create<RunReportArgs>(args);
                if (!arguments.ArgumentsValid)
                {
                    Console.WriteLine("Invalid argument(s)");
                    return 1;
                }

                var reportRunner = new ReportRunner();
                reportRunner.Run(arguments);
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
