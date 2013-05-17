//-----------------------------------------------------------------------
// <copyright file="IPersistentDictionary.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Utilities.Storage
{
    /// <summary>Interface for dictionaries that are persisted to storage.</summary>
    /// <typeparam name="TValue">Value type. Must be a valid data contract.</typeparam>
    public interface IPersistentDictionary<TValue> : IDictionary<string, TValue>
    {
        /// <summary>Gets the name of the store where the dictionary is persisted.</summary>
        string StoreName { get; }

        /// <summary>Deletes the store where the dictionary is persisted.</summary>
        void Delete();
    }
}
