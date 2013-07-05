//-----------------------------------------------------------------------
// <copyright file="JsonMeasureSource.cs" company="Rare Crowds Inc">
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
using System.Linq;
using Newtonsoft.Json;

namespace DynamicAllocation
{
    /// <summary>Base class for measure sources loaded from JSON</summary>
    public abstract class JsonMeasureSource : AbstractMeasureSource, IMeasureSource
    {
        /// <summary>Backing field for IMeasureSource.Measures</summary>
        private IDictionary<long, IDictionary<string, object>> measures;

        /// <summary>Backing field for SourceId</summary>
        private string sourceId;

        /// <summary>Initializes a new instance of the JsonMeasureSource class</summary>
        /// <param name="sourceId">The measure source id</param>
        protected JsonMeasureSource(string sourceId)
        {
            this.sourceId = sourceId;
        }

        /// <summary>Gets the source id</summary>
        public override string SourceId
        {
            get { return this.sourceId; }
        }

        /// <summary>Gets the measures from this source</summary>
        public override IDictionary<long, IDictionary<string, object>> Measures
        {
            get
            {
                if (this.measures == null)
                {
                    this.measures = JsonConvert.DeserializeObject<IDictionary<long, IDictionary<string, object>>>(this.MeasureJson);
                }

                return this.measures;
            }
        }

        /// <summary>Gets the minimum measure id for this source</summary>
        public sealed override long BaseMeasureId
        {
            get { return this.Measures.Keys.Min(); }
        }

        /// <summary>Gets the maximum measure id for this source</summary>
        public sealed override long MaxMeasureId
        {
            get { return this.Measures.Keys.Max(); }
        }

        /// <summary>Gets the JSON containing this source's measures</summary>
        protected abstract string MeasureJson { get; }
    }
}
