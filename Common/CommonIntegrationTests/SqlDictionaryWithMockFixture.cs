//-----------------------------------------------------------------------
// <copyright file="SqlDictionaryWithMockFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using SqlUtilities.Storage;
using TestUtilities;
using Utilities.Data;
using Utilities.Storage;

namespace CommonIntegrationTests
{
    /// <summary>Tests for SqlDictionary</summary>
    /// <remarks>
    /// Not actually integration tests, but must be in the same assembly
    /// as PersistentDictionaryFixtureBase in order to work.
    /// </remarks>
    [TestClass]
    public class SqlDictionaryWithMockFixture : PersistentDictionaryFixtureBase
    {
        /// <summary>Sql connection string</summary>
        private ISqlClient mockSqlClient;

        /// <summary>Dictionary of mock table rows</summary>
        private IDictionary<string, MockTableRow> mockTable;

        /// <summary>Name of the store for the test</summary>
        private string storeName;

        /// <summary>
        /// Whether the mock for the SetEntry procedure should
        /// raise an ETag error when called.
        /// </summary>
        private bool raiseETagError;

        /// <summary>
        /// Sets up the ISqlClient mock and per-test variables
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.raiseETagError = false;
            this.InitializeMockSqlClient();
            this.storeName = Guid.NewGuid().ToString("N");
        }

        /// <summary>Test when an eTag error is raised by the stored procedure</summary>
        /// <remarks>
        /// In most cases eTag conflicts should be spotted by the checks in
        /// SqlDictionary.GetEntry. However, there is a chance that between
        /// that check and the time the stored procedure executes that the
        /// eTag could change. In that case the stored procedure will raise
        /// an error. The InvalidOperationException thrown in this situation
        /// should have the same message as ones thrown from the check within
        /// SqlDictionary.GetEntry and have the original SqlException with the
        /// raised error as its inner exception.
        /// </remarks>
        [TestMethod]
        public void EtagErrorFromStoredProcedure()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            var dictionaryA = this.CreateTestDictionary<string>();
            var dictionaryB = this.CreateTestDictionary<string>();
            
            // Start by triggering the exception from within GetEntry
            InvalidOperationException exception = null;
            dictionaryA[key] = value;
            dictionaryB[key] = value;

            try
            {
                dictionaryA[key] = value;
                Assert.Fail("The expected InvalidOperationException was not thrown");
            }
            catch (InvalidETagException e)
            {
                var message = e.Message.ToUpperInvariant();
                Assert.IsTrue(message.Contains("ETAG"));
                Assert.IsTrue(message.Contains(key.ToUpperInvariant()));
                Assert.IsTrue(message.Contains(this.storeName.ToUpperInvariant()));

                // Verify exception thrown from GetEntry
                var source = e.StackTrace.ToString().Split('\n').FirstOrDefault();
                Assert.IsNotNull(source);
                Assert.IsTrue(source.Contains("GetEntry"));

                exception = e;
            }
            
            this.raiseETagError = true;
            value = dictionaryA[key];
            try
            {
                dictionaryA[key] = value;
                Assert.Fail("Expected InvalidOperationException not thrown");
            }
            catch (InvalidETagException e)
            {
                // Verify messages are the same format (eTag guid, assuming formatted correctly)
                Assert.AreEqual(
                    exception.Message.Substring(exception.Message.IndexOf(')')),
                    e.Message.Substring(e.Message.IndexOf(')')));
                
                // Verify exception was thrown from within SqlDictionaryEntry.WriteAllBytes
                var source = e.StackTrace.ToString().Split('\n').FirstOrDefault();
                Assert.IsNotNull(source);
                Assert.IsTrue(source.Contains("SqlDictionaryEntry.WriteAllBytes"));

                // Verify wrapped SqlException
                var innerException = e.InnerException as SqlException;
                Assert.IsNotNull(innerException);
                var error = innerException.Errors[0];
                Assert.IsNotNull(error);
                Assert.IsTrue(error.Message.ToUpperInvariant().Contains("ETAG"));
            }
        }

        /// <summary>Asserts the underlying store for the dictionary was created</summary>
        protected override void AssertPersistentStoreCreated()
        {
            // Nothing is actually created for SqlDictionary until entries are written
        }

        /// <summary>Asserts the value with the specified <paramref name="key"/> was persisted</summary>
        /// <param name="key">Key for the value</param>
        protected override void AssertValuePersisted(string key)
        {
            Assert.IsNotNull(this.mockTable.Values
                .SingleOrDefault(row =>
                    row.StoreName == this.storeName &&
                    row.EntryName == key));
        }

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected override IPersistentDictionary<TValue> CreateTestDictionary<TValue>(bool raw)
        {
            return new SqlDictionary<TValue>(this.mockSqlClient, this.storeName, raw);
        }

        /// <summary>
        /// Initializes the mock sql client
        /// </summary>
        private void InitializeMockSqlClient()
        {
            this.mockTable = new Dictionary<string, MockTableRow>();
            this.mockSqlClient = MockRepository.GenerateMock<ISqlClient>();

            // GetEntryNames
            this.mockSqlClient.Stub(f => f.ExecuteStoredProcedure(
                Arg<int>.Is.Anything,
                Arg<string>.Is.Equal(Constants.StoredProcedures.GetEntryNames),
                Arg<SqlParameter[]>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var sqlParameters = call.Arguments[2] as SqlParameter[];
                    var storeName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.StoreName)
                        .Value as string;
                    call.ReturnValue = this.mockTable.Values
                        .Where(row => row.StoreName == storeName)
                        .Select(row => new Dictionary<string, object>()
                            {
                                { Constants.SqlResultValues.EntryName, row.EntryName }
                            });
                });

            // GetEntry
            this.mockSqlClient.Stub(f => f.ExecuteStoredProcedure(
                Arg<int>.Is.Anything,
                Arg<string>.Is.Equal(Constants.StoredProcedures.GetEntry),
                Arg<SqlParameter[]>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var sqlParameters = call.Arguments[2] as SqlParameter[];
                    var storeName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.StoreName)
                        .Value as string;
                    var entryName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.EntryName)
                        .Value as string;
                    var entry =
                    call.ReturnValue = this.mockTable.Values
                        .Where(row =>
                            row.StoreName == storeName &&
                            row.EntryName == entryName)
                        .Select(row =>
                            new Dictionary<string, object>()
                            {
                                { Constants.SqlResultValues.Content, row.Content },
                                { Constants.SqlResultValues.ETag, row.ETag },
                                { Constants.SqlResultValues.Compressed, row.Compressed }
                            });
                });

            // DeleteEntries
            this.mockSqlClient.Stub(f => f.ExecuteStoredProcedure(
                Arg<int>.Is.Anything,
                Arg<string>.Is.Equal(Constants.StoredProcedures.DeleteEntries),
                Arg<SqlParameter[]>.Is.Anything))
                .Return(new IDictionary<string, object>[0])
                .WhenCalled(call =>
                {
                    var sqlParameters = call.Arguments[2] as SqlParameter[];
                    var storeName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.StoreName)
                        .Value as string;
                    var removedCount = this.mockTable
                        .Remove(kvp => kvp.Value.StoreName == storeName);
                    call.ReturnValue = new[]
                        {
                            new Dictionary<string, object>
                            {
                                { Constants.SqlResultValues.Count, removedCount }
                            }
                        };
                });

            // DeleteEntry
            this.mockSqlClient.Stub(f => f.ExecuteStoredProcedure(
                Arg<int>.Is.Anything,
                Arg<string>.Is.Equal(Constants.StoredProcedures.DeleteEntry),
                Arg<SqlParameter[]>.Is.Anything))
                .Return(new IDictionary<string, object>[0])
                .WhenCalled(call =>
                {
                    var sqlParameters = call.Arguments[2] as SqlParameter[];
                    var storeName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.StoreName)
                        .Value as string;
                    var entryName = sqlParameters
                        .Single(p => p.ParameterName == Constants.SqlParameterNames.EntryName)
                        .Value as string;
                    var removedCount = this.mockTable
                        .Remove(kvp =>
                            kvp.Value.StoreName == storeName &&
                            kvp.Value.EntryName == entryName);
                    call.ReturnValue = new[]
                        {
                            new Dictionary<string, object>
                            {
                                { Constants.SqlResultValues.Count, removedCount }
                            }
                        };
                });

            // SetEntry
            this.mockSqlClient.Stub(f => f.ExecuteStoredProcedure(
                Arg<int>.Is.Anything,
                Arg<string>.Is.Equal(Constants.StoredProcedures.SetEntry),
                Arg<SqlParameter[]>.Is.Anything))
                .Return(new IDictionary<string, object>[0])
                .WhenCalled(call =>
                {
                    var sqlParameters = call.Arguments[2] as SqlParameter[];

                    var entry = new MockTableRow
                    {
                        StoreName = sqlParameters
                            .Single(p => p.ParameterName == Constants.SqlParameterNames.StoreName)
                            .Value as string,
                        EntryName = sqlParameters
                            .Single(p => p.ParameterName == Constants.SqlParameterNames.EntryName)
                            .Value as string,
                        Content = sqlParameters
                            .Single(p => p.ParameterName == Constants.SqlParameterNames.Content)
                            .Value as byte[],
                        ETag = sqlParameters
                            .Where(p =>
                                p.ParameterName == Constants.SqlParameterNames.ETag &&
                                p.Value != DBNull.Value)
                            .Select(p => (Guid)p.Value)
                            .SingleOrDefault(),
                        Compressed = sqlParameters
                            .Where(p => p.ParameterName == Constants.SqlParameterNames.Compressed)
                            .Select(p => (bool)p.Value)
                            .SingleOrDefault()
                    };

                    var current = this.mockTable.Values
                        .SingleOrDefault(row =>
                            row.StoreName == entry.StoreName &&
                            row.EntryName == entry.EntryName);

                    entry.UID = current != null ?
                        current.UID :
                        Guid.NewGuid().ToString();

                    if (this.raiseETagError ||
                        (current != null && current.ETag != entry.ETag))
                    {
                        throw SqlExceptionFactory.Create(
                            "Invalid ETag",
                            16,
                            123,
                            "The ETag was invalid",
                            50000,
                            Constants.StoredProcedures.SetEntry,
                            @".\\SQLExpress",
                            62);
                    }

                    entry.ETag = Guid.NewGuid();
                    this.mockTable[entry.UID] = entry;

                    call.ReturnValue = new[]
                    {
                        new Dictionary<string, object>
                        {
                            { Constants.SqlResultValues.ETag, entry.ETag }
                        }
                    };
                });
        }

        /// <summary>Mock table row data structure</summary>
        private class MockTableRow
        {
            /// <summary>Gets or sets the unique id</summary>
            public string UID { get; set; }

            /// <summary>Gets or sets the store name</summary>
            public string StoreName { get; set; }

            /// <summary>Gets or sets the entry name</summary>
            public string EntryName { get; set; }

            /// <summary>Gets or sets the content bytes</summary>
            public byte[] Content { get; set; }

            /// <summary>Gets or sets the ETag</summary>
            public Guid ETag { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the content is compressed
            /// </summary>
            public bool Compressed { get; set; }
        }
    }
}
