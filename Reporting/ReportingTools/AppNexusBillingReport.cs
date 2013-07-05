// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusBillingReport.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationActivities;
using ReportingUtilities;
using Utilities;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;
using dataName = DynamicAllocationActivities.RawDeliveryDataParserBase;

namespace ReportingTools
{
    /// <summary>Billing report for APNX campaign data.</summary>
    public class AppNexusBillingReport : IReportGenerator
    {
        /// <summary>MeasuresInputs backing field.</summary>
        private MeasureSetsInput measuresInputs;

        /// <summary>MeasureSet valuations backing field.</summary>
        private IDictionary<MeasureSet, decimal> valuations;

        /// <summary>DataProviders backing field.</summary>
        private IDictionary<long, string> dataProviders;

        /// <summary>MeasureNames backing field.</summary>
        private Dictionary<long, string> measureNames;

        /// <summary>MeasureGroupings backing field.</summary>
        private Dictionary<long, string> measureGroupings;

        /// <summary>NodeMap backing field.</summary>
        private Dictionary<string, MeasureSet> nodeMap;

        /// <summary>MeasureInfo backing field.</summary>
        private MeasureInfo measureInfo;

        /// <summary>MeasureMap backing field.</summary>
        private MeasureMap measureMap;

        /// <summary>Initializes a new instance of the <see cref="AppNexusBillingReport"/> class.</summary>
        /// <param name="entityRepository">The entity repository.</param>
        /// <param name="dynamicAllocationCampaign">A dynamic allocation campaign wrapper.</param>
        public AppNexusBillingReport(IEntityRepository entityRepository, IDynamicAllocationCampaign dynamicAllocationCampaign)
        {
            this.Repository = entityRepository;
            this.CompanyEntity = dynamicAllocationCampaign.CompanyEntity;
            this.CampaignEntity = dynamicAllocationCampaign.CampaignEntity;
            this.AllocationParameters = dynamicAllocationCampaign.AllocationParameters;
            this.Dac = dynamicAllocationCampaign;
        }
        
        /// <summary>Gets or sets LatestDeliveryData.</summary>
        public DateTime LatestDeliveryData { get; set; }

        /// <summary>Gets or sets DynamicAllocationCampaign.</summary>
        private IDynamicAllocationCampaign Dac { get; set; }

        /// <summary>Gets MeasuresInputs</summary>
        private MeasureSetsInput MeasuresInputs
        {
            get
            {
                return this.measuresInputs ?? (this.measuresInputs = this.GetMeasuresInputs());
            }
        }

        /// <summary>Gets Valuations.</summary>
        private IDictionary<MeasureSet, decimal> Valuations
        {
            get
            {
                return this.valuations ?? (this.valuations = this.GetValuations());
            }
        }

        /// <summary>Gets DataProviders.</summary>
        private IDictionary<long, string> DataProviders
        {
            get
            {
                return this.dataProviders 
                    ?? (this.dataProviders = GetDataProviders(this.MeasureInfo, this.MeasuresInputs));
            }
        }

        /// <summary>Gets MeasureNames.</summary>
        private Dictionary<long, string> MeasureNames
        {
            get
            {
                return this.measureNames
                    ?? (this.measureNames = GetMeasureNames(this.MeasuresInputs, this.MeasureMap));
            }
        }

        /// <summary>Gets MeasureGroupings.</summary>
        private Dictionary<long, string> MeasureGroupings
        {
            get
            {
                return this.measureGroupings 
                    ?? (this.measureGroupings = GetMeasureGroupings(this.MeasuresInputs));
            }
        }
        
        /// <summary>Gets or sets AllocationParameters.</summary>
        private AllocationParameters AllocationParameters { get; set; }

        /// <summary>Gets NodeMap.</summary>
        private Dictionary<string, MeasureSet> NodeMap
        {
            get
            {
                return this.nodeMap 
                    ?? (this.nodeMap = this.Dac.RetrieveAllocationNodeMap());
            }
        }

        /// <summary>Gets MeasureInfo.</summary>
        private MeasureInfo MeasureInfo 
        { 
            get
            {
                return this.measureInfo ?? (this.measureInfo = new MeasureInfo(this.MeasureMap));
            }
        }

        /// <summary>Gets MeasureMap.</summary>
        private MeasureMap MeasureMap
        {
            get
            {
                return this.measureMap 
                    ?? (this.measureMap = this.Dac.RetrieveMeasureMap());
            }
        }

        /// <summary>Gets or sets Repository.</summary>
        private IEntityRepository Repository { get; set; }

        /// <summary>Gets or sets CampaignEntity.</summary>
        private CampaignEntity CampaignEntity { get; set; }

        /// <summary>Gets or sets CompanyEntity.</summary>
        private CompanyEntity CompanyEntity { get; set; }

        /// <summary>Build an report of the specified type.</summary>
        /// <param name="reportType">A ReportTypes string constant.</param>
        /// <param name="verbose">True for a verbose report.</param>
        /// <returns>A StringBuilder with the report.</returns>
        public StringBuilder BuildReport(string reportType, bool verbose)
        {
            if (reportType == ReportTypes.ClientCampaignBilling)
            {
                return this.BuildCampaignReport(verbose);
            }

            if (reportType == ReportTypes.DataProviderBilling)
            {
                return this.BuildDataProviderReport(verbose);
            }

            throw new AppsGenericException("Report type {0} not supported by AppNexusBillingReport."
                .FormatInvariant(reportType));
        }

        /// <summary>Build report for a single DA campaign</summary>
        /// <param name="verbose">True to output details.</param>
        /// <returns>A StringBuilder containing the campaign report.</returns>
        internal StringBuilder BuildCampaignReport(bool verbose)
        {
            // Build header row
            var headerList = new List<string>
                {
                    "DA Campaign Name",
                    "Network",
                    "Campaign Id",
                    "Hour",
                    "Allocation Id",
                    "Data Provider Names",
                    "Valuation",
                    "Impressions",
                    "Ecpm",
                    "Media Spend",
                    "Clicks",
                    "Ctr",
                    "Billable Data Cost",
                    "Effective Rate",
                    "Serving Cost Rate",
                    "Serving Cost",
                    "Profit",
                    "Total Spend",
                    "Number Of Segments"                    
                };

            this.AddGroupHeaders(ref headerList, verbose);
            this.AddDataProviderCostHeaders(ref headerList, false);
            return this.ProcessReport(headerList, this.BuildGenericReportRow);
        }

        /// <summary>Build report for a single DA campaign with information per data provider.</summary>
        /// <param name="verbose">True to output details.</param>
        /// <returns>A StringBuilder containing the data provider report.</returns>
        internal StringBuilder BuildDataProviderReport(bool verbose)
        {
            // Build header row
            var headerList = new List<string>
                {
                    "Advertiser Name",
                    "Network",
                    "Campaign Id",
                    "Campaign Name",
                    "Hour",
                    "Data Provider Names",
                    "Valuation",
                    "Media Spend",
                    "Impressions",
                    "Billable Data Cost",
                    "Effective Rate",
                    "Number Of Segments"                    
                };

            this.AddDataProviderCostHeaders(ref headerList, true);
            this.AddMeasureHeaders(ref headerList, verbose);
            return this.ProcessReport(headerList, this.BuildGenericReportRow);
        }

        /// <summary>Build report for a single DA campaign.</summary>
        /// <param name="includeDataProviders">True to include Data Providers.</param>
        /// <param name="verboseDataProviders">True for verbose Data Providers.</param>
        /// <param name="includeGroups">True to include Groups.</param>
        /// <param name="verboseGroups">True for verbose Groups.</param>
        /// <param name="includeMeasures">True to include Measures.</param>
        /// <param name="verboseMeasures">True for verbose Measures.</param>
        /// <returns>A StringBuilder containing the report.</returns>
        internal StringBuilder BuildGenericReport(
            bool includeDataProviders,
            bool verboseDataProviders,
            bool includeGroups,
            bool verboseGroups,
            bool includeMeasures, 
            bool verboseMeasures)
        {
            // Build header row
            var headerList = new List<string>
                {
                    "DA Campaign Name",
                    "Advertiser Name",
                    "Network",
                    "Campaign Id",
                    "Campaign Name",
                    "Hour",
                    "Allocation Id",
                    "Data Provider Names",
                    "Valuation",
                    "Media Spend",
                    "Impressions",
                    "Ecpm",
                    "Clicks",
                    "Ctr", 
                    "Billable Data Cost",
                    "Effective Rate",
                    "Serving Cost Rate",
                    "Serving Cost",
                    "Profit",
                    "Total Spend",
                    "Number Of Segments"                    
                };

            if (includeDataProviders)
            {
                this.AddDataProviderCostHeaders(ref headerList, verboseDataProviders);
            }

            if (includeGroups)
            {
                this.AddGroupHeaders(ref headerList, verboseGroups);
            }

            if (includeMeasures)
            {
                this.AddMeasureHeaders(ref headerList, verboseMeasures);
            }

            return this.ProcessReport(headerList, this.BuildGenericReportRow);
        }
        
        /// <summary>Build a map of row metrics calculated from the raw delivery row.</summary>
        /// <param name="network">The network.</param>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>The map of output values.</returns>
        internal Dictionary<string, string> BuildRowMetrics(
            DeliveryNetworkDesignation network,
            MeasureSet measureSet,
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            // Build the delivery metrics
            var deliveryMetrics = this.BuildDeliveryMetrics(network, measureSet, rawDeliveryRow);

            // Build the data costs and rates across all data providers (may be zero)
            var dataProviderMetrics = this.BuildDataProviderMetrics(measureSet, rawDeliveryRow);

            // Build the stats for the measures in the measureSet
            var measureMetrics = this.BuildMeasureMetrics(measureSet, rawDeliveryRow);

            // Build the stats for the groups in the measureSet
            var groupsMetrics = this.BuildGroupMetrics(measureSet, rawDeliveryRow);

            var rowMetrics = new List<KeyValuePair<string, string>>();
            rowMetrics.AddRange(deliveryMetrics);
            rowMetrics.AddRange(dataProviderMetrics);
            rowMetrics.AddRange(measureMetrics);
            rowMetrics.AddRange(groupsMetrics);

            return rowMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Process each row of raw delivery data on the campaign with the given row handler.
        /// Update LatestDeliveryData time as we go.
        /// </summary>
        /// <param name="rowOutputHandler">The row output handler.</param>
        internal void ProcessRawDeliveryData(Action<DeliveryNetworkDesignation, Dictionary<string, PropertyValue>> rowOutputHandler)
        {
            // Get the raw delivery data out of the blob
            var rawDeliveryDataIndexes = this.Dac
                .RawDeliveryData.RetrieveRawDeliveryDataIndexItems();

            this.LatestDeliveryData = DateTime.MinValue;

            if (rawDeliveryDataIndexes == null)
            {
                return;
            }

            var dedupedRows =
                new Dictionary<string, Tuple<DateTime, DeliveryNetworkDesignation, Dictionary<string, PropertyValue>>>();

            foreach (var rawDeliveryDataIndexEntry in rawDeliveryDataIndexes)
            {
                // Get the network
                var network = rawDeliveryDataIndexEntry.DeliveryNetwork;

                // Get the index.
                var rawDeliveryDataIndex = rawDeliveryDataIndexEntry.RawDeliveryDataEntityIds;

                foreach (var rawDataEntityId in rawDeliveryDataIndex)
                {
                    var result = this.Dac
                        .RawDeliveryData.RetrieveRawDeliveryDataItem(rawDataEntityId);
                    var rawDeliveryData = result.RawDeliveryData;
                    var deliveryReportDate = result.DeliveryDataReportDate;

                    // No partial success.
                    if (rawDeliveryData == null)
                    {
                        return;
                    }

                    var canonicalDeliveryData = new CanonicalDeliveryData(network);
                    var parser = GetCampaignDeliveryDataActivity.NetworkRawDeliveryDataParserMap[network];
                    if (!canonicalDeliveryData.AddRawData(rawDeliveryData, deliveryReportDate, parser))
                    {
                        return;
                    }

                    var deliveryData = canonicalDeliveryData.DeliveryDataForNetwork;

                    // Dedupe raw data
                    foreach (var rawDeliveryRow in deliveryData)
                    {
                        var key = rawDeliveryRow[dataName.HourFieldName].SerializationValue + rawDeliveryRow[dataName.CampaignIdFieldName].SerializationValue;
                        if (dedupedRows.ContainsKey(key) && dedupedRows[key].Item1 > deliveryReportDate)
                        {
                            continue;
                        }

                        dedupedRows[key] = new Tuple<DateTime, DeliveryNetworkDesignation, Dictionary<string, PropertyValue>>(
                            deliveryReportDate, network, rawDeliveryRow);
                    }
                }
            }

            foreach (var dedupedRow in dedupedRows)
            {
                var network = dedupedRow.Value.Item2;
                var rawDeliveryRow = dedupedRow.Value.Item3;

                // Update latest delivery hour
                var hour = (DateTime)rawDeliveryRow[dataName.HourFieldName];
                this.LatestDeliveryData = hour > this.LatestDeliveryData ? hour : this.LatestDeliveryData;

                // Call the row output handler
                rowOutputHandler(network, rawDeliveryRow);
            }
        }

        /// <summary>Get measure names.</summary>
        /// <param name="measuresInputs">The measures inputs.</param>
        /// <param name="measureMap">The measure map.</param>
        /// <returns>The measure Id to measure name dictionary.</returns>
        private static Dictionary<long, string> GetMeasureNames(MeasureSetsInput measuresInputs, MeasureMap measureMap)
        {
            var distinctMeasures = measuresInputs.Measures.Select(m => m.Measure).Distinct();
            var measureNames = distinctMeasures.ToDictionary(m => m, measureMap.GetDisplayNameForMeasure);
            return measureNames;
        }

        /// <summary>Get the measure groupings from measure inputs.</summary>
        /// <param name="measureInputs">The measures inputs.</param>
        /// <returns>The measure groupings.</returns>
        private static Dictionary<long, string> GetMeasureGroupings(MeasureSetsInput measureInputs)
        {
            return measureInputs.Measures.ToDictionary(
                n => n.Measure,
                n => string.IsNullOrEmpty(n.Group) ? n.Measure.ToString(CultureInfo.InvariantCulture) : n.Group);
        }

        /// <summary>Get the data providers for the measures used.</summary>
        /// <param name="measureInfo">The measure Info.</param>
        /// <param name="measureSetsInput">The measure Sets Input.</param>
        /// <returns>The data providers mapped to measure id.</returns>
        private static IDictionary<long, string> GetDataProviders(MeasureInfo measureInfo, MeasureSetsInput measureSetsInput)
        {
            var dataProviders =
                measureInfo.ExtractDataProviders(new MeasureSet(measureSetsInput.Measures.Select(m => m.Measure)));
            return dataProviders;
        }
        
        /// <summary>Build outputs for a data provider report.</summary>
        /// <param name="headerList">The header list.</param>
        /// <param name="rowMetrics">The calculated row values.</param>
        /// <returns>The mapped output row.</returns>
        private static string BuildOutputForRow(
            List<string> headerList,
            IDictionary<string, string> rowMetrics)
        {
            var outputs = headerList.ToDictionary(header => header, header => rowMetrics[header]);
            return string.Join(",", headerList.Select(h => outputs.ContainsKey(h) ? outputs[h] : string.Empty));
        }

        /// <summary>Format a number string for report output.</summary>
        /// <typeparam name="T">Numeric type</typeparam>
        /// <param name="number">The number.</param>
        /// <returns>Rounded, string representation of number.</returns>
        private static string FormatNum<T>(T number)
        {
            if (number is decimal)
            {
                return Math.Round(Convert.ToDecimal(number, CultureInfo.InvariantCulture), 6).ToString(CultureInfo.InvariantCulture);
            }

            if (number is double)
            {
                return Math.Round(Convert.ToDouble(number, CultureInfo.InvariantCulture), 6).ToString(CultureInfo.InvariantCulture);
            }

            if (number is int || number is long)
            {
                return Convert.ToInt64(number, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        /// <summary>Get the measures inputs for the campaign.</summary>
        /// <returns>The MeasureSetsInput</returns>
        private MeasureSetsInput GetMeasuresInputs()
        {
            return ValuationsCache.BuildValuationInputs(this.CampaignEntity).MeasureSetsInput;
        }

        /// <summary>Get the measureSet valuations for the campaign.</summary>
        /// <returns>The Valuations.</returns>
        private IDictionary<MeasureSet, decimal> GetValuations()
        {
            var cache = new ValuationsCache(this.Repository);
            return cache.GetValuations(this.Dac, true);
        }

        /// <summary>Add the measure headers to the header list.</summary>
        /// <param name="headerList">The header list.</param>
        /// <param name="outputDetail">True to output detail measure info.</param>
        private void AddMeasureHeaders(ref List<string> headerList, bool outputDetail)
        {
            foreach (var measureId in this.MeasureGroupings.Keys)
            {
                var measureIdOut = FormatNum(measureId);
                var columnPrefix = "{0}:{1}".FormatInvariant("Id", measureIdOut);
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Measure Name"));

                if (!outputDetail)
                {
                    continue;
                }

                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Group"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Cost"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "MinCpm"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Percent of Spend"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Effective Cost"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Provider"));
            }
        }

        /// <summary>Add the measure headers to the header list.</summary>
        /// <param name="headerList">The header list.</param>
        /// <param name="verbose">True to output detail group info.</param>
        private void AddGroupHeaders(ref List<string> headerList, bool verbose)
        {
            foreach (var groupName in this.MeasureGroupings.Values.Distinct())
            {
                var columnPrefix = "{0}:{1}".FormatInvariant("Group", groupName);
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Measure Name"));

                if (!verbose)
                {
                    continue;
                }

                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "MeasureId"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Cost"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "MinCpm"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Percent of Spend"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Effective Cost"));
                headerList.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Provider"));
            }
        }

        /// <summary>Add the per data provider cost headers to the header list.</summary>
        /// <param name="headerList">The header list.</param>
        /// <param name="verbose">True to output detail measure info.</param>
        private void AddDataProviderCostHeaders(ref List<string> headerList, bool verbose)
        {
            foreach (var dataProvider in this.DataProviders.Values.Distinct())
            {
                headerList.Add("{0}:{1}".FormatInvariant(dataProvider, "Effective Data Cost"));

                if (!verbose)
                {
                    continue;
                }

                headerList.Add("{0}:{1}".FormatInvariant(dataProvider, "Impression Based Rate"));
                headerList.Add("{0}:{1}".FormatInvariant(dataProvider, "Impression Based Data Cost"));
                headerList.Add("{0}:{1}".FormatInvariant(dataProvider, "Percent of Media Spend Rate"));
                headerList.Add("{0}:{1}".FormatInvariant(dataProvider, "Percent of Media Spend Data Cost"));
            }
        }

        /// <summary>Process a report outputting the values for each row of raw data mapping to the headers supplied.</summary>
        /// <param name="headerList">The header list.</param>
        /// <param name="rowHandler">The row handler.</param>
        /// <returns>Report as StringBuilder.</returns>
        private StringBuilder ProcessReport(
            List<string> headerList, 
            Func<DeliveryNetworkDesignation, List<string>, Dictionary<string, PropertyValue>, string> rowHandler)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headerList));
            
            this.ProcessRawDeliveryData(
                    (network, rawDeliveryRow) => sb.AppendLine(rowHandler(network, headerList, rawDeliveryRow)));

            return sb;
        }

        /// <summary>Build the most generic report row.</summary>
        /// <param name="network">Delivery network</param>
        /// <param name="headerList">The header list.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>The output row string.</returns>
        private string BuildGenericReportRow(
            DeliveryNetworkDesignation network, 
            List<string> headerList, 
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            // Get the measure set
            var measureSet = this.NodeMap[rawDeliveryRow[dataName.AllocationIdFieldName]];
            
            // Build the map of output values for the row
            var rowMetricsMap = this.BuildRowMetrics(network, measureSet, rawDeliveryRow);

            // Build the output string for the row, ordered by the leader list
            return BuildOutputForRow(headerList, rowMetricsMap);
        }

        /// <summary>Build the delivery metrics for a delivery row.</summary>
        /// <param name="network">The network.</param>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>Dictionary in the form (header, value)</returns>
        private Dictionary<string, string> BuildDeliveryMetrics(
            DeliveryNetworkDesignation network, 
            MeasureSet measureSet, 
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            var nodeValuation = this.Valuations[measureSet];
            var margin = this.AllocationParameters.Margin;
            var perMilleFees = this.AllocationParameters.PerMilleFees;
            var allocationId = (string)rawDeliveryRow[RawDeliveryDataParserBase.AllocationIdFieldName];
            var impressions = (long)rawDeliveryRow[dataName.ImpressionsFieldName];
            var mediaSpend = (decimal)rawDeliveryRow[dataName.MediaSpendFieldName];
            var clicks = (long)rawDeliveryRow[dataName.ClicksFieldName];
            var ecpm = (decimal)rawDeliveryRow[dataName.EcpmFieldName];

            // Calculated values
            var numSegments = measureSet.Count;
            var ctr = impressions > 0 ? Math.Round(((decimal)clicks / impressions) * 100, 6) : 0;
            var totalSpendForHour = this.MeasureInfo.CalculateTotalSpend(
                measureSet, impressions, mediaSpend, margin, perMilleFees);
            var servingCost = impressions / 1000m * perMilleFees;
            var billableDataCost = this.MeasureInfo.CalculateDataProviderCosts(measureSet, impressions, mediaSpend);
            var effectiveDataCostRate = impressions > 0 ? (billableDataCost * 1000) / impressions : 0;
            var profit = totalSpendForHour - billableDataCost - mediaSpend - servingCost;

            // Build the invariant part of the campaign name with allocation id
            var apnxCampaignName = "{0}--{1}".FormatInvariant(this.CampaignEntity.ExternalName, allocationId);
            
            var deliveryMetrics = new Dictionary<string, string>
                {
                    { "DA Campaign Name", this.CampaignEntity.ExternalName.Value.SerializationValue },
                    { "Advertiser Name", this.CompanyEntity.ExternalName.Value.SerializationValue },
                    { "Network", network.ToString() },
                    { "Campaign Id", rawDeliveryRow[dataName.CampaignIdFieldName].SerializationValue },
                    { "Campaign Name", apnxCampaignName },
                    { "Valuation", FormatNum(nodeValuation) },
                    { "Hour", rawDeliveryRow[dataName.HourFieldName].SerializationValue },
                    { "Allocation Id", allocationId },
                    { "Impressions", FormatNum(impressions) },
                    { "Ecpm", FormatNum(ecpm) },
                    { "Media Spend", FormatNum(mediaSpend) },
                    { "Clicks", FormatNum(clicks) },
                    { "Ctr", FormatNum(ctr) },
                    { "Serving Cost Rate", FormatNum(perMilleFees) },
                    { "Serving Cost", FormatNum(servingCost) },
                    { "Profit", FormatNum(profit) },
                    { "Total Spend", FormatNum(totalSpendForHour) },
                    { "Number Of Segments", FormatNum(numSegments) },
                    { "Effective Rate", FormatNum(effectiveDataCostRate) },
                    { "Billable Data Cost", FormatNum(billableDataCost) },
                };
            return deliveryMetrics;
        }

        /// <summary>Build the data costs and rates across all data providers (may be zero)</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>Dictionary in the form (header, value)</returns>
        private Dictionary<string, string> BuildDataProviderMetrics(
            MeasureSet measureSet, 
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            var impressions = (long)rawDeliveryRow[dataName.ImpressionsFieldName];
            var mediaSpend = (decimal)rawDeliveryRow[dataName.MediaSpendFieldName];
            var dataProviderMetrics = new Dictionary<string, string>();

            // Get the data providers
            var dataProvidersForMeasureSet = this.MeasureInfo.ExtractDataProviders(measureSet);

            // Add a concatonated list of data provider names.
            var dataProviderNamesForMeasureSet = string.Join(
                ":", dataProvidersForMeasureSet.Values.Distinct().OrderByDescending(x => x));
            dataProviderMetrics.Add("Data Provider Names", dataProviderNamesForMeasureSet);

            // Add the per-provider metrics
            foreach (var dataProvider in this.DataProviders.Values.Distinct())
            {
                var measuresForProvider = MeasureInfo.GetMeasuresForProvider(dataProvidersForMeasureSet, dataProvider);

                var impressionCost = 0m;
                var impressionCostRate = 0m;
                var percentOfMediaCost = 0m;
                var percentOfMediaRate = 0m;

                var effectiveCost = this.MeasureInfo.CalculateDataProviderCosts(measuresForProvider, impressions, mediaSpend);

                if (this.MeasureInfo.NoDataCosts(measuresForProvider))
                {
                    // No costs for the measures. Leave values as 0.
                }
                else if (this.MeasureInfo.UseDataCostOnly(measuresForProvider))
                {
                    impressionCostRate = this.MeasureInfo.CalculateCostRateUsingMaxMethod(measuresForProvider);
                    impressionCost = this.MeasureInfo.CalculateCostUsingMaxMethod(measuresForProvider, impressions);
                }
                else
                {
                    impressionCostRate = this.MeasureInfo.CalculateCostPerMille(measuresForProvider);
                    impressionCost = MeasureInfo.CalculateImpressionCosts(impressions, impressionCostRate);
                    percentOfMediaRate = this.MeasureInfo.CalculatePercentOfMediaSpendRate(measuresForProvider);
                    percentOfMediaCost = MeasureInfo.CalculatePercentOfMediaSpendCost(mediaSpend, percentOfMediaRate);
                }

                dataProviderMetrics.Add(
                    "{0}:{1}".FormatInvariant(dataProvider, "Effective Data Cost"), FormatNum(effectiveCost));
                dataProviderMetrics.Add(
                    "{0}:{1}".FormatInvariant(dataProvider, "Impression Based Rate"), FormatNum(impressionCostRate));
                dataProviderMetrics.Add(
                    "{0}:{1}".FormatInvariant(dataProvider, "Impression Based Data Cost"), FormatNum(impressionCost));
                dataProviderMetrics.Add(
                    "{0}:{1}".FormatInvariant(dataProvider, "Percent of Media Spend Rate"), FormatNum(percentOfMediaRate));
                dataProviderMetrics.Add(
                    "{0}:{1}".FormatInvariant(dataProvider, "Percent of Media Spend Data Cost"), FormatNum(percentOfMediaCost));
            }

            return dataProviderMetrics;
        }

        /// <summary>Build the per-row output for the measures of the measureSet.</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>Dictionary in the form (header, value)</returns>
        private Dictionary<string, string> BuildMeasureMetrics(
            MeasureSet measureSet,
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            var measureMetrics = new List<KeyValuePair<string, string>>();
            foreach (var measureId in this.MeasureGroupings.Keys)
            {
                var measureIdOut = FormatNum(measureId);
                var columnPrefix = "{0}:{1}".FormatInvariant("Id", measureIdOut);

                // Setup for a default set of empty outputs
                long measureIdToInclude = -1;
                var groupName = string.Empty;

                // If measure is present in measureSet setup for real metrics
                if (measureSet.Contains(measureId))
                {
                    measureIdToInclude = measureId;
                    groupName = this.MeasureGroupings[measureId];
                }

                // Get the common measure metrics
                var commonMeasureMetrics = this.BuildCommonMeasureMetrics(rawDeliveryRow, columnPrefix, measureIdToInclude);

                // Add a group name metric
                commonMeasureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Group"), groupName);

                measureMetrics.AddRange(commonMeasureMetrics);
            }

            return measureMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>Build the per-row output for the groups of the measureSet.</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <returns>Dictionary in the form (header, value)</returns>
        private Dictionary<string, string> BuildGroupMetrics(
            MeasureSet measureSet,
            Dictionary<string, PropertyValue> rawDeliveryRow)
        {
            var groupMetrics = new List<KeyValuePair<string, string>>();
            foreach (var groupName in this.MeasureGroupings.Values.Distinct())
            {
                var columnPrefix = "{0}:{1}".FormatInvariant("Group", groupName);

                // Setup for a default set of empty outputs
                long measureIdToInclude = -1;
                var measureIdOut = string.Empty;

                // If group is present in measureSet setup for real metrics
                var groupMeasures = this.MeasureGroupings.Where(g => g.Value == groupName).Select(g => g.Key).ToArray();
                if (measureSet.Any(groupMeasures.Contains))
                {
                    // Only a single measure can appear from a group
                    measureIdToInclude = measureSet.Single(groupMeasures.Contains);
                    measureIdOut = FormatNum(measureIdToInclude);
                }

                // Get the common measure metrics
                var commoneMeasureMetrics = this.BuildCommonMeasureMetrics(rawDeliveryRow, columnPrefix, measureIdToInclude);
                
                // Add a MeasureId metric
                commoneMeasureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "MeasureId"), measureIdOut);

                groupMetrics.AddRange(commoneMeasureMetrics);
            }

            return groupMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>Build the per-row output for the measures for common metrics.</summary>
        /// <param name="rawDeliveryRow">The raw delivery row.</param>
        /// <param name="columnPrefix">Either the measure group name or measure id.</param>
        /// <param name="measureId">The measureId for which to get metrics.</param>
        /// <returns>Dictionary in the form (header, value)</returns>
        private Dictionary<string, string> BuildCommonMeasureMetrics(
            Dictionary<string, PropertyValue> rawDeliveryRow,
            string columnPrefix,
            long measureId)
        {
            var measureMetrics = new Dictionary<string, string>();

            // Output blank columns for measures that are not in the measure set
            var dataProvider = string.Empty;
            var measureName = string.Empty;
            var dataCostOut = string.Empty;
            var minCpmOut = string.Empty;
            var percentSpendCostOut = string.Empty;
            var effectiveCostOut = string.Empty;

            // If measure is present
            if (measureId != -1)
            {
                var impressions = (long)rawDeliveryRow[dataName.ImpressionsFieldName];
                var mediaSpend = (decimal)rawDeliveryRow[dataName.MediaSpendFieldName];

                // There can be only one...and if not fail
                var dataCost = this.MeasureMap.TryGetDataCost(measureId);
                var minCpm = this.MeasureMap.TryGetMinCostPerMille(measureId);
                var percentSpendCost = this.MeasureMap.TryGetPercentOfMedia(measureId);
                var effectiveCost = this.MeasureInfo.CalculateDataProviderCosts(
                    new MeasureSet(new[] { measureId }), impressions, mediaSpend);

                if (this.MeasureMap.TryGetDataProviderForMeasure(measureId) != null)
                {
                    dataProvider = this.MeasureMap.TryGetDataProviderForMeasure(measureId);
                }

                measureName = this.MeasureNames[measureId];
                dataCostOut = FormatNum(dataCost.HasValue ? dataCost.Value : 0m);
                minCpmOut = FormatNum(minCpm.HasValue ? minCpm.Value : 0m);
                percentSpendCostOut = FormatNum(percentSpendCost.HasValue ? percentSpendCost.Value : 0m);
                effectiveCostOut = FormatNum(effectiveCost);
            }

            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Provider"), dataProvider);
            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Measure Name"), measureName);
            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Data Cost"), dataCostOut);
            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "MinCpm"), minCpmOut);
            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Percent of Spend"), percentSpendCostOut);
            measureMetrics.Add("{0}:{1}".FormatInvariant(columnPrefix, "Effective Cost"), effectiveCostOut);

            return measureMetrics;
        }
    }
}
