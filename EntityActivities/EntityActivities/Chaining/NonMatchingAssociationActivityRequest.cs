//-----------------------------------------------------------------------
// <copyright file="NonMatchingAssociationActivityRequest.cs" company="Rare Crowds Inc">
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
using Activities;
using DataAccessLayer;

namespace EntityActivities.Chaining
{
    /// <summary>
    /// Used to conditionally generate activity requests for unmatching associations
    /// </summary>
    public class NonMatchingAssociationActivityRequest
    {
        /// <summary>
        /// Initializes a new instance of the NonMatchingAssociationActivityRequest class.
        /// </summary>
        public NonMatchingAssociationActivityRequest()
        {
            this.AssociationNames = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Gets the name of the associations that if unmatching
        /// indicates this request should be submitted.
        /// </summary>
        public IDictionary<string, string> AssociationNames { get; private set;  }

        /// <summary>
        /// Gets or sets a delegate function that creates the request.
        /// </summary>
        public ChainedEntityActivityRequest ChainedActivityRequest { get; set; }

        /// <summary>
        /// Checks whether this unmatching association request applies to the
        /// the specified entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>True if this activity request applies; Otherwise, false.</returns>
        public bool Applies(IEntity entity)
        {
            // Return whether any of the association pairs for this activity request are
            // present and if so whether or not they match based upon whether one is null
            // and the other is not or if they are both not null and their values are
            // different. This implies false if both associations in a pair are null.
            return this.AssociationNames.Any(associationName =>
                
                // One is null and the other isn't
                ((entity.TryGetAssociationByName(associationName.Key) != null) !=
                 (entity.TryGetAssociationByName(associationName.Value) != null)) ||
                
                // Neither is null and their values don't match
                (entity.TryGetAssociationByName(associationName.Key) != null &&
                 entity.TryGetAssociationByName(associationName.Value) != null &&
                 entity.TryGetAssociationByName(associationName.Key).TargetEntityId !=
                 entity.TryGetAssociationByName(associationName.Value).TargetEntityId));
        }
    }
}
