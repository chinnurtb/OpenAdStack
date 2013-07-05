//-----------------------------------------------------------------------
// <copyright file="MailLoggerFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Net.Mail;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Diagnostics;
using Utilities.Net.Mail;
using Utilities.Storage.Testing;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the mail logger</summary>
    [TestClass]
    public class MailLoggerFixture
    {
        /// <summary>Mock IMailClient instance</summary>
        private IMailClient mockMailClient;

        /// <summary>Default embedded mail template provider</summary>
        private IMailTemplateProvider mailTemplateProvider;

        /// <summary>Number of mails have been "sent" to the mock mail client</summary>
        private int alertMailCount;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["Logging.AlertRecipients"] = "nobody@rc.dev";
            ConfigurationManager.AppSettings["Mail.LogAlerts.SmtpHost"] = "mail.rc.dev";
            ConfigurationManager.AppSettings["Mail.LogAlerts.Username"] = string.Empty;
            ConfigurationManager.AppSettings["Mail.LogAlerts.Password"] = string.Empty;
            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new ILogger[0]);

            this.mailTemplateProvider = new DefaultMailTemplates.EmbeddedMailTemplateProvider();
            this.InitializeMockMailClient();
        }

        /// <summary>Test initializing an instance of MailLogger</summary>
        [TestMethod]
        public void InitializeLogManagerWithMailLogger()
        {
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider);
            LogManager.Initialize(new[] { mailLogger });
        }

        /// <summary>Test logging an information message</summary>
        [TestMethod]
        public void LogInformationMessage()
        {
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider) { MailClient = this.mockMailClient };
            LogManager.Initialize(new[] { mailLogger });
            LogManager.Log(LogLevels.Information, true, "Test Message {0}", Guid.NewGuid());
            this.AssertSendMailWasCalled();
        }

        /// <summary>Test logging an warning message</summary>
        [TestMethod]
        public void LogWarningMessage()
        {
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider) { MailClient = this.mockMailClient };
            LogManager.Initialize(new[] { mailLogger });
            LogManager.Log(LogLevels.Warning, true, "Test Message {0}", Guid.NewGuid());
            this.AssertSendMailWasCalled();
        }

        /// <summary>Test logging an error message</summary>
        [TestMethod]
        public void LogErrorMessage()
        {
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider) { MailClient = this.mockMailClient };
            LogManager.Initialize(new[] { mailLogger });
            LogManager.Log(LogLevels.Error, true, "Test Message {0}", Guid.NewGuid());
            this.AssertSendMailWasCalled();
        }

        /// <summary>Test that sending of mail is throttled</summary>
        [TestMethod]
        public void ThrottleMails()
        {
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider) { MailClient = this.mockMailClient };
            LogManager.Initialize(new[] { mailLogger });

            for (int i = 0; i < MailAlertLogger.MaximumAlertsPerMinute * 2; i++)
            {
                LogManager.Log(LogLevels.Error, true, "Test Message {0}", i);
            }

            Assert.AreEqual(MailAlertLogger.MaximumAlertsPerMinute, this.alertMailCount);
        }

        /// <summary>Assert that the mock IMailClient.SendMail method was called</summary>
        private void AssertSendMailWasCalled()
        {
            this.mockMailClient.AssertWasCalled(f =>
                f.SendMail(
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<object[]>.Is.Anything,
                    Arg<object[]>.Is.Anything));
        }

        /// <summary>Initializes the IMailClient mock</summary>
        private void InitializeMockMailClient()
        {
            this.mockMailClient = MockRepository.GenerateMock<IMailClient>();
            this.mockMailClient.Stub(f =>
                f.SendMail(
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<object[]>.Is.Anything,
                    Arg<object[]>.Is.Anything))
                .WhenCalled(call =>
                {
                    this.alertMailCount++;

                    var mailTemplateName = call.Arguments[0] as string;
                    var recipient = call.Arguments[1] as string;
                    var subjectArgs = call.Arguments[2] as object[];
                    var bodyArgs = call.Arguments[3] as object[];

                    Assert.IsFalse(string.IsNullOrWhiteSpace(mailTemplateName));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(recipient));
                    Assert.IsNotNull(subjectArgs);
                    Assert.AreNotEqual(0, subjectArgs.Length);
                    Assert.IsNotNull(bodyArgs);
                    Assert.AreNotEqual(0, bodyArgs.Length);

                    var mailTemplate = this.mailTemplateProvider.GetMailTemplate(mailTemplateName);
                    Assert.IsNotNull(mailTemplate);

                    try
                    {
                        var subject = mailTemplate.SubjectFormat.FormatInvariant(subjectArgs);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(subject));
                    }
                    catch (FormatException fe)
                    {
                        Assert.Fail("Error formatting subject:\n{0}\n\n{1}", mailTemplate.SubjectFormat, fe);
                    }

                    try
                    {
                        var body = mailTemplate.BodyFormat.FormatInvariant(bodyArgs);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(body));
                    }
                    catch (FormatException fe)
                    {
                        Assert.Fail("Error formatting body:\n{0}\n\n{1}", mailTemplate.SubjectFormat, fe);
                    }
                });
        }
    }
}
