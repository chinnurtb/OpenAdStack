// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewerCommand.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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