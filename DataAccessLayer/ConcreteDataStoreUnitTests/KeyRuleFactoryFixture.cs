// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KeyRuleFactoryFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for KeyRuleFactory.</summary>
    [TestClass]
    public class KeyRuleFactoryFixture
    {
        /// <summary>Default construction should give us a valid IKeyRuleFactory</summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            var keyRule = new KeyRuleFactory() as IKeyRuleFactory;
            Assert.IsNotNull(keyRule);
        }

        /// <summary>Test we can retrieve a key rule</summary>
        [TestMethod]
        public void GetKeyRule()
        {
            var keyRule = new KeyRuleFactory();
            keyRule.GetKeyRule(new Entity(), "Azure", "Partition");
        }
    }
}
