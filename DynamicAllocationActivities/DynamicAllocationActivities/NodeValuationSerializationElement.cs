// -----------------------------------------------------------------------
// <copyright file="NodeValuationSerializationElement.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DynamicAllocationActivities
{
    /// <summary>Collection element for node valuation set.</summary>
    public class NodeValuationSerializationElement
    {
        /// <summary>Gets or sets MeasureSet.</summary>
        [SuppressMessage("Microsoft.Usage", "CA2227", Justification = "Serialization object")]
        public IList<string> MeasureSet { get; set; }

        /// <summary>Gets or sets MaxValuation.</summary>
        public double MaxValuation { get; set; }

        /// <summary>Gets or sets IdealValuation.</summary>
        public double IdealValuation { get; set; }
    }
}