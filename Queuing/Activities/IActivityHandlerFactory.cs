// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActivityHandlerFactory.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Activities
{
    /// <summary>
    /// Interface for activity handler factories
    /// </summary>
    public interface IActivityHandlerFactory
    {
        /// <summary>Create the activity handler.</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="context">The activity context.</param>
        /// <returns>An IActivityHandler instance.</returns>
        IActivityHandler CreateActivityHandler(ActivityRequest request, IDictionary<Type, object> context);
    }
}