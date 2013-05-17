//-----------------------------------------------------------------------
// <copyright file="IMailTemplateProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Utilities.Net.Mail
{
    /// <summary>Interface for provider of mail templates</summary>
    public interface IMailTemplateProvider
    {
        /// <summary>Gets a named mail template</summary>
        /// <param name="mailTemplateName">Template name</param>
        /// <returns>Mail Template</returns>
        MailTemplate GetMailTemplate(string mailTemplateName);

        /// <summary>Sets a named mail template</summary>
        /// <param name="mailTemplateName">Template name</param>
        /// <param name="mailTemplate">Mail template</param>
        void SetMailTemplate(string mailTemplateName, MailTemplate mailTemplate);
    }
}
