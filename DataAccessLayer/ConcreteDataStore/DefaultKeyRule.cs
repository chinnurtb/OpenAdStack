// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultKeyRule.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>A default IKeyRule implementation.</summary>
    internal class DefaultKeyRule : IKeyRule
    {
        /// <summary>Generate the key field.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The key field.</returns>
        public string GenerateKeyField(IRawEntity entity)
        {
            // At present this only handles partition, and there is just a partition per entity type.
            if ((string)entity.EntityCategory == CompanyEntity.CompanyEntityCategory)
            {
                return "CompanyPartition";
            }

            if ((string)entity.EntityCategory == CampaignEntity.CampaignEntityCategory)
            {
                return "CampaignPartition";
            }

            if ((string)entity.EntityCategory == UserEntity.UserEntityCategory)
            {
                return "UserPartition";
            }

            return "EntityPartition";
        }
    }
}
