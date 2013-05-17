// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIndexStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition of factory method for Index Data Stores
    /// </summary>
    public interface IIndexStoreFactory
    {
        /// <summary>Get the one and only index store object.</summary>
        /// <returns>The index store object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Factory method.")]
        IIndexStore GetIndexStore();
    }
}
