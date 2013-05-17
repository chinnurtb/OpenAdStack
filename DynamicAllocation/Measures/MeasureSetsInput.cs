// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasureSetsInput.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace DynamicAllocation
{
    /// <summary>
    /// class to represent the MeasureSets input expected from the ApiLayer
    /// </summary>
    public class MeasureSetsInput
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
        public IEnumerable<MeasuresInput> Measures { get; set; }
    }
}
