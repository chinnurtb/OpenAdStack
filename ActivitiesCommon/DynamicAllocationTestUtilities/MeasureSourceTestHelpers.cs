//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DynamicAllocationTestUtilities
{
    /// <summary>Test helpers for working with measure sources</summary>
    public static class MeasureSourceTestHelpers
    {
        /// <summary>
        /// Default timeout for loading measures asynchronously
        /// </summary>
        private const int DefaultLoadMeasuresTimeoutSeconds = 180;

        /// <summary>Loads measures from the source</summary>
        /// <remarks>
        /// If the source is async, waits up to 120 seconds before failing.
        /// </remarks>
        /// <param name="source">The measure source</param>
        /// <returns>The loaded measures</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting is appropriate here")]
        public static IDictionary<long, IDictionary<string, object>> LoadMeasures(
            IMeasureSource source)
        {
            return LoadMeasures(source, DefaultLoadMeasuresTimeoutSeconds);
        }

        /// <summary>Loads measures from the source</summary>
        /// <remarks>
        /// If the source is async, waits up to <paramref name="timeoutSeconds"/> before failing.
        /// </remarks>
        /// <param name="source">The measure source</param>
        /// <param name="timeoutSeconds">Loading timeout</param>
        /// <returns>The loaded measures</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting is appropriate here")]
        public static IDictionary<long, IDictionary<string, object>> LoadMeasures(
            IMeasureSource source,
            int timeoutSeconds)
        {
            var cachedSource = source as CachedMeasureSource;
            if (cachedSource != null && cachedSource.AsyncUpdate)
            {
                var measureLoadTimeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                while (cachedSource.Measures == null)
                {
                    Assert.IsTrue(
                        DateTime.UtcNow < measureLoadTimeout,
                        "Failed to load measures within {0} seconds",
                        timeoutSeconds);
                    Thread.Sleep(100);
                }
            }
            else
            {
                Assert.IsNotNull(source.Measures);
            }

            return source.Measures;
        }

        /// <summary>Preloads all measure sources for the configured test campaign</summary>
        /// <param name="companyEntity">The company entity</param>
        /// <param name="campaignEntity">The campaign entity</param>
        /// <param name="campaignOwner">The campaign owner</param>
        public static void PreloadMeasureSources(
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
        {
            var sources = MeasureSourceFactory.CreateMeasureSources(
                DeliveryNetworkDesignation.AppNexus,
                campaignEntity.GetExporterVersion(),
                companyEntity,
                campaignEntity,
                campaignOwner);
            var measures = sources
                .AsParallel()
                .SelectMany(s => LoadMeasures(s))
                .ToDictionary();
            Console.Write("Loaded {0} measures from {1} source(s)", measures.Count, sources.Count());
        }

        /// <summary>
        /// Initialize the MeasureSourceFactory with the provided campaign and measure map
        /// </summary>
        /// <param name="exporterVersion">Exporter version to match</param>
        /// <param name="deliveryNetwork">DeliveryNetwork to match</param>
        /// <param name="measureMap">The measure map</param>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nesting of generics is appropriate here")]
        public static void InitializeMockMeasureSource(
            int exporterVersion,
            DeliveryNetworkDesignation deliveryNetwork,
            IDictionary<long, IDictionary<string, object>> measureMap)
        {
            var mockMeasureSource = MockRepository.GenerateMock<IMeasureSource>();
            mockMeasureSource.Stub(f => f.Measures)
                .Return(measureMap);
            mockMeasureSource.Stub(f => f.MaxMeasureId)
                .Return(measureMap.Keys.Max());
            mockMeasureSource.Stub(f => f.BaseMeasureId)
                .Return(measureMap.Keys.Min());
            mockMeasureSource.Stub(f => f.SourceId)
                .Return("MockMeasureSource");

            var mockMeasureSourceProvider = MockRepository.GenerateMock<IMeasureSourceProvider>();
            mockMeasureSourceProvider.Stub(f => f.GetMeasureSources(Arg<object[]>.Is.Anything))
                .Return(new[] { mockMeasureSource });
            mockMeasureSourceProvider.Stub(f => f.Version)
                .Return(exporterVersion);
            mockMeasureSourceProvider.Stub(f => f.DeliveryNetwork)
                .Return(deliveryNetwork);

            MeasureSourceFactory.Initialize(new[] { mockMeasureSourceProvider });
        }
    }
}
