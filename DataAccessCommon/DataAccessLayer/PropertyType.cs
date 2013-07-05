// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyType.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
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
