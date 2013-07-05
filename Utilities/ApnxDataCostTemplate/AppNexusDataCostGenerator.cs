// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusDataCostGenerator.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.IO;
using AppNexusActivities.Measures;
using AppNexusClient;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace Utilities.AppNexus.DataCostTemplate
{
    /// <summary>Generates AppNexus data cost CSVs</summary>
    internal class AppNexusDataCostGenerator
    {
        /// <summary>
        /// Configuration for the segment measure source
        /// </summary>
        private readonly IConfig Config;

        /// <summary>
        /// Initializes a new instance of the AppNexusDataCostGenerator class
        /// </summary>
        /// <param name="endpoint">AppNexus API endpoint</param>
        /// <param name="username">AppNexus username</param>
        /// <param name="password">AppNexus password</param>
        public AppNexusDataCostGenerator(
            string endpoint,
            string username,
            string password)
        {
            this.Config = new ConfigManager.CustomConfig(new Dictionary<string, string>
                {
                    { "AppNexus.Endpoint", endpoint },
                    { "AppNexus.Username", username },
                    { "AppNexus.Password", password },
                });

            LogManager.Initialize(new[] { new TraceLogger() });
            PersistentDictionaryFactory.Initialize(new[]
            {
                new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Sql)
            });
            DeliveryNetworkClientFactory.Initialize(new[]
            {
                new GenericDeliveryNetworkClientFactory<IAppNexusApiClient, AppNexusApiClient>()
            });
        }

        /// <summary>
        /// Create the segment data cost CSV template using the AppNexus
        /// SegmentMeasureSource and write it to the output stream.
        /// </summary>
        /// <param name="output">Output stream</param>
        public void GenerateAndSaveDataCostCsvTemplate(Stream output)
        {
            const string InfoFormat =
@"Generating data cost CSV for '{0}'.
To deploy upload to the 'datacosts' store as '{1}'.
Example: dview -c Set -s ""datacosts"" -k ""{1}"" -i ""datacosts.csv""";

            var segmentMeasureSource = new SegmentMeasureSource(null, this.Config);
            Console.Out.WriteLine(InfoFormat, segmentMeasureSource.SourceId, segmentMeasureSource.SegmentDataCostsCsvName);

            var dataCostCsv = segmentMeasureSource.CreateSegmentDataCostCsvTemplate(true);
            using (var writer = new StreamWriter(output))
            {
                writer.Write(dataCostCsv);
            }
        }
    }
}
