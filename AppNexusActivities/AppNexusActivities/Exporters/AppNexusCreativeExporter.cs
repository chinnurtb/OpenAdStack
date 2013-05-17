//-----------------------------------------------------------------------
// <copyright file="AppNexusCreativeExporter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using AppNexusClient;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using EntityActivities;
using EntityUtilities;

namespace AppNexusActivities
{
    /// <summary>Exports creatives to AppNexus</summary>
    internal class AppNexusCreativeExporter : IDisposable
    {
        /// <summary>
        /// Dictionary of AppNexus creative format names for supported creative types
        /// </summary>
        private static readonly IDictionary<CreativeType, string> AppNexusFormats =
            new Dictionary<CreativeType, string>
            {
                { CreativeType.ImageAd, "image" },
                { CreativeType.ThirdPartyAd, "iframe-html" },
                { CreativeType.FlashAd, "flash" },
            };

        /// <summary>Exporter configuration</summary>
        private readonly IConfig Config;

        /// <summary>AppNexus advertiser id</summary>
        private readonly int AdvertiserId;

        /// <summary>Creative to be exported</summary>
        private readonly CreativeEntity CreativeEntity;

        /// <summary>AppNexus API client</summary>
        private readonly IAppNexusApiClient Client;

        /// <summary>Backing field for CreativeFormats</summary>
        private IDictionary<string, object>[] creativeFormats;

        /// <summary>Backing field for CreativeTemplates</summary>
        private IDictionary<string, object>[] creativeTemplates;

        /// <summary>
        /// Initializes a new instance of the AppNexusCreativeExporter class
        /// </summary>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <param name="companyEntity">The company entity</param>
        /// <param name="creativeEntity">The creative entity</param>
        /// <param name="creativeOwner">The creative owner</param>
        public AppNexusCreativeExporter(
            int advertiserId,
            CompanyEntity companyEntity,
            CreativeEntity creativeEntity,
            UserEntity creativeOwner)
        {
            if (!SupportedTypes.Contains(creativeEntity.GetCreativeType()))
            {
                var message =
                    "Unsupported creative type '{0}' for creative '{1}' ({2})"
                    .FormatInvariant(
                    creativeEntity.GetCreativeType(),
                    creativeEntity.ExternalEntityId,
                    creativeEntity.ExternalName);
                throw new ArgumentException(message, "creativeEntity");                    
            }

            this.AdvertiserId = advertiserId;
            this.CreativeEntity = creativeEntity;
            this.Config = BuildConfig(companyEntity, this.CreativeEntity, creativeOwner);
            this.Client = DeliveryNetworkClientFactory.CreateClient<IAppNexusApiClient>(this.Config);
        }

        /// <summary>Gets the types of creatives supported by the exporter</summary>
        public static CreativeType[] SupportedTypes
        {
            get { return AppNexusFormats.Keys.ToArray(); }
        }

        /// <summary>Gets the AppNexus creative formats</summary>
        private IDictionary<string, object>[] AppNexusCreativeFormats
        {
            get
            {
                return this.creativeFormats = this.creativeFormats ??
                    this.Client.GetCreativeFormats();
            }
        }

        /// <summary>Gets the AppNexus creative formats</summary>
        private IDictionary<string, object>[] AppNexusCreativeTemplates
        {
            get
            {
                return this.creativeTemplates = this.creativeTemplates ??
                    this.Client.GetCreativeTemplates();
            }
        }

        /// <summary>Export the creative</summary>
        /// <returns>The AppNexus id of the exported creative</returns>
        public int ExportCreative()
        {
            var creativeType = this.CreativeEntity.GetCreativeType();
            switch (creativeType)
            {
                case CreativeType.ThirdPartyAd: return this.ExportThirdPartyAd();
                case CreativeType.ImageAd: return this.ExportImageAd();
                case CreativeType.FlashAd: return this.ExportFlashAd();
                default: throw new NotSupportedException(creativeType.ToString());
            }
        }

        /// <summary>Dispose of resources</summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>Locates a creative format for a CreativeType in AppNexus</summary>
        /// <param name="creativeType">Creative type</param>
        /// <returns>Creative format id</returns>
        internal int LookupAppNexusCreativeFormatId(CreativeType creativeType)
        {
            var formatName = AppNexusFormats[creativeType];
            var format = this.AppNexusCreativeFormats
                .FirstOrDefault(f => (string)f[AppNexusValues.Name] == formatName);
            if (format == null)
            {
                throw new DeliveryNetworkExporterException(
                    "Unable to find creative format '{0}' ({1}) in AppNexus for creative '{2}' ({3})",
                    formatName,
                    creativeType,
                    this.CreativeEntity.ExternalName,
                    this.CreativeEntity.ExternalEntityId);
            }

            return (int)format[AppNexusValues.Id];
        }

        /// <summary>Locates a standard template for a creative format in AppNexus</summary>
        /// <param name="formatId">Creative format id</param>
        /// <returns>The standard template id for the format</returns>
        internal int LookupAppNexusStandardTemplateId(int formatId)
        {
            var standardTemplate = this.AppNexusCreativeTemplates
                .FirstOrDefault(t =>
                    (int)((IDictionary<string, object>)t[AppNexusValues.Format])[AppNexusValues.Id] == formatId &&
                    ((string)t[AppNexusValues.Name]).ToLowerInvariant() == "standard");
            if (standardTemplate == null)
            {
                throw new DeliveryNetworkExporterException(
                    "Unable to find an AppNexus standard creative template with format '{0}' for creative '{1}' ({2})",
                    formatId,
                    this.CreativeEntity.ExternalName,
                    this.CreativeEntity.ExternalEntityId);
            }

            return (int)standardTemplate[AppNexusValues.Id];
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        /// <param name="disposing">
        /// Whether to clean up managed resources as well as unmanaged
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Client.Dispose();
            }
        }

        /// <summary>Builds the IConfig used by the exporter</summary>
        /// <param name="companyEntity">The company entity</param>
        /// <param name="creativeEntity">The creative entity</param>
        /// <param name="creativeOwner">The creative owner</param>
        /// <returns>The config</returns>
        private static IConfig BuildConfig(
            CompanyEntity companyEntity,
            CreativeEntity creativeEntity,
            UserEntity creativeOwner)
        {
            var config = EntityActivity.BuildCustomConfigFromEntities(
                companyEntity, creativeEntity, creativeOwner);
            var isAppNexusApp = creativeOwner.GetUserType() == UserType.AppNexusApp;
            config.Overrides["AppNexus.IsApp"] = isAppNexusApp.ToString();
            if (isAppNexusApp)
            {
                config.Overrides["AppNexus.App.UserId"] = creativeOwner.UserId;
            }

            return config;
        }

        /// <summary>
        /// Find the standard template for the creative format in AppNexus
        /// </summary>
        /// <param name="creativeType">Creative type</param>
        /// <returns>The AppNexus creative template id</returns>
        private int GetAppNexusTemplateId(CreativeType creativeType)
        {
            var formatId = this.LookupAppNexusCreativeFormatId(creativeType);
            return this.LookupAppNexusStandardTemplateId(formatId);
        }

        /// <summary>Export a ThirdPartyAd creative</summary>
        /// <returns>The AppNexus id of the exported creative</returns>
        private int ExportThirdPartyAd()
        {
            return this.Client.CreateCreative(
                this.AdvertiserId,
                this.CreativeEntity.ExternalName,
                this.CreativeEntity.ExternalEntityId.ToString(),
                this.GetAppNexusTemplateId(this.CreativeEntity.GetCreativeType()),
                (int)this.CreativeEntity.GetWidth(),
                (int)this.CreativeEntity.GetHeight(),
                this.CreativeEntity.GetThirdPartyAdTag());
        }

        /// <summary>Export a ThirdPartyAd creative</summary>
        /// <returns>The AppNexus id of the exported creative</returns>
        private int ExportImageAd()
        {
            return this.Client.CreateCreative(
                this.AdvertiserId,
                this.CreativeEntity.ExternalName,
                this.CreativeEntity.ExternalEntityId.ToString(),
                this.GetAppNexusTemplateId(this.CreativeEntity.GetCreativeType()),
                (int)this.CreativeEntity.GetWidth(),
                (int)this.CreativeEntity.GetHeight(),
                Convert.ToBase64String(this.CreativeEntity.GetImageBytes()),
                this.CreativeEntity.GetImageName(),
                this.CreativeEntity.GetClickUrl());
        }

        /// <summary>Export a ThirdPartyAd creative</summary>
        /// <returns>The AppNexus id of the exported creative</returns>
        private int ExportFlashAd()
        {
            return this.Client.CreateCreative(
                this.AdvertiserId,
                this.CreativeEntity.ExternalName,
                this.CreativeEntity.ExternalEntityId.ToString(),
                this.GetAppNexusTemplateId(this.CreativeEntity.GetCreativeType()),
                (int)this.CreativeEntity.GetWidth(),
                (int)this.CreativeEntity.GetHeight(),
                Convert.ToBase64String(this.CreativeEntity.GetFlashBytes()),
                this.CreativeEntity.GetFlashName(),
                this.CreativeEntity.GetClickUrl(),
                Convert.ToBase64String(this.CreativeEntity.GetImageBytes()),
                this.CreativeEntity.GetImageName(),
                this.CreativeEntity.GetFlashClickVariable());
        }
    }
}
