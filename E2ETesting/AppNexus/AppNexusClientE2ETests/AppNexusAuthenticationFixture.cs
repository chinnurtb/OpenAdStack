//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthenticationFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;
using AppNexusClient;
using AppNexusTestUtilities;
using ConfigManager;
using Diagnostics;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Net;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusClientE2ETests
{
    /// <summary>Test the AppNexus authenticator</summary>
    [TestClass]
    public class AppNexusAuthenticationFixture
    {
        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void Initialize()
        {
            LogManager.Initialize(new[] { new TraceLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();
            AppNexusRestClient.AuthTokens.Clear();
        }

        /// <summary>Test authenticating to the AppNexus API service</summary>
        [TestMethod]
        public void Authenticate()
        {
            using (var client = new AppNexusRestClient(new CustomConfig()))
            {
                Assert.IsNotNull(client.Authenticate());
            }
        }

        /// <summary>Test authenticating to the AppNexus API service as an app user</summary>
        /// <remarks>Disabled pending assistance from AppNexus to fix "Invalid Signature" error</remarks>
        [TestMethod]
        public void AuthenticateAppUser()
        {
            var config = new CustomConfig(new Dictionary<string, string>
            {
                { "AppNexus.IsApp", "true" },
                { "AppNexus.App.AppId", "105" },
                { "AppNexus.App.UserId", "3275" },
                { "AppNexus.App.PrivateKey", "-----BEGIN PRIVATE KEY-----\nMIIBVgIBADANBgkqhkiG9w0BAQEFAASCAUAwggE8AgEAAkEAnadejBfx81WSaGpm\nvY5Rz7IKdyKyN9wwUrByoxe5HFDQstvqcq3d+qJPxk2R/NAc4HIlgYeCpPKhm0jL\nJw1t0wIDAQABAkEAjnGA3bds5t10UV+BwNdsV+qXxhjVSd9q0euXSIDQwiFfsRHb\nMMC718ek2SfiRHwn8TaNx+GDUQwM0/0qUK154QIhAM1QCFfIieU4AldlxR+WjLox\n3Bzj+kJe6M/meDSmGJjpAiEAxJM/2DTklLVLRiZbALiRg6vnSkafMjzbwCdN9CWq\nm1sCIFBrFrl7nTehVplxDWMwDvMncHYIfg/dKQe12EOXA29xAiEAm8SDJvRi3WP7\nzg6+tgeLZ2dk0/q6U7jd+Zorr3fZhVkCIQCCkOHn11oGvTbSoQfDgyRJnfVjX9ri\nx+9cbbohVIG3eg==\n-----END PRIVATE KEY-----" },
            });
            using (var client = new AppNexusAppRestClient(config))
            {
                    Assert.IsNotNull(client.Authenticate());
            }
        }

        /// <summary>Test that authentication is shared across instances</summary>
        [TestMethod]
        public void SingletonAuthToken()
        {
            Assert.AreEqual(0, AppNexusRestClient.AuthTokens.Count);

            using (var clientA = new AppNexusRestClient(new CustomConfig()))
            {
                var requestA = new HttpRequestMessage();
                clientA.AddAuthentication(ref requestA);
                var tokenA = requestA.Headers["Authorization"];
                Assert.IsNotNull(tokenA);

                using (var clientB = new AppNexusRestClient(new CustomConfig()))
                {
                    Assert.AreEqual(clientA.Id, clientB.Id);
                    var requestB = new HttpRequestMessage();
                    clientB.AddAuthentication(ref requestB);
                    var tokenB = requestB.Headers["Authorization"];
                    Assert.IsNotNull(tokenB);
                    Assert.AreEqual(tokenA, tokenB);
                }

                // Re-authenticate
                AppNexusRestClient.AuthTokens.Clear();
                var requestA2 = new HttpRequestMessage();
                clientA.AddAuthentication(ref requestA2);
                var tokenA2 = requestA2.Headers["Authorization"];
                Assert.IsNotNull(tokenA2);
                Assert.AreNotEqual(tokenA, tokenA2);
            }
        }

        /// <summary>
        /// Test that auth tokens for each account are separate
        /// </summary>
        [TestMethod]
        public void SeparateAuthTokensPerAccount()
        {
            const string PrivateKey = "-----BEGIN PRIVATE KEY-----\nMIIBVgIBADANBgkqhkiG9w0BAQEFAASCAUAwggE8AgEAAkEAnadejBfx81WSaGpm\nvY5Rz7IKdyKyN9wwUrByoxe5HFDQstvqcq3d+qJPxk2R/NAc4HIlgYeCpPKhm0jL\nJw1t0wIDAQABAkEAjnGA3bds5t10UV+BwNdsV+qXxhjVSd9q0euXSIDQwiFfsRHb\nMMC718ek2SfiRHwn8TaNx+GDUQwM0/0qUK154QIhAM1QCFfIieU4AldlxR+WjLox\n3Bzj+kJe6M/meDSmGJjpAiEAxJM/2DTklLVLRiZbALiRg6vnSkafMjzbwCdN9CWq\nm1sCIFBrFrl7nTehVplxDWMwDvMncHYIfg/dKQe12EOXA29xAiEAm8SDJvRi3WP7\nzg6+tgeLZ2dk0/q6U7jd+Zorr3fZhVkCIQCCkOHn11oGvTbSoQfDgyRJnfVjX9ri\nx+9cbbohVIG3eg==\n-----END PRIVATE KEY-----";
            var configA = new CustomConfig(new Dictionary<string, string>
            {
                { "AppNexus.IsApp", "true" },
                { "AppNexus.App.AppId", "105" },
                { "AppNexus.App.UserId", "3772" },
                { "AppNexus.App.PrivateKey", PrivateKey },
            });
            var configB = new CustomConfig(new Dictionary<string, string>
            {
                { "AppNexus.IsApp", "true" },
                { "AppNexus.App.AppId", "105" },
                { "AppNexus.App.UserId", "3773" },
                { "AppNexus.App.PrivateKey", PrivateKey },
            });
            var configC = new CustomConfig();

            using (IAppNexusRestClient
                clientA = new AppNexusAppRestClient(configA),
                clientB = new AppNexusAppRestClient(configB),
                clientC = new AppNexusRestClient(configC))
            {
                var memberA = clientA.Get("member");
                Assert.IsNotNull(memberA);
                Assert.IsTrue(AppNexusRestClientBase.AuthTokens.ContainsKey(clientA.Id));

                var memberB = clientB.Get("member");
                Assert.IsNotNull(memberB);
                Assert.IsTrue(AppNexusRestClientBase.AuthTokens.ContainsKey(clientB.Id));

                var memberC = clientC.Get("member");
                Assert.IsNotNull(memberC);
                Assert.IsTrue(AppNexusRestClientBase.AuthTokens.ContainsKey(clientC.Id));

                Assert.AreNotEqual(
                    AppNexusRestClientBase.AuthTokens[clientA.Id],
                    AppNexusRestClientBase.AuthTokens[clientB.Id]);

                Assert.AreNotEqual(
                    AppNexusRestClientBase.AuthTokens[clientB.Id],
                    AppNexusRestClientBase.AuthTokens[clientC.Id]);

                Assert.AreNotEqual(
                    AppNexusRestClientBase.AuthTokens[clientA.Id],
                    AppNexusRestClientBase.AuthTokens[clientC.Id]);
            }
        }

        /// <summary>Test exceeding maximum auth attempts</summary>
        [TestMethod]
        [TestCategory("NonBVT")]
        public void ExceedMaxAuthAttempts()
        {
            const int AuthLimitPeriodSeconds = 300;
            const int MaxAuthAttempts = 10;

            // Make auth requests until the expected failure occurs
            using (var client = new AppNexusRestClient(new CustomConfig()))
            {
                var attempts = 0;
                while (true)
                {
                    var attemptTime = DateTime.UtcNow;
                    try
                    {
                        attempts++;
                        client.Authenticate();
                    }
                    catch (AppNexusClientException apce)
                    {
                        // Assert that Authenticate waited before throwing
                        Assert.AreEqual(AppNexusErrorId.System, apce.ErrorId);
                        Assert.IsTrue(apce.ErrorMessage.Contains("You have exceeded your authentication limit"));
                        var periodEndTime = attemptTime.AddSeconds(AuthLimitPeriodSeconds);
                        Assert.IsTrue(DateTime.UtcNow >= periodEndTime);
                        Assert.IsTrue(attempts <= MaxAuthAttempts + 1);
                        break;
                    }
                }

                // Try authenticating now that the period has lapsed
                Assert.IsNotNull(client.Authenticate());
            }
        }
    }
}
