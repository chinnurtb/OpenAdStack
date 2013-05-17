//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryExtensionsFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace CommonUnitTests
{
    /// <summary>Tests for the PersistentDictionaryExtensions</summary>
    [TestClass]
    public class PersistentDictionaryExtensionsFixture
    {
        /// <summary>Clears simulated persistent storage before the tests</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentStorage.Clear();
        }

        /// <summary>Try updating a value</summary>
        [TestMethod]
        public void TryUpdateEntryFunction()
        {
            var dictionary = new MemoryPersistentDictionary<int>("test");
            dictionary["foo"] = 21;
            dictionary.TryUpdateValue("foo", i => i * 2);
            var value = dictionary["foo"];
            Assert.AreEqual(42, value);
        }

        /// <summary>Try updating a value</summary>
        [TestMethod]
        public void TryUpdateEntryAction()
        {
            var dictionary = new MemoryPersistentDictionary<TestReferenceType>("test");
            dictionary["foo"] = new TestReferenceType { Value = 21 };
            dictionary.TryUpdateValue("foo", v => v.Double());
            var value = dictionary["foo"].Value;
            Assert.AreEqual(42, value);
        }

        /// <summary>Try updating a value</summary>
        [TestMethod]
        public void TryUpdateDictionaryEntryAction()
        {
            var dictionary = new MemoryPersistentDictionary<Dictionary<string, int>>("test");
            dictionary["foo"] = new Dictionary<string, int>();
            dictionary.TryUpdateValue("foo", l => l.Add("foo", 21));
            dictionary.TryUpdateValue("foo", l => l.Add("bar", 21));
            var value = dictionary["foo"].Values.Sum();
            Assert.AreEqual(42, value);
        }

        //// TODO: Add tests for unhappy paths

        /// <summary>Test byref type</summary>
        [DataContract]
        private class TestReferenceType
        {
            /// <summary>Gets or sets the value</summary>
            [DataMember]
            public int Value { get; set; }

            /// <summary>Doubles the value</summary>
            public void Double()
            {
                this.Value *= 2;
            }
        }
    }
}
