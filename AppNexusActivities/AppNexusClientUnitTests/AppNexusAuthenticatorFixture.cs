//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthenticatorFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net;
using AppNexusClient;
using ConfigManager;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Utilities.Net;
using Utilities.Net.Http;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusClientUnitTests
{
    /// <summary>Test the AppNexus authentication</summary>
    [TestClass]
    public class AppNexusAuthenticatorFixture
    {
        /// <summary>Mock http client used for tests</summary>
        private IHttpClient mockHttpClient;

        /// <summary>Test configuration</summary>
        private IConfig testConfig;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.testConfig = new CustomConfig(new Dictionary<string, string>
            {
                { "AppNexus.Endpoint", "http://localhost/api" },
                { "AppNexus.Timeout", "00:00:05" },
                { "AppNexus.Retries", "5" },
                { "AppNexus.RetryWait", "00:00:00.500" },
                { "AppNexus.Username", "username" },
                { "AppNexus.Password", "password" },
            });

            SimulatedPersistentDictionaryFactory.Initialize();

            this.mockHttpClient = MockRepository.GenerateMock<IHttpClient>();
            this.mockHttpClient.Stub(f => f.BaseAddress).Return(new Uri("http://example.com/"));
            this.mockHttpClient.Stub(f => f.TransportSettings).Return(new HttpWebRequestTransportSettings());
        }

        /// <summary>Test authentication</summary>
        [TestMethod]
        public void Authenticate()
        {
            var authResponseJsonFormat = @"
{{
  ""response"": {{
    ""status"": ""OK"",
    ""token"": ""{0}""
  }}
}}";
            var authToken = Guid.NewGuid().ToString("n");
            var authResponseJson = string.Format(
                CultureInfo.InvariantCulture,
                authResponseJsonFormat,
                authToken);

            this.mockHttpClient.Stub(f => f.Send(Arg<HttpRequestMessage>.Is.Anything))
                .Return(BuildResponseMessage(authResponseJson));

            var client = new AppNexusRestClient(this.mockHttpClient, this.testConfig);
            var token = client.Authenticate();
            Assert.AreEqual(authToken, token);
        }

        /// <summary>
        /// Builds an HttpResponseMessage with the specified content
        /// </summary>
        /// <param name="content">Content to include in the response</param>
        /// <returns>The built response</returns>
        private static HttpResponseMessage BuildResponseMessage(string content)
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpContent.Create(content)
            };
        }
    }
}
