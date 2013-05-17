// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Configuration;
using System.Globalization;
using ConsoleAppUtilities;

namespace Utilities.DeleteLogs
{
    /// <summary>Contains the program entry point</summary>    
    public static class Program
    {
        /// <summary>Parsed command-line arguments</summary>
        private static DeleteLogsArgs arguments;

        /// <summary>Gets usage information</summary>
        private static string Usage
        {
            get
            {
                return
                    "Deletes Logs older than an input hours ago (or a config default).\n" +
                    @"Usage: DeleteLogs.exe [optional arguments] " +
                    CommandLineArguments.GetDescriptions<DeleteLogsArgs>();
            }
        }

        /// <summary>Program entry point</summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>The error code</returns>
        public static int Main(string[] args)
        {
            try
            {
                arguments = CommandLineArguments.Create<DeleteLogsArgs>(args);
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
                
            // use default if not command line specified or invalid
            var configHoursAgoThresholdForDeleting = int.Parse(ConfigurationManager.AppSettings["HoursAgoThresholdForDeleting"], CultureInfo.InvariantCulture);
            arguments.HoursAgoThresholdForDeleting = arguments.HoursAgoThresholdForDeleting < configHoursAgoThresholdForDeleting ?
                configHoursAgoThresholdForDeleting :
                arguments.HoursAgoThresholdForDeleting;

            Console.WriteLine("Deleting logs older than " + arguments.HoursAgoThresholdForDeleting + " hours old from container(s) " + arguments.LogContainers + ".");

            return DeleteLogsEngine.DeleteLogs(arguments);
        }
    }
}
