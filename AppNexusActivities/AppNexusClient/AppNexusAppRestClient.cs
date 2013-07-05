//-----------------------------------------------------------------------
// <copyright file="AppNexusAppRestClient.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using ConfigManager;
using Diagnostics;
using Microsoft.Http;
using Utilities.Cryptography;
using Utilities.Net;
using Utilities.Net.Http;
using Utilities.Storage;

namespace AppNexusClient
{
    /// <summary>REST client for AppNexus using credentials from config</summary>
    [SuppressMessage("Microsoft.Design", "CA1063", Justification = "IDisposable is correctly implemented by HttpRestClient")]
    public class AppNexusAppRestClient : AppNexusRestClientBase, IAppNexusRestClient
    {
        /// <summary>URI for the auth request</summary>
        private const string AuthRequestUriFormat = "auth?user_id={0}&plugin_id={1}&signature={2}";

        /// <summary>Backing field for AppUserId. DO NOT USE DIRECTLY.</summary>
        private string appUserId;

        /// <summary>Backing field for CryptoProvider. DO NOT USE DIRECTLY.</summary>
        private ICryptoProvider cryptoProvider;

        /// <summary>Initializes a new instance of the AppNexusAppRestClient class.</summary>
        /// <param name="config">Configuration to use</param>
        public AppNexusAppRestClient(IConfig config)
            : base(config)
        {
        }

        /// <summary>Gets a string identifying this AppNexus client</summary>
        public override string Id
        {
            get { return this.AppUserId; }
        }

        /// <summary>Gets the AppNexus App UserId</summary>
        private string AppUserId
        {
            get { return this.appUserId = this.appUserId ?? this.Config.GetValue("AppNexus.App.UserId"); }
        }

        /// <summary>Gets the AppNexus App Id</summary>
        private string AppId
        {
            get { return this.Config.GetValue("AppNexus.App.AppId"); }
        }

        /// <summary>Gets the crypto provider</summary>
        private ICryptoProvider CryptoProvider
        {
            get { return this.cryptoProvider = this.cryptoProvider ?? CreateCryptoProvider(); }
        }

        /// <summary>Authenticates the REST client</summary>
        /// <returns>The new authentication token</returns>
        internal override string Authenticate()
        {
            var signature = this.CreateEncryptedSignature();
            var authRequestUri = AuthRequestUriFormat.FormatInvariant(this.AppUserId, this.AppId, signature);
            var httpResponse = this.Client.Send(new HttpRequestMessage
            {
                Uri = new Uri(this.Client.BaseAddress, authRequestUri),
                Method = HttpMethod.GET.ToString(),
            });

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new AppNexusClientException(
                    "AppNexus Authentication Failed: Status Code: {0}".FormatInvariant(httpResponse.StatusCode),
                    httpResponse.Content.ReadAsString());
            }

            var responseContent = httpResponse.Content.ReadAsString();
            var values = TryGetResponseValues(responseContent);
            if (values == null)
            {
                throw new AppNexusClientException(
                    "Authentication Failed: Unable to get values from response",
                    responseContent);
            }

            if (IsErrorResponse(values))
            {
                var exception = new AppNexusClientException(
                    "AppNexus Authentication Failed",
                    responseContent,
                    values);
                if (exception.ErrorId == AppNexusErrorId.System &&
                    exception.ErrorMessage.Contains(AuthLimitExceededErrorMessage))
                {
                    // Authentication limit has been exceeded
                    // Sleep until the auth limit period has passed
                    Thread.Sleep(AuthLimitPeriodSeconds * 1000);
                }

                throw exception;
            }

            if (!values.ContainsKey(AppNexusValues.AuthToken))
            {
                throw new AppNexusClientException(
                    "AppNexus Authentication Failed (response did not contain an auth token)",
                    responseContent,
                    values);
            }

            return values[AppNexusValues.AuthToken] as string;
        }

        /// <summary>Create the crypto provider</summary>
        /// <remarks>
        /// TODO: Create and use a static CryptoManager to get the ICryptoProvider instance
        /// registered in the RunTimeIoc and resolved in the role initialization.
        /// </remarks>
        /// <returns>The crypto provider</returns>
        private static ICryptoProvider CreateCryptoProvider()
        {
            return new Utilities.Cryptography.OpenSsl.OpenSslCryptoProvider();
        }

        /// <summary>Gets the AppNexus App private key from config</summary>
        /// <remarks>
        /// TODO: Retrieve the key from the system certificate store instead.
        /// </remarks>
        /// <returns>A container with the private key</returns>
        private IKeyContainer ReadPrivateKeyFromConfig()
        {
            var privateKeyPem = this.Config.GetValue("AppNexus.App.PrivateKey");
            var keyContainer = this.CryptoProvider.CreatePemKeyContainer();
            keyContainer.ReadPem(privateKeyPem);
            return keyContainer;
        }

        /// <summary>Creates a base-64, encrypted signature for AppNexus auth</summary>
        /// <remarks>
        /// Combines seconds since unix epoch with the user's AppNexus user id and signs it with
        /// the private key corresponding to the public key that has been provided to AppNexus.
        /// </remarks>
        /// <seealso href="https://wiki.appnexus.com/display/apps/3.2+-+Get+your+app+on+Sand+and+Authenticate+with+AppNexus#3.2-GetyourapponSandandAuthenticatewithAppNexus-ObtaininganAuthenticationToken"/>
        /// <returns>The encrypted signature</returns>
        private string CreateEncryptedSignature()
        {
            var rsa = this.CryptoProvider.CreateCipherEngine("RSA");
            rsa.KeyContainer = this.ReadPrivateKeyFromConfig();

            var time = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var signatureText = "{0}|{1}".FormatInvariant(time, this.AppUserId);
            var signatureBytes = Encoding.UTF8.GetBytes(signatureText);

            var encryptedSignature = rsa.Encrypt(signatureBytes);
            var signatureBase64 = Convert.ToBase64String(encryptedSignature);
            return signatureBase64;
        }
    }
}
