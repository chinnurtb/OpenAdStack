//-----------------------------------------------------------------------
// <copyright file="DfpMeasureSourceBase.cs" company="Rare Crowds Inc">
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
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityActivities;
using GoogleDfpClient;
using Utilities.Storage;

namespace GoogleDfpActivities.Measures
{
    /// <summary>Base class for Google DFP measure sources</summary>
    /// <remarks>
    /// Provides IGoogleDfpClient and IConfig created from the
    /// provided entities for derived measure source classes.
    /// </remarks>
    internal abstract class DfpMeasureSourceBase : CachedMeasureSource, IMeasureSource, IDisposable
    {
        /// <summary>Measure id network prefix for Google DFP measures</summary>
        public const byte GoogleDfpMeasureIdPrefix = 2;

        /// <summary>Entities used to create the IConfig</summary>
        private readonly IEntity[] entities;

        /// <summary>Prefix for the source name</summary>
        private readonly string sourceNamePrefix;

        /// <summary>Backing field for Config</summary>
        private IConfig config;

        /// <summary>Backing field for DfpClient</summary>
        private IGoogleDfpClient dfpClient;

        /// <summary>Backing field for SourceName</summary>
        private string sourceName;

        /// <summary>Initializes a new instance of the DfpMeasureSourceBase class</summary>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="companyEntity">CompanyEntity (for config)</param>
        /// <param name="campaignEntity">CampaignEntity (for config)</param>
        protected DfpMeasureSourceBase(
            byte sourceMeasureIdPrefix,
            string sourceName,
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity)
            : this(sourceMeasureIdPrefix, sourceName, companyEntity, campaignEntity, PersistentDictionaryType.Sql)
        {
        }

        /// <summary>Initializes a new instance of the DfpMeasureSourceBase class</summary>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="companyEntity">CompanyEntity (for config)</param>
        /// <param name="campaignEntity">CampaignEntity (for config)</param>
        /// <param name="dictionaryType">Persistent dictionary type used for caching (default is SQL)</param>
        protected DfpMeasureSourceBase(
            byte sourceMeasureIdPrefix,
            string sourceName,
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            PersistentDictionaryType dictionaryType)
            : base(GoogleDfpMeasureIdPrefix, sourceMeasureIdPrefix, dictionaryType)
        {
            this.sourceNamePrefix = "dfp-{0}-".FormatInvariant(sourceName);
            this.entities = new IEntity[] { companyEntity, campaignEntity };
        }

        /// <summary>Gets the config</summary>
        protected IConfig Config
        {
            get
            {
                return this.config =
                    this.config ??
                    EntityActivity.BuildCustomConfigFromEntities(this.entities);
            }
        }

        /// <summary>Gets the Google DFP client instance</summary>
        protected IGoogleDfpClient DfpClient
        {
            get
            {
                return this.dfpClient =
                    this.dfpClient ??
                    DeliveryNetworkClientFactory.CreateClient<IGoogleDfpClient>(this.Config);
            }
        }

        /// <summary>Gets the cache name</summary>
        protected sealed override string SourceName
        {
            get
            {
                return this.sourceName =
                    this.sourceName ??
                    this.sourceNamePrefix + this.Config.GetValue("GoogleDfp.NetworkId");
            }
        }

        /// <summary>Gets the master category display name</summary>
        protected sealed override string MasterCategoryDisplayName
        {
            get { return "Google DFP"; }
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>Gets the targeting type</summary>
        protected abstract string MeasureType { get; }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Creates a Google DFP measure</summary>
        /// <param name="displayName">Display name</param>
        /// <param name="dfpId">Google DFP targeting id</param>
        /// <param name="measureSubType">Measure subtype</param>
        /// <returns>The measure</returns>
        protected virtual IDictionary<string, object> CreateDfpMeasure(
            string displayName,
            long dfpId,
            string measureSubType = null)
        {
            return this.CreateDfpMeasure(
                new[] { displayName },
                dfpId,
                measureSubType);
        }

        /// <summary>Creates a Google DFP measure</summary>
        /// <param name="displayNameParts">Display name parts</param>
        /// <param name="dfpId">Google DFP targeting id</param>
        /// <param name="measureSubType">Measure subtype</param>
        /// <returns>The measure</returns>
        protected virtual IDictionary<string, object> CreateDfpMeasure(
            object[] displayNameParts,
            long dfpId,
            string measureSubType = null)
        {
            return new Dictionary<string, object>
            {
                { MeasureValues.DisplayName, this.MakeMeasureDisplayName(displayNameParts) },
                { MeasureValues.DataProvider, MeasureInfo.DataProviderNoCost },
                { MeasureValues.DeliveryNetwork, DeliveryNetworkDesignation.GoogleDfp },
                { MeasureValues.Type, this.MeasureType },
                { MeasureValues.SubType, measureSubType },
                { DfpMeasureValues.DfpId, dfpId },
            };
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        /// <param name="disposing">
        /// Whether to clean up managed resources as well as unmanaged
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.dfpClient != null)
                {
                    this.dfpClient.Dispose();
                    this.dfpClient = null;
                }
            }
        }
    }
}
