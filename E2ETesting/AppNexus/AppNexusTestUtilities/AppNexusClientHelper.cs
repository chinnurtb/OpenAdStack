//-----------------------------------------------------------------------
// <copyright file="AppNexusClientHelper.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using AppNexusClient;
using DeliveryNetworkUtilities;

namespace AppNexusTestUtilities
{
    /// <summary>Test helpers for AppNexus integration tests</summary>
    public static class AppNexusClientHelper
    {
        /// <summary>
        /// List of ids of advertisers created in AppNexus
        /// </summary>
        private static readonly IList<int> advertiserIds = new List<int>();

        /// <summary>
        /// List of ids of domain lists created in AppNexus
        /// </summary>
        private static readonly IList<int> domainListIds = new List<int>();

        /// <summary>
        /// Initializes the delivery network client factory for IAppNexusApiClient/AppNexusApiClient
        /// </summary>
        public static void InitializeDeliveryNetworkClientFactory()
        {
            DeliveryNetworkClientFactory.Initialize(new[]
            {
                new GenericDeliveryNetworkClientFactory<IAppNexusApiClient, AppNexusApiClient>()
            });
        }

        /// <summary>
        /// Cleanup AppNexus advertisers and domain lists created by the tests
        /// </summary>
        public static void Cleanup()
        {
            var client = new AppNexusApiClient();
            var listActions = new Dictionary<IList<int>, Action<int>>
                {
                    { advertiserIds, client.DeleteAdvertiser },
                    { domainListIds, client.DeleteDomainList },
                };
            foreach (var listAction in listActions)
            {
                foreach (var id in listAction.Key)
                {
                    try
                    {
                        listAction.Value(id);
                    }
                    catch (AppNexusClientException)
                    {
                    }
                }

                listAction.Key.Clear();
            }
        }

        /// <summary>
        /// Adds an advertiser to the list to be cleaned up
        /// </summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        public static void AddAdvertiserForCleanup(int advertiserId)
        {
            advertiserIds.Add(advertiserId);
        }

        /// <summary>
        /// Adds a domain list to the list to be cleaned up
        /// </summary>
        /// <param name="domainListId">Domain List AppNexus id</param>
        public static void AddDomainListForCleanup(int domainListId)
        {
            domainListIds.Add(domainListId);
        }
    }
}
