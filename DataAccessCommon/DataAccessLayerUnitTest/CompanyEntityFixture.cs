// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompanyEntityFixture.cs" company="Rare Crowds Inc">
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

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test CompanyEntity class.</summary>
    [TestClass]
    public class CompanyEntityFixture
    {
        /// <summary>Entity object with company properties for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = new Entity
            {
                ExternalEntityId = new EntityProperty("ExternalEntityId", new EntityId()),
                ExternalName = new EntityProperty("ExternalName", TestEntityBuilder.ExternalName),
                EntityCategory = new EntityProperty("EntityCategory", CompanyEntity.CategoryName),
                ExternalType = new EntityProperty("ExternalType", TestEntityBuilder.AgencyExternalType),
                CreateDate = new EntityProperty("CreateDate", DateTime.Now),
                LastModifiedDate = new EntityProperty("LastModifiedDate", DateTime.Now),
                LocalVersion = new EntityProperty("LocalVersion", 1),
                Key = MockRepository.GenerateStub<IStorageKey>()
            };
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var companyEntity = new CompanyEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, companyEntity.WrappedEntity);

            var blobEntityBase = this.wrappedEntity.BuildWrappedEntity();
            Assert.AreSame(this.wrappedEntity, blobEntityBase.SafeUnwrapEntity());
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var companyEntity = new CompanyEntity(this.wrappedEntity);
            var companyEntityWrap = new CompanyEntity(companyEntity);
            Assert.AreSame(this.wrappedEntity, companyEntityWrap.WrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Company.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void FailValidationIfCategoryPropertyNotCompany()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new CompanyEntity(this.wrappedEntity);
        }
    }
}
