// -----------------------------------------------------------------------
// <copyright file="DynamicAllocationCampaign.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;
using Utilities.Serialization;
using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>Helper methods for accessing data from a dynamic allocation campaign entity.</summary>
    public class DynamicAllocationCampaign : IDynamicAllocationCampaign
    {
        /// <summary>Campaign entity id</summary>
        private readonly EntityId campaignEntityId;

        /// <summary>Company entity id</summary>
        private readonly EntityId companyEntityId;

        /// <summary>Backing field for campaign version</summary>
        private int? campaignVersion;

        /// <summary>Backing field for CompanyEntity</summary>
        private CompanyEntity companyEntityBack;

        /// <summary>Backing field for CampiagnEntity</summary>
        private CampaignEntity campaignEntityBack;

        /// <summary>Backing field for CampaignOwner</summary>
        private UserEntity campaignOwnerBack;

        /// <summary>Backing field for CampaignConfig</summary>
        private IConfig campaignConfigBack;

        /// <summary>Backing field for AllocationParameters</summary>
        private AllocationParameters allocationParametersBack;

        /// <summary>Backing field for DeliveryNetwork</summary>
        private DeliveryNetworkDesignation? deliveryNetworkBack;

        /// <summary>Backing field for AllocationHistory</summary>
        private BudgetAllocationHistory budgetAllocationHistoryBack;

        /// <summary>Backing field for RawDeliveryData</summary>
        private RawDeliveryData rawDeliveryDataBack;

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance to use.</param>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        public DynamicAllocationCampaign(
            IEntityRepository repository, EntityId companyEntityId, EntityId campaignEntityId)
            : this(repository, companyEntityId, campaignEntityId, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance to use.</param>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <param name="version">The version of the campaign to use.</param>
        public DynamicAllocationCampaign(
            IEntityRepository repository, EntityId companyEntityId, EntityId campaignEntityId, int? version)
        {
            this.Repository = repository;
            this.companyEntityId = companyEntityId;
            this.campaignEntityId = campaignEntityId;
            this.campaignVersion = version;
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance to use.</param>
        /// <param name="companyEntity">The company entity associated with this DA campaign.</param>
        /// <param name="campaignEntity">The campaign entity associated with this DA campaign.</param>
        internal DynamicAllocationCampaign(
            IEntityRepository repository, CompanyEntity companyEntity, CampaignEntity campaignEntity) 
            : this(repository, companyEntity, campaignEntity, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance to use.</param>
        /// <param name="companyEntity">The company entity associated with this DA campaign.</param>
        /// <param name="campaignEntity">The campaign entity associated with this DA campaign.</param>
        /// <param name="campaignConfig">The campaign config instance to use.</param>
        /// <exception cref="NullReferenceException">Thrown if null entities are passed.</exception>
        internal DynamicAllocationCampaign(
            IEntityRepository repository, CompanyEntity companyEntity, CampaignEntity campaignEntity, IConfig campaignConfig)
            : this(repository, companyEntity.ExternalEntityId, campaignEntity.ExternalEntityId)
        {
            this.companyEntityBack = companyEntity;
            this.campaignEntityBack = campaignEntity;
            this.campaignConfigBack = campaignConfig;
        }

        /// <summary>Gets the company entity associated with the DA Campaign.</summary>
        public CompanyEntity CompanyEntity
        {
            get
            {
                return this.companyEntityBack ?? (this.companyEntityBack = this.GetCompanyEntity());
            }
        }

        /// <summary>Gets the campaign entity associated with the DA Campaign.</summary>
        public CampaignEntity CampaignEntity
        {
            get
            {
                return this.campaignEntityBack ?? (this.campaignEntityBack = this.GetCampaignEntity());
            }
        }

        /// <summary>Gets the user entity owner of the campaign</summary>
        public UserEntity CampaignOwner
        {
            get
            {
                return this.campaignOwnerBack ?? (this.campaignOwnerBack = this.GetCampaignOwner());
            }
        }

        /// <summary>Gets the custom config parameters for the DA Campaign.</summary>
        public IConfig CampaignConfig
        {
            get
            {
                return this.campaignConfigBack ?? (this.campaignConfigBack = 
                    EntityActivityUtilities.BuildCustomConfigFromEntities(true, this.CompanyEntity, this.CampaignEntity));
            }
        }

        /// <summary>Gets the allocation parameters for the DA Campaign.</summary>
        public AllocationParameters AllocationParameters
        {
            get
            {
                return this.allocationParametersBack ?? (this.allocationParametersBack =
                    new AllocationParameters(this.CampaignConfig));
            }
        }

        /// <summary>Gets the default delivery network</summary>
        public DeliveryNetworkDesignation DeliveryNetwork
        {
            get
            {
                if (!this.deliveryNetworkBack.HasValue)
                {
                    this.deliveryNetworkBack = this.RetrieveDeliveryNetwork();
                }

                return this.deliveryNetworkBack.Value;
            }
        }

        /// <summary>Gets an object to manage BudgetAllocationHistory.</summary>
        public IBudgetAllocationHistory BudgetAllocationHistory
        {
            get
            {
                return this.budgetAllocationHistoryBack ?? (this.budgetAllocationHistoryBack = 
                    new BudgetAllocationHistory(this.Repository, this.CompanyEntity, this.CampaignEntity));
            }
        }

        /// <summary>Gets an object to manage RawDeliveryData.</summary>
        public IRawDeliveryData RawDeliveryData
        {
            get
            {
                return this.rawDeliveryDataBack ?? (this.rawDeliveryDataBack =
                    new RawDeliveryData(this.Repository, this.CompanyEntity, this.CampaignEntity));
            }
        }

        /// <summary>Gets the IEntityRepository instance associated with the DA Campaign.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>
        /// Create a DynamicAllocationEngine instance using measure sources, etc from the entities
        /// </summary>
        /// <returns>The created DynamicAllocationEngine instance</returns>
        public IDynamicAllocationEngine CreateDynamicAllocationEngine()
        {
            return new DynamicAllocationEngine(this.RetrieveMeasureMap());
        }

        /// <summary>Gets the AllocationNodeMap or initializes a new one if it doesn't exist</summary>
        /// <returns>The allocation node map (empty if not yet initialized).</returns>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if associated blob entity not found.</exception>
        /// <exception cref="DataAccessTypeMismatchException">Thrown if associated entity is not BlobEntity.</exception>
        public Dictionary<string, MeasureSet> RetrieveAllocationNodeMap()
        {
            var allocationNodeMap = new Dictionary<string, MeasureSet>();

            // see if there's an association to the campaign
            var allocationNodeMapBlobAssociation = this.CampaignEntity.TryGetAssociationByName(daName.AllocationNodeMap);
            if (allocationNodeMapBlobAssociation != null)
            {
                // if there's an association, get the blob
                var allocationNodeMapBlob = this.Repository.GetEntity<BlobEntity>(
                    new RequestContext { ExternalCompanyId = this.CompanyEntity.ExternalEntityId },
                    allocationNodeMapBlobAssociation.TargetEntityId);

                allocationNodeMap = allocationNodeMapBlob.DeserializeBlob<Dictionary<string, MeasureSet>>();
            }

            return allocationNodeMap;
        }

        /// <summary>Creates a measure map for the company and campaign</summary>
        /// <returns>The measure map</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if static measure providers have not been initialized in the runtime context.
        /// </exception>
        public MeasureMap RetrieveMeasureMap()
        {
            // Get the measures version
            int version = this.CampaignEntity.GetExporterVersion();

            // Get the measures sources
            var measureSources = MeasureSourceFactory.CreateMeasureSources(
                this.DeliveryNetwork,
                version,
                this.CompanyEntity,
                this.CampaignEntity,
                this.CampaignOwner);

            // Create a measure map from the sources and return it
            return new MeasureMap(measureSources);
        }

        /// <summary>Get the active budget allocation</summary>
        /// <returns>The active allocation (or null if none associated)</returns>
        public BudgetAllocation RetrieveActiveAllocation()
        {
            BudgetAllocation activeAllocation = null;

            // see if there's an association to the campaign
            var activeAllocationBlobAssociation = this.CampaignEntity.TryGetAssociationByName(daName.AllocationSetActive);
            if (activeAllocationBlobAssociation != null)
            {
                // if there's an association, get the blob
                var activeAllocationBlob = this.Repository.GetEntity<BlobEntity>(
                    new RequestContext { ExternalCompanyId = this.CompanyEntity.ExternalEntityId },
                    activeAllocationBlobAssociation.TargetEntityId);

                activeAllocation = activeAllocationBlob.DeserializeBlob<BudgetAllocation>();
            }

            return activeAllocation;
        }

        /// <summary>
        /// Build a new blob containing the the allocation and associate it to the campaign as the active allocation
        /// </summary>
        /// <param name="newActiveAllocation">The new active allocation</param>
        /// <returns>The created budget allocation blob</returns>
        public BlobEntity CreateAndAssociateActiveAllocationBlob(BudgetAllocation newActiveAllocation)
        {
            // Create a new blob containing the new active allocation
            var activeAllocationJson = AppsJsonSerializer.SerializeObject(newActiveAllocation);
            var activeAllocationBlob = BlobEntity.BuildBlobEntity(new EntityId(), activeAllocationJson);

            // Associate the new active allocation set blob with the campaign and set the initial allocations flag
            this.CampaignEntity.AssociateEntities(
                daName.AllocationSetActive,
                string.Empty,
                new HashSet<IEntity> { activeAllocationBlob },
                AssociationType.Relationship,
                true);

            // Return the created allocation blob
            return activeAllocationBlob;
        }

        /// <summary>
        /// Gets the Delivery Network of a campaign, if available, or of a company if not available.
        /// </summary>
        /// <returns>
        /// Campaign or company delivery network if available;
        /// Otherwise, DeliveryNetworkDesignation.Unknown.
        /// </returns>
        private DeliveryNetworkDesignation RetrieveDeliveryNetwork()
        {
            var deliveryNetwork = this.CampaignEntity.GetDeliveryNetwork();
            if (deliveryNetwork != DeliveryNetworkDesignation.Unknown)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Got delivery network from campaign '{0}' ({1}): {2}",
                    this.CampaignEntity.ExternalName,
                    this.CampaignEntity.ExternalEntityId,
                    deliveryNetwork);
            }
            else
            {
                deliveryNetwork = this.CompanyEntity.GetDeliveryNetwork();
                if (deliveryNetwork != DeliveryNetworkDesignation.Unknown)
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "Got delivery network for campaign '{0}' ({1}) from company '{2}' ({3}): {4}",
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        this.CompanyEntity.ExternalName,
                        this.CompanyEntity.ExternalEntityId,
                        deliveryNetwork);
                }
                else
                {
                    deliveryNetwork = this.GetDefaultDeliveryNetwork();
                    LogManager.Log(
                        LogLevels.Warning,
                        "Invalid/missing delivery network for campaign '{0}' ({1}) and company '{2}' ({3}). Defaulting to {4}",
                        this.CampaignEntity.ExternalName,
                        this.CampaignEntity.ExternalEntityId,
                        this.CompanyEntity.ExternalName,
                        this.CompanyEntity.ExternalEntityId,
                        deliveryNetwork);
                }
            }

            return deliveryNetwork;
        }

        /// <summary>Gets the default delivery network</summary>
        /// <returns>The default delivery network.</returns>
        private DeliveryNetworkDesignation GetDefaultDeliveryNetwork()
        {
            try
            {
                return this.CampaignConfig.GetEnumValue<DeliveryNetworkDesignation>("Delivery.DefaultNetwork");
            }
            catch (ArgumentException)
            {
                return DeliveryNetworkDesignation.Unknown;
            }
        }

        /// <summary>Get the Company Entity.</summary>
        /// <returns>A CompanyEntity.</returns>
        private CompanyEntity GetCompanyEntity()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = this.companyEntityId,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            return this.Repository.GetEntity<CompanyEntity>(context, this.companyEntityId);
        }

        /// <summary>Get the Campaign Entity.</summary>
        /// <returns>A CampaignEntity.</returns>
        private CampaignEntity GetCampaignEntity()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = this.companyEntityId,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            if (this.campaignVersion != null)
            {
                context.EntityFilter.AddVersionToEntityFilter(this.campaignVersion.Value);
            }

            return this.Repository.GetEntity<CampaignEntity>(context, this.campaignEntityId);
        }

        /// <summary>Get the campaign owner User Entity.</summary>
        /// <returns>A UserEntity.</returns>
        private UserEntity GetCampaignOwner()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = this.companyEntityId,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            return this.Repository.GetUser(context, this.CampaignEntity.GetOwnerId());
        }
    }
}
