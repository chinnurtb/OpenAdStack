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
using EntityActivities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Interface for providers of activities
    /// </summary>
    public class ActivityProvider : IActivityProvider
    {
        /// <summary>
        /// Initializes a new instance of the ActivityProvider class.
        /// </summary>
        /// <param name="repository">repository for the entity activities</param>
        /// <param name="userAccessRepository">repository for user access</param>
        public ActivityProvider(IEntityRepository repository, IUserAccessRepository userAccessRepository)
        {
            this.ActivityTypes = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => typeof(Activity).IsAssignableFrom(t) && !t.IsAbstract);

            this.ActivityContext = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), repository },
                { typeof(IUserAccessRepository), userAccessRepository },
            };
        }

        /// <summary>
        /// Gets the entity activity types
        /// </summary>
        public IEnumerable<Type> ActivityTypes { get; private set; }

        /// <summary>
        /// Gets the object context for the entity activities
        /// </summary>
        public IDictionary<Type, object> ActivityContext { get; private set; }
    }
}
