// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssociationFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
                TargetEntityCategory = CompanyEntity.CompanyEntityCategory,
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
            differByTargetEntityCategory.TargetEntityCategory = UserEntity.UserEntityCategory;
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
