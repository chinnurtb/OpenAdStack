// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultKeyRule.cs" company="Rare Crowds Inc">
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

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>A default IKeyRule implementation.</summary>
    internal class DefaultKeyRule : IKeyRule
    {
        /// <summary>Generate the key field.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The key field.</returns>
        public string GenerateKeyField(IEntity entity)
        {
            // At present this only handles partition, and there is just a partition per entity type.
            if ((string)entity.EntityCategory == CompanyEntity.CategoryName)
            {
                return "CompanyPartition";
            }

            if ((string)entity.EntityCategory == CampaignEntity.CategoryName)
            {
                return "CampaignPartition";
            }

            if ((string)entity.EntityCategory == UserEntity.CategoryName)
            {
                return "UserPartition";
            }

            return "EntityPartition";
        }
    }
}
