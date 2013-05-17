//-----------------------------------------------------------------------
// <copyright file="MailAlertLogger.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using ConfigManager;
using Diagnostics;
using Utilities.Net.Mail;

namespace Utilities.Diagnostics
{
    /// <summary>Outputs log alert messages as email messages.</summary>
    public class MailAlertLogger : ILogger
    {
        /// <summary>Maximum number of alerts sent per minute</summary>
        internal const int MaximumAlertsPerMinute = 5;

        /// <summary>Name of the mail template to use</summary>
        private const string MailTemplateName = "LogAlert";

        /// <summary>Maximum length of message excerpt to use is mail subject</summary>
        private const int SubjectMessageLength = 80;

        /// <summary>Mail template provider</summary>
        private readonly IMailTemplateProvider mailTemplateProvider;

        /// <summary>Backing field for MailClient</summary>
        private IMailClient mailClient;

        /// <summary>When the current throttle period started</summary>
        private DateTime throttlePeriodStart;

        /// <summary>How many alerts have been sent in the current throttle period</summary>
        private int throttleAlertsSent;

        /// <summary>Initializes a new instance of the MailAlertLogger class.</summary>
        /// <param name="mailTemplateProvider">Mail template provider</param>
        public MailAlertLogger(IMailTemplateProvider mailTemplateProvider)
        {
            this.mailTemplateProvider = mailTemplateProvider;
            this.throttlePeriodStart = DateTime.UtcNow;
            this.throttleAlertsSent = 0;
        }

        /// <summary>Gets the log levels supported by this logger</summary>
        public LogLevels LogLevels
        {
            get { return LogLevels.Error | LogLevels.Warning | LogLevels.Information; }
        }

        /// <summary>Gets a value indicating whether only alerts are supported</summary>
        public bool AlertsOnly
        {
            get { return true; }
        }

        /// <summary>Gets or sets the mail client instance</summary>
        internal IMailClient MailClient
        {
            get
            {
                return this.mailClient = this.mailClient ??
                    new SmtpMailClient(
                        SmtpHostname,
                        SmtpUsername,
                        SmtpPassword,
                        this.mailTemplateProvider);
            }

            // For testing only
            set
            {
                this.mailClient = value;
            }
        }

        /// <summary>Gets the recipients for alert mails</summary>
        private static string AlertRecipients
        {
            get { return Config.GetValue("Logging.AlertRecipients"); }
        }

        /// <summary>Gets the SMTP host</summary>
        private static string SmtpHostname
        {
            get { return Config.GetValue("Mail.LogAlerts.SmtpHost"); }
        }

        /// <summary>Gets the SMTP username</summary>
        private static string SmtpUsername
        {
            get { return Config.GetValue("Mail.LogAlerts.Username"); }
        }

        /// <summary>Gets the SMTP password</summary>
        private static string SmtpPassword
        {
            get { return Config.GetValue("Mail.LogAlerts.Password"); }
        }

        /// <summary>Logs a message with the specified log level</summary>
        /// <param name="level">The level of the log message</param>
        /// <param name="instance">The role instance of the log message</param>
        /// <param name="thread">The thread of the log message</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The content of the log message</param>
        public void LogMessage(LogLevels level, string instance, string thread, string source, string message)
        {
            // Reset the throttle every minute
            if ((DateTime.UtcNow - this.throttlePeriodStart).TotalMinutes >= 1)
            {
                this.throttleAlertsSent = 0;
                this.throttlePeriodStart = DateTime.UtcNow;
            }

            // Don't send more than the maximum alerts in one minute
            if (++this.throttleAlertsSent > MaximumAlertsPerMinute)
            {
                return;
            }

            // Compose args for the mail template and send the message
            var subjectArgs = new[]
            {
                level.ToString(),
                source,
                message.Left(SubjectMessageLength)
            };
            var bodyArgs = new[]
            {
                level.ToString(),
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                instance,
                thread,
                source,
                message
            };
            this.MailClient.SendMail(MailTemplateName, AlertRecipients, subjectArgs, bodyArgs);
        }
    }
}
