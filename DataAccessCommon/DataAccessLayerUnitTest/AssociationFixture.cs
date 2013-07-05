// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssociationFixture.cs" company="Rare Crowds Inc">
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
    using System;
    using System.Collections.Generic;

    /// <summary>Test fixture for Association class.</summary>
    [TestClass]
    public class AssociationFixture
    {
        /// <summary>Test association</summary>
        private Association association1;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.association1 = new Association
            {
                ExternalName = "MyFoos",
                AssociationType = AssociationType.Relationship,
                TargetEntityId = 1,
                TargetEntityCategory = CompanyEntity.CategoryName,
                TargetExternalType = "targetfoo"
            };
        }

        /// <summary>Test that equality operators are correct.</summary>
        [TestMethod]
        public void Equality()
        {
            var assocCopy = new Association(this.association1);
            Assert.IsTrue(this.association1 == assocCopy);
            Assert.IsTrue(this.association1.Equals(assocCopy));
            Assert.IsTrue(this.association1.Equals((object)assocCopy));

            var differByTargetEntityId = new Association(assocCopy);
            differByTargetEntityId.TargetEntityId = 2;
            Assert.IsFalse(this.association1 == differByTargetEntityId);
            Assert.IsTrue(this.association1 != differByTargetEntityId);

            var differByTargetEntityCategory = new Association(assocCopy);
            differByTargetEntityCategory.TargetEntityCategory = UserEntity.CategoryName;
            Assert.IsFalse(this.association1 == differByTargetEntityCategory);
            Assert.IsTrue(this.association1 != differByTargetEntityCategory);

            var differByName = new Association(assocCopy);
            differByName.ExternalName = "foo1";
            Assert.IsFalse(this.association1 == differByName);
            Assert.IsTrue(this.association1 != differByName);

            var differByType = new Association(assocCopy);
            differByType.AssociationType = AssociationType.Child;
            Assert.IsFalse(assocCopy == differByType);
            Assert.IsTrue(assocCopy != differByType);

            var differByTargetType = new Association(assocCopy);
            differByTargetType.TargetExternalType = "targetfoo1";
            Assert.IsFalse(this.association1 == differByTargetType);
            Assert.IsTrue(this.association1 != differByTargetType);
        }
    }
}
