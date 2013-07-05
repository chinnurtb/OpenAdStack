// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartnerEntityFixture.cs" company="Rare Crowds Inc">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test PartnerEntity class.</summary>
    [TestClass]
    public class PartnerEntityFixture
    {
        /// <summary>Wrapped Entity object with for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", PartnerEntity.CategoryName),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.ExternalType)
            };
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var partnerEntity = new PartnerEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, partnerEntity.WrappedEntity);

            var blobEntityBase = this.wrappedEntity.BuildWrappedEntity();
            Assert.AreSame(this.wrappedEntity, blobEntityBase.SafeUnwrapEntity());
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var partnerEntity = new PartnerEntity(this.wrappedEntity);
            var partnerEntityWrap = new PartnerEntity(partnerEntity);
            Assert.AreSame(this.wrappedEntity, partnerEntityWrap.WrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Partner.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void FailValidationIfCategoryPropertyNotPartner()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new PartnerEntity(this.wrappedEntity);
        }
    }
}
