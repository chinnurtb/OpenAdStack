//-----------------------------------------------------------------------
// <copyright file="ModifiedPropertyActivityRequest.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
    /// Used to conditionally generate activity requests
    /// </summary>
    public class ModifiedPropertyActivityRequest
    {
        /// <summary>
        /// Initializes a new instance of the ModifiedPropertyActivityRequest class.
        /// </summary>
        public ModifiedPropertyActivityRequest()
        {
            this.PropertyNames = new List<string>();
        }
        
        /// <summary>
        /// Gets the name of the modified properties that indicate this
        /// request should be submitted.
        /// </summary>
        public ICollection<string> PropertyNames { get; private set; }

        /// <summary>
        /// Gets or sets the type of change that indicates this request should
        /// be submitted.
        /// </summary>
        public PropertyChangeTypes ChangeType { get; set; }

        /// <summary>
        /// Gets or sets a delegate function that creates the request.
        /// </summary>
        public ChainedEntityActivityRequest ChainedActivityRequest { get; set; }

        /// <summary>
        /// Checks whether this modified property request applies to the changes
        /// between the specified original and updated entities.
        /// </summary>
        /// <param name="original">The original entity</param>
        /// <param name="updated">The updated entity</param>
        /// <returns>True if this activity request applies; Otherwise, false.</returns>
        public bool Applies(EntityWrapperBase original, EntityWrapperBase updated)
        {
            if (this.ChangeType == PropertyChangeTypes.None)
            {
                throw new InvalidOperationException("ChangeType cannot be None");
            }

            // Return whether any of the properties have been modified in a way that
            // indicates this activity request should be submitted.
            return this.PropertyNames
                .Any(propertyName =>
                
                // Added?
                (((this.ChangeType & PropertyChangeTypes.Added) != PropertyChangeTypes.None) &&
                   (original.TryGetPropertyValueByName(propertyName) == null &&
                    updated.TryGetPropertyValueByName(propertyName) != null)) ||
                
                // Removed?
                (((this.ChangeType & PropertyChangeTypes.Removed) != PropertyChangeTypes.None) &&
                   (original.TryGetPropertyValueByName(propertyName) != null &&
                    updated.TryGetPropertyValueByName(propertyName) == null)) ||
                
                // Changed?
                (((this.ChangeType & PropertyChangeTypes.Changed) != PropertyChangeTypes.None) &&
                   (original.TryGetPropertyValueByName(propertyName) !=
                    updated.TryGetPropertyValueByName(propertyName))));
        }
    }
}
