//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using AppNexusApp.AppNexusAuth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppNexusAppUnitTests
{
    /// <summary>Tests for AppNexusAuth</summary>
    [TestClass]
    public class AppNexusAuthFixture
    {
        /// <summary>Test getting the user id from json</summary>
        [TestMethod]
        public void GetUserIdFromJson()
        {
            const string ResponseJson = "{\"response\":{\"status\":\"OK\",\"user-id\":\"12345\",\"dbg_info\":{\"instance\":\"05.hbapi.sand-08.lax1\",\"slave_hit\":false,\"db\":\"master\",\"awesomesauce_cache_used\":false,\"warnings\":[],\"time\":25.25782585144,\"start_microtime\":1353375151.7433,\"version\":\"1.12\"}}}\n";
            var userId = AppNexusUserClaimRetriever.GetUserIdFromJson(ResponseJson);
            Assert.AreEqual("12345", userId);
        }
    }
}
