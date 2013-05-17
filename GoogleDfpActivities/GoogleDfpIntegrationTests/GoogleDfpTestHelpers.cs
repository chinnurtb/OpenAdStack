//-----------------------------------------------------------------------
// <copyright file="GoogleDfpTestHelpers.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using Google.Api.Ads.Common.Util;
using Google.Api.Ads.Dfp.Lib;
using GoogleDfpClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Tests helpers for Google DFP integration tests</summary>
    [TestClass]
    public static class GoogleDfpTestHelpers
    {
        /// <summary>Test ad-unit width</summary>
        private const int TestAdUnitWidth = 300;

        /// <summary>Test ad-unit height</summary>
        private const int TestAdUnitHeight = 250;

        /// <summary>Gets the test logger</summary>
        public static TestLogger TestLogger { get; private set; }

        /// <summary>Per-test run initialization</summary>
        /// <param name="context">Test context</param>
        [AssemblyInitialize]
        [SuppressMessage("Microsoft.Usage", "CA1801", Justification = "Context not needed")]
        public static void AssemblyInitialize(TestContext context)
        {
            // Register the delivery network client factory
            DeliveryNetworkClientFactory.Initialize(new[]
            {
                new GenericDeliveryNetworkClientFactory<IGoogleDfpClient, GoogleDfpWrapper>()
            });

            // Initialize the test logger
            LogManager.Initialize(new[] { TestLogger = new TestLogger() });
        }

        /// <summary>Creates a test AdUnit</summary>
        /// <param name="adUnitName">AdUnit name</param>
        /// <param name="width">AdUnit width</param>
        /// <param name="height">AdUnit height</param>
        /// <returns>The AdUnit's id</returns>
        public static string CreateAdUnit(string adUnitName, int width, int height)
        {
            var client = new GoogleDfpWrapper();
            var effectiveRootAdUnitId = client.NetworkService.getCurrentNetwork().effectiveRootAdUnitId;
            var adUnit = client.InventoryService.createAdUnit(
                new Dfp.AdUnit
                {
                    name = adUnitName,
                    parentId = effectiveRootAdUnitId,
                    description = adUnitName,
                    targetWindow = Dfp.AdUnitTargetWindow.BLANK,
                    adUnitSizes = new[]
                    {
                        new Dfp.AdUnitSize
                        {
                            size = new Dfp.Size { width = width, height = height },
                            environmentType = Dfp.EnvironmentType.BROWSER
                        }
                    }
                });
            return adUnit.id;
        }

        /// <summary>Creates a test placement</summary>
        /// <param name="placementName">Placement name</param>
        /// <param name="adUnitId">Target AdUnit id</param>
        /// <returns>The placement's id</returns>
        public static long CreatePlacement(string placementName, string adUnitId)
        {
            var client = new GoogleDfpWrapper();
            var placement = client.PlacementService.createPlacement(
                new Dfp.Placement
                {
                    name = placementName,
                    description = placementName,
                    targetedAdUnitIds = new[] { adUnitId }
                });
            return placement.id;
        }

        /// <summary>Loads measures from the source</summary>
        /// <remarks>
        /// If the source is async, waits up to <paramref name="timeoutSeconds"/> before failing.
        /// </remarks>
        /// <param name="source">The measure source</param>
        /// <param name="timeoutSeconds">Loading timeout (default: 60)</param>
        /// <returns>The loaded measures</returns>
        public static IDictionary<long, IDictionary<string, object>> LoadMeasures(
            IMeasureSource source,
            int timeoutSeconds = 120)
        {
            if (source is CachedMeasureSource &&
                ((CachedMeasureSource)source).AsyncUpdate)
            {
                var measureLoadTimeout = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                while (source.Measures == null)
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
    }
}
