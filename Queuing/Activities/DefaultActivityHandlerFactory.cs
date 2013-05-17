// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultActivityHandlerFactory.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Activities
{
    /// <summary>
    /// Default (no-op) activity handler factory
    /// </summary>
    public class DefaultActivityHandlerFactory : IActivityHandlerFactory
    {
        /// <summary>Create the activity handler.</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="context">The activity context.</param>
        /// <returns>An IActivityHandler instance.</returns>
        public IActivityHandler CreateActivityHandler(ActivityRequest request, IDictionary<Type, object> context)
        {
            return new DefaultActivityHandler();
        }
    }
}