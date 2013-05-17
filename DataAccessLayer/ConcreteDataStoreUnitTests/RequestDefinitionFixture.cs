// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestDefinitionFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
