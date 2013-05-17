//-----------------------------------------------------------------------
// <copyright file="ApiToAppNexusExportFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using E2ETestUtilities;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace ScenarioE2ETests
{
    /// <summary>
    /// End-to-End scenario tests from creation via API through AppNexus export
    /// </summary>
    [TestClass]
    public class ApiToAppNexusExportFixture : ApiFixtureBase
    {
        /// <summary>
        /// Create a simple campaign and export it to AppNexus
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CreateAndExportCampaign()
        {
            Assert.IsFalse(true);
        }
    }
}
