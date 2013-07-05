// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test Fixture for Entity class.</summary>
    [TestClass]
    public class EntityFixture
    {
        /// <summary>ExternalType of an 'Agency' company.</summary>
        private string agencyExternalType = "Company.Agency";

        /// <summary>Interface properties are backed by their own property bag.</summary>
        [TestMethod]
        public void GetSetInterfacePropertyUsersPropertyBag()
        {
            var entity = new Entity();

            // Property bag should be empty
            Assert.AreEqual(0, entity.InterfaceProperties.Count);

            // Assign properties as un-named EntityProperties
            entity.ExternalEntityId = new EntityId();
            entity.EntityCategory = CompanyEntity.CategoryName;
            entity.CreateDate = DateTime.UtcNow;
            entity.LastModifiedDate = DateTime.UtcNow;
            entity.ExternalName = "CompanyFoo";
            entity.ExternalType = this.agencyExternalType;
            entity.LocalVersion = 1;
            entity.LastModifiedUser = "abc123";
            entity.SchemaVersion = 0;

            // Verify that properties are correctly added to the property bag with the correct name
            Assert.AreEqual(9, entity.InterfaceProperties.Count);
            Assert.AreSame(entity.ExternalEntityId, entity.InterfaceProperties.Single(p => p.Name == "ExternalEntityId"));
            Assert.AreSame(entity.EntityCategory, entity.InterfaceProperties.Single(p => p.Name == "EntityCategory"));
            Assert.AreSame(entity.CreateDate, entity.InterfaceProperties.Single(p => p.Name == "CreateDate"));
            Assert.AreSame(entity.LastModifiedDate, entity.InterfaceProperties.Single(p => p.Name == "LastModifiedDate"));
            Assert.AreSame(entity.ExternalName, entity.InterfaceProperties.Single(p => p.Name == "ExternalName"));
            Assert.AreSame(entity.ExternalType, entity.InterfaceProperties.Single(p => p.Name == "ExternalType"));
            Assert.AreSame(entity.LocalVersion, entity.InterfaceProperties.Single(p => p.Name == "LocalVersion"));
            Assert.AreSame(entity.LastModifiedUser, entity.InterfaceProperties.Single(p => p.Name == "LastModifiedUser"));
            Assert.AreSame(entity.SchemaVersion, entity.InterfaceProperties.Single(p => p.Name == "SchemaVersion"));
        }
    }
}
