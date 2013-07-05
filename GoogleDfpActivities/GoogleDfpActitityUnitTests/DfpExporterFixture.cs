//-----------------------------------------------------------------------
// <copyright file="DfpExporterFixture.cs" company="Rare Crowds Inc">
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
    public class DfpExporterFixture
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