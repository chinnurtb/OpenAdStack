//-----------------------------------------------------------------------
// <copyright file="IGoogleDfpClient.cs" company="Rare Crowds Inc">
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
using DeliveryNetworkUtilities;
using Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpClient
{
    /// <summary>
    /// Interface for a client used to interract with the Google DFP service
    /// without the requiring use of DFP API specific types.
    /// </summary>
    public interface IGoogleDfpClient : IDeliveryNetworkClient
    {
        /// <summary>Creates an agency company in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        long CreateAgency(
            string companyName,
            string externalId);

        /// <summary>Creates a house agency company in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        long CreateHouseAgency(
            string companyName,
            string externalId);

        /// <summary>Creates an advertiser in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        long CreateAdvertiser(
            string companyName,
            string externalId);

        /// <summary>Creates a house advertiser in DFP</summary>
        /// <param name="companyName">The company name</param>
        /// <param name="externalId">The company external id</param>
        /// <returns>The created company's id</returns>
        long CreateHouseAdvertiser(
            string companyName,
            string externalId);

        /// <summary>Gets a Company</summary>
        /// <param name="companyId">The company id</param>
        /// <returns>The company</returns>
        Company GetCompany(
            long companyId);

        /// <summary>Creates an bare-bones order in DFP</summary>
        /// <param name="advertiserId">The DFP advertiser id</param>
        /// <param name="orderName">The order name</param>
        /// <param name="startDate">The order start DateTime</param>
        /// <param name="endDate">The order end DateTime</param>
        /// <returns>The created order's id</returns>
        long CreateOrder(
            long advertiserId,
            string orderName,
            System.DateTime startDate,
            System.DateTime endDate);

        /// <summary>Updates an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <param name="startDate">The order start DateTime</param>
        /// <param name="endDate">The order end DateTime</param>
        void UpdateOrder(
            long orderId,
            System.DateTime startDate,
            System.DateTime endDate);

        /// <summary>Gets an order as a dictionary of key-value pairs</summary>
        /// <param name="orderId">The order id</param>
        /// <returns>The order values</returns>
        Order GetOrder(
            long orderId);

        /// <summary>Approves an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <returns>True if the order was approved</returns>
        bool ApproveOrder(
            long orderId);

        /// <summary>Deletes an order in DFP</summary>
        /// <param name="orderId">The DFP order id</param>
        /// <returns>True if the order was deleted</returns>
        bool DeleteOrder(
            long orderId);

        /// <summary>Gets the line-items for the specified orderId</summary>
        /// <param name="orderId">The order id</param>
        /// <returns>The line-items</returns>
        LineItem[] GetLineItemsForOrder(
            long orderId);

        /// <summary>Creates a bare-bones LineItem in DFP</summary>
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
        long CreateLineItem(
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
            Creative[] creatives);
        
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
        long UpdateLineItem(
            long lineItemId,
            string name,
            decimal costPerUnitUsd,
            long unitsBought,
            System.DateTime startDate,
            System.DateTime endDate);

        /// <summary>Activates line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items activated</returns>
        int ActivateLineItems(
            params long[] lineItemIds);

        /// <summary>Pauses line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items paused</returns>
        int PauseLineItems(
            params long[] lineItemIds);

        /// <summary>Resumes line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items resumed</returns>
        int ResumeLineItems(
            params long[] lineItemIds);

        /// <summary>Deletes line-items in DFP</summary>
        /// <param name="lineItemIds">Line-item ids</param>
        /// <returns>Count of line-items deleted</returns>
        int DeleteLineItems(
            params long[] lineItemIds);

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
        long CreateImageCreative(
            long advertiserId,
            string name,
            string destinationUrl,
            int width,
            int height,
            bool sizeIsAspectRatio,
            string imageName,
            string imageUrl);

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
        long CreateImageCreative(
            long advertiserId,
            string name,
            string destinationUrl,
            int width,
            int height,
            bool sizeIsAspectRatio,
            string imageName,
            byte[] imageBytes);
        
        /// <summary>Gets the specified creatives from DFP</summary>
        /// <param name="creativeIds">Creative ids</param>
        /// <returns>The creatives</returns>
        Creative[] GetCreatives(
            long[] creativeIds);

        /// <summary>Creates a LineItem to Creative association and activates it</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <param name="creativeId">The creative id</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool AddCreativeToLineItem(
            long lineItemId,
            long creativeId);

        /// <summary>Remove a LineItem to Creative association</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <param name="creativeId">The creative id</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool RemoveCreativeFromLineItem(
            long lineItemId,
            long creativeId);

        /// <summary>Gets the Creatives for a LineItem</summary>
        /// <param name="lineItemId">The line-item id</param>
        /// <returns>The creatives</returns>
        Creative[] GetCreativesForLineItem(
            long lineItemId);

        /// <summary>Requests a delivery report for an Order</summary>
        /// <param name="orderId">The order id</param>
        /// <param name="reportStartDate">Start date for the report period</param>
        /// <param name="reportEndDate">End date for the report period</param>
        /// <returns>The report job id</returns>
        long RequestDeliveryReport(
            long orderId,
            System.DateTime reportStartDate,
            System.DateTime reportEndDate);

        /// <summary>Checks the status of a report job</summary>
        /// <param name="reportJobId">The report job id</param>
        /// <returns>The report status</returns>
        ReportJobStatus CheckReportStatus(
            long reportJobId);

        /// <summary>Downloads the results of a report job</summary>
        /// <param name="reportJobId">The report job id</param>
        /// <returns>The report results as comma-separated values</returns>
        string RetrieveReport(
            long reportJobId);
        
        /// <summary>Gets the specified AdUnits</summary>
        /// <param name="adUnitIds">The AdUnitIds</param>
        /// <returns>The AdUnits</returns>
        AdUnit[] GetAdUnits(
            string[] adUnitIds);
        
        /// <summary>Gets all AdUnits for the network</summary>
        /// <returns>The AdUnits</returns>
        AdUnit[] GetAllAdUnits();

        /// <summary>Gets the specified placements</summary>
        /// <param name="placementIds">The placement ids</param>
        /// <returns>The placements</returns>
        Placement[] GetPlacements(
            long[] placementIds);

        /// <summary>Gets all placements for the network</summary>
        /// <returns>The placements</returns>
        Placement[] GetAllPlacements();

        /// <summary>Runs a PQL select query</summary>
        /// <param name="selectStatement">The query</param>
        /// <returns>The result values</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Nested generic is appropriate here")]
        IEnumerable<IDictionary<string, object>> SelectQuery(
            Statement selectStatement);
    }
}
