// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Activation;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using ConfigManager;
using Diagnostics;
using Microsoft.IdentityModel.Web;
using Microsoft.IdentityModel.Web.Configuration;
using Microsoft.Practices.Unity;
using OAuthSecurity;
using RuntimeIoc.WebRole;
using Utilities.IdentityFederation;
using Utilities.Storage;

namespace ApiLayer
{
    /// <summary>
    /// Class for asp.net application which has events related to global application/session objects
    /// </summary>
    public class Global : HttpApplication
    {
        /// <summary>
        /// RegEx patterns matching URIs to which anonymous access is allowed
        /// </summary>
        private static readonly string[] AnonymousAccessUriPatterns = new[]
        {
            "/api/apnx/register"
        };

        /// <summary>Backing field for AnonymousAccessUris</summary>
        private static Regex[] anonymousAccessUris;

        /// <summary>Backing field for AuthenticationManager</summary>
        private IAuthenticationManager authenticationManager;

        /// <summary>Backing field for AuthorizationManager</summary>
        private IAuthorizationManager authorizationManager;

        /// <summary>
        /// Gets a value indicating whether the application is running as an AppNexus app
        /// </summary>
        private static bool IsApnxApp
        {
            get { return Config.GetBoolValue("AppNexus.IsApp"); }
        }

        /// <summary>
        /// Gets a list of RegEx patterns matching URIs to which anonymous access is allowed
        /// </summary>
        private static Regex[] AnonymousAccessUris
        {
            get
            {
                return anonymousAccessUris = anonymousAccessUris ??
                    AnonymousAccessUriPatterns.Select(p => new Regex(p)).ToArray();
            }
        }

        /// <summary>Gets the authentication manager</summary>
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return this.authenticationManager = this.authenticationManager ??
                    RuntimeIocContainer.Instance.Resolve<IAuthenticationManager>();
            }
        }

        /// <summary>Gets the authorization manager</summary>
        private IAuthorizationManager AuthorizationManager
        {
            get
            {
                return this.authorizationManager = this.authorizationManager ??
                    RuntimeIocContainer.Instance.Resolve<IAuthorizationManager>();
            }
        }

        /// <summary>Event is called when the application starts</summary>
        /// <param name="sender">caller of the event</param>
        /// <param name="e">parameter sent to the event by the callee</param>
        protected void Application_Start(object sender, EventArgs e)
        {
            // Resolve the persistent dictionary factory and logger
            PersistentDictionaryFactory.Initialize(RuntimeIocContainer.Instance.ResolveAll<IPersistentDictionaryFactory>());
            LogManager.Initialize(RuntimeIocContainer.Instance.ResolveAll<ILogger>());

            RegisterRoutes();
            FederatedAuthentication.ServiceConfigurationCreated += this.OnServiceConfigurationCreated;

            LogManager.Log(LogLevels.Information, "ApiLayer Application_Start");
        }

        /// <summary>Event is called to authenticate requests</summary>
        /// <param name="sender">caller of the event</param>
        /// <param name="e">parameter sent to the event by the callee</param>
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            if (!IsApnxApp)
            {
                // Handled by ACS Identity Federation
                return;
            }

            // Check if anonymous access is allowed
            if (AnonymousAccessUris.Any(u => u.IsMatch(HttpContext.Current.Request.Url.AbsolutePath.ToLowerInvariant())))
            {
                return;
            }

            // Verify user is valid
            if (!this.AuthenticationManager.CheckValidUser())
            {
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                HttpContext.Current.Response.AddHeader("WWW-Authenticate", "INVALID USER");
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.End();
            }
        }

        /// <summary>Event is called to authorize requests</summary>
        /// <param name="sender">caller of the event</param>
        /// <param name="e">parameter sent to the event by the callee</param>
        protected void Application_AuthorizeRequest(object sender, EventArgs e)
        {
            if (!IsApnxApp)
            {
                // Handled by ACS Identity Federation
                return;
            }

            // Check if anonymous access is allowed
            if (AnonymousAccessUris.Any(u => u.IsMatch(HttpContext.Current.Request.Url.AbsolutePath.ToLowerInvariant())))
            {
                return;
            }

            // Verify user has authorization to access the resource
            if (!this.AuthorizationManager.CheckAccess(
                HttpContext.Current.Request.HttpMethod,
                HttpContext.Current.Request.Url.AbsoluteUri))
            {
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                HttpContext.Current.Response.AddHeader("WWW-Authenticate", "ACCESS DENIED");
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.End();
            }
        }

        /// <summary>
        /// Registers all the routes that are handled by this application
        /// </summary>
        private static void RegisterRoutes()
        {
            LogManager.Log(LogLevels.Information, "Registering Routes");

            // Add the service class name that maps to the url request
            RouteTable.Routes.Add(new ServiceRoute("Entity", new WebServiceHostFactory(), typeof(EntityService)));
            RouteTable.Routes.Add(new ServiceRoute("Data", new WebServiceHostFactory(), typeof(DataService)));

            // Add the Apnx service if running as an AppNexus app
            if (IsApnxApp)
            {
                RouteTable.Routes.Add(new ServiceRoute("Apnx", new WebServiceHostFactory(), typeof(AppNexusAppService)));
            }
        }

        /// <summary>
        /// Method that is called when the service configuration is created
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
