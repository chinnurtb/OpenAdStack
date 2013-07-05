//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryType.cs" company="Rare Crowds Inc">
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
