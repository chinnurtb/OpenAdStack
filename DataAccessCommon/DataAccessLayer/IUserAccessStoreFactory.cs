//-----------------------------------------------------------------------
// <copyright file="IUserAccessStoreFactory.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition of factory method for UserAccess Data Stores
    /// </summary>
    public interface IUserAccessStoreFactory
    {
        /// <summary>Get the one and only user access store object.</summary>
        /// <returns>The user access store object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Factory method.")]
        IUserAccessStore GetUserAccessStore();
    }
}
