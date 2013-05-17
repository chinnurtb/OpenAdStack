// -----------------------------------------------------------------------
// <copyright file="MeasureSetsInputSerialized.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DynamicAllocationActivities
{
    /// <summary>Class representing the serialized form of the MeasureSetsInput valuation inputs.</summary>
    public class MeasureSetsInputSerialized
    {
        /// <summary>
        /// Gets or sets the IdealValuation
        /// </summary>
        public decimal IdealValuation { get; set; }

        /// <summary>
        /// Gets or sets the MaxValuation
        /// </summary>
        public decimal MaxValuation { get; set; }

        /// <summary>
        /// Gets or sets the Measures
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
        public IList<MeasuresInputElement> Measures { get; set; }
    }
}