//-----------------------------------------------------------------------
// <copyright file="MeasureMapCacheEntry.cs" company="Rare Crowds Inc">
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
