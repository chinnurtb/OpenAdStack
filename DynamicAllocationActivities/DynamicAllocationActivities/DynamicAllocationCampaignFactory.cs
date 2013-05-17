// -----------------------------------------------------------------------
// <copyright file="DynamicAllocationCampaignFactory.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Activities;
using DataAccessLayer;
using Diagnostics;
using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// IDynamicAllocationCampaign object factory
    /// </summary>
    public class DynamicAllocationCampaignFactory : IDynamicAllocationCampaignFactory
    {
        /// <summary>Gets or sets the IEntityRepository object</summary>
        internal IEntityRepository Repository { get; set; }

        /// <summary>Gets or sets the IEntityConverter object</summary>
        internal IEntityConverter Converter { get; set; }

        /// <summary>Bind the factory to runtime objects.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        public void BindRuntime(IEntityRepository repository)
        {
            BindRuntime(repository, new DefaultCampaignConverter(repository));
        }

        /// <summary>Bind the factory to runtime objects.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        /// <param name="converter">The IEntityConverter instance.</param>
        public void BindRuntime(IEntityRepository repository, IEntityConverter converter)
        {
            this.Repository = repository;
            this.Converter = converter;
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        /// <exception cref="ArgumentNullException">Thrown if null parameters are passed.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if company or campaign cannot be retrieved.</exception>
        /// <exception cref="DataAccessTypeMismatchException">Thrown if company or campaign id's are not the correct entity category.</exception>
        public IDynamicAllocationCampaign MigrateDynamicAllocationCampaign(
            EntityId companyEntityId, EntityId campaignEntityId)
        {
            this.Converter.ConvertEntity(campaignEntityId, companyEntityId);
            return new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId);
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        /// <exception cref="ArgumentNullException">Thrown if null parameters are passed.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if company or campaign cannot be retrieved.</exception>
        /// <exception cref="DataAccessTypeMismatchException">Thrown if company or campaign id's are not the correct entity category.</exception>
        public IDynamicAllocationCampaign BuildDynamicAllocationCampaign(
            EntityId companyEntityId, EntityId campaignEntityId)
        {
            return this.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId, false);
        }

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <param name="useApprovedInputs">True to use the version of the campaign with approved inputs.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        /// <exception cref="ArgumentNullException">Thrown if null parameters are passed.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if company or campaign cannot be retrieved.</exception>
        /// <exception cref="DataAccessTypeMismatchException">Thrown if company or campaign id's are not the correct entity category.</exception>
        public IDynamicAllocationCampaign BuildDynamicAllocationCampaign(
            EntityId companyEntityId, EntityId campaignEntityId, bool useApprovedInputs)
        {
            var dac = new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId);
            if (useApprovedInputs)
            {
                var inputsApprovedVersion = dac.CampaignEntity.TryGetPropertyByName(daName.InputsApprovedVersion, -1);
                if (inputsApprovedVersion == -1)
                {
                    var msg = "Cannot retrieve approved inputs for campaign tha has not been approved: {0}"
                        .FormatInvariant(campaignEntityId.ToString());
                    LogManager.Log(LogLevels.Error, msg);
                    throw new ActivityException(ActivityErrorId.GenericError, msg);
                }

                dac = new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId, inputsApprovedVersion);
            }

            return dac;
        }
    }
}
