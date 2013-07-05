//-----------------------------------------------------------------------
// <copyright file="GeotargetingMeasureSourceBase.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using DynamicAllocation;

namespace AppNexusActivities.Measures
{
    /// <summary>Base class for AppNexus geotargetin measure sources</summary>
    internal abstract class GeotargetingMeasureSourceBase : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for geotargeting measures</summary>
        public const string TargetingType = "geotargeting";

        /// <summary>Initializes a new instance of the GeotargetingMeasureSourceBase class</summary>
        /// <param name="measureIdPrefix">Source measure id prefix</param>
        /// <param name="measureSourceName">Source name</param>
        /// <param name="entities">Entities (for config)</param>
        public GeotargetingMeasureSourceBase(
            byte measureIdPrefix,
            string measureSourceName,
            IEntity[] entities)
            : base(measureIdPrefix, measureSourceName, entities)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected sealed override string CategoryDisplayName
        {
            get { return "Geotargeting"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected sealed override string MeasureType
        {
            get { return "geotargeting"; }
        }

        /// <summary>Gets the display name for the measure source subcategory</summary>
        protected override abstract string SubCategoryDisplayName { get; }

        /// <summary>Gets the targeting subtype for region measures</summary>
        protected abstract string MeasureSubType { get; }

        /// <summary>Creates a AppNexus geotargeting measure</summary>
        /// <param name="apnxId">AppNexus targeting id</param>
        /// <param name="displayNameParts">Display name parts</param>
        /// <returns>The measure</returns>
        protected IDictionary<string, object> CreateAppNexusGeotargetingMeasure(
            object apnxId,
            params object[] displayNameParts)
        {
            return base.CreateAppNexusMeasure(
                displayNameParts,
                apnxId,
                this.MeasureSubType);
        }

        /// <summary>Unsupported. Use CreateAppNexusGeotargetingMeasure instead</summary>
        /// <param name="displayNameParts">Display name parts</param>
        /// <param name="apnxId">AppNexus targeting id</param>
        /// <param name="measureSubType">Measure subtype</param>
        /// <returns>The measure</returns>
        protected sealed override IDictionary<string, object> CreateAppNexusMeasure(
            object[] displayNameParts,
            object apnxId,
            string measureSubType = null)
        {
            throw new NotImplementedException(
                "Unsupported. Use GeotargetingMeasureSourceBase.CreateAppNexusGeotargetingMeasure.");
        }
    }
}
