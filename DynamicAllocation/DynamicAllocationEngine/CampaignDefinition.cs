// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignDefinition.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DynamicAllocation
{
    /// <summary>
    /// Class to represent a campaign defintion 
    /// </summary>
    [DataContract]
    public class CampaignDefinition
    {
        /// <summary>
        /// Gets or sets MaxPersonaValuation.
        /// </summary>
        [DataMember]
        public decimal MaxPersonaValuation { get; set; }

        /// <summary>
        /// Gets or sets MeasureGroupings.
        /// This determines which segments should be grouped as logical OR
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
        [DataMember]
        public IDictionary<long, string> MeasureGroupings { get; set; }

        /// <summary>
        /// Gets or sets ExplicitValuations.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
        [DataMember]
        public IDictionary<MeasureSet, decimal> ExplicitValuations { get; set; }

        /// <summary>
        /// Gets or sets attributes that must be present in all output valuations
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227", Justification = "It's OK for transport objects to have their collection properties replaced")]
        [DataMember]
        public ICollection<long> PinnedMeasures { get; set; }
    }
}
