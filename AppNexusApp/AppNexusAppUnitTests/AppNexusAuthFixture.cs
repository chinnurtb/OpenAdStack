//-----------------------------------------------------------------------
// <copyright file="AppNexusAuthFixture.cs" company="Rare Crowds Inc">
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
