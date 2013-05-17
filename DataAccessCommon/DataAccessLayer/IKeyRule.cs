// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IKeyRule.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>Interface definition for a key rule.</summary>
    public interface IKeyRule
    {
        /// <summary>Generate the key field.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The key field.</returns>
        string GenerateKeyField(IRawEntity entity);
    }
}
