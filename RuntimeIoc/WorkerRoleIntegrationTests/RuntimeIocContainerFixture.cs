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

using System;
using System.Configuration;
using System.Linq;
using Activities;
using ActivityProcessor;
using AzureUtilities.Storage;
using ConcreteDataStore;
using DataAccessLayer;
using DefaultMailTemplates;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityActivities.UserMail;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using Queuing;
using Queuing.Azure;
using RuntimeIoc.WorkerRole;
using ScheduledWorkItems;
using TestUtilities;
using Utilities.Diagnostics;
using Utilities.Net.Mail;
using Utilities.Runtime;
using Utilities.Storage;
using WorkItems;

namespace RuntimeIoc.WorkerRole.IntegrationTests
{
    /// <summary>
    /// Test fixture for default Lucy UnityContainer
    /// </summary>
    [TestClass]
    public class RuntimeIocContainerFixture
    {
        /// <summary>Queue processor categories for test configuration</summary>
        private static readonly ActivityRuntimeCategory[][] QueueProcessorCategories = new[]
            {
                new[] { ActivityRuntimeCategory.Background, ActivityRuntimeCategory.BackgroundFetch },
                new[] { ActivityRuntimeCategory.Interactive, ActivityRuntimeCategory.InteractiveFetch },
                new[] { ActivityRuntimeCategory.InteractiveFetch }
            };

        /// <summary>Singleton instance of IOC container</summary>
        private static IUnityContainer container;

        /// <summary>One time initialization for all tests in fixture.</summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void FixtureAssemblyInitialize(TestContext context)
        {
            ConfigurationManager.AppSettings["Logging.ConnectionString"] = "UseDevelopmentStorage=true";
            ConfigurationManager.AppSettings["Index.ConnectionString"] = "SomeConnectionString";
            ConfigurationManager.AppSettings["Entity.ConnectionString"] = "SomeConnectionString";
            ConfigurationManager.AppSettings["Logging.BlobContainer"] = "quotalogs";
            ConfigurationManager.AppSettings["Logging.MaximumSizeInMegabytes"] = "1024";
            ConfigurationManager.AppSettings["Logging.RootPath"] = ".";
            ConfigurationManager.AppSettings["Logging.ScheduledTransferPeriodMinutes"] = "5";
            ConfigurationManager.AppSettings["Logging.MailAlerts"] = "true";
            ConfigurationManager.AppSettings["Mail.SmtpHost"] = "mail.rc.dev";
            ConfigurationManager.AppSettings["Queue.ConnectionString"] = "UseDevelopmentStorage=true";
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = "someaddress";
            ConfigurationManager.AppSettings["AppNexus.Endpoint"] = "http://hb.sand-08.adnxs.net/";
            ConfigurationManager.AppSettings["AppNexus.Timeout"] = "00:00:01";
            ConfigurationManager.AppSettings["AppNexus.Username"] = "username";
            ConfigurationManager.AppSettings["AppNexus.Password"] = "password";
            ConfigurationManager.AppSettings["AppNexus.MaxReportRequests"] = "5";
            ConfigurationManager.AppSettings["GoogleDfp.MaxReportRequests"] = "10";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateBudgetAllocationsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.RetrieveCampaignReportsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.UpdateCreativeStatusSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.ExportDACampaignsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.CleanupCampaignsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.ExportCreativesSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            ConfigurationManager.AppSettings["Dictionary.Blob.ConnectionString"] = "UseDevelopmentStorage=true";
            ConfigurationManager.AppSettings["QueueProcessor.Categories"] = string.Join("|", QueueProcessorCategories.Select(q => string.Join(",", q.Select(c => (int)c))));
            ConfigurationManager.AppSettings["Mail.SmtpHost"] = "mail.rc.dev";
            ConfigurationManager.AppSettings["Mail.Username"] = string.Empty;
            ConfigurationManager.AppSettings["Mail.Password"] = string.Empty;
            ConfigurationManager.AppSettings["PaymentProcessor.ApiSecretKey"] = "somekey";

            // Start up azure storage emulator
            // This is required for classes that create persistent dictionaries during initialization
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);

            LogManager.Initialize(new ILogger[0]);

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

            // IPersistentDictionary Factories
            var defaultPersistentDictionaryFactories = container.ResolveAll<IPersistentDictionaryFactory>();
            Assert.AreEqual(3, defaultPersistentDictionaryFactories.Count());
            Assert.IsNotNull(defaultPersistentDictionaryFactories.SingleOrDefault(d => d.DictionaryType == PersistentDictionaryType.Memory));
            Assert.IsNotNull(defaultPersistentDictionaryFactories.SingleOrDefault(d => d.DictionaryType == PersistentDictionaryType.Cloud));
            Assert.IsNotNull(defaultPersistentDictionaryFactories.SingleOrDefault(d => d.DictionaryType == PersistentDictionaryType.Sql));
            PersistentDictionaryFactory.Initialize(defaultPersistentDictionaryFactories.ToArray());

            // Mail alert logger (dependent upon PersistentDictionaryFactory.Initialize)
            var mailAlertLogger = container.Resolve<ILogger>("MailAlertLogger");
            Assert.IsInstanceOfType(mailAlertLogger, typeof(MailAlertLogger));

            // IIndexStoreFactory
            var defaultIndexStoreFactory = container.Resolve<IIndexStoreFactory>();
            Assert.IsInstanceOfType(defaultIndexStoreFactory, typeof(SqlIndexStoreFactory));

            // IEntityStoreFactory
            var defaultEntityStoreFactory = container.Resolve<IEntityStoreFactory>();
            Assert.IsInstanceOfType(defaultEntityStoreFactory, typeof(AzureEntityStoreFactory));

            // IBlobStoreFactory
            var defaultBlobStoreFactory = container.Resolve<IBlobStoreFactory>();
            Assert.IsInstanceOfType(defaultBlobStoreFactory, typeof(AzureBlobStoreFactory));

            // IKeyRuleFactory
            var defaultKeyRuleFactory = container.Resolve<IKeyRuleFactory>();
            Assert.IsInstanceOfType(defaultKeyRuleFactory, typeof(KeyRuleFactory));

            // IStorageKeyFactory
            var defaultStorageKeyFactory = container.Resolve<IStorageKeyFactory>();
            Assert.IsInstanceOfType(defaultStorageKeyFactory, typeof(AzureStorageKeyFactory));

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

            // IQueuer
            var defaultQueuer = container.Resolve<IQueuer>();
            Assert.IsInstanceOfType(defaultQueuer, typeof(Queue));

            // IDequeuer
            var defaultDequeuer = container.Resolve<IDequeuer>();
            Assert.IsInstanceOfType(defaultDequeuer, typeof(Queue));

            // Mail Template Provider
            var defaultMailTemplateProvider = container.Resolve<IMailTemplateProvider>();
            Assert.IsInstanceOfType(defaultMailTemplateProvider, typeof(EmbeddedMailTemplateProvider));

            // Delivery Network Client Factories
            var deliveryNetworkClientFactories = container.Resolve<IDeliveryNetworkClientFactory[]>();
            Assert.AreEqual(2, deliveryNetworkClientFactories.Length);
            Assert.AreEqual(1, deliveryNetworkClientFactories.Count(factory => factory.ClientType == typeof(AppNexusClient.IAppNexusApiClient)));
            Assert.AreEqual(1, deliveryNetworkClientFactories.Count(factory => factory.ClientType == typeof(GoogleDfpClient.IGoogleDfpClient)));

            // Measure Source Providers
            var measureSourceProviders = container.Resolve<IMeasureSourceProvider[]>();
            Assert.AreEqual(3, measureSourceProviders.Length);
            Assert.AreEqual(1, measureSourceProviders.OfType<AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider>().Count());
            Assert.AreEqual(1, measureSourceProviders.OfType<AppNexusActivities.Measures.AppNexusMeasureSourceProvider>().Count());
            Assert.AreEqual(1, measureSourceProviders.OfType<GoogleDfpActivities.Measures.DfpMeasureSourceProvider>().Count());

            // Billing Payment Processor
            var paymentProcessor = container.Resolve<IPaymentProcessor>();
            Assert.IsInstanceOfType(paymentProcessor, typeof(StripePaymentProcessor));

            // IActivityProviders
            var activityProviders = container.Resolve<IActivityProvider[]>();
            Assert.AreEqual(6, activityProviders.Length);

            // 5 containers of activities derived from EntityActivity
            // (serves as a base for other domain specific activities)
            Assert.AreEqual(
                6,
                activityProviders
                .Count(provider =>
                    provider.ActivityTypes.Any(type =>
                        typeof(EntityActivities.EntityActivity).IsAssignableFrom(type))));

            // 1 container of activities derived from DynamicAllocationActivity
            Assert.AreEqual(
                1,
                activityProviders
                .Count(provider =>
                    provider.ActivityTypes.Any(type =>
                        typeof(DynamicAllocationActivities.DynamicAllocationActivity).IsAssignableFrom(type))));

            // 1 container of activities derived from AppNexusActivity
            Assert.AreEqual(
                1,
                activityProviders
                .Count(provider =>
                    provider.ActivityTypes.Any(type =>
                        typeof(AppNexusActivities.AppNexusActivity).IsAssignableFrom(type))));

            // 1 container of activities derived from DfpActivity
            Assert.AreEqual(
                1,
                activityProviders
                .Count(provider =>
                    provider.ActivityTypes.All(type =>
                        typeof(GoogleDfpActivities.DfpActivity).IsAssignableFrom(type))));

            // IScheduledWorkItemSourceProviders
            var scheduledActivityProviders = container.Resolve<IScheduledWorkItemSourceProvider[]>();
            Assert.AreEqual(2, scheduledActivityProviders.Length);

            // IWorkItemProcessor
            var defaultWorkItemProcessor = container.Resolve<IWorkItemProcessor>();
            Assert.IsInstanceOfType(defaultWorkItemProcessor, typeof(ActivityWorkItemProcessor));

            // IRunners
            var runners = container.ResolveAll<IRunner>();

            // WorkItemSubmitter
            var defaultWorkItemSubmitter = runners.OfType<WorkItemSubmitter>().SingleOrDefault();
            Assert.IsNotNull(defaultWorkItemSubmitter);

            // QueueProcessors
            var processorCategories = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7, 8, 9 } };
            var queueProcessors = runners.OfType<QueueProcessor>().ToArray();
            Assert.AreEqual(processorCategories.Length, queueProcessors.Length);
            for (int i = 0; i < processorCategories.Length; i++)
            {
                var categories = QueueProcessorCategories[i].Select(c => c.ToString()).ToArray();
                Assert.AreEqual(categories.Length, queueProcessors[i].Categories.Length);
                for (int j = 0; j < categories.Length; j++)
                {
                    Assert.AreEqual(categories[j], queueProcessors[i].Categories[j]);
                }
            }
        }
    }
}
