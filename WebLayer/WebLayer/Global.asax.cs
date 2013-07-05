// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;
using ConfigManager;
using Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Web;
using Microsoft.IdentityModel.Web.Configuration;
using Microsoft.Practices.Unity;
using OAuthSecurity;
using RuntimeIoc.WebRole;

namespace WebLayer
{
    /// <summary>
    /// Class for asp.net application which has events related to global application/session objects
    /// </summary>
    public class Global : HttpApplication
    {
        /// <summary>
        /// Event is called when the application starts
        /// </summary>
        /// <param name="sender">caller of the event</param>
        /// <param name="e">parameter sent to the event by the callee</param>
        protected void Application_Start(object sender, EventArgs e)
        {
            LogManager.Initialize(RuntimeIocContainer.Instance.ResolveAll<ILogger>());
            LogManager.Log(LogLevels.Information, "WebLayer Application_Start");
            if (!Config.GetBoolValue("AppNexus.IsApp"))
            {
                FederatedAuthentication.ServiceConfigurationCreated += this.OnServiceConfigurationCreated;
            }
        }

        /// <summary>
        /// Event that manages sliding session
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">event arguments</param>
        protected void SessionAuthenticationModule_SessionSecurityTokenReceived(object sender, SessionSecurityTokenReceivedEventArgs e)
        {
            if (Config.GetBoolValue("AppNexus.IsApp"))
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            var sessionToken = e.SessionToken;
            SymmetricSecurityKey symmetricSecurityKey = null;

            if (sessionToken.SecurityKeys != null)
            {
                symmetricSecurityKey = sessionToken.SecurityKeys.OfType<SymmetricSecurityKey>().FirstOrDefault();
                if (symmetricSecurityKey != null)
                {
                    // If now is during second half of session
                    if ((now < sessionToken.ValidTo) && (now > sessionToken.ValidFrom.AddMinutes((sessionToken.ValidTo.Minute - sessionToken.ValidFrom.Minute) / 2)))
                    {
                        int tokenDurationMinutes;
                        if (int.TryParse(Config.GetValue("ACS.TokenDurationMinutes"), System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out tokenDurationMinutes))
                        {
                            // create a token of duration from config
                            e.SessionToken = new SessionSecurityToken(
                                        sessionToken.ClaimsPrincipal,
                                        sessionToken.ContextId,
                                        sessionToken.Context,
                                        sessionToken.EndpointId,
                                        new TimeSpan(0, tokenDurationMinutes, 0),
                                        symmetricSecurityKey);

                            e.ReissueCookie = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event that occurs during application error
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">event arguments</param>
        protected void Application_Error(object sender, EventArgs e)
        {
            LogManager.Log(LogLevels.Error, "Error: {0}".FormatInvariant(Server.GetLastError()));

            if (Server.GetLastError().GetType().Equals(typeof(System.Security.SecurityException)) ||
                Server.GetLastError().GetType().Equals(typeof(System.Security.Cryptography.CryptographicException)) ||
                Server.GetLastError().GetType().Equals(typeof(System.InvalidOperationException)))
            {
                // redirect to logoff page
                var redirect = Config.GetValue("WL.LogOffUrl");
                Response.Redirect(redirect, true);
            }
        }

        /// <summary>
        /// Event that occurs when the service configuration is created
        /// Refer to http://msdn.microsoft.com/en-us/library/windowsazure/gg185962.aspx
        /// </summary>
        /// <param name="sender">sender of this event</param>
        /// <param name="e">event arguments</param>
        private void OnServiceConfigurationCreated(object sender, ServiceConfigurationCreatedEventArgs e)
        {
            List<CookieTransform> sessionTransforms = new List<CookieTransform>(new CookieTransform[]
            {
                new DeflateCookieTransform(),
                new RsaEncryptionCookieTransform(e.ServiceConfiguration.ServiceCertificate),
                new RsaSignatureCookieTransform(e.ServiceConfiguration.ServiceCertificate)
            });

            SimpleWebTokenHandler sessionHandler = new SimpleWebTokenHandler(sessionTransforms.AsReadOnly());
            e.ServiceConfiguration.SecurityTokenHandlers.AddOrReplace(sessionHandler);
        }
    }
}