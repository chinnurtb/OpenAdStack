//-----------------------------------------------------------------------
// <copyright file="DefaultCampaignConverter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
