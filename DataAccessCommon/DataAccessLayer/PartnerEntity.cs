// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartnerEntity.cs" company="Rare Crowds Inc">
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

namespace DataAccessLayer
{
    /// <summary>
    /// An entity wrapper for partner-defined entities that do not well-known properties.
    /// </summary>
    public class PartnerEntity : EntityWrapperBase
    {
        /// <summary>Category Name for Partner Entities.</summary>
        public const string CategoryName = "Partner";

        /// <summary>Initializes a new instance of the <see cref="PartnerEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public PartnerEntity(IEntity entity)
        {
            this.Initialize(entity);
        }

        /// <summary>Initializes a new instance of the <see cref="PartnerEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public PartnerEntity(EntityId externalEntityId, IEntity rawEntity)
        {
            this.Initialize(externalEntityId, CategoryName, rawEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="PartnerEntity"/> class.</summary>
        public PartnerEntity()
        {
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected override void ValidateEntityType(IEntity entity)
        {
            ThrowIfCategoryMismatch(entity, CategoryName);
        }
    }
}
