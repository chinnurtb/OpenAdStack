//-----------------------------------------------------------------------
// <copyright file="MeasureInfo.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics;

namespace DynamicAllocation
{
    /// <summary>Class to load data cost information and calculate costs for a measure set</summary>
    public class MeasureInfo
    {
        /// <summary>the name of Lotame</summary>
        public const string DataProviderNameLotame = "Lotame";

        /// <summary>the name of Excelate</summary>
        public const string DataProviderNameExelate = "exelate";

        /// <summary>the name of BlueKai</summary>
        public const string DataProviderNameBlueKai = "BlueKai";

        /// <summary>the name of Peer39</summary>
        public const string DataProviderNamePeer39 = "Peer39";

        /// <summary>the no cost "data provider"</summary>
        public const string DataProviderNoCost = "NoCost";

        /// <summary>unknown data provider</summary>
        public const string DataProviderUnknown = "Unknown";

        /// <summary>Collection of info for each data provider we use</summary>
        internal readonly Dictionary<string, Func<MeasureSet, long, decimal, decimal>> DataProviderInfo;

        /// <summary>the measure map</summary>
        private readonly MeasureMap measureMap;

        /// <summary>Initializes a new instance of the MeasureInfo class</summary>
        /// <param name="measureMap">The measure map</param>
        public MeasureInfo(MeasureMap measureMap)
        {
            this.measureMap = measureMap;
            this.DataProviderInfo = new Dictionary<string, Func<MeasureSet, long, decimal, decimal>>
                {
                    { DataProviderNameLotame, (m, i, s) => RateToCost(this.CalculateCostRateUsingMaxMethod(m), i) },
                    { DataProviderNameExelate, (m, i, s) => RateToCost(this.CalculateCostRateUsingMaxMethod(m), i) },
                    { DataProviderNameBlueKai, (m, i, s) => RateToCost(this.CalculateCostRateUsingMaxMethod(m), i) },
                    { DataProviderNamePeer39, (m, i, s) => this.CalculateCostUsingPercentOfSpend(m, i, s) },
                    { DataProviderNoCost, (m, i, s) => 0 },
                    { DataProviderUnknown, (m, i, s) => 0 },
                };
        }

        /// <summary>Calculate data cost given a percent of media spend rate.</summary>
        /// <param name="mediaSpend">The media spend.</param>
        /// <param name="spendRate">The spend rate.</param>
        /// <returns>The data cost.</returns>
        public static decimal CalculatePercentOfMediaSpendCost(decimal mediaSpend, decimal spendRate)
        {
            return mediaSpend * spendRate;
        }

        /// <summary>Calculate data cost of impressions given a per mille rate.</summary>
        /// <param name="impressionCount">The impression count. </param>
        /// <param name="costPerMille">The cost per mille.</param>
        /// <returns>The data cost.</returns>
        public static decimal CalculateImpressionCosts(long impressionCount, decimal costPerMille)
        {
            return RateToCost(costPerMille, impressionCount);
        }

        /// <summary>Return a measureSet for a given provider from a dictionary of measures - provider pairs</summary>
        /// <param name="dataProviders">data providers by measure</param>
        /// <param name="providerName">provider name to get</param>
        /// <returns>a measure set</returns>
        public static MeasureSet GetMeasuresForProvider(IDictionary<long, string> dataProviders, string providerName)
        {
            if (dataProviders == null || dataProviders.Count == 0)
            {
                return new MeasureSet();
            }

            var measures = dataProviders.Where(dp => CompareDataProviderName(providerName, dp.Value)).Select(dp => dp.Key);

            return new MeasureSet(measures);
        }

        /// <summary>Calculate the data costs for a measure set using the max cost method.</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>The data cost.</returns>
        public decimal CalculateCostRateUsingMaxMethod(MeasureSet measureSet)
        {
            // Nothing to do
            if (measureSet.Count == 0)
            {
                return 0m;
            }

            return measureSet.Select(this.measureMap.GetDataCostForMeasure).Max();
        }

        /// <summary>
        /// Calculate Cost Using Percent Of Spend
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="impressionCount">the impressionCount</param>
        /// <param name="mediaSpend">the proxyMediaSpend</param>
        /// <returns>the cost</returns>
        public decimal CalculateCostUsingPercentOfSpend(MeasureSet measureSet, long impressionCount, decimal mediaSpend)
        {
            // Nothing to do
            if (measureSet.Count == 0)
            {
                return 0m;
            }

            if (this.UseDataCostOnly(measureSet))
            {
                return this.CalculateCostUsingMaxMethod(measureSet, impressionCount);
            }

            var costPerMille = this.CalculateCostPerMille(measureSet);
            var spendRate = this.CalculatePercentOfMediaSpendRate(measureSet);
            var impressionCosts = CalculateImpressionCosts(impressionCount, costPerMille);
            var spendCosts = CalculatePercentOfMediaSpendCost(mediaSpend, spendRate); 
            return impressionCosts > spendCosts ? impressionCosts : spendCosts;
        }

        /// <summary>
        /// Calculate the minimum allowed percent of media spend rate if available.
        /// This is the maximum of the rates of measures in the measureSet.
        /// </summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>Minimum allowed percent of media spend rate.</returns>
        public decimal CalculatePercentOfMediaSpendRate(MeasureSet measureSet)
        {
            var spendRate = measureSet
                .Where(measure => this.measureMap.TryGetPercentOfMedia(measure) != null)
                .Max(measure => this.measureMap.TryGetPercentOfMedia(measure));

            if (spendRate == null)
            {
                var message = 
                    "Missing PercentOfMedia info for measureSet in measure map: ({0})."
                    .FormatInvariant(string.Join(",", measureSet.ToList()));
                LogManager.Log(LogLevels.Error, message);
                throw new ArgumentException(message);
            }

            return spendRate.Value;
        }

        /// <summary>
        /// Calculate minimum allowed cost per mille if available. This is the maximum
        /// of the cost per mille rates of measures in the measureSet.
        /// </summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>Minimum allowed cost per mille.</returns>
        public decimal CalculateCostPerMille(MeasureSet measureSet)
        {
            var costPerMille = measureSet
                .Where(measure => this.measureMap.TryGetMinCostPerMille(measure) != null)
                .Max(measure => this.measureMap.TryGetMinCostPerMille(measure));
            
            if (costPerMille == null)
            {
                var message =
                    "Missing MinCPM info for measureSet in measure map: ({0})."
                    .FormatInvariant(string.Join(",", measureSet.ToList()));
                LogManager.Log(LogLevels.Error, message);
                throw new ArgumentException(message);
            }

            return costPerMille.Value;
        }

        /// <summary>Calculate data cost useing the Max rate method</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="impressionCount">The impression count.</param>
        /// <returns>Data cost</returns>
        public decimal CalculateCostUsingMaxMethod(MeasureSet measureSet, long impressionCount)
        {
            return RateToCost(this.CalculateCostRateUsingMaxMethod(measureSet), impressionCount);
        }

        /// <summary>Determine if we have a Data Cost rate - if so, we should use that.</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>True if we should use the data cost value based on the max rate method.</returns>
        public bool UseDataCostOnly(MeasureSet measureSet)
        {
            return !measureSet.Any(measure => this.measureMap.TryGetDataCost(measure) == null);
        }

        /// <summary>Determine if we have no data costs</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>True if we do not have any data costs for the measures.</returns>
        public bool NoDataCosts(MeasureSet measureSet)
        {
            return measureSet.All(measure =>
                    new[]
                    {
                        this.measureMap.TryGetDataCost(measure),
                        this.measureMap.TryGetMinCostPerMille(measure),
                        this.measureMap.TryGetPercentOfMedia(measure)
                    }
                    .All(cost => cost == null || cost == 0));
        }

        /// <summary>Calculate the data costs for a list of measure sets using the max cost method.</summary>
        /// <param name="measureSets">The measure sets.</param>
        /// <returns>The data costs.</returns>
        public IDictionary<MeasureSet, decimal> CalculateCostUsingMaxMethod(IEnumerable<MeasureSet> measureSets)
        {
            return measureSets.ToDictionary(ms => ms, this.CalculateCostRateUsingMaxMethod);
        }

        /// <summary>
        /// Calculate total spend for a given volume of delivery (without regard to the delivery period).
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="impressionCount">the impression count</param>
        /// <param name="mediaSpend">the media spend</param>
        /// <param name="margin">margin multiplier</param>
        /// <param name="perMilleFees">per mille fees</param>
        /// <returns>the cost</returns>
        public decimal CalculateTotalSpend(MeasureSet measureSet, long impressionCount, decimal mediaSpend, decimal margin, decimal perMilleFees)
        {
            var accumluatedDataCost = this.CalculateDataProviderCosts(measureSet, impressionCount, mediaSpend);
            return ((accumluatedDataCost + mediaSpend) * margin) + RateToCost(perMilleFees, impressionCount);
        }

        /// <summary>
        /// Calculate total data provider costs for a given volume of delivery (without regard to the delivery period).
        /// </summary>
        /// <param name="measureSet">the measureSet</param>
        /// <param name="impressionCount">the impression count</param>
        /// <param name="mediaSpend">the media spend</param>
        /// <returns>The data provider costs</returns>
        public decimal CalculateDataProviderCosts(MeasureSet measureSet, long impressionCount, decimal mediaSpend)
        {
            decimal accumluatedDataCost = 0m;

            // Get the data providers for a given measureSet and iterate over them.
            var dataProvidersForMeasureSet = this.ExtractDataProviders(measureSet);
            foreach (var dataProviderName in dataProvidersForMeasureSet.Values.Distinct())
            {
                var measuresForProvider = GetMeasuresForProvider(dataProvidersForMeasureSet, dataProviderName);
                var coster = this.DataProviderInfo[dataProviderName];
                var costForProvider = coster(measuresForProvider, impressionCount, mediaSpend);
                accumluatedDataCost += costForProvider;
            }

            return accumluatedDataCost;
        }

        /// <summary> Calculate a media spend component of a given total spend (without regard to the delivery period).</summary>
        /// <param name="measureSet">measure set</param>
        /// <param name="totalSpend">total spend</param>
        /// <param name="proxyMediaSpend">proxy media spend (must correspond to proxyImpressions)</param>
        /// <param name="proxyImpressions">proxy impressions (must correspond to proxyMediaSpend)</param>
        /// <param name="margin">margin multiplier</param>
        /// <param name="perMilleFees">per mille fees</param>
        /// <returns>media spend</returns>
        public decimal CalculateMediaSpend(MeasureSet measureSet, decimal totalSpend, decimal proxyMediaSpend, long proxyImpressions, decimal margin, decimal perMilleFees)
        {
            if (proxyMediaSpend == 0 || proxyImpressions == 0)
            {
                return 0m;
            }

            // Calculate a spend ratio using a proxy media spend and impressions
            var proxyTotalSpend = this.CalculateTotalSpend(measureSet, proxyImpressions, proxyMediaSpend, margin, perMilleFees);
            var spendRatio = proxyMediaSpend / proxyTotalSpend;

            // Approximate the media spend from the total spend and the spend ration
            var mediaSpend = totalSpend * spendRatio;
            return mediaSpend;
        }

        /// <summary>Get the data providers for the measureSet</summary>
        /// <param name="measureSet">The measure set.</param>
        /// <returns>A list of measures grouped by data provider</returns>
        public IDictionary<long, string> ExtractDataProviders(MeasureSet measureSet)
        {
            var dataProviders = measureSet
                .ToDictionary(m => m, this.GetCanonicalDataProviderName)
                .Where(kvp => kvp.Value != null)
                .ToDictionary();
            return dataProviders;
        }

        /// <summary>Determine if two data provider names refer to the same data provider</summary>
        /// <param name="providerNameFirst">The first provider name.</param>
        /// <param name="providerNameSecond">The second provider name.</param>
        /// <returns>True of the provider names match.</returns>
        internal static bool CompareDataProviderName(string providerNameFirst, string providerNameSecond)
        {
            // Case-insenstive comparison
            return string.Compare(providerNameFirst, providerNameSecond, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>Convert per mille rate to cost.</summary>
        /// <param name="rate">the rate</param>
        /// <param name="impressionCount">the number of impressions</param>
        /// <returns>the cost</returns>
        private static decimal RateToCost(decimal rate, long impressionCount)
        {
            return impressionCount * rate / 1000;
        }

        /// <summary>Get a canonical data provider name given the name from the measure map.</summary>
        /// <param name="measure">The measure.</param>
        /// <returns>The canonical name.</returns>
        /// <exception cref="ArgumentException">If canonical name not found.</exception>
        private string GetCanonicalDataProviderName(long measure)
        {
            var dataProviderName = this.measureMap.TryGetDataProviderForMeasure(measure);
            return this.DataProviderInfo.Keys.SingleOrDefault(name => CompareDataProviderName(name, dataProviderName));
        }
     }
}
