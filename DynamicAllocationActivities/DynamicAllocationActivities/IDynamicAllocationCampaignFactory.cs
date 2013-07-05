// -----------------------------------------------------------------------
// <copyright file="IDynamicAllocationCampaignFactory.cs" company="Rare Crowds Inc">
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

using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Interface defining a factory for IDynamicAllocationCampaign objects
    /// </summary>
    public interface IDynamicAllocationCampaignFactory
    {
        /// <summary>Bind the factory to runtime objects.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        void BindRuntime(IEntityRepository repository);

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        IDynamicAllocationCampaign MigrateDynamicAllocationCampaign(EntityId companyEntityId, EntityId campaignEntityId);

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        IDynamicAllocationCampaign BuildDynamicAllocationCampaign(EntityId companyEntityId, EntityId campaignEntityId);

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaign"/> class.</summary>
        /// <param name="companyEntityId">The entity id of the company entity associated with this DA campaign.</param>
        /// <param name="campaignEntityId">The entity id of the campaign entity associated with this DA campaign.</param>
        /// <param name="useApprovedInputs">True to use the version of the campaign with approved inputs.</param>
        /// <returns>A newly created DynamicAllocationCampaign.</returns>
        IDynamicAllocationCampaign BuildDynamicAllocationCampaign(
            EntityId companyEntityId, EntityId campaignEntityId, bool useApprovedInputs);
    }
}