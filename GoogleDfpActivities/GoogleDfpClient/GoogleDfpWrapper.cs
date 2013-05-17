//-----------------------------------------------------------------------
// <copyright file="GoogleDfpWrapper.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using ConfigManager;
using Google.Api.Ads.Common.Lib;
using Google.Api.Ads.Common.Util;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.Util.v201206;
using Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpClient
{
    /// <summary>Implementation of IGoogleDfpClient wrapping the Google Ads DFP APIs</summary>
    internal class GoogleDfpWrapper : GoogleDfpWrapperBase, IGoogleDfpClient
    {
        /// <summary>Multiplier for converting USD to DEF Money.microAmount</summary>
        internal const long DollarsToDfpMoneyMicrosMultiplier = 1000000L;

        /// <summary>Maximum length for Company.name</summary>
        private const int MaxCompanyNameLength = 127;

        /// <summary>Maximum length for Company.externalid</summary>
        private const int MaxCompanyExternalIdLength = 255;

        /// <summary>Maximum length for Order.name</summary>
        private const int MaxOrderName = 128;

        /// <summary>Initializes a new instance of the GoogleDfpWrapper class</summary>
        public GoogleDfpWrapper()
            : this(new CustomConfig())
        {
        }

        /// <summary>Initializes a new instance of the GoogleDfpWrapper class</summary>
        /// <param name="config">Configuration to use.</param>
        public GoogleDfpWrapper(IConfig config)
            : base(config)
        {
        }

        /// <summary>Creates a Company of type AGENCY in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateAgency(string companyName, string externalId)
        {
            return this.CreateCompany(companyName, externalId, CompanyType.AGENCY);
        }

        /// <summary>Creates a Company of type HOUSE_AGENCY in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateHouseAgency(string companyName, string externalId)
        {
            return this.CreateCompany(companyName, externalId, CompanyType.HOUSE_AGENCY);
        }

        /// <summary>Creates a Company of type ADVERTISER in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateAdvertiser(string companyName, string externalId)
        {
            return this.CreateCompany(companyName, externalId, CompanyType.ADVERTISER);
        }

        /// <summary>Creates a Company of type HOUSE_ADVERTISER in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateHouseAdvertiser(string companyName, string externalId)
        {
            return this.CreateCompany(companyName, externalId, CompanyType.HOUSE_ADVERTISER);
        }

        /// <summary>Gets a Company</summary>
        /// <param name="companyId">The company id</param>
        /// <returns>The company</returns>
        public Company GetCompany(long companyId)
        {
            return this.CompanyService.Invoke(svc =>
                svc.getCompany(companyId));
        }

        /// <summary>Creates an bare-bones order in DFP</summary>
        /// <param name="advertiserId">The DFP advertiser id</param>
        /// <param name="orderName">The order name</param>
        /// <param name="startDate">The order start DateTime</param>
        /// <param name="endDate">The order end DateTime</param>
        /// <returns>The created order's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateOrder(long advertiserId, string orderName, System.DateTime startDate, System.DateTime endDate)
        {
            var order = new Order
            {
                advertiserId = advertiserId,
                name = orderName.Left(MaxOrderName),
                traffickerId = this.TraffickerId,
                startDateTime = startDate.ToDfpDateTime(),
                endDateTime = endDate.ToDfpDateTime()
            };
            order = this.OrderService.Invoke(
                svc => svc.createOrder(order));
            return order.id;
        }

        /// <summary>Gets an order as a dictionary of key-value pairs</summary>
        /// <param name="orderId">The order id</param>
        /// <returns>The order values</returns>
        public Order GetOrder(long orderId)
        {
            return this.OrderService.Invoke(
                svc => svc.getOrder(orderId));
        }

        /// <summary>Updates an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <param name="startDate">The order start DateTime</param>
        /// <param name="endDate">The order end DateTime</param>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public void UpdateOrder(long orderId, System.DateTime startDate, System.DateTime endDate)
        {
            var order = this.OrderService.getOrder(orderId);
            order.startDateTime = startDate.ToDfpDateTime();
            order.endDateTime = endDate.ToDfpDateTime();
            this.OrderService.Invoke(
                svc => svc.updateOrder(order));
        }

        /// <summary>Approves an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <returns>True if the order was approved</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public bool ApproveOrder(long orderId)
        {
            return this.PerformOrderAction<ApproveOrders>(orderId);
        }

        /// <summary>Deletes an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <returns>True if the order was deleted</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public bool DeleteOrder(long orderId)
        {
            return this.PerformOrderAction<DeleteOrders>(orderId);
        }

        /// <summary>Gets the line-items for the specified orderId</summary>
        /// <param name="orderId">The order id</param>
        /// <returns>The line-items</returns>
        public LineItem[] GetLineItemsForOrder(long orderId)
        {
            IList<LineItem> lineItems = new List<LineItem>();
            LineItemPage page;
            int offset = 0;
            while (true)
            {
                var statement = new StatementBuilder(
                    "WHERE orderId = :orderId LIMIT 500 OFFSET {0}"
                        .FormatInvariant(offset))
                    .AddValue("orderId", orderId)
                    .ToStatement();
                page = this.LineItemService.getLineItemsByStatement(statement);
                if (page.results == null || page.results.Length == 0)
                {
                    break;
                }

                lineItems.Add(page.results);
                offset += 500;
            }

            return lineItems.ToArray();
        }

        /// <summary>Creates a bare-bones LineItem in DFP</summary>
        /// <remarks>
        /// Defaults to 'Standard' line-item with CPM cost type.
        /// See https://developers.google.com/doubleclick-publishers/docs/reference/v201206/LineItemService.LineItem for more information.
        /// </remarks>
        /// <param name="orderId">Order id</param>
        /// <param name="name">LineItem name</param>
        /// <param name="externalId">External ID (for application use)</param>
        /// <param name="costPerUnitUsd">Cost-per-unit (USD)</param>
        /// <param name="unitsBought">Units bought (ex: impressions)</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="adUnitIds">AdUnitIds to include in targeting</param>
        /// <param name="includeAdUnitDescendants">Whether to include descendants</param>
        /// <param name="placementIds">Placement ids to target</param>
        /// <param name="locationIds">Location ids to target</param>
        /// <param name="technologyTargeting">Technology targeting</param>
        /// <param name="creatives">Creatives for the line-item</param>
        /// <returns>The created line-item's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long CreateLineItem(
            long orderId,
            string name,
            string externalId,
            decimal costPerUnitUsd,
            long unitsBought,
            System.DateTime startDate,
            System.DateTime endDate,
            string[] adUnitIds,
            bool includeAdUnitDescendants,
            long[] placementIds,
            long[] locationIds,
            TechnologyTargeting technologyTargeting,
            Creative[] creatives)
        {
            return CreateLineItem(
                orderId,
                name,
                externalId,
                LineItemType.STANDARD, // TODO: PRICE_PRIORITY?
                CostType.CPM,
                costPerUnitUsd,
                unitsBought,
                LineItemSummaryDuration.LIFETIME, // TODO: NONE?
                startDate,
                endDate,
                adUnitIds,
                includeAdUnitDescendants,
                placementIds,
                locationIds,
                technologyTargeting,
                creatives);
        }

        /// <summary>Updates a line-item in DFP</summary>
        /// <param name="lineItemId">Line-item id</param>
        /// <param name="name">LineItem name</param>
        /// <param name="costPerUnitUsd">Cost-per-unit (USD)</param>
        /// <param name="unitsBought">Units bought (ex: impressions)</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>The updated line-item's id (TODO: return something more valuable?)</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        public long UpdateLineItem(
            long lineItemId,
            string name,
            decimal costPerUnitUsd,
            long unitsBought,
            System.DateTime startDate,
            System.DateTime endDate)
        {
            var lineItem = this.LineItemService.Invoke(svc =>
                svc.getLineItem(lineItemId));
            lineItem.name = name;
            lineItem.costPerUnit = new Money
            {
                microAmount = (long)Math.Floor(costPerUnitUsd * DollarsToDfpMoneyMicrosMultiplier),
                currencyCode = "USD"
            };
            lineItem.unitsBought = unitsBought;
            lineItem.startDateTime = startDate.ToDfpDateTime();
            lineItem.endDateTime = startDate.ToDfpDateTime();

            lineItem = this.LineItemService.Invoke(
                svc => svc.updateLineItem(lineItem));
            return lineItem.id;
        }
        
        /// <summary>Activates line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items activated</returns>
        public int ActivateLineItems(params long[] lineItemIds)
        {
            if (this.PerformLineItemAction<ReserveLineItems>(lineItemIds) == 0)
            {
                return 0;
            }

            return this.PerformLineItemAction<ActivateLineItems>(lineItemIds);
        }

        /// <summary>Pauses line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items paused</returns>
        public int PauseLineItems(params long[] lineItemIds)
        {
            return this.PerformLineItemAction<PauseLineItems>(lineItemIds);
        }

        /// <summary>Resumes line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items resumed</returns>
        public int ResumeLineItems(params long[] lineItemIds)
        {
            return this.PerformLineItemAction<ResumeLineItems>(lineItemIds);
        }

        /// <summary>Deletes line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items deleted</returns>
        public int DeleteLineItems(params long[] lineItemIds)
        {
            return this.PerformLineItemAction<DeleteLineItems>(lineItemIds);
        }

        /// <summary>Creates an image creative in DFP</summary>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="name">Creative name</param>
        /// <param name="destinationUrl">Destination URL</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="sizeIsAspectRatio">
        /// Whether the width/height is an aspect ratio
        /// </param>
        /// <param name="imageName">Image file name</param>
        /// <param name="imageUrl">Source image URL</param>
        /// <returns>The created creative's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1054", Justification = "DFP API uses string URLs")]
        public long CreateImageCreative(
            long advertiserId,
            string name,
            string destinationUrl,
            int width,
            int height,
            bool sizeIsAspectRatio,
            string imageName,
            string imageUrl)
        {
            var imageBytes = MediaUtilities.GetAssetDataFromUrl(imageUrl);
            return this.CreateImageCreative(
                advertiserId,
                name,
                destinationUrl,
                width,
                height,
                sizeIsAspectRatio,
                imageName,
                imageBytes);
        }

        /// <summary>Creates an image creative in DFP</summary>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="name">Creative name</param>
        /// <param name="destinationUrl">Destination URL</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="sizeIsAspectRatio">
        /// Whether the width/height is an aspect ratio
        /// </param>
        /// <param name="imageName">Image file name</param>
        /// <param name="imageBytes">Image bytes</param>
        /// <returns>The created creative's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1054", Justification = "DFP API uses string URLs")]
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "Only actual image bytes are appropriate")]
        public long CreateImageCreative(
            long advertiserId,
            string name,
            string destinationUrl,
            int width,
            int height,
            bool sizeIsAspectRatio,
            string imageName,
            byte[] imageBytes)
        {
            var creative = (Creative)new ImageCreative
            {
                advertiserId = advertiserId,
                name = name,
                destinationUrl = destinationUrl,
                imageName = imageName,
                imageByteArray = imageBytes,
                size = new Size
                {
                    width = width,
                    height = height,
                    isAspectRatio = sizeIsAspectRatio
                }
            };
            creative = this.CreativeService.Invoke(
                svc => svc.createCreative(creative));
            return creative.id;
        }

        /// <summary>Gets the specified creatives from DFP</summary>
        /// <param name="creativeIds">Creative ids</param>
        /// <returns>The creatives</returns>
        public Creative[] GetCreatives(long[] creativeIds)
        {
            var statement = new StatementBuilder(
                "WHERE id IN ({0}) LIMIT 500".FormatInvariant(string.Join(",", creativeIds)))
                .ToStatement();
            var creativePage = this.CreativeService.Invoke(svc =>
                svc.getCreativesByStatement(statement));
            return creativePage.results;
        }

        /// <summary>Creates a LineItem to Creative association and activates it</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <param name="creativeId">The creative id</param>
        /// <returns>True if successful, otherwise false.</returns>
        public bool AddCreativeToLineItem(
            long lineItemId,
            long creativeId)
        {
            var lica = new LineItemCreativeAssociation
            {
                creativeId = creativeId,
                lineItemId = lineItemId
            };
            lica = this.LicaService.Invoke(svc =>
                svc.createLineItemCreativeAssociation(lica));
            var statement = new StatementBuilder(
                "WHERE lineItemId = :lineItemId and creativeId = creativeId")
                .AddValue("lineItemId", lica.lineItemId)
                .AddValue("creativeId", lica.creativeId)
                .ToStatement();
            var result = this.LicaService.performLineItemCreativeAssociationAction(
                new ActivateLineItemCreativeAssociations(),
                statement);
            return result.numChanges > 0;
        }

        /// <summary>Remove a LineItem to Creative association</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <param name="creativeId">The creative id</param>
        /// <returns>True if successful, otherwise false.</returns>
        public bool RemoveCreativeFromLineItem(
            long lineItemId,
            long creativeId)
        {
            var statement = new StatementBuilder(
                "WHERE lineItemId = :lineItemId and creativeId = creativeId")
                .AddValue("lineItemId", lineItemId)
                .AddValue("creativeId", creativeId)
                .ToStatement();
            var result = this.LicaService.performLineItemCreativeAssociationAction(
                new DeactivateLineItemCreativeAssociations(),
                statement);
            return result.numChanges > 0;
        }

        /// <summary>Gets the Creatives for a LineItem</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <returns>The creatives</returns>
        public Creative[] GetCreativesForLineItem(
            long lineItemId)
        {
            // TODO: Multiple pages?
            var statement = new StatementBuilder(
                "WHERE lineItemId = :lineItemId LIMIT 500")
                .AddValue("lineItemId", lineItemId)
                .ToStatement();
            var page = this.CreativeService.Invoke(svc =>
                svc.getCreativesByStatement(statement));
            return page.results;            
        }

        /// <summary>Requests a delivery report for an Order</summary>
        /// <param name="orderId">The order id</param>
        /// <param name="reportStartDate">Start date for the report period</param>
        /// <param name="reportEndDate">End date for the report period</param>
        /// <returns>The report job id</returns>
        public long RequestDeliveryReport(
            long orderId,
            System.DateTime reportStartDate,
            System.DateTime reportEndDate)
        {
            var reportJob = new ReportJob
            {
                reportQuery = new ReportQuery
                {
                    statement = new StatementBuilder(
                        "WHERE ORDER_ID = :orderId")
                        .AddValue("orderId", orderId)
                        .ToStatement(),
                    startDate = reportStartDate.ToDfpDateTime().date,
                    endDate = reportEndDate.ToDfpDateTime().date,
                    dimensions = new[]
                    {
                        Dimension.LINE_ITEM,
                        Dimension.DATE,
                        Dimension.HOUR
                    },
                    columns = new[]
                    {
                        Column.AD_SERVER_IMPRESSIONS,
                        Column.AD_SERVER_AVERAGE_ECPM,
                        Column.AD_SERVER_CPM_AND_CPC_REVENUE,
                        Column.AD_SERVER_CLICKS,
                    }
                }
            };
            reportJob = this.ReportService.Invoke(svc =>
                svc.runReportJob(reportJob));
            return reportJob.id;
        }

        /// <summary>Checks the status of a report job</summary>
        /// <param name="reportJobId">The report job id</param>
        /// <returns>The report status</returns>
        public ReportJobStatus CheckReportStatus(
            long reportJobId)
        {
            var reportJob = this.ReportService.Invoke(svc =>
                svc.getReportJob(reportJobId));
            return reportJob.reportJobStatus;
        }

        /// <summary>Downloads the results of a report job</summary>
        /// <param name="reportJobId">The report job id</param>
        /// <returns>The report results as comma-separated values</returns>
        public string RetrieveReport(
            long reportJobId)
        {
            var downloadUrl = this.ReportService.Invoke(svc =>
                svc.getReportDownloadURL(reportJobId, ExportFormat.CSV_DUMP));
            var gzipReport = MediaUtilities.GetAssetDataFromUrl(downloadUrl);
            var reportBytes = MediaUtilities.DeflateGZipData(gzipReport);
            var reportCsv = Encoding.UTF8.GetString(reportBytes);
            return reportCsv;
        }

        /// <summary>Gets the specified AdUnits</summary>
        /// <param name="adUnitIds">The AdUnitIds</param>
        /// <returns>The AdUnits</returns>
        public AdUnit[] GetAdUnits(
            string[] adUnitIds)
        {
            var adUnits = new List<AdUnit>();
            AdUnitPage page;
            var offset = 0;
            do
            {
                var statement = new StatementBuilder(
                    "WHERE id IN ({0}) LIMIT 500 OFFSET {1}"
                        .FormatInvariant(string.Join(",", adUnitIds), offset))
                    .ToStatement();
                page = this.InventoryService.Invoke(svc =>
                    svc.getAdUnitsByStatement(statement));
                adUnits.Add(page.results);
                offset += page.results.Length;
            }
            while (offset < page.totalResultSetSize);
            return adUnits.ToArray();
        }

        /// <summary>Gets all AdUnits for the network</summary>
        /// <returns>The AdUnits</returns>
        public AdUnit[] GetAllAdUnits()
        {
            var adUnits = new List<AdUnit>();
            AdUnitPage page;
            var offset = 0;
            do
            {
                var statement = new StatementBuilder(
                    "WHERE status = :status LIMIT 500 OFFSET {0}"
                        .FormatInvariant(offset))
                    .AddValue("status", "ACTIVE")
                    .ToStatement();
                page = this.InventoryService.Invoke(svc =>
                    svc.getAdUnitsByStatement(statement));
                adUnits.Add(page.results);
                offset += page.results.Length;
            }
            while (offset < page.totalResultSetSize);
            return adUnits.ToArray();
        }

        /// <summary>Gets the specified placements</summary>
        /// <param name="placementIds">The placement ids</param>
        /// <returns>The placements</returns>
        public Placement[] GetPlacements(
            long[] placementIds)
        {
            var placements = new List<Placement>();
            PlacementPage page;
            var offset = 0;
            do
            {
                var statement = new StatementBuilder(
                    "WHERE id IN ({0}) LIMIT 500 OFFSET {1}"
                        .FormatInvariant(string.Join(",", placementIds), offset))
                    .ToStatement();
                page = this.PlacementService.Invoke(svc =>
                    svc.getPlacementsByStatement(statement));
                placements.Add(page.results);
                offset += page.results.Length;
            }
            while (offset < page.totalResultSetSize);
            return placements.ToArray();
        }

        /// <summary>Gets the specified placements</summary>
        /// <returns>The placements</returns>
        public Placement[] GetAllPlacements()
        {
            var placements = new List<Placement>();
            PlacementPage page;
            var offset = 0;
            do
            {
                var statement = new StatementBuilder(
                    "WHERE status = :status LIMIT 500 OFFSET {0}"
                        .FormatInvariant(offset))
                    .AddValue("status", "ACTIVE")
                    .ToStatement();
                page = this.PlacementService.Invoke(svc =>
                    svc.getPlacementsByStatement(statement));
                placements.Add(page.results);
                offset += page.results.Length;
            }
            while (offset < page.totalResultSetSize);
            return placements.ToArray();
        }

        /// <summary>Runs a PQL select query</summary>
        /// <param name="selectStatement">The query</param>
        /// <returns>The result values</returns>
        public IEnumerable<IDictionary<string, object>> SelectQuery(
            Statement selectStatement)
        {
            var results = this.QueryService.Invoke(svc =>
                svc.select(selectStatement));
            var values = results.rows
                .Select(row =>
                    results.columnTypes
                    .Zip(row.values)
                    .ToDictionary(
                        value => value.Item1.labelName,
                        value => this.DfpValueToObject(value.Item2)));
            return values;
        }

        /// <summary>Creates a bare-bones LineItem in DFP</summary>
        /// <param name="orderId">Order id</param>
        /// <param name="name">LineItem name</param>
        /// <param name="externalId">External ID (for application use)</param>
        /// <param name="lineItemType">LineItem type</param>
        /// <param name="costType">Cost type</param>
        /// <param name="costPerUnitUsd">Cost-per-unit (USD)</param>
        /// <param name="unitsBought">Units bought (ex: impressions)</param>
        /// <param name="duration">
        /// Period over which the goal or cap should be reached. Limited by lineItemType.
        /// More information: https://developers.google.com/doubleclick-publishers/docs/reference/v201206/LineItemService.LineItem#duration
        /// </param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="adUnitIds">AdUnitIds to include in targeting</param>
        /// <param name="includeAdUnitDescendants">Whether to include descendants</param>
        /// <param name="placementIds">Placement ids to target</param>
        /// <param name="locationIds">Location ids to target</param>
        /// <param name="technologyTargeting">Technology targeting</param>
        /// <param name="creatives">Creatives for the line-item</param>
        /// <returns>The created line-item's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        internal long CreateLineItem(
            long orderId,
            string name,
            string externalId,
            LineItemType lineItemType,
            CostType costType,
            decimal costPerUnitUsd,
            long unitsBought,
            LineItemSummaryDuration duration,
            System.DateTime startDate,
            System.DateTime endDate,
            string[] adUnitIds,
            bool includeAdUnitDescendants,
            long[] placementIds,
            long[] locationIds,
            TechnologyTargeting technologyTargeting,
            Creative[] creatives)
        {
            var costPerUnit = new Money
            {
                microAmount = (long)Math.Floor(costPerUnitUsd * DollarsToDfpMoneyMicrosMultiplier),
                currencyCode = "USD"
            };

            var creativePlaceholders = creatives
                .Select(c => new CreativePlaceholder
                {
                    size = c.size
                })
                .ToArray();

            var targeting = new Targeting
            {
                inventoryTargeting = CreateInventoryTargeting(
                    adUnitIds,
                    includeAdUnitDescendants,
                    placementIds),
                geoTargeting = new GeoTargeting
                {
                    targetedLocations = locationIds
                        .Select(locationId => new Location { id = locationId })
                        .ToArray()
                },
                technologyTargeting = technologyTargeting
            };

            var targetPlatform =
                technologyTargeting.ContainsMobileTargets() ?
                TargetPlatform.MOBILE : TargetPlatform.WEB;

            var lineItem = new LineItem
            {
                targetPlatform = targetPlatform,
                orderId = orderId,
                name = name,
                externalId = externalId,
                lineItemType = lineItemType,
                costPerUnit = costPerUnit,
                costType = costType,
                unitsBought = unitsBought,
                duration = duration,
                startDateTime = startDate.ToDfpDateTime(),
                endDateTime = endDate.ToDfpDateTime(),
                creativePlaceholders = creativePlaceholders,
                targeting = targeting,
            };

            lineItem = this.LineItemService.Invoke(
                svc => svc.createLineItem(lineItem));

            return lineItem.id;
        }

        /// <summary>Creates an InventoryTargeting from AdUnitIds and PlacementIds</summary>
        /// <param name="adUnitIds">AdUnitIds to include in targeting</param>
        /// <param name="includeAdUnitDescendants">Whether to include descendants</param>
        /// <param name="placementIds">PlacementIds to target</param>
        /// <returns>The created InventoryTargeting</returns>
        private static InventoryTargeting CreateInventoryTargeting(
            string[] adUnitIds,
            bool includeAdUnitDescendants,
            long[] placementIds)
        {
            return new InventoryTargeting
            {
                targetedAdUnits = adUnitIds
                    .Select(id => new AdUnitTargeting
                    {
                        adUnitId = id,
                        includeDescendants = includeAdUnitDescendants
                    })
                    .ToArray(),
                targetedPlacementIds = placementIds
            };
        }

        /// <summary>Creates a Company of the specified type in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <param name="companyType">The company type</param>
        /// <returns>The created company's id</returns>
        /// <exception cref="GoogleDfpClientException">
        /// Exception wrapping any DFP errors that occur.
        /// </exception>
        private long CreateCompany(
            string companyName,
            string externalId,
            CompanyType companyType)
        {
            var company = new Company
            {
                name = companyName.Left(MaxCompanyNameLength),
                externalId = externalId.Left(MaxCompanyExternalIdLength),
                type = companyType
            };
            company = this.CompanyService.Invoke(
                svc => svc.createCompany(company));
            return company.id;
        }

        /// <summary>Performs an action on an order in DFP</summary>
        /// <typeparam name="TAction">Action to perform</typeparam>
        /// <param name="orderId">Order id</param>
        /// <returns>True if the action was performed</returns>
        private bool PerformOrderAction<TAction>(
            long orderId)
            where TAction : OrderAction, new()
        {
            var statement = new StatementBuilder(
                "WHERE id = :orderId LIMIT 500")
                .AddValue("orderId", orderId)
                .ToStatement();
            var result = this.OrderService.Invoke(
                svc => svc.performOrderAction(new TAction(), statement));
            return result.numChanges > 0;
        }

        /// <summary>Performs an action on line-items in DFP</summary>
        /// <typeparam name="TAction">Action to perform</typeparam>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Number of line-items changed</returns>
        private int PerformLineItemAction<TAction>(
            long[] lineItemIds)
            where TAction : LineItemAction, new()
        {
            var statement = new StatementBuilder(
                "WHERE id IN ({0})".FormatInvariant(string.Join(",", lineItemIds)))
                .ToStatement();
            var result = this.LineItemService.Invoke(
                svc => svc.performLineItemAction(new TAction(), statement));
            return result.numChanges;
        }

        /// <summary>Converts a DFP value to a .Net object</summary>
        /// <param name="value">DFP value</param>
        /// <returns>.Net object</returns>
        private object DfpValueToObject(Value value)
        {
            switch (value.ValueType)
            {
                case "TextValue":
                    return ((TextValue)value).value;
                case "NumberValue":
                    return Convert.ToInt64(((NumberValue)value).value, CultureInfo.InvariantCulture);
                case "DateValue":
                    return ((DateTimeValue)value).value.ToSystemDateTime(this.NetworkTimezone);
                case "BooleanValue":
                    return ((BooleanValue)value).value;
                default:
                    throw new ArgumentException(
                        "Unsupported value type: {0}"
                        .FormatInvariant(value.ValueType),
                        "value");
            }
        }
    }
}
