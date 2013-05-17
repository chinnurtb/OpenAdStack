// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBlobStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition of factory method for Entity Data Stores
    /// </summary>
    public interface IBlobStoreFactory
    {
        /// <summary>Get a blob store object.</summary>
        /// <returns>The a blob store object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Factory method.")]
        IBlobStore GetBlobStore();
    }
}
