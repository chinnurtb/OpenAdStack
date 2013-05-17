// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RawDeliverySimulator.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicAllocation;

namespace AllocationSimulator
{
    /// <summary>
    /// Generates configurable raw delivery data 
    /// </summary>
    public class RawDeliverySimulator
    {
        /// <summary>
        /// the tier that directly has a delivery probability of baseTierDeliveryProbability
        /// </summary>
        private int baseTier = 4;

        /// <summary>
        /// Contains all the simulated node rates
        /// </summary>
        private Dictionary<MeasureSet, decimal> nodeRates = new Dictionary<MeasureSet, decimal>();

        /// <summary>
        /// Contains all the simulated node rates
        /// </summary>
        private Dictionary<long, decimal> measureRates = new Dictionary<long, decimal>();

        /// <summary>
        /// Gets or sets the percentage +- a campaign can deliver on any given hour over or below 
        /// the rate it gets locked into the first time it serves (which is to exactly serve its budget for the period).
        /// this can be made zero so there is no randomness
        /// </summary>
        public double RandPercentage { get; set; }

        /// <summary>
        /// Gets or sets the probability that a base tier node will not goose-egg
        /// </summary>
        public double BaseTierDeliveryProbability { get; set; }

        /// <summary>
        /// Gets or sets the rate at which the probability of delivering decays by tier
        /// ie if tier 4 have an 80% chance of serving, and this is .5, then tier 5 will
        /// have a 40% chance of serving (and tier 3 will have 100% chance of serving)
        /// </summary>
        public double DeliveryProbabilityDecayRate { get; set; }

        /// <summary>
        /// Gets or sets Random number generator
        /// </summary>
        public Random Random { get; set; }

        /// <summary>
        /// Gets or sets AverageMeasureRate
        /// </summary>
        public decimal AverageMeasureRate { get; set; }

        /// <summary>
        /// Gets or sets RateConversionFactor
        /// </summary>
        public decimal RateConversionFactor { get; set; }

        /// <summary>
        /// Gets or sets AverageEcpm
        /// </summary>
        public decimal AverageEstimatedCostPerMille { get; set; }
        
        /// <summary>
        /// Gets or sets MeasureRateDeviationPercentage
        /// </summary>
        public decimal MeasureRateDeviationPercentage { get; set; }

        /// <summary>
        /// Gets or sets the DeliverySimulationType (currently "1" for the older tier based way and "2" for the lineage based way)
        /// </summary>
        public string DeliverySimulationType { get; set; }

        /// <summary>
        /// Get Simulated Delievery Data
        /// </summary>
        /// <param name="exportedAllocations">the exported budget allocations to simulate delivery.</param>
        /// <returns>the simulated delivery data</returns>
        public string GetSimulatedRawDeliveryDataTierBased(IList<BudgetAllocation> exportedAllocations)
        {
            var rawDeliveryData = new StringBuilder();
            rawDeliveryData.AppendLine("campaign_id,hour,campaign_code,imps,ecpm,spend,clicks");

            foreach (var exportedAllocation in exportedAllocations)
            {
                var periodNumberofHours = (int)exportedAllocation.PeriodDuration.TotalHours;
                var previouslyExportedNodes = exportedAllocation.PerNodeResults.Where(pnr => pnr.Value.ExportBudget > 0);
                var measureCount = exportedAllocation.PerNodeResults.Max(pnr => pnr.Key.Count);

                // set new nodes delivery rates
                foreach (var node in previouslyExportedNodes)
                {
                    // Determine if the node will serve zero or something
                    if (!this.nodeRates.ContainsKey(node.Key))
                    {
                        var localPeriodHours = Math.Max(
                                periodNumberofHours * (decimal)Random.NextDouble() * (decimal)(1 + (12 * node.Key.Count / measureCount)), // lower tiers should have a higher probability of serving in fewer hours
                                (decimal)(node.Key.Count * .15)); // Min number of hours to spend initial budget
                        this.nodeRates[node.Key] = Random.NextDouble() <= this.DeliveryProbability(node.Key.Count) * (node.Value.LineagePenalty > 0 ? node.Value.LineagePenalty : 1) ?
                            node.Value.PeriodMediaBudget / localPeriodHours : 0;
                    }
                    else
                    {
                        this.nodeRates[node.Key] = node.Value.EffectiveMediaSpendRate;
                    }

                    var rate = this.nodeRates[node.Key];

                    ////Dont do this if we shot a goose egg
                    if (rate > 0)
                    {
                        var ecpm = node.Value.MaxBid;
                        var periodSpend = 0m;

                        for (var hour = 0; hour < periodNumberofHours; hour++)
                        {
                            var hourlyRate = Math.Round(rate * (decimal)(1 - this.RandPercentage + (2 * this.RandPercentage * Random.NextDouble())), 2);
                       
                            // make the last hour stop at the correct max period spend
                            if (periodSpend + hourlyRate > node.Value.ExportBudget)
                            {
                                hourlyRate = Math.Max(0, node.Value.ExportBudget - periodSpend);
                            }

                            var imps = (int)Math.Round(hourlyRate / ecpm * 1000);
                            if (imps > 0)
                            {
                                var record = string.Join(
                                    ",",
                                    0,
                                    exportedAllocation.PeriodStart.AddHours(hour).ToString(),
                                    node.Value.AllocationId,
                                    imps,
                                    ecpm,
                                    hourlyRate,
                                        0);
                                rawDeliveryData.AppendLine(record);
                                periodSpend += hourlyRate;
                            }
                        }
                    }
                }
            }

            return rawDeliveryData.ToString();
        }

        /// <summary>
        /// Get Simulated Delievery Data
        /// </summary>
        /// <param name="exportedAllocations">the exported budget allocations to simulate delivery.</param>
        /// <returns>the simulated delivery data</returns>
        public string GetSimulatedRawDeliveryDataLineageBased(IList<BudgetAllocation> exportedAllocations)
        {
            var rawDeliveryData = new StringBuilder();
            rawDeliveryData.AppendLine("campaign_id,hour,campaign_code,imps,ecpm,spend,clicks");

            foreach (var exportedAllocation in exportedAllocations)
            {
                var periodNumberofHours = (int)exportedAllocation.PeriodDuration.TotalHours;
                var previouslyExportedNodes = exportedAllocation.PerNodeResults.Where(pnr => pnr.Value.ExportBudget > 0);
          
                var measures = exportedAllocation.PerNodeResults.SelectMany(pnr => pnr.Key).Distinct();
                var measureRateDeviation = this.MeasureRateDeviationPercentage * this.AverageMeasureRate;

                // adjust the rate if measures can have zero rates so that the average is still correct
                var averageRate = this.AverageMeasureRate > measureRateDeviation ?
                    this.AverageMeasureRate :
                    (decimal)((2 * Math.Pow((double)(measureRateDeviation * this.AverageMeasureRate), .5)) - (double)measureRateDeviation);

                // TODO: move this into an initializer so it gets done once and for all
                foreach (var measure in measures)
                {
                    if (!this.measureRates.ContainsKey(measure))
                    {
                        // the rate is a value around the average, but not less than zero
                        this.measureRates[measure] = (decimal)Math.Max(0, averageRate + (measureRateDeviation * (decimal)((2 * this.Random.NextDouble()) - 1)));
                    }
                }

                // set new nodes delivery rates
                foreach (var node in previouslyExportedNodes)
                {
                    // Give the node a rate if it doesn't have one
                    // TODO: add configurable noise at this step
                    if (!this.nodeRates.ContainsKey(node.Key))
                    {
                        this.nodeRates[node.Key] = this.RateConversionFactor * node.Key.Aggregate(1m, (a, b) => a * this.measureRates[b]);
                    }

                    var rate = this.nodeRates[node.Key];

                    ////Dont do this if we shot a goose egg
                    if (rate > 0)
                    {
                        // TODO: add radomization and/or tier variation to ecpm
                        var ecpm = this.AverageEstimatedCostPerMille;
                        var periodSpend = 0m;

                        // TODO: add day time flucuations
                        for (var hour = 0; hour < periodNumberofHours; hour++)
                        {
                            var hourlyRate = Math.Round(rate * (decimal)(1 + (this.RandPercentage * ((2 * Random.NextDouble()) - 1))), 2);

                            // make the last hour stop at the correct max period spend
                            if (periodSpend + hourlyRate > node.Value.ExportBudget)
                            {
                                hourlyRate = Math.Max(0, node.Value.ExportBudget - periodSpend);
                            }

                            var imps = (int)Math.Round(hourlyRate / ecpm * 1000);
                            if (imps > 0)
                            {
                                var record = string.Join(
                                    ",",
                                    0,
                                    exportedAllocation.PeriodStart.AddHours(hour).ToString(),
                                    node.Value.AllocationId,
                                    imps,
                                    ecpm,
                                    hourlyRate,
                                        0);
                                rawDeliveryData.AppendLine(record);
                                periodSpend += hourlyRate;
                            }
                        }
                    }
                }
            }

            return rawDeliveryData.ToString();
        }

        /// <summary>
        /// the probabiltiy of a node on a given tier delivering
        /// </summary>
        /// <param name="tier">the tier</param>
        /// <returns>the probabiltiy of delivering</returns>
        private double DeliveryProbability(int tier)
        {
            return Math.Min(1, this.BaseTierDeliveryProbability * Math.Pow(this.DeliveryProbabilityDecayRate, tier - this.baseTier));
        }
    }
}
