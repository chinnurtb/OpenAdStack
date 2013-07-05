// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RuntimeIocContainerFixture.cs" company="Rare Crowds Inc">
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

using System.Configuration;
using System.Linq;
using AzureUtilities.Storage;
using ConcreteDataStore;
using DataAccessLayer;
using Diagnostics;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Queuing.Azure;
using RuntimeIoc.WebRole;
using TestUtilities;
using Utilities.IdentityFederation;
using Utilities.Storage;

namespace RuntimeIoc.WebRole.IntegrationTests
{
    /// <summary>
    /// Test fixture for default Lucy UnityContainer
    /// </summary>
    [TestClass]
    public class RuntimeIocContainerFixture
    {
        /// <summary>Singleton instance of IOC container</summary>
        private static IUnityContainer container;

        /// <summary>One time initialization for all tests in fixture.</summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void FixtureAssemblyInitialize(TestContext context)
        {
            ConfigurationManager.AppSettings["Index.ConnectionString"] = "SomeConnectionString";
            ConfigurationManager.AppSettings["Entity.ConnectionString"] = "SomeConnectionString";
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = "someaddress";
            ConfigurationManager.AppSettings["Logging.BlobContainer"] = "quotalogs";
            ConfigurationManager.AppSettings["Logging.RootPath"] = "quotalogs";
            ConfigurationManager.AppSettings["Logging.MaximumSizeInMegabytes"] = "1024";
            ConfigurationManager.AppSettings["Logging.ScheduledTransferPeriodMinutes"] = "5";
            ConfigurationManager.AppSettings["Testing.HttpHeaderClaimOverrides"] = "false";
            ConfigurationManager.AppSettings["AppNexus.IsApp"] = "false";

            // Start up azure storage emulator
            // This is required for classes that create persistent dictionaries during initialization
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);

            container = RuntimeIocContainer.Instance;
        }

        /// <summary>Verify singleton behavior.</summary>
        [TestMethod]
        public void IocContainerIsSingleton()
        {
            var newContainer = RuntimeIocContainer.Instance;

            Assert.AreSame(container, newContainer);
        }

        /// <summary>Verify object resolutions mappings are correct.</summary>
        [TestMethod]
        public void IocContainerMappings()
        {
            // ILogger
            var quotaLogger = container.Resolve<ILogger>("QuotaLogger");
            Assert.IsInstanceOfType(quotaLogger, typeof(QuotaLogger));
            var traceLogger = container.Resolve<ILogger>("TraceLogger");
            Assert.IsInstanceOfType(traceLogger, typeof(TraceLogger));
            LogManager.Initialize(container.ResolveAll<ILogger>());

            // IIndexStoreFactory
            var defaultIndexStoreFactory = container.Resolve<IIndexStoreFactory>();
            Assert.IsInstanceOfType(defaultIndexStoreFactory, typeof(SqlIndexStoreFactory));

            // IEntityStoreFactory
            var defaultEntityStoreFactory = container.Resolve<IEntityStoreFactory>();
            Assert.IsInstanceOfType(defaultEntityStoreFactory, typeof(AzureEntityStoreFactory));

            // IKeyRuleFactory
            var defaultKeyRuleFactory = container.Resolve<IKeyRuleFactory>();
            Assert.IsInstanceOfType(defaultKeyRuleFactory, typeof(KeyRuleFactory));

            // IStorageKeyFactory
            var defaultStorageKeyFactory = container.Resolve<IStorageKeyFactory>();
            Assert.IsInstanceOfType(defaultStorageKeyFactory, typeof(AzureStorageKeyFactory));

            // IBlobStoreFactory
            var defaultBlobStoreFactory = container.Resolve<IBlobStoreFactory>();
            Assert.IsInstanceOfType(defaultBlobStoreFactory, typeof(AzureBlobStoreFactory));
            
            // IEntityRepository
            var defaultEntityRepository = container.Resolve<IEntityRepository>();
            Assert.IsInstanceOfType(defaultEntityRepository, typeof(ConcreteEntityRepository));

            // IUserAccessStoreFactory
            var defaultUserAccessStoreFactory = container.Resolve<IUserAccessStoreFactory>();
            Assert.IsInstanceOfType(defaultUserAccessStoreFactory, typeof(SqlUserAccessStoreFactory));

            // IUserAccessRepository
            var defaultUserAccessRepository = container.Resolve<IUserAccessRepository>();
            Assert.IsInstanceOfType(defaultUserAccessRepository, typeof(ConcreteUserAccessRepository));

            // ICategorizedAzureQueue
            var defaultCategorizedQueue = container.Resolve<ICategorizedQueue>();
            Assert.IsInstanceOfType(defaultCategorizedQueue, typeof(CategorizedAzureQueue));

            // IPersistentDictionary Factories
            var defaultPersistentDictionaryFactories = container.ResolveAll<IPersistentDictionaryFactory>();
            Assert.AreEqual(2, defaultPersistentDictionaryFactories.Count());
            Assert.IsNotNull(defaultPersistentDictionaryFactories.SingleOrDefault(d => d.DictionaryType == PersistentDictionaryType.Cloud));
            Assert.IsNotNull(defaultPersistentDictionaryFactories.SingleOrDefault(d => d.DictionaryType == PersistentDictionaryType.Sql));

            // IQueuer
            var defaultQueuer = container.Resolve<IQueuer>();
            Assert.IsInstanceOfType(defaultQueuer, typeof(Queue));

            // IClaimRetriever
            var defaultClaimRetriever = container.Resolve<IClaimRetriever>();
            Assert.IsInstanceOfType(defaultClaimRetriever, typeof(HttpContextClaimRetriever));
        }
    }
}
