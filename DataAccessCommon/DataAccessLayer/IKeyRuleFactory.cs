// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IKeyRuleFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>Factory interface for classes that generate IKeyRule objects.</summary>
    public interface IKeyRuleFactory
    {
        /// <summary>Get a rule to generate a specific element of a key.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="storageType">The storage type.</param>
        /// <param name="keyFieldType">The type of key element the rule is for.</param>
        /// <returns>An IKeyRule</returns>
        IKeyRule GetKeyRule(IRawEntity entity, string storageType, string keyFieldType);
    }
}
