//-----------------------------------------------------------------------
// <copyright file="DfpAdvertiserExporter.cs" company="Rare Crowds Inc">
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
    /// <summary>Exporter for Google DFP advertisers</summary>
    internal class DfpAdvertiserExporter : DeliveryNetworkExporterBase<IGoogleDfpClient>
    {
        /// <summary>Version of the exporter</summary>
        private const int ExporterVersion = 1;

        /// <summary>Initializes a new instance of the DfpAdvertiserExporter class.</summary>
        /// <param name="companyEntity">Company Entity</param>
        public DfpAdvertiserExporter(CompanyEntity companyEntity)
            : base(DeliveryNetworkDesignation.GoogleDfp, ExporterVersion)
        {
            this.CompanyEntity = companyEntity;
        }

        /// <summary>
        /// Gets a value indicating whether an advertiser exists in DFP for the exporter's CompanyEntity
        /// </summary>
        /// <returns>True if an advertiser exists; otherwise, false.</returns>
        public bool AdvertiserExists
        {
            get
            {
                var advertiserId = this.CompanyEntity.GetDfpAdvertiserId();
                if (!advertiserId.HasValue)
                {
                    return false;
                }

                try
                {
                    this.Client.GetCompany(advertiserId.Value);
                    return true;
                }
                catch (GoogleDfpClientException)
                {
                    return false;
                }
            }
        }

        /// <summary>Gets the DFP advertiser id for the exporter's CompanyEntity</summary>
        public long AdvertiserId
        {
            get { return this.CompanyEntity.GetDfpAdvertiserId().Value; }
        }

        /// <summary>Gets the entities used in the exporter's config</summary>
        protected override IEnumerable<IEntity> ConfigEntities
        {
            get { return new IEntity[] { this.CompanyEntity }; }
        }

        /// <summary>Gets the exporter's CompanyEntity</summary>
        protected CompanyEntity CompanyEntity { get; private set; }

        /// <summary>Create a Google DFP advertiser for the exporter's CompanyEntity.</summary>
        /// <returns>Google DFP advertiser id</returns>
        public long CreateAdvertiser()
        {
            var advertiserId = this.Client.CreateAdvertiser(
                this.CompanyEntity.ExternalName,
                this.CompanyEntity.ExternalEntityId.ToString());
            LogManager.Log(
                LogLevels.Information,
                "Created Google DFP Advertiser '{0}' for CompanyEntity '{1}' ({2})",
                advertiserId,
                this.CompanyEntity.ExternalName,
                this.CompanyEntity.ExternalEntityId);
            return advertiserId;
        }
    }
}
