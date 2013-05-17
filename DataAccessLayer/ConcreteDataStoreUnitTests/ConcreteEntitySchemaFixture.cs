// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConcreteEntitySchemaFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for ConcreteEntitySchema</summary>
    [TestClass]
    public class ConcreteEntitySchemaFixture
    {
        /// <summary>Schema version is no longer zero.</summary>
        [TestMethod]
        public void CurrentVersionGreaterThanZero()
        {
            var entitySchema = new ConcreteEntitySchema();
            Assert.IsTrue(entitySchema.CurrentSchemaVersion > 0);
        }

        /// <summary>NameEncoding feature recognized in version 1.</summary>
        [TestMethod]
        public void NameEncodingFeatureRecognized()
        {
            var entitySchema = new ConcreteEntitySchema();
            Assert.IsFalse(entitySchema.CheckSchemaFeature(0, EntitySchemaFeatureId.NameEncoding));
            Assert.IsTrue(entitySchema.CheckSchemaFeature(1, EntitySchemaFeatureId.NameEncoding));
            Assert.IsTrue(entitySchema.CheckSchemaFeature(2, EntitySchemaFeatureId.NameEncoding));
        }
    }
}
