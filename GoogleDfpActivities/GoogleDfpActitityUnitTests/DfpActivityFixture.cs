//-----------------------------------------------------------------------
// <copyright file="DfpActivityFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using DataAccessLayer;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;

namespace GoogleDfpActivitiesUnitTests
{
    /// <summary>Tests for the DfpActivity base class</summary>
    [TestClass]
    public class DfpActivityFixture
    {
        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Test entity repository</summary>
        private IEntityRepository repository;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
        }

        /// <summary>Test creating a DFP client with entity customized configuration</summary>
        [TestMethod]
        [Ignore]
        public void CreateDfpClientWithEntityCustomConfig()
        {
        }
    }
}