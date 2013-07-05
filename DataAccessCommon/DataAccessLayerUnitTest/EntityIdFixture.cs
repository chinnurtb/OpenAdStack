// -----------------------------------------------------------------------
// <copyright file="EntityIdFixture.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>
    /// EntityId test fixture.
    /// </summary>
    [TestClass]
    public class EntityIdFixture
    {
        /// <summary>Default contruction - ToString should give us something
        /// that can be parsed to a guid.
        /// </summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            var id = new EntityId();
            Guid guid;
            Assert.IsNotNull(Guid.TryParse(id.ToString(), out guid));
        }

        /// <summary>Test initialize from Guid.</summary>
        [TestMethod]
        public void InitializeFromGuid()
        {
            var guid = Guid.NewGuid();
            var expectedString = GetCanonicalIdString(guid);
            var id = new EntityId(guid);
            Assert.AreEqual(expectedString, id.ToString());
            id = guid;
            Assert.AreEqual(expectedString, id.ToString());
        }

        /// <summary>Test initialize from null and empty Guid</summary>
        [TestMethod]
        public void InitializeFromEmptyGuid()
        {
            var guid = Guid.Empty;

            // All zeros
            var expectedString = GetCanonicalIdString(guid);
            var id = new EntityId(guid);
            Assert.AreEqual(expectedString, id.ToString());

            // Round-trip for a string of all zeros
            id = new EntityId(expectedString);
            Assert.AreEqual(Guid.Empty, (Guid)id);
        }

        /// <summary>Test assignment to Guid.</summary>
        [TestMethod]
        public void AssignToGuid()
        {
            var guid1 = Guid.NewGuid();
            var id = new EntityId(guid1);
            Guid guid2 = id;
            Assert.AreEqual(guid1, guid2);
        }

        /// <summary>Test assignment of null to Guid.</summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void AssignNullToGuid()
        {
            Guid badGuid = (EntityId)null;
            Assert.Fail();
        }

        /// <summary>Test assignment to string.</summary>
        [TestMethod]
        public void AssignToString()
        {
            var id = new EntityId();
            var idStr = (string)id;
            Assert.AreEqual(idStr, id.ToString());
            Assert.AreEqual(idStr, "{0}".FormatInvariant(id));
        }

        /// <summary>Test assignment of null to string.</summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void AssignNullToString()
        {
            string badStr = (EntityId)null;
            Assert.Fail();
        }

        /// <summary>Test initialize from string representation.</summary>
        [TestMethod]
        public void InitializeFromString()
        {
            var idString = GetCanonicalIdString(Guid.NewGuid());
            var id = new EntityId(idString);
            Assert.AreEqual(idString, id.ToString());
            Assert.IsTrue(EntityId.IsValidEntityId(idString));
        }

        /// <summary>Must be able to interpret string as Guid or Int64</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeFromStringInvalidFormat()
        {
            new EntityId("xyz");
            Assert.IsFalse(EntityId.IsValidEntityId("xyz"));
        }
        
        /// <summary>Null EntityId should throw</summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void InitializeFromNullEntityId()
        {
            new EntityId((EntityId)null);
        }

        /// <summary>Null string should throw</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeFromNullString()
        {
            new EntityId((string)null);
        }

        /// <summary>Test initialize from a long.</summary>
        [TestMethod]
        public void InitializeFromLong()
        {
            // Maximum value for a long
            var longId = long.MaxValue;
            var id = new EntityId(longId);
            Assert.AreEqual("00000000000009223372036854775807", id.ToString());
            id = longId;
            Assert.AreEqual("00000000000009223372036854775807", id.ToString());
        }

        /// <summary>Test initialize from a negative long.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeFromLongNegative()
        {
            ulong longId = 0xFFFFFFFFFFFFFFFF;
            new EntityId((long)longId);
        }

        /// <summary>Test equality operations</summary>
        [TestMethod]
        public void Equality()
        {
            var e1 = new EntityId();
            var e2 = new EntityId();
            Assert.IsFalse(e1 == e2);
            Assert.IsTrue(e1 != e2);

            var ee1 = new EntityId(e1);
            var ee2 = new EntityId(e2);
            Assert.IsFalse(ee1 == ee2);
            Assert.IsTrue(ee1 != ee2);

            Assert.IsTrue(e1 == ee1);
            Assert.IsTrue(e1.Equals(ee1));
            Assert.IsTrue(e1.Equals((object)ee1));
        }

        /// <summary>
        /// Get a canonical string (for EntityId) from a guid.
        /// </summary>
        /// <param name="guid">The guid.</param>
        /// <returns>Canonical string representation.</returns>
        private static string GetCanonicalIdString(Guid guid)
        {
            return guid.ToString("N", CultureInfo.InvariantCulture);
        }
    }
}
