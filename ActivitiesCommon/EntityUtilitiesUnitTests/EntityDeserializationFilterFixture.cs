﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityDeserializationFilterFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityUtilitiesUnitTests
{
    /// <summary>Test fixture for EntitydeserializationFilter.</summary>
    [TestClass]
    public class EntityDeserializationFilterFixture
    {
        /// <summary>Test default construction.</summary>
        [TestMethod]
        public void DefaultConstructor()
        {
            var filter = new EntityDeserializationFilter();
            Assert.IsTrue(filter.IncludeDefaultProperties);
            Assert.IsTrue(filter.IncludeSystemProperties);
            Assert.IsFalse(filter.IncludeExtendedProperties);
            Assert.IsFalse(filter.IncludeAssociations);
            Assert.AreEqual(2, filter.Filters.Count);
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Default));
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.System));
            Assert.IsFalse(filter.ContainsFilter(PropertyFilter.Extended));

            // By default if there is no query string CheckPropertyRegexMatch will succeed
            var entity = new PartnerEntity(new EntityId(), new Entity { EntityCategory = PartnerEntity.PartnerEntityCategory });
            Assert.IsTrue(filter.EntityQueries.CheckPropertyRegexMatch(entity));
        }

        /// <summary>Test EntityActivityQuery construction.</summary>
        [TestMethod]
        public void NonDefaultConstructor()
        {
            var filter = new EntityDeserializationFilter(true, true, true);
            Assert.IsTrue(filter.IncludeDefaultProperties);
            Assert.IsTrue(filter.IncludeSystemProperties);
            Assert.IsTrue(filter.IncludeExtendedProperties);
            Assert.IsTrue(filter.IncludeAssociations);
            Assert.AreEqual(3, filter.Filters.Count);
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Default));
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.System));
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Extended));
        }

        /// <summary>Test filter cloning</summary>
        [TestMethod]
        public void Clone()
        {
            var filter = new EntityDeserializationFilter();
            filter.EntityQueries.QueryStringParams.Add("name", "value");
            var clonedFilter = filter.Clone();

            Assert.AreNotSame(filter, clonedFilter);
            Assert.AreNotSame(filter.EntityQueries, clonedFilter.EntityQueries);
            Assert.AreNotSame(filter.EntityQueries.QueryStringParams, clonedFilter.EntityQueries.QueryStringParams);
            Assert.AreEqual(filter.IncludeDefaultProperties, clonedFilter.IncludeDefaultProperties);
            Assert.AreEqual(filter.IncludeSystemProperties, clonedFilter.IncludeSystemProperties);
            Assert.AreEqual(filter.IncludeExtendedProperties, clonedFilter.IncludeExtendedProperties);
            Assert.AreEqual(filter.IncludeAssociations, clonedFilter.IncludeAssociations);
            Assert.AreEqual("value", clonedFilter.EntityQueries.QueryStringParams["name"]);
        }
    }
}
