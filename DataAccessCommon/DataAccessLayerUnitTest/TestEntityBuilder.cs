// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestEntityBuilder.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;

namespace DataAccessLayerUnitTests
{
    /// <summary>Helper class to build test entities.</summary>
    public static class TestEntityBuilder
    {
        /// <summary>External Name.</summary>
        public const string ExternalName = "MyFooThingy";

        /// <summary>External Type.</summary>
        public const string ExternalType = "FooThingy";

        /// <summary>PersonaName of a campaign.</summary>
        public const string PersonaName = "SuperShopper";

        /// <summary>Budget a campaign.</summary>
        public const long Budget = 1234567890;

        /// <summary>StartDate of a campaign.</summary>
        public static readonly DateTime StartDate = StringConversions.StringToNativeDateTime("1800-01-01T08:00:00.0000000Z");

        /// <summary>StartDate of a campaign.</summary>
        public static readonly DateTime EndDate = StringConversions.StringToNativeDateTime("9999-12-31T23:59:59.0000000Z");

        /// <summary>ExternalType of an 'Agency' company.</summary>
        public const string AgencyExternalType = "Company.Agency";

        /// <summary>Wrapped entity UserId property value.</summary>
        public const string UserId = "abc123";

        /// <summary>Wrapped entity FullName property value.</summary>
        public const string FullName = "Full Name";

        /// <summary>Wrapped entity ContactEmail property value.</summary>
        public const string ContactEmail = "contact@email.com";

        /// <summary>Wrapped entity FirstName property value.</summary>
        public const string FirstName = "first";

        /// <summary>Wrapped entity LastName property value.</summary>
        public const string LastName = "last";

        /// <summary>Wrapped entity ContactPhone property value.</summary>
        public const string ContactPhone = "123-456-7890";

        /// <summary>Helper method to build a campaign entity.</summary>
        /// <returns>A CampaignEntity.</returns>
        public static CampaignEntity BuildCampaignEntity()
        {
            return BuildCampaignEntity(new EntityId());
        }
        
        /// <summary>Helper method to build a campaign entity.</summary>
        /// <param name="externalEntityId">Entity Id</param>
        /// <returns>A CampaignEntity</returns>
        public static CampaignEntity BuildCampaignEntity(EntityId externalEntityId)
        {
            // Set up campaign entity to save (note the date range Azure accepts is not quite the maxrange for DateTime)
            return new CampaignEntity(
                externalEntityId,
                new Entity
                {
                    ExternalName = "MyFooThingy",
                    Properties =
                    {
                        new EntityProperty("Budget", new PropertyValue(PropertyType.Int64, "1234567890")),
                        new EntityProperty("StartDate", new PropertyValue(PropertyType.Date, "1800-01-01T08:00:00.0000000Z")),
                        new EntityProperty("EndDate", new PropertyValue(PropertyType.Date, "9999-12-31T23:59:59.0000000Z")),
                        new EntityProperty("PersonaName", new PropertyValue(PropertyType.String, "SuperShopper"))
                    }
                });
        }

        /// <summary>Helper method to build a user entity.</summary>
        /// <returns>A UserEntity.</returns>
        public static UserEntity BuildUserEntity()
        {
            return BuildUserEntity(new EntityId());
        }

        /// <summary>Helper method to build a user entity.</summary>
        /// <param name="externalEntityId">Entity Id</param>
        /// <returns>A UserEntity.</returns>
        public static UserEntity BuildUserEntity(EntityId externalEntityId)
        {
            return new UserEntity(
                externalEntityId,
                new Entity
                {
                    Properties =
                    {
                        new EntityProperty("UserId", "abc123"),
                        new EntityProperty("FullName", "Full Name"),
                        new EntityProperty("FirstName", "first"),
                        new EntityProperty("LastName", "last"),
                        new EntityProperty("ContactEmail", "contact@email.com"),
                        new EntityProperty("ContactPhone", "123-456-7890"),
                    }
                });
        }

        /// <summary>Helper method to build a company entity.</summary>
        /// <returns>A CompanyEntity.</returns>
        public static CompanyEntity BuildCompanyEntity()
        {
            return BuildCompanyEntity(new EntityId());
        }

        /// <summary>Helper method to build a company entity.</summary>
        /// <param name="externalEntityId">Entity Id</param>
        /// <returns>A CompanyEntity.</returns>
        public static CompanyEntity BuildCompanyEntity(EntityId externalEntityId)
        {
            return new CompanyEntity(
                externalEntityId,
                new Entity
                {
                    ExternalName = "MyFooThingy",
                    ExternalType = "Company.Agency"
                });
        }

        /// <summary>Helper method to build a creative entity.</summary>
        /// <returns>A CreativeEntity.</returns>
        public static CreativeEntity BuildCreativeEntity()
        {
            return BuildCreativeEntity(new EntityId());
        }

        /// <summary>Helper method to build a creative entity.</summary>
        /// <param name="externalEntityId">Entity Id</param>
        /// <returns>A CreativeEntity.</returns>
        public static CreativeEntity BuildCreativeEntity(EntityId externalEntityId)
        {
            return new CreativeEntity(
                externalEntityId,
                new Entity
                {
                    ExternalName = "MyFooThingy"
                });
        }

        /// <summary>Helper method to build a partner entity.</summary>
        /// <returns>A PartnerEntity.</returns>
        public static PartnerEntity BuildPartnerEntity()
        {
            return BuildPartnerEntity(new EntityId());
        }

        /// <summary>Helper method to build a partner entity.</summary>
        /// <param name="externalEntityId">Entity Id</param>
        /// <returns>A PartnerEntity.</returns>
        public static PartnerEntity BuildPartnerEntity(EntityId externalEntityId)
        {
            return new PartnerEntity(
                externalEntityId,
                new Entity
                {
                    ExternalName = "MyFooThingy",
                    ExternalType = "FooThingy"
                });
        }
    }
}
