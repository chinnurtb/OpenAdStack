// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KeyRuleFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Concrete default key rule factory</summary>
    internal class KeyRuleFactory : IKeyRuleFactory
    {
        /// <summary>Get a rule to generate a specific element of a key.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="storageType">The storage type.</param>
        /// <param name="keyFieldType">The type of key element the rule is for.</param>
        /// <returns>An IKeyRule</returns>
        public IKeyRule GetKeyRule(IRawEntity entity, string storageType, string keyFieldType)
        {
            // TODO: Full IKeyRule lookup mechanism.
            return new DefaultKeyRule();
        }
    }
}
