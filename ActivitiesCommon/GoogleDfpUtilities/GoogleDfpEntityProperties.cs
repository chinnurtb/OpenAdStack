//-----------------------------------------------------------------------
// <copyright file="GoogleDfpEntityProperties.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

namespace GoogleDfpUtilities
{
    /// <summary>
    /// Names of the entity properties in which Google DFP related values are kept
    /// </summary>
    public static class GoogleDfpEntityProperties
    {
        /// <summary>Image Property</summary>
        public const string Image = "Image";

        /// <summary>ClickUrl Property</summary>
        public const string ClickUrl = "ClickUrl";

        /// <summary>Google DFP Agency Company ID</summary>
        public const string AgencyId = "DFPAgencyId";

        /// <summary>Google DFP Advertiser Company ID</summary>
        public const string AdvertiserId = "DFPAdvertiserId";

        /// <summary>Google DFP Order ID</summary>
        public const string OrderId = "DFPOrderId";

        /// <summary>Google DFP Creative ID</summary>
        public const string CreativeId = "DFPCreativeId";

        /// <summary>Property name for the raw delivery data from Google DFP</summary>
        public const string DfpRawDeliveryDataIndex = "DFPRawDeliveryDataIndex";
    }
}
