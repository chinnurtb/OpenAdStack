//-----------------------------------------------------------------------
// <copyright file="ActivityProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Activities;
using DataAccessLayer;

namespace GoogleDfpActivities
{
    /// <summary>Google DFP Activity Provider</summary>
    public class ActivityProvider : IActivityProvider
    {
        /// <summary>Initializes a new instance of the ActivityProvider class.</summary>
        /// <param name="repository">repository for the entity activities</param>
        public ActivityProvider(IEntityRepository repository)
        {
            this.ActivityTypes = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => typeof(DfpActivity).IsAssignableFrom(t) && !t.IsAbstract);
            this.ActivityContext = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), repository }
            };
        }

        /// <summary>Gets the entity activity types</summary>
        public IEnumerable<Type> ActivityTypes { get; private set; }

        /// <summary>Gets the object context for the entity activities</summary>
        public IDictionary<Type, object> ActivityContext { get; private set; }
    }
}
