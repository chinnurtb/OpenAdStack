//-----------------------------------------------------------------------
// <copyright file="MailTemplatesFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
