//-----------------------------------------------------------------------
// <copyright file="EmbeddedMailTemplateProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Diagnostics;
using Utilities.Net.Mail;
using Utilities.Storage;

namespace DefaultMailTemplates
{
    /// <summary>Interface for provider of mail templates</summary>
    public class EmbeddedMailTemplateProvider : IMailTemplateProvider
    {
        /// <summary>DataContractSerializer used for reading MailTemplates from embedded resources</summary>
        internal static readonly DataContractSerializer MailTemplateSerializer = new DataContractSerializer(typeof(MailTemplate));

        /// <summary>The assembly which contains mail templates</summary>
        private readonly Assembly resourceAssembly;

        /// <summary>Persisted mail templates</summary>
        private readonly IPersistentDictionary<MailTemplate> mailTemplates;

        /// <summary>Initializes a new instance of the EmbeddedMailTemplateProvider class.</summary>
        public EmbeddedMailTemplateProvider()
        {
            this.resourceAssembly = Assembly.GetExecutingAssembly();
            this.mailTemplates = PersistentDictionaryFactory.CreateDictionary<MailTemplate>("mailtemplates");
        }

        /// <summary>Gets a named mail template</summary>
        /// <param name="mailTemplateName">Template name</param>
        /// <returns>Mail Template</returns>
        public MailTemplate GetMailTemplate(string mailTemplateName)
        {
            var mailTemplateNameNormalized = mailTemplateName.ToUpperInvariant();

            // Lazy initialize persistent dictionary using embedded templates
            if (!this.mailTemplates.ContainsKey(mailTemplateNameNormalized))
            {
                var resourceName = this.resourceAssembly.GetManifestResourceNames()
                    .Where(name => name.ToUpperInvariant().Split('.').Contains(mailTemplateNameNormalized))
                    .SingleOrDefault();
                if (string.IsNullOrEmpty(resourceName))
                {
                    throw new ArgumentException(
                        "Mail template '{0}' was not found in the store '{1}' nor in the assembly '{2}'"
                        .FormatInvariant(
                            mailTemplateName,
                            this.mailTemplates.StoreName,
                            this.resourceAssembly.FullName),
                        "mailTemplateName");
                }

                using (var resource = this.resourceAssembly.GetManifestResourceStream(resourceName))
                {
                    var mailTemplate = (MailTemplate)MailTemplateSerializer.ReadObject(resource);
                    this.mailTemplates[mailTemplateNameNormalized] = mailTemplate;

                    LogManager.Log(
                        LogLevels.Information,
                        "Loaded mail template '{0}' to the '{1}' store from embedded resource '{2}' of assembly '{3}'",
                        mailTemplateNameNormalized,
                        this.mailTemplates.StoreName,
                        resourceName,
                        this.resourceAssembly);
                }
            }

            return this.mailTemplates[mailTemplateNameNormalized];
        }

        /// <summary>Sets a named mail template</summary>
        /// <param name="mailTemplateName">Template name</param>
        /// <param name="mailTemplate">Mail template</param>
        public void SetMailTemplate(string mailTemplateName, MailTemplate mailTemplate)
        {
            var mailTemplateNameNormalized = mailTemplateName.ToUpperInvariant();
            this.mailTemplates[mailTemplateNameNormalized] = mailTemplate;
        }
    }
}
