//-----------------------------------------------------------------------
// <copyright file="SendUserInviteMailActivity.cs" company="Rare Crowds Inc">
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
