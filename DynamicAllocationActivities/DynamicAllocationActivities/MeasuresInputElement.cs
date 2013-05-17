// -----------------------------------------------------------------------
// <copyright file="MeasuresInputElement.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DynamicAllocationActivities
{
    /// <summary>Class representing the serialized form of the MeasureInput valuation inputs.</summary>
    public class MeasuresInputElement
    {
        /// <summary>
        /// Gets or sets measureId: the measure
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
        public string measureId { get; set; }

        /// <summary>
        /// Gets or sets valuation: the base valuation for the measure
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
        public int valuation { get; set; }

        /// <summary>
        /// Gets or sets Group: the OR Group the measure belongs to (may be null if it belongs to its own group)
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
        public string @group { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the measure is pinned
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Serialization object")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Serialization object")]
        public bool pinned { get; set; }
    }
}