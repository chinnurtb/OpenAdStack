//-----------------------------------------------------------------------
// <copyright file="CompanyFixture.cs" company="Rare Crowds Inc">
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
using System.Net;
using E2ETestUtilities;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace ApiIntegrationTests
{
    /// <summary>Tests for company API</summary>
    [TestClass]
    public class CompanyFixture : ApiFixtureBase
    {
        /// <summary>Roundtrip a company</summary>
        [TestMethod]
        public void RoundtripCompany()
        {
            var companyName = Guid.NewGuid().ToString("n");
            var newCompany = new Dictionary<string, object>
            {
                { "EntityCategory", "Company" },
                { "ExternalName", companyName },
                { "ExternalType", "Agency" },
                { "Properties", new Dictionary<string, object> { } }
            };
            var newCompanyJson = JsonConvert.SerializeObject(newCompany);
            var createCompanyResponse = 
                this.RestClient.SendRequest(HttpMethod.POST, "entity/company", newCompanyJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Company")
                .AssertContainsJsonValue("EntityCategory", "Company")
                .AssertContainsJsonValue("ExternalName", companyName)
                .AssertContainsJsonValue("ExternalType", "Agency")
                .AssertContainsJsonValue("ExternalEntityId");
            var createdCompany = createCompanyResponse
                .TryDeserializeContentJson()["Company"]
                as IDictionary<string, object>;
            var companyEntityId = createdCompany["ExternalEntityId"];

            // Get the created company
            var getCompanyUrl =
                "entity/company/{0}"
                .FormatInvariant(companyEntityId);
            var getCompanyResponse =
                this.RestClient.SendRequest(HttpMethod.GET, getCompanyUrl)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Company")
                .AssertContainsJsonValue("EntityCategory", "Company")
                .AssertContainsJsonValue("ExternalName", companyName)
                .AssertContainsJsonValue("ExternalType", "Agency")
                .AssertContainsJsonValue("ExternalEntityId", companyEntityId);
        }

        /// <summary>Test getting all companies</summary>
        /// <remarks>
        /// Likely there will be additional companies created by other
        /// tests returned as well as those created by this test.
        /// </remarks>
        [TestMethod]
        public void GetCompanies()
        {
            var companyNames = new[] { Guid.NewGuid().ToString("n"), Guid.NewGuid().ToString("n"), Guid.NewGuid().ToString("n") };
            var companyEntityIds = new List<string>();
            foreach (var companyName in companyNames)
            {
                var newCompany = new Dictionary<string, object>
                {
                    { "EntityCategory", "Company" },
                    { "ExternalName", companyName },
                    { "ExternalType", "Agency" },
                    { "Properties", new Dictionary<string, object> { } }
                };
                var newCompanyJson = JsonConvert.SerializeObject(newCompany);
                var createCompanyResponse =
                    this.RestClient.SendRequest(HttpMethod.POST, "entity/company", newCompanyJson)
                    .AssertIsValidRedirect()
                    .Sleep(FollowRedirectWait)
                    .FollowIfRedirect(this.RestClient)
                    .AssertStatusCode(HttpStatusCode.OK)
                    .AssertContentIsJson()
                    .AssertContainsJsonValue("Company")
                    .AssertContainsJsonValue("ExternalName", companyName);
                var company = createCompanyResponse
                    .TryDeserializeContentJson()["Company"]
                    as IDictionary<string, object>;
                companyEntityIds.Add((string)company["ExternalEntityId"]);
            }

            // Get all companies for user and verify it contains the created company
            var getCompaniesResponse =
                this.RestClient.SendRequest(HttpMethod.GET, "entity/company/")
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Companies");

            foreach (var companyName in companyNames)
            {
                getCompaniesResponse.AssertContainsJsonValue("ExternalName", companyName);
            }

            foreach (var companyEntityId in companyEntityIds)
            {
                getCompaniesResponse.AssertContainsJsonValue("ExternalEntityId", companyEntityId);
            }
        }

        /// <summary>Test creating and associating an agency and advertiser</summary>
        [TestMethod]
        public void AssociateCompanies()
        {
            // Create agency and advertiser companies
            var agencyName = Guid.NewGuid().ToString("n");
            var advertiserName = Guid.NewGuid().ToString("n");
            var agencyEntityId = CreateCompany(agencyName, "Agency");
            var advertiserEntityId = CreateCompany(advertiserName, "Advertiser");

            // Add the advertiser to the agency
            var advertiserAssociationLabel = "AdvertiserA";
            var addAdvertiserUrl =
                "entity/company/{0}?Message=AddAdvertiser"
                .FormatInvariant(agencyEntityId);
            
            // TODO: Update once AddAdvertiser has been updated to use new payload
            /*
            var addAdvertiserJson =
                @"{{ ""Label"":""{0}"", ""Company"":""{1}"" }}"
                .FormatInvariant(
                    advertiserAssociationLabel,
                    advertiserEntityId);
            */
            var addAdvertiserJson =
                @"{{ ""AssociationName"":""{0}"", ""ParentEntity"":""{1}"", ""ChildEntity"":""{2}"" , ""AssociationType"":""Child"" }}"
                .FormatInvariant(
                    advertiserAssociationLabel,
                    agencyEntityId,
                    advertiserEntityId);

            var addAdvertiserResponse =
                this.RestClient.SendRequest(HttpMethod.POST, addAdvertiserUrl, addAdvertiserJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson();

            // TODO: Redirect will be updated to go to the message status instead of the updated agency company
            /*
            addAdvertiserResponse
                .AssertContainsJsonValue("Message") 
                .AssertContainsJsonValue("Something", agencyEntityId)
                .AssertContainsJsonValue("AnotherThing", agencyName)
                .AssertContainsJsonValue("StatusThing");
            */
            addAdvertiserResponse
                .AssertContainsJsonValue("Company") 
                .AssertContainsJsonValue("ExternalEntityId", agencyEntityId)
                .AssertContainsJsonValue("ExternalName", agencyName);
        }

        /// <summary>Test creating and then updating a company</summary>
        [TestMethod]
        public void UpdateCompany()
        {
            var companyName = Guid.NewGuid().ToString("n");
            var propertyX = Guid.NewGuid();
            var newCompany = new Dictionary<string, object>
            {
                { "EntityCategory", "Company" },
                { "ExternalName", companyName },
                { "ExternalType", "Agency" },
                { "Properties", new Dictionary<string, object> { } },
                {
                    "SystemProperties",
                    new Dictionary<string, object>
                    {
                        { "PropertyX", propertyX }
                    }
                }
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
                .AssertContainsJsonValue("Company")
                .AssertContainsJsonValue("EntityCategory", "Company")
                .AssertContainsJsonValue("ExternalName", companyName)
                .AssertContainsJsonValue("ExternalType", "Agency")
                .AssertContainsJsonValue("ExternalEntityId");

            /*
             * TODO: Uncomment once getting entities with SystemProperties is supported
             * 
            createCompanyResponse
                .AssertContainsJsonValue("SystemProperties")
                .AssertContainsJsonValue("PropertyX", propertyX);
             */
            
            // Get the created company from the response and update it
            var company = createCompanyResponse.TryDeserializeContentJson()["Company"] as IDictionary<string, object>;
            var companyEntityId = company["ExternalEntityId"];
            company["Properties"] = new Dictionary<string, object>
            {
                { "Address", "2018 156th Ave NE" },
                { "Address2", "STE {0}".FormatInvariant(new Random().Next(9999)) },
                { "City", Guid.NewGuid().ToString("n") },
                { "State", "WA" },
                { "PostalCode", "98{0}".FormatInvariant(new Random().Next(999)) }
            };

            var updateCompanyResponse =
                this.RestClient.SendRequest(
                    HttpMethod.PUT,
                    "entity/company/" + companyEntityId,
                    JsonConvert.SerializeObject(company))
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Company")
                .AssertContainsJsonValue("EntityCategory", "Company")
                .AssertContainsJsonValue("ExternalName", companyName)
                .AssertContainsJsonValue("ExternalType", "Agency")
                .AssertContainsJsonValue("ExternalEntityId", company["ExternalEntityId"]);

            /*
             * TODO: Uncomment once getting entities with SystemProperties is supported
             * 
            updateCompanyResponse
                .AssertContainsJsonValue("SystemProperties")
                .AssertContainsJsonValue("PropertyX", propertyX);
            foreach (var property in company["Properties"] as IDictionary<string, object>)
            {
                updateCompanyResponse.AssertContainsJsonValue(property.Key, property.Value);
            }
             */
        }
    }
}
