//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryType.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Storage
{
    /// <summary>
    /// Types of persistent dictionaries
    /// </summary>
    public enum PersistentDictionaryType
    {
        /// <summary>
        /// Unknown type
        /// </summary>
        Unknown,

        /// <summary>
        /// Stored in memory (local to role instance)
        /// </summary>
        Memory,

        /// <summary>
        /// Stored in the cloud (pay-per-transaction)
        /// </summary>
        Cloud,

        /// <summary>
        /// Stored in a SQL database (pay-per-size)
        /// </summary>
        Sql
    }
}
