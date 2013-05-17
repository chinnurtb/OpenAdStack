//-----------------------------------------------------------------------
// <copyright file="IClaimRetriever.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.IdentityFederation
{
    /// <summary>Interface for retrievers of claims</summary>
    public interface IClaimRetriever
    {
        /// <summary>Gets the value for the specified claim.</summary>
        /// <param name="claimType">The claim type.</param>
        /// <returns>The claim value, if available; otherwise null.</returns>
        string GetClaimValue(string claimType);
    }
}
