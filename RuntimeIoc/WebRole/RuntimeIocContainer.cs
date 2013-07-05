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

using System.Diagnostics.CodeAnalysis;
using AzureUtilities.Storage;
using ConcreteDataStore;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.ServiceRuntime;
using Queuing;
using Queuing.Azure;
using SqlUtilities.Storage;
using Utilities.IdentityFederation;
using Utilities.Storage;

namespace RuntimeIoc.WebRole
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

        /// <summary>Do runtime intitialization of the UnityContainer.</summary>
        /// <param name="unityContainer">The unity container.</param>
        private static void InitializeContainerMappings(UnityContainer unityContainer)
        {
            // Default ILogger
            unityContainer.RegisterType<ILogger, QuotaLogger>("QuotaLogger");
            unityContainer.RegisterType<ILogger, TraceLogger>("TraceLogger");

            // Default IIndexStoreFactory
            // TODO: change to AzureSql when available
            var indexStoreConnectionString = Config.GetValue("Index.ConnectionString");
            unityContainer.RegisterType<IIndexStoreFactory, SqlIndexStoreFactory>(new InjectionConstructor(indexStoreConnectionString));

            // Default IEntityStoreFactory
            var entityStoreConnectionString = Config.GetValue("Entity.ConnectionString");
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
            var persistentDictionaryBlobConnectionString = Config.GetValue("Dictionary.Blob.ConnectionString");
            unityContainer.RegisterType<IPersistentDictionaryFactory, CloudBlobDictionaryFactory>("CloudBlobDictionaryFactory", new InjectionConstructor(persistentDictionaryBlobConnectionString));
            var persistentDictionarySqlConnectionString = Config.GetValue("Dictionary.Sql.ConnectionString");
            unityContainer.RegisterType<IPersistentDictionaryFactory, SqlDictionaryFactory>("SqlDictionaryFactory", new InjectionConstructor(persistentDictionarySqlConnectionString));

            // Default IQueuer
            unityContainer.RegisterType<IQueuer, Queue>();

            // Default IClaimRetriever
            if (Config.GetBoolValue("Testing.HttpHeaderClaimOverrides"))
            {
                LogManager.Log(LogLevels.Trace, "Using HttpHeaderTestClaimRetriever (overrides context claims with header)");
                unityContainer.RegisterType<IClaimRetriever, Utilities.IdentityFederation.Testing.HttpHeaderTestClaimRetriever>();
            }
            else if (Config.GetBoolValue("AppNexus.IsApp"))
            {
                LogManager.Log(LogLevels.Trace, "Using AppNexusApp.AppNexusAuth.AppNexusUserClaimRetriever");
                unityContainer.RegisterType<IClaimRetriever, AppNexusApp.AppNexusAuth.AppNexusUserClaimRetriever>();
                unityContainer.RegisterType<IAuthenticationManager, AppNexusApp.AppNexusAuth.AppNexusAuthenticationManager>();
                unityContainer.RegisterType<IAuthorizationManager, AppNexusApp.AppNexusAuth.AppNexusAuthorizationManager>();
            }
            else
            {
                LogManager.Log(LogLevels.Trace, "Using HttpContextClaimRetriever");
                unityContainer.RegisterType<IClaimRetriever, HttpContextClaimRetriever>();
            }
        }
    }
}
