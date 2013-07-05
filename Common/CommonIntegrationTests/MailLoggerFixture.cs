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
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Diagnostics;
using Utilities.Net.Mail;
using Utilities.Storage.Testing;

namespace CommonIntegrationTests
{
    /// <summary>Unit tests for the mail logger</summary>
    [TestClass]
    public class MailLoggerFixture
    {
        /// <summary>Test logger</summary>
        private TestLogger testLogger;

        /// <summary>IMailClient instance</summary>
        private IMailClient mailClient;

        /// <summary>Default embedded mail template provider</summary>
        private IMailTemplateProvider mailTemplateProvider;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["Logging.AlertRecipients"] = "nobody@rc.dev";
            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new ILogger[0]);
            this.testLogger = new TestLogger();

            this.mailTemplateProvider = new DefaultMailTemplates.EmbeddedMailTemplateProvider();
            this.mailClient = new SmtpMailClient(this.mailTemplateProvider);
        }

        /// <summary>Test logging a mail alert message</summary>
        [TestMethod]
        public void LogMailAlertMessage()
        {
            var testMessage =
@"Export summary for campaign 'May 2013 v2' (69081b4f254640e690cffd3bd476fb93):
    Total allocation nodes:     12960
    Allocations for export:     200
    Nodes w/daily media budget: 200
        Nodes without budget:   12760
    Nodes w/export budget:      200
        Nodes without budget:   12760
    AppNexus Export:
        Created campaigns:      179
        Created profiles:       177
        Updated campaigns:      21
        Uncreated campaigns:    0
        Deleted campaigns:      179
    Failed nodes:               0";
            var mailLogger = new MailAlertLogger(this.mailTemplateProvider) { MailClient = this.mailClient };
            LogManager.Initialize(new ILogger[] { mailLogger, this.testLogger });
            LogManager.Log(LogLevels.Information, true, testMessage);
            Assert.IsFalse(this.testLogger.HasMessagesContaining("Error sending mail alert"));
        }
    }
}
