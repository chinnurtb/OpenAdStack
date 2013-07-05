//-----------------------------------------------------------------------
// <copyright file="AppNexusThrottleFixture.cs" company="Rare Crowds Inc">
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
using System.Net;
using System.Threading;
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
    /// <summary>Test the AppNexus throttle</summary>
    [TestClass]
    public class AppNexusThrottleFixture
    {
        /// <summary>Success AppNexus response</summary>
        private const string SuccessResponse = @"
{
    ""response"":
    {
        ""status"":""OK"",
        ""id"":152492,
        ""dbg_info"":
        {
            ""instance"":""02.hbapi.sand-08.nym2"",
            ""slave_hit"":false,
            ""db"":""master"",
            ""reads"":2,
            ""read_limit"":100,
            ""read_limit_seconds"":60,
            ""writes"":4,
            ""write_limit"":60,
            ""write_limit_seconds"":60,
            ""parent_dbg_info"":
            {
                ""instance"":""04.hbapi.sand-08.lax1"",
                ""slave_hit"":false,
                ""db"":""master"",
                ""reads"":0,
                ""read_limit"":100,
                ""read_limit_seconds"":60,
                ""writes"":4,
                ""write_limit"":60,
                ""write_limit_seconds"":60,
                ""awesomesauce_cache_used"":false,
                ""time"":398.32520484924,
                ""start_microtime"":1337390247.1187,
                ""version"":""1.12.4.0""
            },
            ""awesomesauce_cache_used"":false,
            ""time"":607.74183273315,
            ""start_microtime"":1337390246.9503,
            ""version"":""1.12.4.0"",
            ""master_instance"":""04.hbapi.sand-08.lax1"",
            ""proxy"":true,
            ""master_time"":398.32520484924
        }
    }
}";

        /// <summary>NOAUTH response</summary>
        private const string NoAuthResponse = @"
{
    ""response"":
    {
        ""error_id"":""NOAUTH"",
        ""error"":""Authentication failed - not logged in"",
        ""error_description"":null,
        ""service"":null,
        ""method"":null,
        ""error_code"":null,
        ""dbg_info"":
        {
            ""instance"":""02.hbapi.sand-08.nym2"",
            ""slave_hit"":false,
            ""db"":""master"",
            ""awesomesauce_cache_used"":false,
            ""time"":57.261943817139,
            ""start_microtime"":1337400522.6696,
            ""version"":""1.12.4.0""
        }
    }
}";

        /// <summary>Response for auth requests</summary>
        private static readonly string AuthResponse = @"
{{
    ""response"":
    {{
        ""status"": ""OK"",
        ""token"": ""{0}""
    }}
}}"
            .FormatInvariant(authToken = Guid.NewGuid().ToString("N"));

        /// <summary>Token from the auth response</summary>
        private static string authToken;

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
            SimulatedPersistentDictionaryFactory.Initialize();

            this.testConfig = new CustomConfig(new Dictionary<string, string>
            {
                { "AppNexus.Endpoint", "http://localhost/api" },
                { "AppNexus.Timeout", "00:00:05" },
                { "AppNexus.Retries", "5" },
                { "AppNexus.RetryWait", "00:00:00.500" },
                { "AppNexus.Username", "username" },
                { "AppNexus.Password", "password" },
            });

            this.mockHttpClient = MockRepository.GenerateMock<IHttpClient>();
            this.mockHttpClient.Stub(f => f.BaseAddress).Return(new Uri("http://example.com/"));
            this.mockHttpClient.Stub(f => f.TransportSettings).Return(new HttpWebRequestTransportSettings());
        }

        /// <summary>Test authentication</summary>
        [TestMethod]
        public void UpdateThrottleAfterNoAuth()
        {
            // Simulate the request immediately after auth token expires
            var authorized = false;
            this.mockHttpClient.Stub(f => f.Send(Arg<HttpRequestMessage>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                    {
                        var request = call.Arguments[0] as HttpRequestMessage;
                        if (request.Uri.OriginalString.Contains("auth"))
                        {
                            authorized = true;
                            call.ReturnValue = BuildResponseMessage(AuthResponse);
                        }
                        else
                        {
                            call.ReturnValue = BuildResponseMessage(
                                authorized ? SuccessResponse : NoAuthResponse);
                        }

                        Thread.Sleep(50);
                    });

            var preRequestNextPeriodStart = AppNexusThrottle.NextPeriodStart;
            var client = new AppNexusRestClient(this.mockHttpClient, this.testConfig);
            AppNexusRestClient.AuthTokens[client.Id] = Guid.NewGuid().ToString("N");
            var result = client.Get("/path/to/object");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(SuccessResponse, result);
            Assert.IsTrue(AppNexusThrottle.NextPeriodStart > preRequestNextPeriodStart);
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
