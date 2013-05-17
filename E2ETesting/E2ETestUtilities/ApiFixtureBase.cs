//-----------------------------------------------------------------------
// <copyright file="ApiFixtureBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace E2ETestUtilities
{
    /// <summary>Base class for API test fixtures</summary>
    [TestClass]
    public class ApiFixtureBase
    {
        /// <summary>How long to wait before following redirects</summary>
        protected const int FollowRedirectWait = 3000;

        /// <summary>The name identifier claim</summary>
        private const string NameIdentifierClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        /// <summary>Gets the test REST client</summary>
        protected RestTestClient RestClient { get; private set; }

        /// <summary>Gets the named identifier claim value</summary>
        private static string NamedIdentifierClaimValue
        {
            get { return ConfigurationManager.AppSettings["TestUserNamedIdentifier"]; }
        }

        /// <summary>Per-test case initialization</summary>
        [TestInitialize]
        public void Initialize()
        {
            this.RestClient = new RestTestClient("https://localhost/api/");
            this.RestClient.Claims[NameIdentifierClaim] = NamedIdentifierClaimValue;
        }

        /// <summary>Creates company and returns its EntityId</summary>
        /// <param name="companyName">Company name</param>
        /// <param name="companyType">Company type ("Agency" or "Advertiser")</param>
        /// <returns>The created company's EntityId</returns>
        protected string CreateCompany(string companyName, string companyType)
        {
            var newCompany = new Dictionary<string, object>
            {
                { "EntityCategory", "Company" },
                { "ExternalName", companyName },
                { "ExternalType", companyType },
                { "Properties", new Dictionary<string, object> { } }
            };
            var newCompanyJson = JsonConvert.SerializeObject(newCompany);
            var createCompanyResponse =
                this.RestClient.SendRequest(
                    HttpMethod.POST,
                    "entity/company",
                    newCompanyJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Company");
            var createdCompany = createCompanyResponse
                .TryDeserializeContentJson()["Company"]
                as IDictionary<string, object>;
            return (string)createdCompany["ExternalEntityId"];
        }

        /// <summary>Creates a campaign and returns its EntityId</summary>
        /// <param name="campaignName">Campaign name</param>
        /// <param name="campaignType">Campaign type</param>
        /// <param name="advertiserEntityId">Advertiser EntityId</param>
        /// <returns>The created campaign's EntityId</returns>
        protected string CreateCampaign(string campaignName, string campaignType, string advertiserEntityId)
        {
            var newCampaign = new Dictionary<string, object>
                {
                    { "EntityCategory", "Campaign" },
                    { "ExternalName", campaignName },
                    { "ExternalType", campaignType },
                    {
                        "Properties",
                        new Dictionary<string, object>
                        {
                            { "Status", "Draft" },
                            { "Budget", "12345" },
                            { "StartDate", "2012-07-23T16:25:00.0000000Z" },
                            { "EndDate", "2012-08-07T16:25:00.0000000Z" }
                        }
                    }
                };
            var campaignJson = JsonConvert.SerializeObject(newCampaign);
            var createCampaignUrl =
                "entity/company/{0}/campaign"
                .FormatInvariant(advertiserEntityId);
            var createCampaignResponse =
                this.RestClient.SendRequest(HttpMethod.POST, createCampaignUrl, campaignJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Campaign");
            var campaign = createCampaignResponse
                .TryDeserializeContentJson()["Campaign"]
                as IDictionary<string, object>;
            return (string)campaign["ExternalEntityId"];
        }
    }
}
