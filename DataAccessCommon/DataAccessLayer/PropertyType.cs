// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyType.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Enumeration of native types supported in serialized properties
    /// </summary>
    public enum PropertyType
    {
        /// <summary>Serialized Type name for int</summary>
        Int32,

        /// <summary>Serialized Type name for long</summary>
        Int64,

        /// <summary>Serialized Type name for double</summary>
        Double,

        /// <summary>Serialized Type name for string</summary>
        String,

        /// <summary>Serialized Type name for DateTime</summary>
        Date,

        /// <summary>Serialized Type name for bool</summary>
        Bool,

        /// <summary>Serialized Type name for byte[]</summary>
        Binary,

        /// <summary>Serialized Type name for Guid</summary>
        Guid,

        /// <summary>
        /// Serialized Type name for a reference to an item stored as a blob
        /// external to entity storage.
        /// </summary>
        BlobRef
    }
}
