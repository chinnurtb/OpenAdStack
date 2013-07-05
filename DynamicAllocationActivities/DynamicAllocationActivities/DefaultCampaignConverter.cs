//-----------------------------------------------------------------------
// <copyright file="DefaultCampaignConverter.cs" company="Rare Crowds Inc">
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

using DataAccessLayer;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>Default converter for legacy DA campaigns to current formats.</summary>
    public class DefaultCampaignConverter : IEntityConverter
    {
        /// <summary>Initializes a new instance of the <see cref="DefaultCampaignConverter"/> class. </summary>
        /// <param name="repository">A repository instance.</param>
        public DefaultCampaignConverter(IEntityRepository repository)
        {
            this.Repository = repository;
        }

        /// <summary>Gets the IEntityRepository instance associated with the DA Campaign.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Convert an entity to the current schema for that entity.</summary>
        /// <param name="entityToConvert">The entity id to convert.</param>
        /// <param name="companyEntityId">The entity id of the company the entity belongs to.</param>
        public void ConvertEntity(EntityId entityToConvert, EntityId companyEntityId)
        {
            // No currently supported legacy formats. Do nothing.
        }
    }
}
