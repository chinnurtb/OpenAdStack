// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestDefinitionFixture.cs" company="Rare Crowds Inc">
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

using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>RequestDefinition unit test fixture</summary>
    [TestClass]
    public class RequestDefinitionFixture
    {
        /// <summary>shared request definition object</summary>
        private RequestDefinition requestDefinition;

        /// <summary>Per test initialization</summary>
        [TestInitialize]
        public void Initialize()
        {
            var definitionXml = ResourceHelper.LoadXmlResource(@"RequestDefinition.xml");
            this.requestDefinition = new RequestDefinition(definitionXml);
        }

        /// <summary>Get InstructionSequence</summary>
        [TestMethod]
        public void InstructionSequence()
        {
            Assert.IsNotNull(this.requestDefinition.InstructionSequence);
        }

        /// <summary>Get EntityTypes</summary>
        [TestMethod]
        public void EntityTypes()
        {
            Assert.IsNotNull(this.requestDefinition.EntityTypeInfo);
        }

        /// <summary>Get EntityTypes</summary>
        [TestMethod]
        public void EntityTypesEmpty()
        {
            var definitionXml = ResourceHelper.LoadXmlResource(@"EmptyRequestDefinition.xml");
            this.requestDefinition = new RequestDefinition(definitionXml);

            Assert.IsNull(this.requestDefinition.EntityTypeInfo);
            Assert.IsNull(this.requestDefinition.InstructionSequence);
        }
    }
}
