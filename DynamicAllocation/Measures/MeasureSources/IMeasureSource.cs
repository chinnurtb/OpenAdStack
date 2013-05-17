//-----------------------------------------------------------------------
// <copyright file="IMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
