// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultActivityHandler.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Activities
{
    /// <summary>
    /// Default (no-op) activity handler
    /// </summary>
    public class DefaultActivityHandler : IActivityHandler
    {
        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        public IDictionary<string, string> Execute()
        {
            return new Dictionary<string, string>();
        }
    }
}