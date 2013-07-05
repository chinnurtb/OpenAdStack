//-----------------------------------------------------------------------
// <copyright file="AppNexusMeasureTypes.cs" company="Rare Crowds Inc">
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

namespace AppNexusActivities.Measures
{
    /// <summary>
    /// AppNexus Measure Types
    /// </summary>
    public static class AppNexusMeasureTypes
    {
        /// <summary>Age range demographic</summary>
        public const string AgeRange = "AgeRange";

        /// <summary>Gender demographic</summary>
        public const string Gender = "Gender";

        /// <summary>Segment (aka pixel)</summary>
        public const string Segment = "Segment";

        /// <summary>Designated Market Area geotarget</summary>
        public const string Dma = "Dma";

        /// <summary>State level geotarget</summary>
        public const string State = "State";

        /// <summary>Creative position target</summary>
        public const string Position = "Position";

        /// <summary>Inventory attribute target</summary>
        public const string Inventory = "Inventory";

        /// <summary>Include Content SubType</summary>
        public const string ContentCategoryInclude = "ContentCategoryInclude";

        /// <summary>Exclude Content SubType</summary>
        public const string ContentCategoryExclude = "ContentCategoryExclude";

        /// <summary>Include AdUnit</summary>
        public const string AdUnitInclude = "AdUnitInclude";

        /// <summary>Exclude AdUnit</summary>
        public const string AdUnitExclude = "AdUnitExclude";

        /// <summary>Ad Placement</summary>
        public const string Placement = "Placement";
    }
}
