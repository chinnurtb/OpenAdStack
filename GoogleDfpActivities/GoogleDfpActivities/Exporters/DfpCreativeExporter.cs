//-----------------------------------------------------------------------
// <copyright file="DfpCreativeExporter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityActivities;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Utilities.Storage;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpActivities.Exporters
{
    /// <summary>
    /// Exporter for exporting DynamicAllocation nodes to Google DFP line-items
    /// </summary>
    internal class DfpCreativeExporter : DfpAdvertiserExporter
    {
        /// <summary>Initializes a new instance of the DfpCreativeExporter class.</summary>
        /// <param name="companyEntity">Company Entity</param>
        /// <param name="creativeEntity">Creative Entity</param>
        public DfpCreativeExporter(
            CompanyEntity companyEntity,
            CreativeEntity creativeEntity)
            : base(companyEntity)
        {
            this.CreativeEntity = creativeEntity;
        }

        /// <summary>
        /// Gets a value indicating whether a creative exists in DFP for the exporter's CreativeEntity
        /// </summary>
        /// <returns>True if an creative exists; otherwise, false.</returns>
        public bool CreativeExists
        {
            get
            {
                var orderId = this.CreativeEntity.GetDfpCreativeId();
                if (!orderId.HasValue)
                {
                    return false;
                }

                try
                {
                    this.Client.GetOrder(orderId.Value);
                    return true;
                }
                catch (GoogleDfpClientException)
                {
                    return false;
                }
            }
        }

        /// <summary>Gets the entities used in the exporter's config</summary>
        protected override IEnumerable<IEntity> ConfigEntities
        {
            get { return new IEntity[] { this.CompanyEntity, this.CreativeEntity }; }
        }

        /// <summary>Gets the exporter's CreativeEntity</summary>
        protected CreativeEntity CreativeEntity { get; private set; }

        /// <summary>Creates a Google DFP creative for the exporter's CreativeEntity.</summary>
        /// <returns>Google DFP creative id</returns>
        public long CreateCreative()
        {
            long creativeId = -1;
            var creativeType = this.CreativeEntity.GetCreativeType();
            if (creativeType == CreativeType.ImageAd)
            {
                creativeId = this.Client.CreateImageCreative(
                    this.AdvertiserId,
                    this.CreativeEntity.ExternalName,
                    this.CreativeEntity.GetClickUrl(),
                    (int)this.CreativeEntity.GetWidth(),
                    (int)this.CreativeEntity.GetHeight(),
                    false,
                    this.CreativeEntity.ExternalEntityId.ToString(),
                    this.CreativeEntity.GetImageBytes());
            }
            else if (creativeType == CreativeType.ThirdPartyAd)
            {
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                var message =
                    "Google DFP export not supported for creative '{0}' ({1}) of type {2}"
                    .FormatInvariant(
                        this.CreativeEntity.ExternalName,
                        this.CreativeEntity.ExternalEntityId,
                        this.CreativeEntity.ExternalType);
                LogManager.Log(LogLevels.Error, message);
                throw new InvalidOperationException(message);
            }

            LogManager.Log(
                LogLevels.Information,
                "Created Google DFP creative '{0}' for CreativeEntity '{1}' ({2})",
                creativeId,
                this.CreativeEntity.ExternalName,
                this.CreativeEntity.ExternalEntityId);
            return creativeId;
        }
    }
}
