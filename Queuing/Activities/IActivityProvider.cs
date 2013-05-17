//-----------------------------------------------------------------------
// <copyright file="IActivityProvider.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Activities
{
    /// <summary>
    /// Interface for providers of activities
    /// </summary>
    public interface IActivityProvider
    {
        /// <summary>
        /// Gets the activity types from this provider
        /// </summary>
        IEnumerable<Type> ActivityTypes { get; }

        /// <summary>
        /// Gets the context objects for the activities
        /// </summary>
        IDictionary<Type, object> ActivityContext { get; }
    }
}
