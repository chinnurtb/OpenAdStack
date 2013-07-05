// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecurityHelper.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace OAuthSecurity
{
    /// <summary>
    /// Class that contains static helper methods
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Method to send an unauthorized response
        /// </summary>
        /// <param name="error">error message</param>
        /// <param name="context">http context</param>
        public static void SendUnauthorizedResponse(OAuthError error, HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            string errorMessage = string.Format(CultureInfo.CurrentCulture, "OAuth error='{0}', error_description='{1}'", error.Error, error.ErrorDescription);

            context.Response.AddHeader("WWW-Authenticate", errorMessage);
            context.Response.Flush();
            context.Response.End();
        }

        /// <summary>
        /// Method to extract access token from Authentication header
        /// </summary>
        /// <param name="request">http request</param>
        /// <returns>NameValueCollection parameters</returns>
        public static string ExtractAccessTokenFromAuthenticateHeader(HttpRequest request)
        {
            string authHeader;
            string token;

            authHeader = request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authHeader))
            {
                token = null;
            }
            else
            {
                string header = "Bearer ";
                if (string.CompareOrdinal(authHeader, 0, header, 0, header.Length) == 0)
                {
                    token = authHeader.Remove(0, header.Length);
                }
                else
                {
                    throw new InvalidOperationException("the authorization header was invalid");
                }
            }

            return token;
        }

        /// <summary>
        /// Method to determine whether authorization is OAuth protocol
        /// </summary>
        /// <param name="request">http request</param>
        /// <returns>whether authorization is OAuth protocol</returns>
        public static bool IsOAuthAuthorization(HttpRequest request)
        {
            if (!string.IsNullOrEmpty(request.Headers["Authorization"]))
            {
                if (request.Headers["Authorization"].StartsWith("Bearer ", StringComparison.CurrentCulture))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Method to determine whether a string is base64 or not
        /// </summary>
        /// <param name="base64">string to evaluate</param>
        /// <returns>whether string is base64</returns>
        public static bool IsBase64(string base64)
        {
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a certificate from a given store.
        /// </summary>
        /// <param name="name">Certificate Store where to look for the certificate.</param>
        /// <param name="location">StoreLocation of the certificate.</param>
        /// <param name="thumbprint">Thumbprint of the certificate.</param>
        /// <returns>Instance of X509Certificate2.</returns>
        public static X509Certificate2 GetCertificate(StoreName name, StoreLocation location, string thumbprint)
        {
            X509Store store = new X509Store(name, location);
            X509Certificate2Collection certificates = null;
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2 result = null;

                // Every time we call store.Certificates property, a new collection will be returned.
                certificates = store.Certificates;

                for (int i = 0; i < certificates.Count; i++)
                {
                    X509Certificate2 cert = certificates[i];

                    if (cert.Thumbprint.ToUpperInvariant() == thumbprint.ToUpperInvariant())
                    {
                        if (result != null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "There is more than one certificate found for subject Name {0}", thumbprint));
                        }

                        result = new X509Certificate2(cert);
                    }
                }

                if (result == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "No certificate was found for subject Name {0}", thumbprint));
                }

                return result;
            }
            finally
            {
                if (certificates != null)
                {
                    for (int i = 0; i < certificates.Count; i++)
                    {
                        X509Certificate2 cert = certificates[i];
                        cert.Reset();
                    }
                }

                store.Close();
            }
        }
    }
}
