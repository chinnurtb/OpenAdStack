//-----------------------------------------------------------------------
// <copyright file="NewAppUserActivity.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using EntityActivities;
using EntityUtilities;
using Newtonsoft.Json;
using Utilities.Storage;

namespace AppNexusActivities.AppActivities
{
    /// <summary>
    /// Activity for setting up new AppNexus App users
    /// </summary>
    /// <remarks>
    /// Creates an Agency CompanyEntity for the user's AppNexus member if needed.
    /// Adds the user's AppNexus member Agency CompanyEntity to their ACL.
    /// RequiredValues:
    ///   EntityId - The EntityId of the UserEntity
    /// </remarks>
    [Name(AppNexusActivityTasks.NewAppUser)]
    [RequiredValues(EntityActivityValues.EntityId)]
    public class NewAppUserActivity : AppNexusActivity
    {
        /// <summary>Backing field for MemberAgencyMappings</summary>
        private IPersistentDictionary<string> memberAgencyMappings;

        /// <summary>Gets the activity's runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.Background; }
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

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            var context = CreateRepositoryContext(RepositoryContextType.InternalEntitySave, request, null);

            // Get the user freshly created by SaveUserActivity
            var userEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
            UserEntity user = null;
            var retries = 5;
            while (user == null)
            {
                // NOTE: There is a known issue with entities not being immediately available
                // NOTE: particularly just when a worker role has started up.
                // NOTE: As a work-around, get the user in a retry loop.
                user = this.Repository.TryGetEntity(context, userEntityId) as UserEntity;
                if (user == null)
                {
                    if (--retries > 0)
                    {
                        System.Threading.Thread.Sleep(500);
                        continue;
                    }

                    return UserNotFoundError(userEntityId.ToString());
                }
            }

            // Check that user is actually an AppNexusApp user
            if (user.GetUserType() != UserType.AppNexusApp)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Activity not supported for non-AppNexusApp users");
            }

            CompanyEntity agency = null;
            using (var client = CreateAppNexusClient(user.UserId))
            {
                // Lookup user's member
                var member = client.GetMember();
                var memberId = member[AppNexusValues.Id].ToString();

                // Lookup the Agency EntityId for the member
                agency = this.GetAgencyForMember(context, memberId);
                if (agency == null)
                {
                    // Create an agency CompanyEntity for the member
                    agency = CreateAgencyForMember(member);
                    agency.SetAppNexusMemberId(Convert.ToInt32(memberId, CultureInfo.InvariantCulture));

                    // Add the app user id to the agency as a default
                    var agencyConfig = agency.GetConfigSettings();
                    agencyConfig["AppNexus.App.UserId"] = user.UserId.ToString();
                    agency.SetConfigSettings(agencyConfig);

                    // Save the new agency
                    this.Repository.AddCompany(context, agency);

                    // Add a mapping for the member agency
                    this.MemberAgencyMappings[memberId] = agency.ExternalEntityId.ToString();

                    LogManager.Log(
                        LogLevels.Trace,
                        "Created agency company '{0}' for AppNexus member '{1}' ({2})",
                        agency.ExternalEntityId,
                        memberId,
                        agency.ExternalName);
                }
            }

            // Add the member agency company to the user's ACL
            this.UserAccessRepository.AddUserAccessList(
                userEntityId,
                new[] { "COMPANY:{0}:#:*".FormatInvariant(agency.ExternalEntityId) });

            // Associate the user with the company
            // Note: "company" will be replaced later with a constant, probably something like UserEntity.CompanyAssociationName
            // var userId = new EntityId(request.Values["AuthUserId"]);
            this.Repository.AssociateEntities(context, user.ExternalEntityId, "company", new HashSet<IEntity> { agency });

            LogManager.Log(
                LogLevels.Trace,
                "Added access for user '{0}' to company '{1}'",
                user.ExternalEntityId,
                agency.ExternalEntityId);

            // Return the creative ID to the activity request source
            return this.SuccessResult();
        }

        /// <summary>Creates an agency CompanyEntity for the AppNexus member</summary>
        /// <param name="member">AppNexus member</param>
        /// <returns>The agency CompanyEntity</returns>
        private static CompanyEntity CreateAgencyForMember(IDictionary<string, object> member)
        {
            var agencyJson = JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "EntityCategory", "Company" },
                    { "ExternalName", member[AppNexusValues.Name] },
                    { "ExternalType", "Agency" },
                    { "Properties", new Dictionary<string, object> { } }
                });
            var agency = EntityJsonSerializer.DeserializeCompanyEntity(new EntityId(), agencyJson);
            agency.SetAppNexusMemberId((int)member[AppNexusValues.Id]);
            return agency;
        }

        /// <summary>Gets the agency CompanyEntity for the AppNexus member</summary>
        /// <param name="context">Repository context</param>
        /// <param name="memberId">AppNexus member id</param>
        /// <returns>If found, the agency CompanyEntity; otherwise, null.</returns>
        private CompanyEntity GetAgencyForMember(RequestContext context, string memberId)
        {
            string memberAgencyEntityId = null;
            if (this.MemberAgencyMappings.TryGetValue(memberId, out memberAgencyEntityId))
            {
                return this.Repository.TryGetEntity(context, new EntityId(memberAgencyEntityId)) as CompanyEntity;
            }

            return null;
        }
    }
}
