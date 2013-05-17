//-----------------------------------------------------------------------
// <copyright file="SendUserMailActivityBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DefaultMailTemplates;
using Diagnostics;
using EntityUtilities;
using Microsoft.Practices.Unity;
using Utilities.Net.Mail;

namespace EntityActivities.UserMail
{  
    /// <summary>
    /// Base class for activities that send mail to users
    /// </summary>
    /// <remarks>
    /// TODO: Move mail activities to their own assembly and remove dependency on EntityRepository
    /// RequiredValues:    
    ///   UserId - User.UserId of the user to send mail to
    /// </remarks>
    public abstract class SendUserMailActivityBase : EntityActivity
    {
        /// <summary>Backing field for MailClient. DO NOT USE DIRECTLY.</summary>
        private IMailClient mailClient;

        /// <summary>Gets or sets the mail config provider</summary>
        internal IMailClient MailClient
        {
            get
            {
                return this.mailClient =
                    this.mailClient ??
                    new SmtpMailClient(
                        this.SmtpHostname,
                        this.SmtpUsername,
                        this.SmtpPassword,
                        new EmbeddedMailTemplateProvider());
            }

            set
            {
                this.mailClient = value;
            }
        }

        /// <summary>Gets the name of the mail config</summary>
        protected abstract string MailTemplateName { get; }

        /// <summary>Gets the SMTP host</summary>
        protected virtual string SmtpHostname
        {
            get { return Config.GetValue("Mail.SmtpHost"); }
        }

        /// <summary>Gets the SMTP username</summary>
        protected virtual string SmtpUsername
        {
            get { return Config.GetValue("Mail.Username"); }
        }

        /// <summary>Gets the SMTP password</summary>
        protected virtual string SmtpPassword
        {
            get { return Config.GetValue("Mail.Password"); }
        }

        /// <summary>
        /// Gets the arguments for the formatted message subject from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The subject arguments</returns>
        protected abstract object[] GetSubjectArgs(ActivityRequest request);

        /// <summary>
        /// Gets the arguments for the formatted message body from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The body arguments</returns>
        protected abstract object[] GetBodyArgs(ActivityRequest request);

        /// <summary>
        /// Gets the recipient email from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The recipient email</returns>
        protected virtual string GetRecipientEmail(ActivityRequest request)
        {
            var context = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var userEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);

            try
            {
                var user = this.Repository.GetEntity(context, userEntityId) as UserEntity;
                return (string)user.ContactEmail;
            }
            catch (DataAccessEntityNotFoundException ae)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to get contact email for user '{0}'\n{1}",
                    userEntityId,
                    ae);
                return null;
            }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected sealed override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var recipientEmail = this.GetRecipientEmail(request);

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return ErrorResult(ActivityErrorId.FailureSendingEmail, "Unable to get recipient email");
            }

            try
            {
                this.MailClient.SendMail(
                    this.MailTemplateName,
                    recipientEmail,
                    this.GetSubjectArgs(request),
                    this.GetBodyArgs(request));
                return this.SuccessResult();
            }
            catch (SmtpException se)
            {
                return this.ErrorResult(
                    ActivityErrorId.FailureSendingEmail,
                    @"Unable to send '{0}' mail to ""{1}"" ({2}): {3}",
                    this.MailTemplateName,
                    recipientEmail,
                    request.Values[EntityActivityValues.EntityId],
                    se);
            }
        }
    }
}
