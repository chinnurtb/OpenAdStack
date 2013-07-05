//-----------------------------------------------------------------------
// <copyright file="SqlUserAccessDataStoreFixture.cs" company="Rare Crowds Inc.">
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
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for SqlUserAccessDataStore</summary>
    [TestClass]
    public class SqlUserAccessDataStoreFixture
    {
        /// <summary>Sql store stub for testing.</summary>
        private ISqlStore sqlStore;

        /// <summary>Access descriptor for testing.</summary>
        private string accessDescriptor;

        /// <summary>Access list for testing.</summary>
        private List<string> accessList;

        /// <summary>Per-test intialization.</summary>
        [TestInitialize]
        public void TestInitializer()
        {
            this.accessDescriptor = "SomeAccess";
            this.accessList = new List<string> { this.accessDescriptor };
        }

        /// <summary>Test default construction of SqlUserAccessDataStore.</summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            // Assert store returned by factory is correctly constructed
            var userAccessStoreFactory = new SqlUserAccessStoreFactory("fooConnection");
            var userAccessStore = userAccessStoreFactory.GetUserAccessStore() as SqlUserAccessDataStore;
            Assert.IsNotNull(userAccessStore);
            Assert.IsNotNull(userAccessStore.SqlStore);
            Assert.IsTrue(userAccessStore.SqlStore is ConcreteSqlStore);
            Assert.AreEqual("fooConnection", userAccessStore.SqlStore.ConnectionString);
        }

        /// <summary>Get user access list - happy path.</summary>
        [TestMethod]
        public void GetUserAccessListSuccess()
        {
            var queryRecord = new QueryRecord(new Dictionary<string, object> { { "AccessDescriptor", this.accessDescriptor } });
            var sqlResult = new QueryResult();
            sqlResult.AddRecordSet();
            sqlResult.AddRecord(queryRecord, 0);
            var userAccessStore = this.SetupUserAccessDataStore(sqlResult);

            var actualAccessList = userAccessStore.GetUserAccessList(new EntityId()).ToList();

            Assert.IsTrue(actualAccessList.Contains(this.accessDescriptor));
            Assert.AreEqual(1, actualAccessList.Count());
        }

        /// <summary>Get user access list - empty OK.</summary>
        [TestMethod]
        public void GetUserAccessListEmptyOk()
        {
            var sqlResult = new QueryResult();
            var userAccessStore = this.SetupUserAccessDataStore(sqlResult);

            var actualAccessList = userAccessStore.GetUserAccessList(new EntityId()).ToList();

            Assert.AreEqual(0, actualAccessList.Count());
        }

        /// <summary>Get user access list - fails because of a sql exception or connection issue.</summary>
        [TestMethod]
        public void GetUserAccessListSqlFail()
        {
            var userAccessStore = this.SetupUserAccessDataStore(null);

            var actualAccessList = userAccessStore.GetUserAccessList(new EntityId()).ToList();

            // This will return an empty list and AuthZ will fail
            Assert.AreEqual(0, actualAccessList.Count());
        }

        /// <summary>Add user access list - happy path.</summary>
        [TestMethod]
        public void AddUserAccessListSuccess()
        {
            // Just need a non-null result
            var sqlResult = new QueryResult();
            var userAccessStore = this.SetupUserAccessDataStore(sqlResult);

            var result = userAccessStore.AddUserAccessList(new EntityId(), this.accessList);

            // SqlStore should be called with our new access descriptor
            this.AssertSqlStoreCalled(this.accessDescriptor);

            // Result should be true
            Assert.IsTrue(result);
        }

        /// <summary>Add user access list failure.</summary>
        [TestMethod]
        public void AddUserAccessListFail()
        {
            // There will be a null sql result
            var userAccessStore = this.SetupUserAccessDataStore(null);

            var result = userAccessStore.AddUserAccessList(new EntityId(), this.accessList);

            // SqlStore should be called with our new access descriptor
            this.AssertSqlStoreCalled(this.accessDescriptor);

            // Result should be false
            Assert.IsFalse(result);
        }

        /// <summary>Remove user access list - happy path.</summary>
        [TestMethod]
        public void RemoveUserAccessListSuccess()
        {
            // Just need a non-null result
            var sqlResult = new QueryResult();
            var userAccessStore = this.SetupUserAccessDataStore(sqlResult);

            var result = userAccessStore.RemoveUserAccessList(new EntityId(), this.accessList);

            // SqlStore should be called with our new access descriptor
            this.AssertSqlStoreCalled(this.accessDescriptor);

            // Result should be true
            Assert.IsTrue(result);
        }

        /// <summary>Remove user access list failure.</summary>
        [TestMethod]
        public void RemoveUserAccessListFail()
        {
            // There will be a null sql result
            var userAccessStore = this.SetupUserAccessDataStore(null);

            var result = userAccessStore.RemoveUserAccessList(new EntityId(), this.accessList);

            // SqlStore should be called with our new access descriptor
            this.AssertSqlStoreCalled(this.accessDescriptor);

            // Result should be false
            Assert.IsFalse(result);
        }

        /// <summary>Assert the sql store was called with the given access descriptor.</summary>
        /// <param name="accessDescriptorParam">The access descriptor.</param>
        private void AssertSqlStoreCalled(string accessDescriptorParam)
        {
            this.sqlStore.AssertWasCalled(f =>
                f.TryExecuteStoredProcedure(
                    Arg<string>.Is.Anything,
                    Arg<IList<SqlParameter>>.Matches(
                        p => p.Any(s => s.SqlDbType == SqlDbType.VarChar && (string)s.Value == accessDescriptorParam))));
        }

        /// <summary>Setup the access repository with stubbed dependencies.</summary>
        /// <param name="sqlResult">The sql result.</param>
        /// <returns>A user access repository.</returns>
        private IUserAccessStore SetupUserAccessDataStore(QueryResult sqlResult)
        {
            this.sqlStore = MockRepository.GenerateStub<ISqlStore>();
            this.sqlStore.Stub(
                f => f.TryExecuteStoredProcedure(Arg<string>.Is.Anything, Arg<List<SqlParameter>>.Is.Anything)).Return(
                    sqlResult);
            var userAccessStore = new SqlUserAccessDataStore(this.sqlStore);
            return userAccessStore;
        }
    }
}
