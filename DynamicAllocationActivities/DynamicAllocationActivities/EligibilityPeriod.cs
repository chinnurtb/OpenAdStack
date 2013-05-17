// -----------------------------------------------------------------------
// <copyright file="EligibilityPeriod.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace DynamicAllocationActivities
{
    /// <summary>Eligibility period encapsulation.</summary>
    public class EligibilityPeriod
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EligibilityPeriod"/> class.
        /// </summary>
        internal EligibilityPeriod()
        {
            this.EligibilityStart = DateTime.MinValue;
            this.EligibilityDuration = new TimeSpan(1, 0, 0);
        }

        /// <summary>
        /// Gets or sets EligibilityStart.
        /// </summary>
        internal DateTime EligibilityStart { get; set; }

        /// <summary>
        /// Gets or sets EligibilityDuration.
        /// </summary>
        internal TimeSpan EligibilityDuration { get; set; }

        /// <summary>
        /// Gets EligibilityEnd.
        /// </summary>
        internal DateTime EligibilityEnd 
        { 
            get
            {
                //// |         ...          |       1 hour      |
                //// ^EligibilityStart      ^EligibilityEnd     ^EligibilityStart + EligibilityDuration
                ////                         (start of last trailing hour)
                return this.EligibilityStart + this.EligibilityDuration - new TimeSpan(1, 0, 0);
            } 
        }
    }
}