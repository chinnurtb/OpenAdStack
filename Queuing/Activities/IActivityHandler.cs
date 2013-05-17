// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActivityHandler.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Activities
{
    /// <summary>
    /// Interface defining and activity handler
    /// </summary>
    public interface IActivityHandler
    {
        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        IDictionary<string, string> Execute();
    }
}