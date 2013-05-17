//-----------------------------------------------------------------------
// <copyright file="MeasureMapCacheEntry.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DynamicAllocation
{
    /// <summary>Represents a cached measure map</summary>
    [DataContract]
    public sealed class MeasureMapCacheEntry
    {
        /// <summary>Backing field for MeasureMap</summary>
        private IDictionary<long, IDictionary<string, object>> measureMap;

        /// <summary>Gets or sets the expiry time of the measure map</summary>
        [DataMember]
        public DateTime Expiry { get; set; }

        /// <summary>Gets or sets the cached measure map JSON</summary>
        [DataMember]
        public string MeasureMapJson { get; set; }

        /// <summary>Gets the cached measure map</summary>
        internal IDictionary<long, IDictionary<string, object>> MeasureMap
        {
            get
            {
                return this.measureMap = this.measureMap ??
                    JsonConvert.DeserializeObject<IDictionary<long, IDictionary<string, object>>>(this.MeasureMapJson);
            }
        }
    }
}
