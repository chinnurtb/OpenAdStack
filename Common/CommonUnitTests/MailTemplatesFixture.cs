//-----------------------------------------------------------------------
// <copyright file="MailTemplatesFixture.cs" company="Rare Crowds Inc">
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

using DefaultMailTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Net.Mail;
using Utilities.Storage.Testing;

namespace CommonUnitTests
{
    /// <summary>Unit tests for Mail Templates</summary>
    [TestClass]
    public class MailTemplatesFixture
    {
        /// <summary>Per test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
        }

        /// <summary>Make sure the expected templates are available</summary>
        [TestMethod]
        public void GetTemplates()
        {
            var expectedTemplates = new[]
            {
                "UserInvite",
                "LogAlert"
            };

            var templateProvider = new EmbeddedMailTemplateProvider();

            foreach (var expectedTemplate in expectedTemplates)
            {
                var template = templateProvider.GetMailTemplate(expectedTemplate);
                Assert.IsNotNull(template);
            }
        }

/****
        /// <summary>"Test" used for quickly creating templates</summary>
        [TestMethod]
        [Ignore]
        public void MakeTemplate()
        {
            var template = new MailTemplate
            {
                Sender = @"test@rarecrowds.com",
                SubjectFormat = @"Test Invitation",
                BodyFormat = @"<a href=""{0}"">{0}</a>",
                IsBodyHtml = true
            };

            var xml = string.Empty;
            using (var writer = new System.IO.StringWriter())
            {
                using (var xmlWriter = new System.Xml.XmlTextWriter(writer))
                {
                    EmbeddedMailTemplateProvider.MailTemplateSerializer.WriteObject(xmlWriter, template);
                }

                xml = writer.ToString();
            }

            Assert.IsNotNull(xml);
        }
  ****/
    }
}
