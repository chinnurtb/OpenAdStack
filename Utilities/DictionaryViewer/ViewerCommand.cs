// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewerCommand.cs" company="Rare Crowds Inc">
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

namespace Utilities.DictionaryViewer
{
    /// <summary>Available commands</summary>
    public enum ViewerCommand
    {
        /// <summary>Unknown command</summary>
        Unknown,

        /// <summary>Get a dictionary entry</summary>
        /// <remarks>Requires -o</remarks>
        Get,

        /// <summary>Set a dictionary entry</summary>
        /// <remarks>Requires -i</remarks>
        Set,

        /// <summary>Display a dictionary entry</summary>
        View,

        /// <summary>List entries</summary>
        List,

        /// <summary>Remove a dictionary entry</summary>
        Remove,

        /// <summary>Remove all entries from a dictionary</summary>
        Delete,

        /// <summary>Display an index of all dictionaries</summary>
        Index,
    }
}