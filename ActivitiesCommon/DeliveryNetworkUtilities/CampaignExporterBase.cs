//-----------------------------------------------------------------------
// <copyright file="CampaignExporterBase.cs" company="Rare Crowds Inc">
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
using System.Linq;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;

namespace DeliveryNetworkUtilities
{
    /// <summary>Base class for delivery network exporters encapsulating lazy-initialized properties</summary>
    /// <typeparam name="TDeliveryNetworkClient">Type of the delivery network client</typeparam>
    public abstract class CampaignExporterBase<TDeliveryNetworkClient> : DeliveryNetworkExporterBase<TDeliveryNetworkClient>
        where TDeliveryNetworkClient : class, IDeliveryNetworkClient
    {
        /// <summary>The measure map</summary>
        private MeasureMap measureMap;

        /// <summary>Initializes a new instance of the CampaignExporterBase class.</summary>
        /// <param name="deliveryNetwork">The delivery network being exported to</param>
        /// <param name="version">Version of the exporter</param>
        /// <param name="companyEntity">Advertiser company</param>
        /// <param name="campaignEntity">Campaign being exported</param>
        /// <param name="campaignOwner">Owner of the campaign being exported</param>
        protected CampaignExporterBase(
            DeliveryNetworkDesignation deliveryNetwork,
            int version,
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
            : base(deliveryNetwork, version)
        {
            this.CompanyEntity = companyEntity;
            this.CampaignEntity = campaignEntity;
            this.CampaignOwner = campaignOwner;
        }

        /// <summary>Gets the company corresponding to the advertiser</summary>
        protected CompanyEntity CompanyEntity { get; private set; }

        /// <summary>Gets the campaign being exported</summary>
        protected CampaignEntity CampaignEntity { get; private set; }

        /// <summary>Gets the owner of the campaign being exported</summary>
        protected UserEntity CampaignOwner { get; private set; }

        /// <summary>Gets the measure map</summary>
        protected MeasureMap MeasureMap
        {
            get
            {
                return this.measureMap = this.measureMap ??
                    new MeasureMap(
                        MeasureSourceFactory.CreateMeasureSources(
                            this.DeliveryNetwork,
                            this.Version,
                            this.CompanyEntity,
                            this.CampaignEntity,
                            this.CampaignOwner));                    
            }
        }

        /// <summary>Gets the entities used to create the config</summary>
        protected override IEnumerable<IEntity> ConfigEntities
        {
            get
            {
                return new IEntity[]
                {
                    this.CompanyEntity,
                    this.CampaignEntity
                };
            }
        }
    }
}
