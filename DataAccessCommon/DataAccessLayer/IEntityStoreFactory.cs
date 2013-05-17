// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityStoreFactory.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition of factory method for Entity Data Stores
    /// </summary>
    public interface IEntityStoreFactory
    {
        /// <summary>Get the one and only entity store object.</summary>
        /// <returns>The entity store object.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Factory method.")]
        IEntityStore GetEntityStore();
    }
}
