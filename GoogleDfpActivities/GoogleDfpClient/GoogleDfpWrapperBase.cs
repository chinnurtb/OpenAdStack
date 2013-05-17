//-----------------------------------------------------------------------
// <copyright file="GoogleDfpWrapperBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigManager;
using DeliveryNetworkUtilities;
using Google.Api.Ads.Common.Lib;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpClient
{
    /// <summary>Encapsulation of the basics of wrapping the Google Ads DFP APIs</summary>
    public abstract class GoogleDfpWrapperBase : DeliveryNetworkClientBase, IDisposable
    {
        /// <summary>Backing dictionary for service properties</summary>
        private readonly IDictionary<Type, DfpSoapClient> adsClients;

        /// <summary>Configuration for settings</summary>
        private IConfig config;

        /// <summary>
        /// Initializes a new instance of the GoogleDfpWrapperBase class
        /// using configuration from app.config.
        /// </summary>
        internal GoogleDfpWrapperBase()
            : this(new CustomConfig())
        {
            this.adsClients = new Dictionary<Type, DfpSoapClient>();
        }

        /// <summary>
        /// Initializes a new instance of the GoogleDfpWrapperBase class
        /// using the provided configuration.
        /// </summary>
        /// <param name="config">Configuration to use.</param>
        internal GoogleDfpWrapperBase(IConfig config)
        {
            this.adsClients = new Dictionary<Type, DfpSoapClient>();
            this.config = config;
            var headers = new Dictionary<string, string>
            {
                { "ApplicationName", this.config.GetValue("GoogleDfp.ApplicationName") },
                { "NetworkCode", this.config.GetValue("GoogleDfp.NetworkId") },
                { "Email", this.config.GetValue("GoogleDfp.Username") },
                { "Password", this.config.GetValue("GoogleDfp.Password") },
                { "DfpApi.Server", "https://www.google.com" },
            };
            this.DfpUser = new DfpUser(headers);
            this.NetworkTimezone = TimeZoneInfo.FindSystemTimeZoneById(this.config.GetValue("GoogleDfp.NetworkTimezone"));
        }

        /// <summary>Gets the CompanyService</summary>
        internal NetworkService NetworkService
        {
            get { return this.GetAdsClient<NetworkService>(DfpService.v201206.NetworkService); }
        }

        /// <summary>Gets the CompanyService</summary>
        internal CompanyService CompanyService
        {
            get { return this.GetAdsClient<CompanyService>(DfpService.v201206.CompanyService); }
        }

        /// <summary>Gets the OrderService</summary>
        internal OrderService OrderService
        {
            get { return this.GetAdsClient<OrderService>(DfpService.v201206.OrderService); }
        }

        /// <summary>Gets the LineItemService</summary>
        internal LineItemService LineItemService
        {
            get { return this.GetAdsClient<LineItemService>(DfpService.v201206.LineItemService); }
        }

        /// <summary>Gets the CreativeService</summary>
        internal InventoryService InventoryService
        {
            get { return this.GetAdsClient<InventoryService>(DfpService.v201206.InventoryService); }
        }

        /// <summary>Gets the CreativeService</summary>
        internal PlacementService PlacementService
        {
            get { return this.GetAdsClient<PlacementService>(DfpService.v201206.PlacementService); }
        }

        /// <summary>Gets the CreativeService</summary>
        internal CreativeService CreativeService
        {
            get { return this.GetAdsClient<CreativeService>(DfpService.v201206.CreativeService); }
        }

        /// <summary>Gets the CreativeService</summary>
        internal LineItemCreativeAssociationService LicaService
        {
            get { return this.GetAdsClient<LineItemCreativeAssociationService>(DfpService.v201206.LineItemCreativeAssociationService); }
        }

        /// <summary>Gets the ReportService</summary>
        internal ReportService ReportService
        {
            get { return this.GetAdsClient<ReportService>(DfpService.v201206.ReportService); }
        }

        /// <summary>Gets the UserService</summary>
        internal UserService UserService
        {
            get { return this.GetAdsClient<UserService>(DfpService.v201206.UserService); }
        }

        /// <summary>Gets the PQL Service</summary>
        internal PublisherQueryLanguageService QueryService
        {
            get { return this.GetAdsClient<PublisherQueryLanguageService>(DfpService.v201206.PublisherQueryLanguageService); }
        }

        /// <summary>Gets the timezone for the Google DFP network</summary>
        internal TimeZoneInfo NetworkTimezone { get; private set; }

        /// <summary>Gets the DFP API user</summary>
        internal DfpUser DfpUser { get; private set; }

        /// <summary>Gets the current API user's id</summary>
        protected long CurrentUserId
        {
            get { return this.UserService.getCurrentUser().id; }
        }

        /// <summary>Gets the Google DFP Trafficker Id</summary>
        /// <remarks>
        /// The role setting GoogleDfp.TraffickerId will be used if set/valid.
        /// Otherwise, defaults to the id of the API user.
        /// </remarks>
        protected long TraffickerId
        {
            get
            {
                try
                {
                    var userId = System.Convert.ToInt64(this.config.GetValue("GoogleDfp.TraffickerId"), System.Globalization.CultureInfo.InvariantCulture);
                    var user = this.UserService.getUser(userId);
                    return user != null ? user.id : this.CurrentUserId;
                }
                catch (ArgumentException)
                {
                    return this.CurrentUserId;
                }
            }
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        /// <param name="disposing">
        /// Whether to clean up managed resources as well as unmanaged
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose the DfpSoapClient instances
                foreach (var clientType in this.adsClients.Keys)
                {
                    if (this.adsClients[clientType] != null)
                    {
                        this.adsClients[clientType].Dispose();
                        this.adsClients[clientType] = null;
                    }
                }
            }
        }
        
        /// <summary>Gets the service specified by the service signature</summary>
        /// <typeparam name="TClient">Type of the service</typeparam>
        /// <param name="serviceSignature">The service signature</param>
        /// <returns>The service</returns>
        private TClient GetAdsClient<TClient>(ServiceSignature serviceSignature)
            where TClient : DfpSoapClient, AdsClient
        {
            DfpSoapClient client;
            if (!this.adsClients.TryGetValue(typeof(TClient), out client))
            {
                client = (DfpSoapClient)this.DfpUser.GetService(serviceSignature);
            }

            return (TClient)client;
        }
    }
}
