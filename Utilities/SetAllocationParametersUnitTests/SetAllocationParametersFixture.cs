// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetAllocationParametersFixture.cs" company="Rare Crowds Inc">
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
using ConsoleAppUtilities;
using DataAccessLayer;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using SetAllocationParameters;

namespace SetAllocationParametersUnitTests
{
    /// <summary>Test fixture for SetAllocationParameters</summary>
    [TestClass]
    public class SetAllocationParametersFixture
    {
        /// <summary>company entity id for testing</summary>
        private EntityId companyId;

        /// <summary>target entity id for testing</summary>
        private EntityId targetId;

        /// <summary>minimum argument set for testing</summary>
        private string[] minArgs;

        /// <summary>maximum argument set for testing</summary>
        private string[] maxArgs;

        /// <summary>repository stub for testing</summary>
        private IEntityRepository repository;

        /// <summary>target entity for testing</summary>
        private CampaignEntity targetEntity;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyId = new EntityId();
            this.targetId = new EntityId();
            this.targetEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.targetId, "foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");
            this.minArgs = new[] { "-params", "testparams.js", "-company", this.companyId.ToString() };
            this.maxArgs = new[]
                {
                    "-params", 
                    "testparams.js", 
                    "-replace", 
                    "-company", 
                    this.companyId.ToString(), 
                    "-target", 
                    this.targetId.ToString(),
                    "-log",
                    ".\\logfile.txt"
                };
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
        }

        /// <summary>Test a valid command-line with all options passes.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void MinValidateArgumentsSuccess()
        {
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(this.minArgs);
            Assert.IsTrue(arguments.ArgumentsValid);
        }

        /// <summary>Test a valid command-line with all options passes.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void MaxValidateArgumentsSuccess()
        {
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(this.maxArgs);
            Assert.IsTrue(arguments.ArgumentsValid);
        }

        /// <summary>Test missing param file argument or file fails validation.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void InvalidOrMissingParamsFile()
        {
            var args = new[] { "-company", (new EntityId()).ToString() };
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            Assert.IsFalse(arguments.ArgumentsValid);

            args = new[] { "-params", "nothere.js", "-company", (new EntityId()).ToString() };
            arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            Assert.IsFalse(arguments.ArgumentsValid);
        }

        /// <summary>Test missing company or invalid id fails validation.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void InvalidOrMissingOrInvalidCompanyId()
        {
            var args = new[] { "-params", "testparams.js" };
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            Assert.IsFalse(arguments.ArgumentsValid);

            args = new[] { "-params", "testparams.js", "-company", "notavalidid" };
            arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            Assert.IsFalse(arguments.ArgumentsValid);
        }

        /// <summary>Test invalid target id fails validation.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void InvalidTargetId()
        {
            var args = new[] { "-params", "testparams.js", "-company", (new EntityId()).ToString(), "-target", "notavalidid" };
            var arguments = CommandLineArguments.Create<SetAllocationParametersArgs>(args);
            Assert.IsFalse(arguments.ArgumentsValid);
        }

        /// <summary>Test args only construction with all args.</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void WorkHandlerFactoryMaxArgs()
        {
            var handler = WorkHandlerFactory.BuildWorkHandler(this.maxArgs);
            Assert.IsNotNull(handler.Repository);
            Assert.AreEqual(this.companyId, handler.CompanyEntityId);
            Assert.AreEqual(this.targetId, handler.TargetEntityId);
            Assert.IsTrue(handler.Replace);
            Assert.IsNotNull(handler.ParamsJson);
        }

        /// <summary>Test args only construction with minimum args</summary>
        [TestMethod]
        [DeploymentItem("testparams.js")]
        public void WorkHandlerFactoryDefaults()
        {
            var handler = WorkHandlerFactory.BuildWorkHandler(this.minArgs);
            Assert.IsNotNull(handler.Repository);
            Assert.AreEqual(this.companyId, handler.CompanyEntityId);
            
            // target should be company
            Assert.AreEqual(this.companyId, handler.TargetEntityId);

            Assert.IsFalse(handler.Replace);
            Assert.IsNotNull(handler.ParamsJson);
        }

        /// <summary>Test replace params on target entity.</summary>
        [TestMethod]
        public void ReplaceParams()
        {
            // Set up existing params
            var existingConfigs = new Dictionary<string, string>
                {
                    { "DynamicAllocation.Margin", "1.17647059" }
                };
            this.targetEntity.SetConfigSettings(existingConfigs);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.targetId, this.targetEntity, false);

            // Set up save capture
            CampaignEntity saveEntity = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => saveEntity = e, false);

            var newParamsJson = "{\"DynamicAllocation.Margin\" : \"1.5\", \"DynamicAllocation.MinBudget\" : \".3\"}";
            var handler = WorkHandlerFactory.BuildWorkHandler(
                this.repository, newParamsJson, this.companyId, this.targetId, true);
            
            handler.Run();
            
            var configs = saveEntity.GetConfigSettings();
            Assert.AreEqual(2, configs.Count);
            Assert.AreEqual("1.5", configs["DynamicAllocation.Margin"]);
            Assert.AreEqual(".3", configs["DynamicAllocation.MinBudget"]);
        }

        /// <summary>Test replace params doesn't remove non-allocation parameter configs.</summary>
        [TestMethod]
        public void ReplaceParamsDoesNotRemoveNonAllocationConfig()
        {
            // Set up existing params
            var existingConfigs = new Dictionary<string, string>
                {
                    { "SomeOtherNamespace.Config", "not ours" },
                    { "DynamicAllocation.Margin", "1.17647059" }
                };
            this.targetEntity.SetConfigSettings(existingConfigs);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.targetId, this.targetEntity, false);

            // Set up save capture
            CampaignEntity saveEntity = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => saveEntity = e, false);

            var newParamsJson = "{\"DynamicAllocation.Margin\" : \"1.5\", \"DynamicAllocation.MinBudget\" : \".3\"}";
            var handler = WorkHandlerFactory.BuildWorkHandler(
                this.repository, newParamsJson, this.companyId, this.targetId, true);
            
            handler.Run();
            
            var configs = saveEntity.GetConfigSettings();
            Assert.AreEqual(3, configs.Count);
            Assert.AreEqual("not ours", configs["SomeOtherNamespace.Config"]);
            Assert.AreEqual("1.5", configs["DynamicAllocation.Margin"]);
            Assert.AreEqual(".3", configs["DynamicAllocation.MinBudget"]);
        }

        /// <summary>Test add/update params on target entity.</summary>
        [TestMethod]
        public void AddAndUpdateParams()
        {
            // Set up existing params
            var existingConfigs = new Dictionary<string, string>
                {
                    { "DynamicAllocation.Thud", "3.33" },
                    { "DynamicAllocation.Margin", "1.17647059" }
                };
            this.targetEntity.SetConfigSettings(existingConfigs);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.targetId, this.targetEntity, false);

            // Set up save capture
            CampaignEntity saveEntity = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repository, e => saveEntity = e, false);

            var newParamsJson = "{\"DynamicAllocation.Margin\" : \"1.5\", \"DynamicAllocation.MinBudget\" : \".3\"}";
            var handler = WorkHandlerFactory.BuildWorkHandler(
                this.repository, newParamsJson, this.companyId, this.targetId, false);
            
            handler.Run();
            
            var configs = saveEntity.GetConfigSettings();
            Assert.AreEqual(3, configs.Count);
            Assert.AreEqual("3.33", configs["DynamicAllocation.Thud"]);
            Assert.AreEqual("1.5", configs["DynamicAllocation.Margin"]);
            Assert.AreEqual(".3", configs["DynamicAllocation.MinBudget"]);
        }
    }
}
