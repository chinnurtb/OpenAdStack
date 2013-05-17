//-----------------------------------------------------------------------
// <copyright file="QuotaLogger.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using ConfigManager;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Diagnostics
{
    /// <summary>
    /// Outputs log messages as XML fragments to quota managed files which are
    /// periodically copied to an Azure storage blob container using quotas.
    /// </summary>
    public class QuotaLogger : ILogger
    {
        /// <summary>Format for message xml elements</summary>
        private const string LogMessageXmlFormat = @"
<msg time=""{0}"" lvl=""{1}"" inst=""{2}"" thrd=""{3}"" src=""{4}"" stk=""{5}""><![CDATA[
{6}
]]></msg>";

        /// <summary>
        /// Lock object for writing to the log (thread safety)
        /// </summary>
        private static object writerLock = new object();

        /// <summary>
        /// Name of the current log file
        /// </summary>
        private string logFileName;

        /// <summary>Gets the log levels supported by this logger</summary>
        public LogLevels LogLevels
        {
            get { return LogLevels.Trace | LogLevels.Information | LogLevels.Warning | LogLevels.Error; }
        }

        /// <summary>Gets a value indicating whether only alerts are supported</summary>
        public bool AlertsOnly
        {
            get { return false; }
        }

        #region Configuration

        /// <summary>Gets the Windows Azure storage connection string</summary>
        private static string ConnectionString
        {
            get { return Config.GetValue("Logging.ConnectionString"); }
        }

        /// <summary>Gets the name for the blob container in Windows Azure storage.</summary>
        private static string ContainerName
        {
            get { return Config.GetValue("Logging.BlobContainer"); }
        }

        /// <summary>Gets the local resource</summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        private static LocalResource LocalResource
        {
            get { return RoleEnvironment.GetLocalResource(Config.GetValue("Logging.LocalResource")); }
        }

        /// <summary>Gets the scheduled transfer period time span</summary>
        private static TimeSpan ScheduledTransferPeriod
        {
            get { return TimeSpan.FromMinutes(Config.GetDoubleValue("Logging.ScheduledTransferPeriodMinutes")); }
        }

        /// <summary>Gets the size for individual log files</summary>
        private static int LogFileSizeBytes
        {
            get { return Config.GetIntValue("Logging.FileSizeBytes"); }
        }

        /// <summary>
        /// Gets the directory size quota (in megabytes)
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        private static int MaximumSizeInMegabytes
        {
            get
            {
                return RoleEnvironment.IsAvailable ?
                    LocalResource.MaximumSizeInMegabytes :
                    Config.GetIntValue("Logging.MaximumSizeInMegabytes");
            }
        }

        /// <summary>
        /// Gets the root log path for the role
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        private static string RootPath
        {
            get
            {
                return RoleEnvironment.IsAvailable ?
                    LocalResource.RootPath :
                    Config.GetValue("Logging.RootPath");
            }
        }

        #endregion

        /// <summary>
        /// Gets the current stacktrace, excluding the Diagnostics namespace
        /// </summary>
        private static string Stacktrace
        {
            get
            {
                var stack = new StackTrace().ToString()
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !s.Contains("at Diagnostics."));
                return string.Join("\n", stack);
            }
        }

        /// <summary>
        /// Gets the full path of the current log file
        /// </summary>
        private string LogFilePath
        {
            get
            {
                // Getting the log file path for the first time?
                if (this.logFileName == null)
                {
                    // Find the last file for the current timestamp (if any)
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
                    int part = 0, filePart;
                    foreach (var fileName in Directory.GetFiles(RootPath, timestamp + ".*"))
                    {
                        if (int.TryParse(new FileInfo(fileName).Extension, out filePart) &&
                            filePart > part)
                        {
                            part = filePart;
                        }
                    }

                    this.logFileName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", timestamp, part);
                }
                else
                {
                    this.UpdateTimeStampFileName();
                    this.UpdatePartNumberExtension();
                }

                return RootPath + this.logFileName;
            }
        }

        /// <summary>
        /// Initializes diagnostics which facilitate scheduled transfer of logs
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running in Azure context.")]
        public static void InitializeDiagnostics()
        {
            // Get the default initial configuration for DiagnosticMonitor.
            DiagnosticMonitorConfiguration diagnosticConfiguration = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // Add the directoryConfiguration to the Directories collection.
            diagnosticConfiguration.Directories.DataSources.Add(
                new DirectoryConfiguration
                {
                    Container = ContainerName,
                    DirectoryQuotaInMB = MaximumSizeInMegabytes,
                    Path = RootPath
                });

            // Schedule a transfer period.
            diagnosticConfiguration.Directories.ScheduledTransferPeriod = ScheduledTransferPeriod;

            // Get the storage account
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(ConnectionString);

            // Only works in AppFabric
            if (RoleEnvironment.IsAvailable)
            {
                DiagnosticMonitor.Start(cloudStorageAccount, diagnosticConfiguration);
            }
        }

        /// <summary>Logs a message with the specified log level</summary>
        /// <param name="level">The level of the log message</param>
        /// <param name="instance">The role instance of the log message</param>
        /// <param name="thread">The thread of the log message</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The content of the log message</param>
        public void LogMessage(LogLevels level, string instance, string thread, string source, string message)
        {
            var logMessage = string.Format(
                CultureInfo.InvariantCulture,
                LogMessageXmlFormat,
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Enum.GetName(typeof(LogLevels), level),
                SecurityElement.Escape(instance),
                SecurityElement.Escape(thread),
                SecurityElement.Escape(source),
                SecurityElement.Escape(Stacktrace),
                message);
            lock (writerLock)
            {
                File.AppendAllText(this.LogFilePath, logMessage);
            }
        }

        /// <summary>
        /// Update the log file name to the current time stamp
        /// </summary>
        private void UpdateTimeStampFileName()
        {
            var logFileInfo = new FileInfo(RootPath + this.logFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
            if (timestamp != logFileInfo.GetFileName())
            {
                this.logFileName = timestamp + ".0";
            }
        }

        /// <summary>
        /// Increment the log file part number extension if the current part is full
        /// </summary>
        private void UpdatePartNumberExtension()
        {
            var logFileInfo = new FileInfo(RootPath + this.logFileName);
            if (logFileInfo.Exists && logFileInfo.Length > LogFileSizeBytes)
            {
                int part = 0;
                if (int.TryParse(logFileInfo.Extension.Substring(1), out part))
                {
                    part++;
                }

                this.logFileName = logFileInfo.GetFileName() + "." + part.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
