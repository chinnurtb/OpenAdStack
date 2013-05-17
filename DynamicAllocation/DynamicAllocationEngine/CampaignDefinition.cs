// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignDefinition.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
