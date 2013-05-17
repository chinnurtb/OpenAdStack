//-----------------------------------------------------------------------
// <copyright file="TestNetwork.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DataAccessLayer;
using EntityTestUtilities;
using GoogleDfpUtilities;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Values for the integration test Google DFP account/network</summary>
    public static class TestNetwork
    {
        /// <summary>Test network id</summary>
        public const int NetworkId = 8010464;

        /// <summary>Test account user name</summary>
        public const string Username = "dfp.dev@rarecrowds.com";

        /// <summary>Test account password</summary>
        public const string Password = "{73577E69-5B15-4047-BA82-041DEE8CA1FB}";

        /// <summary>Test advertiser name</summary>
        public const string AdvertiserName = "Acme Advertiser";

        /// <summary>Test advertiser id</summary>
        public const long AdvertiserId = 12904024;

        /// <summary>Test AdUnitIds</summary>
        public static readonly string[] AdUnitCodes = new[]
        {
            "AcmeSupplyCo_Home_ATF_header_728x90",
            "AcmeSupplyCo_Home_ATF_skyscraper_90x728",
            "AcmeSupplyCo_Home_ATF_footer_728x90",
            "AcmeSupplyCo_Home_ATF_Left_90x90",
            "AcmeSupplyCo_Home_ATF_Right_300x250",
            "AcmeSupplyCo_Help_ATF_header_728x90",
            "AcmeSupplyCo_Help_ATF_skyscraper_90x728",
            "AcmeSupplyCo_Help_ATF_footer_728x90",
            "AcmeSupplyCo_Help_ATF_Left_90x90",
            "AcmeSupplyCo_Help_ATF_Right_300x250",
            "AcmeSupplyCo_News_ATF_header_728x90",
            "AcmeSupplyCo_News_ATF_skyscraper_90x728",
            "AcmeSupplyCo_News_ATF_footer_728x90",
            "AcmeSupplyCo_News_ATF_Left_90x90",
            "AcmeSupplyCo_News_ATF_Right_300x250",
            "AcmeSupplyCo_Deals_ATF_header_728x90",
            "AcmeSupplyCo_Deals_ATF_skyscraper_90x728",
            "AcmeSupplyCo_Deals_ATF_footer_728x90",
            "AcmeSupplyCo_Deals_ATF_Left_90x90",
            "AcmeSupplyCo_Deals_ATF_Right_300x250",
            "AcmeSupplyCo_Products_ATF_header_728x90",
            "AcmeSupplyCo_Products_ATF_skyscraper_90x728",
            "AcmeSupplyCo_Products_ATF_footer_728x90",
            "AcmeSupplyCo_Products_ATF_Left_90x90",
            "AcmeSupplyCo_Products_ATF_Right_300x250",
        };

        /// <summary>Test placement ids</summary>
        public static readonly IDictionary<long, string> Placements = new Dictionary<long, string>
        {
            { 859984, "AcmeSupplyCo Banners All" },
            { 859504, "AcmeSupplyCo Deals" },
            { 860464, "AcmeSupplyCo Footers All" },
            { 859384, "AcmeSupplyCo Help All" },
            { 859144, "AcmeSupplyCo Home All" },
            { 860224, "AcmeSupplyCo Homepage Header" },
            { 859624, "AcmeSupplyCo News All" },
            { 860344, "AcmeSupplyCo OtherPages Header" },
            { 859744, "AcmeSupplyCo Products" },
            { 860104, "AcmeSupplyCo Skyscrapers All" },
            { 860944, "Test Run Placement - 335edca6-b36b-4848-b35b-35c2dd10290b" },
            { 861184, "Test Run Placement - 4edd9563-7094-45c6-8d80-273cef9bccde" },
            { 861064, "Test Run Placement - 8ffade00-c0fa-4d4a-90c9-7c5b465c9592" },
            { 861304, "Test Run Placement - b9af929c-b2b4-412e-b2e2-9f86d21dddac" },
            { 861424, "Test Run Placement - be91d394-9d50-4b73-a092-3ef93cd08f8f" },
        };

        /// <summary>AdUnit measure ids</summary>
        public static readonly long[] AdUnitMeasures = new[]
        {
            103000000007075384, 103000000007075144, 103000000007075504, 103000000007075624, 103000000007075264,
            103000000007074184, 103000000007073944, 103000000007074304, 103000000007074424, 103000000007074064,
            103000000007073584, 103000000007073344, 103000000007073704, 103000000007073824, 103000000007073464,
            103000000007074784, 103000000007074544, 103000000007074904, 103000000007075024, 103000000007074664,
            103000000007075984, 103000000007075744, 103000000007076104, 103000000007076224, 103000000007075864
        };

        /// <summary>Placement measure ids</summary>
        public static readonly long[] PlacementMeasures = new[]
        {
            102000000000859984, 102000000000859504, 102000000000860464, 102000000000859384, 102000000000859144,
            102000000000860224, 102000000000859624, 102000000000860344, 102000000000859744, 102000000000860104
        };

        /// <summary>Location measure ids</summary>
        public static readonly long[] LocationMeasures = new[]
        {
            101000000000002036, 101000000000002050, 101000000000002056, 101000000000002070, 101000000000002254,
            101000000009006065, 101000000009007486, 101000000009007495, 101000000009024799, 101000000009024812,
            101000000009024927, 101000000009033896, 101000000009033908, 101000000009033975, 101000000009033983,
            101000000009033986, 101000000009034056, 101000000009034062, 101000000009034071
        };

        /// <summary>Technology measure ids</summary>
        public static readonly long[] TechnologyMeasures = new[]
        {
            104000000000000001, 104000000000000007, 104000000000500012, 104000000000500081, 104000000000500042,
            104000000000504003, 104000000000600445, 104000000000600928, 104000000000601277, 104000000000601782,
            104000000000601978, 104000000000601985, 104000000000601992, 104000000000602365, 104000000000603264,
            104000000000605603, 104000000000607366, 104000000000607378, 104000000000607383, 104000000000608655,
            104000000000608669, 104000000000630182, 104000000000630233, 104000000000630258
        };

        /// <summary>Backing field for AdvertiserCompanyEntity</summary>
        private static CompanyEntity advertiserCompanyEntity;

        /// <summary>Gets a company entity setup for the test advertiser</summary>
        public static CompanyEntity AdvertiserCompanyEntity
        {
            get
            {
                if (advertiserCompanyEntity == null)
                {
                    advertiserCompanyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                        new EntityId(), AdvertiserName);
                    advertiserCompanyEntity.SetDfpAdvertiserId(AdvertiserId);
                }

                return advertiserCompanyEntity;
            }
        }
    }
}
