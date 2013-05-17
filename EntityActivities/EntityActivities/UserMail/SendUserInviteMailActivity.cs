//-----------------------------------------------------------------------
// <copyright file="SendUserInviteMailActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Activities;
using ConfigManager;
using DataAccessLayer;

namespace EntityActivities.UserMail
{  
    /// <summary>
    /// Activity for sending new user invite mail
    /// </summary>
    /// <remarks>
    /// Sends User invite email
    /// RequiredValues:    
    ///   EntityId - Id of the user to put in the verification link
    /// </remarks>
    [Name("SendUserInviteMail"), RequiredValues("EntityId")]
    public class SendUserInviteMailActivity : SendUserMailActivityBase
    {
        /// <summary>Gets the SMTP host</summary>
        protected override string SmtpHostname
        {
            get { return Config.GetValue("Mail.UserInvite.SmtpHost"); }
        }

        /// <summary>Gets the SMTP username</summary>
        protected override string SmtpUsername
        {
            get { return Config.GetValue("Mail.UserInvite.Username"); }
        }

        /// <summary>Gets the SMTP password</summary>
        protected override string SmtpPassword
        {
            get { return Config.GetValue("Mail.UserInvite.Password"); }
        }

        /// <summary>Gets the name of the mail config</summary>
        protected override string MailTemplateName
        {
            get { return "UserInvite"; }
        }

        /// <summary>
        /// Gets the arguments for the formatted message subject from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The subject arguments</returns>
        protected override object[] GetSubjectArgs(ActivityRequest request)
        {
            return null;
        }

        /// <summary>
        /// Gets the arguments for the formatted message body from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The body arguments</returns>
        protected override object[] GetBodyArgs(ActivityRequest request)
        {
            var userId = request.Values["EntityId"];
            var linkFormat = Config.GetValue("UserMail.Invitation.LinkFormat");
            var verificationLink = linkFormat.FormatInvariant(userId);
            return new[] { verificationLink };
        }
    }
}
