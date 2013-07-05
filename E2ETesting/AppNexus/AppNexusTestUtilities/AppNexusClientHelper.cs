//-----------------------------------------------------------------------
// <copyright file="AppNexusClientHelper.cs" company="Rare Crowds Inc">
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
