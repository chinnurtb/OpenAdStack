// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestEntity.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;
using EntityUtilities;

namespace EntityUtilitiesUnitTests
{
    /// <summary>Concrete implementation of derived entity for testing.</summary>
    internal class TestEntity : EntityWrapperBase
    {
        /// <summary>Entity category for TestEntity</summary>
        public const string TestEntityCategory = "Entity";

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="wrappedEntity">The entity to wrap.</param>
        public TestEntity(IRawEntity wrappedEntity)
        {
            this.Initialize(wrappedEntity);
        }

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="entityCategory">The Entity Category.</param>
        /// <param name="jsonEntity">The JSON entity from which to construct.</param>
        public TestEntity(EntityId externalEntityId, string entityCategory, string jsonEntity)
        {
            this.Initialize(
                externalEntityId,
                entityCategory,
                EntityJsonSerializer.DeserializeEntity(jsonEntity));
        }

        /// <summary>Initializes a new instance of the <see cref="TestEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="entityCategory">The Entity Category.</param>
        /// <param name="rawEntity">The raw entity from which to construct.</param>
        public TestEntity(EntityId externalEntityId, string entityCategory, IRawEntity rawEntity)
        {
            this.Initialize(externalEntityId, entityCategory, rawEntity);
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="wrappedEntity">The entity.</param>
        public override void ValidateEntityType(IRawEntity wrappedEntity)
        {
            ThrowIfCategoryMismatch(wrappedEntity, TestEntityCategory);
        }
    }
}