//-----------------------------------------------------------------------
// <copyright file="ValuationsCache.cs" company="Rare Crowds Inc">
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
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>Valuations caching behavior.</summary>
    internal class ValuationsCache
    {
        /// <summary>Initializes a new instance of the <see cref="ValuationsCache"/> class.</summary>
        /// <param name="repository">IEntityRepository instance.</param>
        public ValuationsCache(IEntityRepository repository)
        {
            this.Repository = repository;
        }

        /// <summary>Gets the IEntityRepository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Create Valuations from a Campaign Entity</summary>
        /// <param name="dynamicAllocationEngine">dynamic allocation engine</param>
        /// <param name="valuationInputs">The ValuationInputs.</param>
        /// <param name="campaignEntity">the campaign</param>
        /// <returns>the valuations</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        internal static IDictionary<MeasureSet, decimal> CreateValuationsFromInputs(
            IDynamicAllocationEngine dynamicAllocationEngine,
            ValuationInputs valuationInputs,
            IEntity campaignEntity)
        {
            var campaignEntityMsgId = string.Empty;
            try
            {
                campaignEntityMsgId = (EntityId)campaignEntity.ExternalEntityId.Value;

                // call to DA to get valuations
                return dynamicAllocationEngine.GetValuations(valuationInputs.CreateCampaignDefinition());
            }
            catch (Exception e)
            {
                var msg = "Valuations could not be generated from inputs for campaign {0}."
                    .FormatInvariant(campaignEntityMsgId);
                LogManager.Log(LogLevels.Error, msg);
                throw new ActivityException(ActivityErrorId.GenericError, msg, e);
            }
        }

        /// <summary>Try to build ValuationInputs From Campaign Entity.</summary>
        /// <param name="campaignEntity">the campaign</param>
        /// <returns>ValuationInputs or null if not successful.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        internal static ValuationInputs BuildValuationInputs(
            IEntity campaignEntity)
        {
            // Attempt to get the valuation inputs from entity properties first
            var campaignEntityMsgId = string.Empty;
            try
            {
                campaignEntityMsgId = (EntityId)campaignEntity.ExternalEntityId.Value;

                // Measure list
                var measureListsJson = campaignEntity.TryGetPropertyValueByName(daName.MeasureList);
                if (measureListsJson == null)
                {
                    throw new ActivityException(ActivityErrorId.GenericError, "Missing MeasureList");
                }

                // Node overrides (can be null)
                string nodeOverridesJsonValue = null;
                var nodeOverridesJsonProperty = campaignEntity.TryGetPropertyValueByName(daName.NodeValuationSet);
                if (nodeOverridesJsonProperty != null)
                {
                    nodeOverridesJsonValue = nodeOverridesJsonProperty;
                }

                return new ValuationInputs(measureListsJson, nodeOverridesJsonValue);
            }
            catch (AppsJsonException e)
            {
                var msg = "Could not deserialize valuation json for campaign {0}: {1}".FormatInvariant(campaignEntityMsgId, e.Message);
                LogManager.Log(LogLevels.Error, msg);
                throw new ActivityException(ActivityErrorId.InvalidJson, msg, e);
            }
            catch (Exception e)
            {
                var msg = "Could not build valuation inputs for campaign {0}. {1}".FormatInvariant(campaignEntityMsgId, e.Message);
                LogManager.Log(LogLevels.Error, msg);
                throw new ActivityException(ActivityErrorId.GenericError, msg, e);
            }
        }

        /// <summary>Attempt to get the cached valuations.</summary>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <param name="valuationInputsFingerprint">Current valuation inputs fingerprint.</param>
        /// <returns>The cached valuations, or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        internal static IDictionary<MeasureSet, decimal> TryGetCachedValuations(
            IEntity campaignEntity, string valuationInputsFingerprint)
        {
            try
            {
                var cacheFingerprint = campaignEntity.TryGetPropertyByName<string>(daName.ValuationInputsFingerprint, null);
                var cacheDirty = true;

                if (valuationInputsFingerprint != null)
                {
                    cacheDirty = valuationInputsFingerprint != cacheFingerprint;
                }

                if (cacheDirty)
                {
                    return null;
                }

                var cachedValuationsJson = campaignEntity.TryGetPropertyByName<string>(daName.CachedValuations, null);
                if (cachedValuationsJson == null)
                {
                    return null;
                }

                return AppsJsonSerializer.DeserializeObject<Dictionary<MeasureSet, decimal>>(cachedValuationsJson);
            }
            catch (Exception)
            {
                // This should not happen, alert it. However, the caller does not necessarily fail because
                // cached valuations couldn't be loaded.
                var msg = "Could not deserialize cached valuations, campaign id: {0}".FormatInvariant(
                        (string)(EntityId)campaignEntity.ExternalEntityId);
                LogManager.Log(LogLevels.Error, true, msg);
            }

            return null;
        }

        /// <summary>Get cached valuations and update cache if required.</summary>
        /// <param name="dac">IDynamicAllocationCampaign instance.</param>
        /// <returns>the valuations</returns>
        internal IDictionary<MeasureSet, decimal> GetValuations(IDynamicAllocationCampaign dac)
        {
            return this.GetValuations(dac, false);
        }

        /// <summary>Get cached valuations and update cache if required.</summary>
        /// <param name="dac">IDynamicAllocationCampaign instance.</param>
        /// <param name="suppressCacheUpdate">True to prevent cache from being updated.</param>
        /// <returns>the valuations</returns>
        internal IDictionary<MeasureSet, decimal> GetValuations(
            IDynamicAllocationCampaign dac,
            bool suppressCacheUpdate)
        {
            var campaignEntity = dac.CampaignEntity;

            // If there are no inputs there will not be cached valuations
            // nor can they be calculated
            var valuationInputs = BuildValuationInputs(campaignEntity);

            // Try to get cached valuations
            var valuationInputsFingerprint = valuationInputs.ValuationInputsFingerprint;
            var valuations = TryGetCachedValuations(campaignEntity, valuationInputsFingerprint);
            if (valuations != null)
            {
                return valuations;
            }

            // Couldn't get cached valuations so calculate them
            valuations = CreateValuationsFromInputs(dac.CreateDynamicAllocationEngine(), valuationInputs, campaignEntity);
            
            if (suppressCacheUpdate)
            {
                return valuations;
            }

            // Update the cached valuations
            var cachedValuationsJson = AppsJsonSerializer.SerializeObject(valuations);

            this.SaveCachedValuations(dac, cachedValuationsJson, valuationInputsFingerprint);

            return valuations;
        }

        /// <summary>Save the cached valuations to the latest version of the entity.</summary>
        /// <param name="dac">IDynamicAllocationCampaign instance.</param>
        /// <param name="cachedValuationsJson">The cached valuations json.</param>
        /// <param name="valuationInputsFingerprint">The fingerprint of the inputs from which the cached valuations were calculated.</param>
        /// <returns>True if the save was successful.</returns>
        internal bool SaveCachedValuations(
            IDynamicAllocationCampaign dac, string cachedValuationsJson, string valuationInputsFingerprint)
        {
            var campaignEntity = dac.CampaignEntity;
            var extRequestContext = new RequestContext
                {
                    ExternalCompanyId = dac.CompanyEntity.ExternalEntityId,
                    EntityFilter = new RepositoryEntityFilter(true, true, true, true)
                };

            campaignEntity.SetPropertyByName(
                daName.CachedValuations, cachedValuationsJson, PropertyFilter.Extended);
            campaignEntity.SetPropertyByName(
                daName.ValuationInputsFingerprint, valuationInputsFingerprint, PropertyFilter.System);

            var result = this.Repository.TryForceUpdateEntity(
                extRequestContext,
                campaignEntity,
                new List<string> { daName.CachedValuations, daName.ValuationInputsFingerprint },
                null);

            if (!result)
            {
                // If we fail to save cached valuations, we do not fail the request overall.
                // Log an alert, however.
                LogManager.Log(LogLevels.Error, true, "Could not save cached valuations, campaign id: {0}", campaignEntity.ExternalEntityId);
                return false;
            }

            return true;
        }
    }
}
