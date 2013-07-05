//-----------------------------------------------------------------------
// <copyright file="SmtpMailClient.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Mail;
using ConfigManager;
using Diagnostics;

namespace Utilities.Net.Mail
{
    /// <summary>SMTP mail client</summary>
    public class SmtpMailClient : IMailClient
    {
        /// <summary>Mail template provider</summary>
        private readonly IMailTemplateProvider mailTemplateProvider;

        /// <summary>SMTP host to use</summary>
        private readonly string host;

        /// <summary>Credentials to use with the SMTP host (if any)</summary>
        private readonly NetworkCredential credentials;

        /// <summary>Initializes a new instance of the SmtpMailClient class.</summary>
        /// <param name="mailTemplateProvider">Mail template provider</param>
        public SmtpMailClient(IMailTemplateProvider mailTemplateProvider)
            : this(Config.GetValue("Mail.SmtpHost"), Config.GetValue("Mail.Username"), Config.GetValue("Mail.Password"), mailTemplateProvider)
        {
        }

        /// <summary>Initializes a new instance of the SmtpMailClient class.</summary>
        /// <param name="host">SMTP host to use</param>
        /// <param name="mailTemplateProvider">Mail template provider</param>
        public SmtpMailClient(string host, IMailTemplateProvider mailTemplateProvider)
            : this(host, null, null, mailTemplateProvider)
        {
        }

        /// <summary>Initializes a new instance of the SmtpMailClient class.</summary>
        /// <param name="host">SMTP host to use</param>
        /// <param name="username">Username (optional)</param>
        /// <param name="password">Password (optional)</param>
        /// <param name="mailTemplateProvider">Mail template provider</param>
        public SmtpMailClient(string host, string username, string password, IMailTemplateProvider mailTemplateProvider)
        {
            this.mailTemplateProvider = mailTemplateProvider;
            this.host = host;
            this.credentials =
                !string.IsNullOrWhiteSpace(username) &&
                !string.IsNullOrWhiteSpace(password) ?
                new NetworkCredential(username, password) :
                null;
            LogManager.Log(
                LogLevels.Trace,
                @"Initialized SmtpMailClient: host=""{0}"" username=""{1}"" password=""{2}"" mailTemplateProvider=""{3}""",
                this.host,
                this.credentials != null ? this.credentials.UserName : string.Empty,
                this.credentials != null ? this.credentials.Password : string.Empty,
                this.mailTemplateProvider.GetType().FullName);
        }
        
        /// <summary>
        /// Sends an email created from the provided template and values
        /// </summary>
        /// <param name="mailTemplateName">Mail template name</param>
        /// <param name="recipient">Recipient address</param>
        /// <param name="subjectArgs">Values for the formatted subject</param>
        /// <param name="bodyArgs">Values for the formatted body</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// No mail template matching <paramref name="mailTemplateName"/> was found.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// An error occured formatting the message subject and/or body.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// An error occured sending the message.
        /// </exception>
        public void SendMail(string mailTemplateName, string recipient, object[] subjectArgs, object[] bodyArgs)
        {
            var mailTemplate = this.mailTemplateProvider.GetMailTemplate(mailTemplateName);
            var subject = mailTemplate.SubjectFormat.FormatInvariant(subjectArgs);
            var body = mailTemplate.BodyFormat.FormatInvariant(bodyArgs);

            var message = new MailMessage(mailTemplate.Sender, recipient, subject, body)
            {
                IsBodyHtml = mailTemplate.IsBodyHtml
            }; 
            
            try
            {
                using (var smtpClient = new SmtpClient(this.host) { Credentials = this.credentials })
                {
                    smtpClient.Send(message);
                }
            }
            catch (SmtpException se)
            {
                throw new InvalidOperationException("Error sending mail", se);
            }
        }
    }
}
