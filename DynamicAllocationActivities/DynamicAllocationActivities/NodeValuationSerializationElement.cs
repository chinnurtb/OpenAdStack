// -----------------------------------------------------------------------
// <copyright file="NodeValuationSerializationElement.cs" company="Rare Crowds Inc">
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