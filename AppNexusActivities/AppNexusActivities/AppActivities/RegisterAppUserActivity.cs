//-----------------------------------------------------------------------
// <copyright file="RegisterAppUserActivity.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using EntityActivities.UserMail;
using EntityUtilities;
using Newtonsoft.Json;
using Utilities.Storage;

namespace AppNexusActivities.AppActivities
{
    /// <summary>
    /// Activity for registering new AppNexus App users
    /// </summary>
    /// <remarks>
    /// RequiredValues:
    ///   EntityId - The EntityId of the UserEntity
    /// </remarks>
    [Name(AppNexusActivityTasks.AppUserRegistration)]
    [RequiredValues(EntityActivityValues.MessagePayload)]
    public class RegisterAppUserActivity : SendUserMailActivityBase
    {
        /// <summary>Backing field for MemberAgencyMappings</summary>
        private IPersistentDictionary<string> memberAgencyMappings;

        /// <summary>Gets the activity's runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.Background; }
        }
        
        /// <summary>Gets the SMTP host</summary>
        protected override string SmtpHostname
        {
            get { return Config.GetValue("Mail.ApnxAppRegistration.SmtpHost"); }
        }

        /// <summary>Gets the SMTP username</summary>
        protected override string SmtpUsername
        {
            get { return Config.GetValue("Mail.ApnxAppRegistration.Username"); }
        }

        /// <summary>Gets the SMTP password</summary>
        protected override string SmtpPassword
        {
            get { return Config.GetValue("Mail.ApnxAppRegistration.Password"); }
        }

        /// <summary>Gets the name of the mail config</summary>
        protected override string MailTemplateName
        {
            get { return "UserRegistration"; }
        }

        /// <summary>
        /// Gets a repository request context using the system auth user id
        /// </summary>
        private static RequestContext SystemRequestContext
        {
            get
            {
                var request = new ActivityRequest
                {
                    Values =
                    {
                        { EntityActivityValues.AuthUserId, Config.GetValue("System.AuthUserId") }
                    }
                };
                return CreateRepositoryContext(EntityActivities.RepositoryContextType.InternalEntityGet, request);
            }
        }

        /// <summary>Gets the dictionary of member agency mappings</summary>
        private IPersistentDictionary<string> MemberAgencyMappings
        {
            get
            {
                return this.memberAgencyMappings = this.memberAgencyMappings ??
                PersistentDictionaryFactory.CreateDictionary<string>("AppNexusMemberAgencyXRef");
            }
        }

        /// <summary>
        /// Gets the arguments for the formatted message subject from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The subject arguments</returns>
        protected override object[] GetSubjectArgs(ActivityRequest request)
        {
            var user = GetUser(request);
            return new[]
            {
                user.ExternalName
            };
        }

        /// <summary>Gets the recipient email from the request</summary>
        /// <param name="request">The request</param>
        /// <returns>The recipient email</returns>
        protected override string GetRecipientEmail(ActivityRequest request)
        {
            return Config.GetValue("UserMail.Registration.Address");
        }

        /// <summary>
        /// Gets the arguments for the formatted message body from the request
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>The body arguments</returns>
        protected override object[] GetBodyArgs(ActivityRequest request)
        {
            // Deserialize the user from the request
            var user = GetUser(request);
            
            // Lookup the AppNexus member for the user
            var member = GetMemberForUser(user.UserId);
            if (member == null)
            {
                throw new ArgumentException(
                    "Unable to get member for AppNexus user '{0}'".FormatInvariant(user.UserId),
                    "request");
            }

            // Get the Agency CompanyEntity for the member (if one has been created)
            var agency = this.GetAgencyForMember(member[AppNexusValues.Id].ToString());

            // TODO: Include information in link to pre-populate user creation form
            var linkFormat = Config.GetValue("UserMail.Registration.LinkFormat");
            var registerLink = linkFormat.FormatInvariant(user.UserId);

            return new[]
            {
                user.ExternalName.ToString(),
                user.ContactEmail.ToString(),
                user.UserId.ToString(),
                member[AppNexusValues.Id],
                member[AppNexusValues.Name],
                agency != null ? agency.ExternalName.ToString() : "n/a",
                agency != null ? agency.ExternalEntityId.ToString() : "n/a",
                registerLink
            };
        }

        /// <summary>Gets the user from the request</summary>
        /// <param name="request">Activity request</param>
        /// <returns>The user</returns>
        private static UserEntity GetUser(ActivityRequest request)
        {
            var user = EntityJsonSerializer.DeserializeUserEntity(
                new EntityId(),
                request.Values[EntityActivityValues.MessagePayload]);
            user.UserId = request.Values[EntityActivityValues.AuthUserId];
            return user;
        }

        /// <summary>Gets the user's AppNexus member</summary>
        /// <param name="userId">AppNexus user id</param>
        /// <returns>The AppNexus member</returns>
        private static IDictionary<string, object> GetMemberForUser(string userId)
        {
            try
            {
                using (var client = AppNexusActivity.CreateAppNexusClient(userId))
                {
                    return client.GetMember();
                }
            }
            catch (AppNexusClientException e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to get member for AppNexus user '{0}'\n{1}",
                    userId,
                    e);
                return null;
            }
        }

        /// <summary>
        /// Gets the agency (if exists) CompanyEntity corresponding to the user's AppNexus member
        /// </summary>
        /// <param name="memberId">AppNexus member id</param>
        /// <returns>If a mapping exists, the company entity; otherwise, null.</returns>
        private CompanyEntity GetAgencyForMember(string memberId)
        {
            if (!this.MemberAgencyMappings.ContainsKey(memberId))
            {
                // No agency mapping for the member exists
                return null;
            }
            
            var agencyEntityId = this.MemberAgencyMappings[memberId];
            return this.Repository.TryGetEntity(SystemRequestContext, agencyEntityId) as CompanyEntity;
        }
    }
}
