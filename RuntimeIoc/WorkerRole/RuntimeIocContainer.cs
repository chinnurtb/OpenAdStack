// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RuntimeIocContainer.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using Activities;
using ActivityProcessor;
using AzureUtilities.Storage;
using ConcreteDataStore;
using ConfigManager;
using DataAccessLayer;
using DefaultMailTemplates;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using Microsoft.Practices.Unity;
using PaymentProcessor;
using Queuing;
using Queuing.Azure;
using ScheduledWorkItems;
using SqlUtilities.Storage;
using Utilities.Diagnostics;
using Utilities.Net.Mail;
using Utilities.Runtime;
using Utilities.Storage;
using WorkItems;

namespace RuntimeIoc.WorkerRole
{
    /// <summary>
    /// A singleton implementation for the default UnityContainer.
    /// This container should not be extensively referenced - usually only from 
    /// service startup code where the root classes are constructed. Subsequent
    /// construction of the dependency chain should happen silently by Unity.
    /// </summary>
    public static class RuntimeIocContainer
    {
        /// <summary>singleton lock object.</summary>
        private static readonly object LockObj = new object();

        /// <summary>The one and only UnityContainer object.</summary>
        private static UnityContainer container;

        /// <summary>singleton initialization flag.</summary>
        private static bool initialized = false;

        /// <summary>Gets or initializes a singleton UnityContainer instance.</summary>
        /// <value>The unity container.</value>
        public static IUnityContainer Instance
        {
            get
            {
                if (!initialized)
                {
                    lock (LockObj)
                    {
                        if (!initialized)
                        {
                            container = new UnityContainer();
                            InitializeContainerMappings(container);

                            // Make sure flag initialization isn't reordered by the compiler.
                            System.Threading.Thread.MemoryBarrier();
                            initialized = true;
                        }
                    }
                }

                return container;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the mail alert logger should be used
        /// </summary>
        private static bool MailAlertLogging
        {
            get
            {
                try
                {
                    return Config.GetBoolValue("Logging.MailAlerts");
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        /// <summary>Do runtime intitialization of the UnityContainer.</summary>
        /// <param name="unityContainer">The unity container.</param>
        private static void InitializeContainerMappings(UnityContainer unityContainer)
        {
            // Default ILoggers
            unityContainer.RegisterType<ILogger, QuotaLogger>("QuotaLogger");
            unityContainer.RegisterType<ILogger, TraceLogger>("TraceLogger");
            if (MailAlertLogging)
            {
                unityContainer.RegisterType<ILogger, MailAlertLogger>("MailAlertLogger");
            }

            // Default IIndexStoreFactory
            // TODO: change to AzureSql when available
            string indexStoreConnectionString = Config.GetValue("Index.ConnectionString");
            unityContainer.RegisterType<IIndexStoreFactory, SqlIndexStoreFactory>(new InjectionConstructor(indexStoreConnectionString));

            // Default IEntityStoreFactory
            string entityStoreConnectionString = Config.GetValue("Entity.ConnectionString");
            unityContainer.RegisterType<IEntityStoreFactory, AzureEntityStoreFactory>(new InjectionConstructor(entityStoreConnectionString));

            // Default IBlobStoreFactory
            unityContainer.RegisterType<IBlobStoreFactory, AzureBlobStoreFactory>(new InjectionConstructor(entityStoreConnectionString));

            // Default IKeyRuleFactory
            unityContainer.RegisterType<IKeyRuleFactory, KeyRuleFactory>();

            // Default IStorageKeyFactory
            unityContainer.RegisterType<IStorageKeyFactory, AzureStorageKeyFactory>();

            // Default IEntityRepository
            unityContainer.RegisterType<IEntityRepository, ConcreteEntityRepository>();

            // Default IUserAccessStoreFactory
            unityContainer.RegisterType<IUserAccessStoreFactory, SqlUserAccessStoreFactory>(new InjectionConstructor(indexStoreConnectionString));

            // Default IUserAccessRepository
            unityContainer.RegisterType<IUserAccessRepository, ConcreteUserAccessRepository>();

            // Default ICategorizedQueue
            unityContainer.RegisterType<ICategorizedQueue, CategorizedAzureQueue>();

            // Default IPersistentDictionary Factories
            unityContainer.RegisterType<IPersistentDictionaryFactory, MemoryPersistentDictionaryFactory>("MemoryDictionaryFactory");
            var persistentDictionaryBlobConnectionString = Config.GetValue("Dictionary.Blob.ConnectionString");
            unityContainer.RegisterType<IPersistentDictionaryFactory, CloudBlobDictionaryFactory>("CloudBlobDictionaryFactory", new InjectionConstructor(persistentDictionaryBlobConnectionString));
            var persistentDictionarySqlConnectionString = Config.GetValue("Dictionary.Sql.ConnectionString");
            unityContainer.RegisterType<IPersistentDictionaryFactory, SqlDictionaryFactory>("SqlDictionaryFactory", new InjectionConstructor(persistentDictionarySqlConnectionString));

            // Default IQueuer
            unityContainer.RegisterType<IQueuer, Queue>();

            // Default IDequeuer
            unityContainer.RegisterType<IDequeuer, Queue>();

            // Default IWorkItemProcessor
            unityContainer.RegisterType<IWorkItemProcessor, ActivityWorkItemProcessor>();

            // Mail Configuration Provider
            unityContainer.RegisterType<IMailTemplateProvider, EmbeddedMailTemplateProvider>();

            // Billing Payment Processor
            var paymentProcessorSecretKey = TryGetValue("PaymentProcessor.ApiSecretKey", "nonfunctionaldefaultkey");
            unityContainer.RegisterType<IPaymentProcessor, StripePaymentProcessor>(new InjectionConstructor(paymentProcessorSecretKey));

            // Activity Providers
            unityContainer.RegisterType<IActivityProvider, EntityActivities.ActivityProvider>("EntityActivities");
            unityContainer.RegisterType<IActivityProvider, DynamicAllocationActivities.ActivityProvider>("DynamicAllocationActivities");
            unityContainer.RegisterType<IActivityProvider, AppNexusActivities.ActivityProvider>("AppNexusActivities");
            unityContainer.RegisterType<IActivityProvider, GoogleDfpActivities.ActivityProvider>("GoogleDfpActivities");
            unityContainer.RegisterType<IActivityProvider, ReportingActivities.ActivityProvider>("ReportingActivities");
            unityContainer.RegisterType<IActivityProvider, BillingActivities.ActivityProvider>("BillingActivities");

            // Scheduled Activity Request Source Providers
            unityContainer.RegisterType<IScheduledWorkItemSourceProvider, DynamicAllocationActivityDispatchers.ScheduledActivitySourceProvider>("DynamicAllocationActivityDispatchers");
            unityContainer.RegisterType<IScheduledWorkItemSourceProvider, DeliveryNetworkActivityDispatchers.ScheduledActivitySourceProvider>("DeliveryNetworkActivityDispatchers");

            // Default WorkItemSubmitter
            unityContainer.RegisterType<IRunner, WorkItemSubmitter>("WorkItemSubmitter");

            // Delivery Network Client Factories
            unityContainer.RegisterType<IDeliveryNetworkClientFactory, GenericDeliveryNetworkClientFactory<AppNexusClient.IAppNexusApiClient, AppNexusClient.AppNexusApiClient>>("AppNexusApiClient");
            unityContainer.RegisterType<IDeliveryNetworkClientFactory, GenericDeliveryNetworkClientFactory<GoogleDfpClient.IGoogleDfpClient, GoogleDfpClient.GoogleDfpWrapper>>("GoogleDfpClient");

            // Measure Source Providers
            unityContainer.RegisterType<IMeasureSourceProvider, AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider>("AppNexusLegacyMeasures");
            unityContainer.RegisterType<IMeasureSourceProvider, AppNexusActivities.Measures.AppNexusMeasureSourceProvider>("AppNexusMeasures");
            unityContainer.RegisterType<IMeasureSourceProvider, GoogleDfpActivities.Measures.DfpMeasureSourceProvider>("GoogleDfpMeasures");

            // QueueProcessors
            var activityRuntimeCategories =
                (Enum.GetValues(typeof(ActivityRuntimeCategory)) as ActivityRuntimeCategory[])
                .Select(c => c.ToString())
                .ToArray();
            var queueCategories = Config.GetValue("QueueProcessor.Categories");
            var queues = queueCategories.Split(new[] { '|' });
            foreach (var queue in queues)
            {
                var categories = queue.Split(new[] { ',' })
                    .Select(c => int.Parse(c.Trim(), CultureInfo.InvariantCulture))
                    .Select(i => activityRuntimeCategories[i])
                    .ToArray();
                var queueName = "QueueProcessor-" + Guid.NewGuid().ToString("N");
                unityContainer.RegisterType<IRunner, QueueProcessor>(queueName, new InjectionProperty("Categories", categories));
            }
        }

        /// <summary>Get a string value from config. Return default if not found.</summary>
        /// <param name="configKey">The config key.</param>
        /// <param name="defaultValue">The default value if not found.</param>
        /// <returns>The config value.</returns>
        private static string TryGetValue(string configKey, string defaultValue)
        {
            try
            {
                return Config.GetValue(configKey);
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }
    }
}
