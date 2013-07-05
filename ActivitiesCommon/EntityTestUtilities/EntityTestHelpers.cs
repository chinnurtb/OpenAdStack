//-----------------------------------------------------------------------
// <copyright file="EntityTestHelpers.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using EntityUtilities;

namespace EntityTestUtilities
{
    /// <summary>
    /// Helpers for the activity unit tests
    /// </summary>
    public static class EntityTestHelpers
    {
        /// <summary>
        /// Returns a new entity id string
        /// </summary>
        /// <returns>The id string</returns>
        public static string NewEntityIdString()
        {
            return new EntityId().ToString();
        }

        /// <summary>
        /// Creates Company json for tests
        /// </summary>
        /// <param name="companyEntityId">Company Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>Company json</returns>
        public static string CreateCompanyJson(string companyEntityId, string externalName)
        {
            return
@"{{
    ""ExternalEntityId"":""{0}"",
    ""ExternalName"":""{1}""
}}"
            .FormatInvariant(companyEntityId, externalName);
        }

        /// <summary>
        /// Creates Company json for tests
        /// </summary>
        /// <param name="userEntityId">The user's ExternalEntityId</param>
        /// <param name="userId">The user's UserId</param>
        /// <param name="contactEmail">Contact Email</param>
        /// <returns>User json</returns>
        public static string CreateUserJson(string userEntityId, string userId, string contactEmail)
        {
            return
@"{{
    ""ExternalEntityId"":""{0}"",
    ""Properties"":
      {{
        ""UserId"":""{1}"",
        ""ContactEmail"":""{2}""
      }}
}}"
            .FormatInvariant(userEntityId, userId, contactEmail);
        }

        /// <summary>
        /// Creates campaign json for tests
        /// </summary>
        /// <param name="campaignEntityId">The campaign Id</param>
        /// <param name="externalName">The external name</param>
        /// <param name="budget">The budget</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="personaName">The persona name</param>
        /// <returns>Campaign json</returns>
        public static string CreateCampaignJson(string campaignEntityId, string externalName, long budget, DateTime startDate, DateTime endDate, string personaName)
        {
            return
@"{{
    ""ExternalEntityId"":""{0}"",
    ""ExternalName"":""{1}"",
    ""LastModifiedDate"":""{6}"",
    ""Properties"":
    {{
        ""Budget"":{2},
        ""EndDate"":""{3}"",
        ""PersonaName"":""{4}"",
        ""StartDate"":""{5}""
    }}
}}"
            .FormatInvariant(
                campaignEntityId,
                externalName,
                budget,
                (PropertyValue)endDate,
                personaName,
                (PropertyValue)startDate,
                (PropertyValue)DateTime.UtcNow);
        }

        /// <summary>
        /// Creates creative entity json
        /// </summary>
        /// <param name="creativeEntityId">Creative Id</param>
        /// <param name="externalName">Creative external name</param>
        /// <param name="creativeAdTag">Creative third party ad tag</param>
        /// <param name="width">Creative width</param>
        /// <param name="height">Creative height</param>
        /// <param name="appNexusId">APNX CreativeId</param>
        /// <returns>Creative Json</returns>
        public static string CreateCreativeJson(string creativeEntityId, string externalName, string creativeAdTag, int width, int height, int appNexusId)
        {
            return
(@"{{
    ""ExternalEntityId"":""{0}"",
    ""ExternalName"":""{1}"",
    ""Properties"":
    {{
        ""Tag"":""{2}"",
        ""Width"":{3},"
+ (appNexusId > 0 ? @"
        ""APNX_CreativeId"":""{5}"",
" : "\n")
+ @"""Height"":{4}
    }}
}}")
            .FormatInvariant(creativeEntityId, externalName, creativeAdTag, width, height, appNexusId);
        }

        /// <summary>
        /// Creates a test Company entity with the specified values
        /// </summary>
        /// <param name="companyEntityId">Company Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>The Company company entity</returns>
        public static CompanyEntity CreateTestCompanyEntity(string companyEntityId, string externalName)
        {
            return new CompanyEntity(
                new EntityId(companyEntityId),
                new Entity { ExternalName = externalName, LocalVersion = 1 });
        }

        /// <summary>
        /// Creates a test partner entity with the specified values
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>The partner entity</returns>
        public static PartnerEntity CreateTestPartnerEntity(string entityId, string externalName)
        {
            return new PartnerEntity(
                new EntityId(entityId),
                new Entity { ExternalName = externalName, LocalVersion = 1 });
        }

        /// <summary>
        /// Creates a test user entity with the specified values
        /// </summary>
        /// <param name="userEntityId">The user's ExternalEntityId</param>
        /// <param name="userId">The user's UserId</param>
        /// <param name="contactEmail">Contact Email</param>
        /// <returns>The user entity</returns>
        public static UserEntity CreateTestUserEntity(string userEntityId, string userId, string contactEmail)
        {
            return EntityJsonSerializer.DeserializeUserEntity(new EntityId(userEntityId), CreateUserJson(userEntityId, userId, contactEmail));
        }

        /// <summary>
        /// Creates a test campaign entity with the specified values
        /// </summary>
        /// <param name="campaignEntityId">The campaign Id</param>
        /// <param name="externalName">The external name</param>
        /// <param name="budget">The budget</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="personaName">The persona name</param>
        /// <returns>Campaign json</returns>
        public static CampaignEntity CreateTestCampaignEntity(string campaignEntityId, string externalName, long budget, DateTime startDate, DateTime endDate, string personaName)
        {
            return EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(campaignEntityId), CreateCampaignJson(campaignEntityId, externalName, budget, startDate, endDate, personaName));
        }

        /// <summary>
        /// Creates a test creative entity
        /// </summary>
        /// <param name="creativeEntityId">Creative Id</param>
        /// <param name="externalName">Creative external name</param>
        /// <param name="creativeAdTag">Creative third party ad tag</param>
        /// <returns>Creative entity</returns>
        public static CreativeEntity CreateTestCreativeEntity(string creativeEntityId, string externalName, string creativeAdTag)
        {
            return CreateTestCreativeEntity(creativeEntityId, externalName, creativeAdTag, 0, 0);
        }

        /// <summary>
        /// Creates a test creative entity
        /// </summary>
        /// <param name="creativeEntityId">Creative Id</param>
        /// <param name="externalName">Creative external name</param>
        /// <param name="creativeAdTag">Creative third party ad tag</param>
        /// <param name="width">Creative width</param>
        /// <param name="height">Creative height</param>
        /// <returns>Creative entity</returns>
        public static CreativeEntity CreateTestCreativeEntity(string creativeEntityId, string externalName, string creativeAdTag, int width, int height)
        {
            return CreateTestCreativeEntity(creativeEntityId, externalName, creativeAdTag, width, height, -1);
        }

        /// <summary>
        /// Creates a test creative entity
        /// </summary>
        /// <param name="creativeEntityId">Creative Id</param>
        /// <param name="externalName">Creative external name</param>
        /// <param name="creativeAdTag">Creative third party ad tag</param>
        /// <param name="width">Creative width</param>
        /// <param name="height">Creative height</param>
        /// <param name="appNexusId">APNX CreativeId</param>
        /// <returns>Creative entity</returns>
        public static CreativeEntity CreateTestCreativeEntity(string creativeEntityId, string externalName, string creativeAdTag, int width, int height, int appNexusId)
        {
            return EntityJsonSerializer.DeserializeCreativeEntity(new EntityId(), CreateCreativeJson(creativeEntityId, externalName, creativeAdTag, width, height, appNexusId));
        }

        /// <summary>Creates a test image ad creative entity</summary>
        /// <param name="entityId">Creative EntityId</param>
        /// <param name="externalName">Creative ExternalName</param>
        /// <param name="width">Creative width</param>
        /// <param name="height">Creative height</param>
        /// <param name="clickUrl">Creative click URL</param>
        /// <param name="imageBytes">Creative image bytes</param>
        /// <returns>Creative entity</returns>
        [SuppressMessage("Microsoft.Design", "CA1054", Justification = "URL is stored as string")]
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Bytes is bytes")]
        public static CreativeEntity CreateTestImageAdCreativeEntity(
            EntityId entityId,
            string externalName,
            int width,
            int height,
            string clickUrl,
            byte[] imageBytes)
        {
            var base64ImageBytes = Convert.ToBase64String(imageBytes);
            var rawEntity = new Entity
            {
                ExternalName = externalName,
                ExternalType = CreativeType.ImageAd.ToString(),
                LocalVersion = 1,
                Properties =
                {
                    new EntityProperty(DeliveryNetworkEntityProperties.Creative.Width, new PropertyValue(PropertyType.Double, (double)width)),
                    new EntityProperty(DeliveryNetworkEntityProperties.Creative.Height, new PropertyValue(PropertyType.Double, (double)height)),
                    new EntityProperty(DeliveryNetworkEntityProperties.Creative.ClickUrl, new PropertyValue(PropertyType.String, clickUrl)),
                    new EntityProperty(DeliveryNetworkEntityProperties.Creative.ImageBytes, new PropertyValue(PropertyType.String, base64ImageBytes)),
                }
            };
            return new CreativeEntity(entityId, rawEntity);
        }
    }
}
