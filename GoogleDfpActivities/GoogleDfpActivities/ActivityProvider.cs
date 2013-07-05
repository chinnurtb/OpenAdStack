//-----------------------------------------------------------------------
// <copyright file="ActivityProvider.cs" company="Rare Crowds Inc">
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
