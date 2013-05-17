//-----------------------------------------------------------------------
// <copyright file="AbstractMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>Abstract base class for measure sources</summary>
    public abstract class AbstractMeasureSource : IMeasureSource
    {
        /// <summary>Gets the id for this source</summary>
        public abstract string SourceId { get; }

        /// <summary>
        /// Gets the minimum measure id in the range reserved for this source
        /// </summary>
        public abstract long BaseMeasureId { get; }

        /// <summary>
        /// Gets the maximum measure id in the range reserved for this source
        /// </summary>
        public abstract long MaxMeasureId { get; }

        /// <summary>Gets the measures from this source</summary>
        public abstract IDictionary<long, IDictionary<string, object>> Measures { get; }
    }
}
