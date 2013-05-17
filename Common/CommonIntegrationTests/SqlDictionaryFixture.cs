//-----------------------------------------------------------------------
// <copyright file="SqlDictionaryFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlUtilities.Storage;
using TestUtilities;
using Utilities.Storage;

namespace CommonIntegrationTests
{
    /// <summary>Tests for SqlDictionary</summary>
    [TestClass]
    public class SqlDictionaryFixture : PersistentDictionaryFixtureBase
    {
        /// <summary>storeName for the test</summary>
        private string storeName;

        /// <summary>Gets the SQL connection string</summary>
        private static string ConnectionString
        {
            get { return ConfigurationManager.AppSettings["SqlDictionaryConnectionString"]; }
        }

        /// <summary>Cleanup created SqlDictionary tables</summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand("DELETE [Entry]", connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Sets the address of the container to be used for the test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.storeName = Guid.NewGuid().ToString("N");

            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand("DELETE [Entry]", connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>Asserts the underlying store for the dictionary was created</summary>
        protected override void AssertPersistentStoreCreated()
        {
            // Nothing is actually inserted into the database table until an entry is set
        }

        /// <summary>Asserts the value with the specified <paramref name="key"/> was persisted</summary>
        /// <param name="key">Key for the value</param>
        protected override void AssertValuePersisted(string key)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var command = new SqlCommand(
                    "SELECT COUNT(*) FROM [Entry] WHERE EntryName=N'{0}' AND StoreName=N'{1}';"
                    .FormatInvariant(key, this.storeName),
                    connection);
                connection.Open();
                var result = command.ExecuteScalar();
                Assert.AreEqual(1, result);
            }
        }

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected override IPersistentDictionary<TValue> CreateTestDictionary<TValue>(bool raw)
        {
            return new SqlDictionary<TValue>(ConnectionString, this.storeName, raw);
        }
    }
}
