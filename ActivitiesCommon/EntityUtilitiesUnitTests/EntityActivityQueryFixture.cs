// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityActivityQueryFixture.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using DataAccessLayer;

using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityUtilitiesUnitTests
{
    /// <summary>Test fixture for EntityActivityQuery.</summary>
    [TestClass]
    public class EntityActivityQueryFixture
    {
        /// <summary>Test default construction.</summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            var queries = new EntityActivityQuery();
            Assert.AreEqual(0, queries.QueryStringParams.Count);
        }

        /// <summary>Test non-default construction.</summary>
        [TestMethod]
        public void NonDefaultConstruction()
        {
            var queryStringParams = new Dictionary<string, string>
                {
                    { "Flags", "WithSystemProperties WithExtendedProperties WithAssociations" }
                };

            var queries = new EntityActivityQuery(queryStringParams);
            Assert.AreEqual(queryStringParams, queries.QueryStringParams);
        }

        /// <summary>Test flag checks.</summary>
        [TestMethod]
        [SuppressMessage("Microsoft.Naming", "CA1726", Justification = "Corresponds to Api usage.")]
        public void ContainsFlag()
        {
            var queryStringParams = new Dictionary<string, string>
                {
                    { "Flags", "WithSystemProperties WithAssociations" }
                };

            var queries = new EntityActivityQuery(queryStringParams);
            Assert.IsTrue(queries.ContainsFlag("WithSystemProperties"));
            Assert.IsTrue(queries.ContainsFlag("WithAssociations"));
            Assert.IsFalse(queries.ContainsFlag("WithExtendedProperties"));
        }

        /// <summary>Test property regex.</summary>
        [TestMethod]
        public void CheckPropertyRegexMatch()
        {
            var queryStringParams = new Dictionary<string, string>();

            var queries = new EntityActivityQuery(queryStringParams);
            var rawEntity = new Entity { EntityCategory = PartnerEntity.CategoryName };
            var entity = new PartnerEntity(new EntityId(), rawEntity);
            
            // By default if there is no query we succeed
            Assert.IsTrue(queries.CheckPropertyRegexMatch(entity));

            // If there is a query and it matches succeed (only works currently for top-level IEntity
            // properties - param name must be lower case)
            queryStringParams.Add("entitycategory", PartnerEntity.CategoryName);
            Assert.IsTrue(queries.CheckPropertyRegexMatch(entity));

            // If there is a query and it doesn't match fail
            queryStringParams["entitycategory"] = "Foo";
            Assert.IsFalse(queries.CheckPropertyRegexMatch(entity));
        }
    }
}
