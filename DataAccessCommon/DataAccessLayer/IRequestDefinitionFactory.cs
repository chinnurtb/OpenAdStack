// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRequestDefinitionFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Factory interface for requesting a request definition
    /// </summary>
    public interface IRequestDefinitionFactory
    {
        /// <summary>Create RequestDefinition object given a request name.</summary>
        /// <param name="requestName">The request name.</param>
        /// <returns>RequestDefinition object.</returns>
        RequestDefinition CreateRequestDefinition(string requestName);
    }
}
