//-----------------------------------------------------------------------
// <copyright file="NetworkMeasureSource.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>Base class for network measure sources</summary>
    /// <remarks>Deals with measure ids for measures retrieved from networks</remarks>
    public abstract class NetworkMeasureSource : AbstractMeasureSource, IMeasureSource
    {
        /// <summary>Maximum network measure id prefix</summary>
        public const byte MaxNetworkMeasureIdPrefix = 90;

        /// <summary>Maximum source measure id prefix</summary>
        public const byte MaxSourceMeasureIdPrefix = 99;

        /// <summary>Maximum source measure id</summary>
        public const long MaximumSourceMeasureId = SourceMeasureIdPrefixMultiplier - 1;

        /// <summary>Base measure id for Google DFP measures</summary>
        private const long NetworkMeasureIdPrefixMultiplier = 100000000000000000;

        /// <summary>Multiplier for MeasureIdPrefix used to make measure ids</summary>
        private const long SourceMeasureIdPrefixMultiplier = NetworkMeasureIdPrefixMultiplier / 100;

        /// <summary>Separator for measure display name parts</summary>
        private const string MeasureDisplayNamePartSeparator = ":";

        /// <summary>Prefix for SourceId</summary>
        /// <remarks>
        /// Combined with SourceName to create SourceId
        /// </remarks>
        private readonly string sourceIdPrefix;

        /// <summary>Backing field for BaseMeasureId</summary>
        private long baseMeasureId;

        /// <summary>Backing field for MaxMeasureId</summary>
        private long maxMeasureId;

        /// <summary>Backing field for SourceId</summary>
        private string sourceId;

        /// <summary>Initializes a new instance of the NetworkMeasureSource class</summary>
        /// <param name="networkMeasureIdPrefix">Network measure id prefix</param>
        /// <param name="sourceMeasureIdPrefix">Source measure id prefix</param>
        protected NetworkMeasureSource(byte networkMeasureIdPrefix, byte sourceMeasureIdPrefix)
        {
            if (networkMeasureIdPrefix < 0 ||
                networkMeasureIdPrefix > MaxNetworkMeasureIdPrefix)
            {
                throw new InvalidOperationException(
                    "Invalid NetworkSourceMeasureIdPrefix: {0}. Valid values are 0-{1}."
                    .FormatInvariant(networkMeasureIdPrefix, MaxNetworkMeasureIdPrefix));
            }

            if (sourceMeasureIdPrefix < 0 ||
                sourceMeasureIdPrefix > MaxSourceMeasureIdPrefix)
            {
                throw new InvalidOperationException(
                    "Invalid SourceMeasureIdPrefix: {0}. Valid values are 0-{1}."
                    .FormatInvariant(sourceMeasureIdPrefix, MaxSourceMeasureIdPrefix));
            }

            this.baseMeasureId =
                ((long)networkMeasureIdPrefix * NetworkMeasureIdPrefixMultiplier) +
                ((long)sourceMeasureIdPrefix * SourceMeasureIdPrefixMultiplier);
            this.maxMeasureId = this.baseMeasureId + MaximumSourceMeasureId;

            this.sourceIdPrefix = "NETWORK:{0,2:D02}{1,2:D02}:"
                .FormatInvariant(networkMeasureIdPrefix, sourceMeasureIdPrefix);
        }

        /// <summary>
        /// Gets the minimum measure id in the range reserved for this source
        /// </summary>
        public sealed override long BaseMeasureId
        {
            get { return this.baseMeasureId; }
        }

        /// <summary>
        /// Gets the maximum measure id in the range reserved for this source
        /// </summary>
        public sealed override long MaxMeasureId
        {
            get { return this.maxMeasureId; }
        }
        
        /// <summary>Gets the source id</summary>
        public sealed override string SourceId
        {
            get { return this.sourceId = this.sourceId ?? this.sourceIdPrefix + this.SourceName; }
        }

        /// <summary>Gets the source name</summary>
        protected abstract string SourceName { get; }

        /// <summary>Gets the master category display name</summary>
        protected abstract string MasterCategoryDisplayName { get; }

        /// <summary>Gets the category display name</summary>
        protected virtual string CategoryDisplayName
        {
            get { return string.Empty; }
        }

        /// <summary>Gets the subcategory display name</summary>
        protected virtual string SubCategoryDisplayName
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Makes a measure display name from the category display names
        /// and the provided measure name parts.
        /// </summary>
        /// <param name="measureNameParts">The name parts</param>
        /// <returns>The measure display name</returns>
        public string MakeMeasureDisplayName(params object[] measureNameParts)
        {
            var nameParts = new[]
                {
                    this.MasterCategoryDisplayName,
                    this.CategoryDisplayName,
                    this.SubCategoryDisplayName
                }
                .Concat(measureNameParts)
                .Where(part =>
                    part != null)
                .Select(part =>
                    part.ToString())
                .Where(part =>
                    !string.IsNullOrWhiteSpace(part))
                .Select(part =>
                    part.Replace(MeasureDisplayNamePartSeparator, " "));
            return string.Join(
                MeasureDisplayNamePartSeparator,
                nameParts);
        }

        /// <summary>Gets a measure id for source measure</summary>
        /// <param name="sourceMeasureId">The source measure id</param>
        /// <returns>The measure id</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the <paramref name="sourceMeasureId"/> is out of range.
        /// Valid values are 0 through MaximumSourceMeasureId.
        /// </exception>
        protected long GetMeasureId(long sourceMeasureId)
        {
            if (sourceMeasureId < 0 || sourceMeasureId > MaximumSourceMeasureId)
            {
                throw new ArgumentException(
                    "Invalid DFP measure id: {0}. Valid values are 0-{1}"
                    .FormatInvariant(sourceMeasureId, MaximumSourceMeasureId),
                    "sourceMeasureId");
            }

            return this.BaseMeasureId + sourceMeasureId;
        }
    }
}
