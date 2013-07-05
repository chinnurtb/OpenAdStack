// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestEntity.cs" company="Rare Crowds Inc">
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

namespace DataAccessLayerUnitTests
{
    /// <summary>Concrete implementation of derived entity for testing.</summary>
    public class TestEntity : EntityWrapperBase
    {
        /// <summary>Entity category for TestEntity</summary>
        public const string CategoryName = "Entity";

        /// <summary>non-null if there is a required property</summary>
        private string requiredProperty = null;

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="wrappedEntity">The entity to wrap.</param>
        public TestEntity(IEntity wrappedEntity)
        {
            this.Initialize(wrappedEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="wrappedEntity">The entity to wrap.</param>
        /// <param name="requiredProperty">The required Property.</param>
        public TestEntity(IEntity wrappedEntity, string requiredProperty)
        {
            this.requiredProperty = requiredProperty;
            this.Initialize(wrappedEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="entityCategory">The Entity Category.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public TestEntity(EntityId externalEntityId, string entityCategory, IEntity rawEntity)
        {
            this.Initialize(externalEntityId, entityCategory, rawEntity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected override void ValidateEntityType(IEntity entity)
        {
            ThrowIfCategoryMismatch(entity, CategoryName);

            if (this.requiredProperty != null)
            {
                ThrowIfPropertyNotDefined(entity, CategoryName, this.requiredProperty);
            }
        }
    }
}