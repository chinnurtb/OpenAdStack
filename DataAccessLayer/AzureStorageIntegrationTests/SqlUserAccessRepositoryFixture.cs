//-----------------------------------------------------------------------
// <copyright file="SqlUserAccessRepositoryFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzureStorageIntegrationTests
{
    /// <summary>Test fixture for SqlUserAccessDatastore</summary>
    [TestClass]
    public class SqlUserAccessRepositoryFixture
    {
        /// <summary>User access data store for testing.</summary>
        private ConcreteUserAccessRepository userAccessRepository;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            var userAccessStoreFactory = new SqlUserAccessStoreFactory(ConfigurationManager.AppSettings["Index.ConnectionString"]);
            this.userAccessRepository = new ConcreteUserAccessRepository(userAccessStoreFactory);
        }

        /// <summary>Round-trip a user access list from sql - happy path.</summary>
        [TestMethod]
        public void RoundtripAccessListSuccess()
        {
            var userEntityId = new EntityId();
            var expectedAccessList = new List<string>
                {
                    "ATTHISLEVELITSJUSTASTRING1",
                    "ATTHISLEVELITSJUSTASTRING2",
                };

            this.userAccessRepository.AddUserAccessList(userEntityId, expectedAccessList);
            var actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual(2, actualAccessList.Count());
            Assert.AreEqual(0, actualAccessList.Except(expectedAccessList).Count());
        }

        /// <summary>We don't add duplicate entries.</summary>
        [TestMethod]
        public void RoundtripAccessListNoDupes()
        {
            var userEntityId = new EntityId();
            var expectedAccessList = new List<string>
                {
                    "ATTHISLEVELITSJUSTASTRING1",
                    "ATTHISLEVELITSJUSTASTRING2",
                };

            this.userAccessRepository.AddUserAccessList(userEntityId, expectedAccessList);
            this.userAccessRepository.AddUserAccessList(userEntityId, expectedAccessList);
            var actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual(2, actualAccessList.Count());
            Assert.AreEqual(0, actualAccessList.Except(expectedAccessList).Count());
        }

        /// <summary>Not found is valid scenario.</summary>
        [TestMethod]
        public void GetAccessListUserNotFound()
        {
            var userEntityId = new EntityId();
            var actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual(0, actualAccessList.Count());
        }

        /// <summary>Remove access list - happy path.</summary>
        [TestMethod]
        public void RemoveAccessListSuccess()
        {
            var userEntityId = new EntityId();
            var addAccessList = new List<string>
                {
                    "ATTHISLEVELITSJUSTASTRING1",
                    "ATTHISLEVELITSJUSTASTRING2",
                    "ATTHISLEVELITSJUSTASTRING3",
                };
            var removeAccessList = new List<string>
                {
                    "ATTHISLEVELITSJUSTASTRING1",
                    "ATTHISLEVELITSJUSTASTRING2",
                };

            // Set up access list
            this.userAccessRepository.AddUserAccessList(userEntityId, addAccessList);
            var actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual(3, actualAccessList.Count());
            Assert.AreEqual(0, actualAccessList.Except(addAccessList).Count());

            // Remove some
            this.userAccessRepository.RemoveUserAccessList(userEntityId, removeAccessList);

            // Verify remaining
            actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual("ATTHISLEVELITSJUSTASTRING3", actualAccessList.Single());
        }

        /// <summary>Remove access list - not found is valid scenario.</summary>
        [TestMethod]
        public void RemoveAccessListNotFound()
        {
            var userEntityId = new EntityId();
            var removeAccessList = new List<string>
                {
                    "ATTHISLEVELITSJUSTASTRING1",
                };

            var result = this.userAccessRepository.RemoveUserAccessList(userEntityId, removeAccessList);

            Assert.IsTrue(result);
        }
        
        /// <summary>
        /// A failure at the sql or transport level should result in an empty result list.
        /// Note: This is a difficult scenario to automate so it's only tested with manual
        /// intervention on the server at this time.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void SqlFailScenarios()
        {
            var userEntityId = new EntityId();
            var actualAccessList = this.userAccessRepository.GetUserAccessList(userEntityId).ToList();
            Assert.AreEqual(0, actualAccessList.Count());

            var result = this.userAccessRepository.AddUserAccessList(
                userEntityId, new List<string> { "accessor" });
            Assert.IsFalse(result);

            result = this.userAccessRepository.RemoveUserAccessList(
                userEntityId, new List<string> { "accessor" });
            Assert.IsFalse(result);
        }
    }
}
