// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntitySerializationFilterFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityUtilitiesUnitTests
{
    /// <summary>Test fixture for EntitySerializationFilter.</summary>
    [TestClass]
    public class EntitySerializationFilterFixture
    {
        /// <summary>Test default construction.</summary>
        [TestMethod]
        public void DefaultConstructor()
        {
            var filter = new EntitySerializationFilter();
            AssertDefaultConstruction(filter);
        }

        /// <summary>Test EntityActivityQuery construction.</summary>
        [TestMethod]
        public void EntityActivityQueryConstructor()
        {
            var entityQueries = new EntityActivityQuery(null);
            var filter = new EntitySerializationFilter(entityQueries);
            AssertDefaultConstruction(filter);
            Assert.AreSame(entityQueries, filter.EntityQueries);
        }

        /// <summary>Test QueryStringParam construction.</summary>
        [TestMethod]
        public void QueryStringParamConstructor()
        {
            var queryStringParams = new Dictionary<string, string>();
            var filter = new EntitySerializationFilter(queryStringParams);
            AssertDefaultConstruction(filter);
            Assert.AreSame(queryStringParams, filter.EntityQueries.QueryStringParams);
        }

        /// <summary>Test flags are set correctly with non-default input.</summary>
        [TestMethod]
        public void QueryStringSetNonDefault()
        {
            var queryStringParams = new Dictionary<string, string>
                {
                    { "Flags", "WithSystemProperties WithExtendedProperties WithAssociations" }
                };
            var filter = new EntitySerializationFilter(queryStringParams);
            Assert.IsTrue(filter.IncludeDefaultProperties);
            Assert.IsTrue(filter.IncludeSystemProperties);
            Assert.IsTrue(filter.IncludeExtendedProperties);
            Assert.IsTrue(filter.IncludeAssociations);
            Assert.AreEqual(3, filter.Filters.Count);
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Default));
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.System));
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Extended));

            Assert.IsNotNull(filter.EntityQueries);
            Assert.AreSame(queryStringParams, filter.EntityQueries.QueryStringParams);
        }

        /// <summary>Test filter cloning</summary>
        [TestMethod]
        public void Clone()
        {
            var filter = new EntitySerializationFilter();
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

        /// <summary>Assert default construction behavior</summary>
        /// <param name="filter">The IEntity</param>
        private static void AssertDefaultConstruction(EntitySerializationFilter filter)
        {
            Assert.IsTrue(filter.IncludeDefaultProperties);
            Assert.IsFalse(filter.IncludeSystemProperties);
            Assert.IsFalse(filter.IncludeExtendedProperties);
            Assert.IsFalse(filter.IncludeAssociations);
            Assert.AreEqual(1, filter.Filters.Count);
            Assert.IsTrue(filter.ContainsFilter(PropertyFilter.Default));
            Assert.IsFalse(filter.ContainsFilter(PropertyFilter.System));
            Assert.IsFalse(filter.ContainsFilter(PropertyFilter.Extended));

            // By default if there is no query string CheckPropertyRegexMatch will succeed
            Assert.IsNotNull(filter.EntityQueries);
            var entity = new PartnerEntity(new EntityId(), new Entity { EntityCategory = PartnerEntity.PartnerEntityCategory });
            Assert.IsTrue(filter.EntityQueries.CheckPropertyRegexMatch(entity));
        }
    }
}
