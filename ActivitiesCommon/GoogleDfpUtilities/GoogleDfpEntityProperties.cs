//-----------------------------------------------------------------------
// <copyright file="GoogleDfpEntityProperties.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
