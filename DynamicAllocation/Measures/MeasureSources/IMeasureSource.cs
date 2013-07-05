//-----------------------------------------------------------------------
// <copyright file="IMeasureSource.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DynamicAllocation
{
    /// <summary>Describes the interface for sources of measures</summary>
    public interface IMeasureSource
    {
        /// <summary>Gets the id for this source type</summary>
        string SourceId { get; }

        /// <summary>Gets the minimum measure id in the range reserved for this source</summary>
        long BaseMeasureId { get; }
        
        /// <summary>Gets the maximum measure id in the range reserved for this source</summary>
        long MaxMeasureId { get; }

        /// <summary>Gets the measures from this source</summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Accurately reflects the format of the data.")]
        IDictionary<long, IDictionary<string, object>> Measures { get; }
    }
}
