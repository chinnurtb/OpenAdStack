//-----------------------------------------------------------------------
// <copyright file="AppNexusMeasureSourceBase.cs" company="Rare Crowds Inc">
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
using AppNexusClient;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;
using Utilities.Storage;

namespace AppNexusActivities.Measures
{
    /// <summary>Base class for AppNexus measure sources</summary>
    /// <remarks>
    /// Provides IGoogleAppNexusClient and IConfig created from the
    /// provided entities for derived measure source classes.
    /// </remarks>
    internal abstract class AppNexusMeasureSourceBase : CachedMeasureSource, IMeasureSource, IDisposable
    {
        /// <summary>Measure id network prefix for AppNexus measures</summary>
        public const byte AppNexusMeasureIdPrefix = 1;

        /// <summary>Prefix for the source name</summary>
        private readonly string sourceNameFormat;

        /// <summary>Backing field for AppNexusClient</summary>
        private IAppNexusApiClient appNexusClient;

        /// <summary>Backing field for SourceName</summary>
        private string sourceName;

        /// <summary>Initializes a new instance of the AppNexusMeasureSourceBase class</summary>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="entities">Entities (for config)</param>
        protected AppNexusMeasureSourceBase(
            byte sourceMeasureIdPrefix,
            string sourceName,
            IEntity[] entities)
            : this(
                sourceMeasureIdPrefix,
                sourceName,
                entities.OfType<UserEntity>().SingleOrDefault(),
                EntityActivityUtilities.BuildCustomConfigFromEntities(entities))
        {
        }

        /// <summary>Initializes a new instance of the AppNexusMeasureSourceBase class</summary>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        /// <param name="sourceName">Source name</param>
        /// <param name="campaignOwner">Campaign Owner UserEntity (for config/auth)</param>
        /// <param name="config">Configuration used to initialize AppNexus client</param>
        protected AppNexusMeasureSourceBase(
            byte sourceMeasureIdPrefix,
            string sourceName,
            UserEntity campaignOwner,
            IConfig config)
            : base(AppNexusMeasureIdPrefix, sourceMeasureIdPrefix, PersistentDictionaryType.Sql)
        {
            this.sourceNameFormat = @"apnx-{0}-{{0}}".FormatInvariant(sourceName);
            this.Config = config ?? new CustomConfig();

            // Check if the campaign owner is an AppNexus App user
            var isAppNexusApp = campaignOwner != null && campaignOwner.GetUserType() == UserType.AppNexusApp;
            ((CustomConfig)this.Config).Overrides["AppNexus.IsApp"] = isAppNexusApp.ToString();
            if (isAppNexusApp)
            {
                // Add owner's user id to the config for AppNexus API auth
                ((CustomConfig)this.Config).Overrides["AppNexus.App.UserId"] = campaignOwner.UserId;
            }
        }

        /// <summary>Gets the config</summary>
        protected IConfig Config { get; private set; }

        /// <summary>Gets the AppNexus client instance</summary>
        protected IAppNexusApiClient AppNexusClient
        {
            get
            {
                if (!this.Online)
                {
                    throw new AppNexusClientException("Offline measure sources cannot use AppNexusClient", (Exception)null);
                }

                return this.appNexusClient =
                    this.appNexusClient ??
                    DeliveryNetworkClientFactory.CreateClient<IAppNexusApiClient>(this.Config);
            }
        }

        /// <summary>Gets the AppNexus measure source name</summary>
        /// <remarks>Includes user's corresponding member id/name (if available)</remarks>
        protected sealed override string SourceName
        {
            get
            {
                return this.sourceName =
                    this.sourceName ??
                    this.sourceNameFormat.FormatInvariant(
                    this.Online ? this.AppNexusClient.Id : "offline");
            }
        }

        /// <summary>Gets the master category display name</summary>
        protected sealed override string MasterCategoryDisplayName
        {
            get { return "AppNexus"; }
        }

        /// <summary>Gets the category display name</summary>
        protected override abstract string CategoryDisplayName { get; }

        /// <summary>Gets the targeting type</summary>
        protected abstract string MeasureType { get; }

        /// <summary>Gets the default data provider</summary>
        protected virtual string DefaultDataProvider
        {
            get { return MeasureInfo.DataProviderNoCost; }
        }

        /// <summary>Gets a value indicating whether this measure source is "online"</summary>
        /// <remarks>Only Online sources can use the AppNexusClient</remarks>
        protected virtual bool Online
        {
            get { return false; }
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Creates a AppNexus measure</summary>
        /// <param name="displayNameParts">Display name parts</param>
        /// <param name="apnxId">AppNexus targeting id</param>
        /// <param name="measureSubType">Measure subtype</param>
        /// <returns>The measure</returns>
        protected virtual IDictionary<string, object> CreateAppNexusMeasure(
            object[] displayNameParts,
            object apnxId,
            string measureSubType = null)
        {
            return new Dictionary<string, object>
            {
                { MeasureValues.DisplayName, this.MakeMeasureDisplayName(displayNameParts) },
                { MeasureValues.DataProvider, this.DefaultDataProvider },
                { MeasureValues.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString() },
                { MeasureValues.Type, this.MeasureType },
                { MeasureValues.SubType, measureSubType },
                { AppNexusMeasureValues.AppNexusId, apnxId },
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
                if (this.appNexusClient != null)
                {
                    this.appNexusClient.Dispose();
                    this.appNexusClient = null;
                }
            }
        }
    }
}
