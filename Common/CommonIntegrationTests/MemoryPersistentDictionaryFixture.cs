//-----------------------------------------------------------------------
// <copyright file="MemoryPersistentDictionaryFixture.cs" company="Emerging Media Group">
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

namespace CommonIntegrationTests
{
    /// <summary>Tests for the simulated, in-memory IPersistentDictionary</summary>
    [TestClass]
    public class MemoryPersistentDictionaryFixture : PersistentDictionaryFixtureBase
    {
        /// <summary>Name of the store for the test</summary>
        private string storeName;

        /// <summary>Sets the address of the container to be used for the test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.storeName = Guid.NewGuid().ToString("N");
            SimulatedPersistentStorage.DeleteIfExists(this.storeName);
        }

        /// <summary>Deletes the container used by the test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            SimulatedPersistentStorage.DeleteIfExists(this.storeName);
        }

        /// <summary>Asserts the underlying store for the dictionary was created</summary>
        protected override void AssertPersistentStoreCreated()
        {
            Assert.IsFalse(SimulatedPersistentStorage.CreateIfNotExist(this.storeName), "Store was not created");
        }

        /// <summary>Asserts the value with the specified <paramref name="key"/> was persisted</summary>
        /// <param name="key">Key for the value</param>
        protected override void AssertValuePersisted(string key)
        {
            var entry = SimulatedPersistentStorage.GetEntry(this.storeName, key);
            Assert.IsNotNull(entry);
            Assert.IsTrue(SimulatedPersistentStorage.DeleteEntryIfExists(this.storeName, key), "Entry did not exist");
        }

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected override IPersistentDictionary<TValue> CreateTestDictionary<TValue>(bool raw)
        {
            return new MemoryPersistentDictionary<TValue>(this.storeName, raw);
        }
    }
}
