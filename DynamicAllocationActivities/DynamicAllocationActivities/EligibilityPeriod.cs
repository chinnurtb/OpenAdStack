// -----------------------------------------------------------------------
// <copyright file="EligibilityPeriod.cs" company="Rare Crowds Inc">
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