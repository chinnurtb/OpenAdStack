// -----------------------------------------------------------------------
// <copyright file="MeasuresInputElement.cs" company="Rare Crowds Inc">
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