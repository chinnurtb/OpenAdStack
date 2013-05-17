// -----------------------------------------------------------------------
// <copyright file="CanonicalDeliveryData.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Nested class to hold a collection of canonical delivery data from a single network</summary>
    internal class CanonicalDeliveryData : ICanonicalDeliveryData
    {
        /// <summary>Minimum date value for delivery and report dates/times.</summary>
        internal static readonly DateTime MinimumDeliveryDate = DateTime.MinValue;

        /// <summary>Maximum date value for delivery and report dates/times.</summary>
        internal static readonly DateTime MaximumDeliveryDate = DateTime.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="CanonicalDeliveryData"/> class.
        /// </summary>
        internal CanonicalDeliveryData() : this(DeliveryNetworkDesignation.Unknown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CanonicalDeliveryData"/> class.
        /// </summary>
        /// <param name="network">
        /// The network.
        /// </param>
        internal CanonicalDeliveryData(DeliveryNetworkDesignation network)
        {
            this.Network = network;
            this.LatestDeliveryDataDate = MinimumDeliveryDate;
            this.LatestDeliveryReportDate = MinimumDeliveryDate;
            this.EarliestDeliveryDataDate = MaximumDeliveryDate;
            this.EarliestDeliveryReportDate = MaximumDeliveryDate;
            this.ParsedRecords = new Dictionary<string, Dictionary<string, PropertyValue>>();
        }

        /// <summary>
        /// Gets or sets Network the delivery data comes from.
        /// </summary>
        public DeliveryNetworkDesignation Network { get; set; }

        /// <summary>
        ///  Gets DeliveryDataForNetwork.
        /// </summary>
        public IList<Dictionary<string, PropertyValue>> DeliveryDataForNetwork 
        { 
            get { return this.ParsedRecords.Values.ToList(); }
        }

        /// <summary>
        /// Gets or sets LatestDeliveryReportDate (latest report pull time included).
        /// </summary>
        public DateTime LatestDeliveryReportDate { get; set; }

        /// <summary>
        /// Gets or sets EarliestDeliveryReportDate (earliest report pull time included).
        /// </summary>
        public DateTime EarliestDeliveryReportDate { get; set; }

        /// <summary>
        /// Gets or sets LatestDeliveryDataDate (last reported hour bucket in the raw data).
        /// </summary>
        public DateTime LatestDeliveryDataDate { get; set; }

        /// <summary>
        /// Gets or sets EarliestDeliveryDataDate (first reported hour bucket in the raw data).
        /// </summary>
        public DateTime EarliestDeliveryDataDate { get; set; }

        /// <summary>
        /// Gets or sets ParsedRecords (collection of records we have already parsed).
        /// </summary>
        private Dictionary<string, Dictionary<string, PropertyValue>> ParsedRecords { get; set; }

        /// <summary>Apply a lookback making sure it is not an invalid DateTime</summary>
        /// <param name="lookBackDuration">The look back duration.</param>
        /// <param name="lookBackStart">The lookback start.</param>
        /// <returns>The new date with lookback subtracted, or DateTime.MinValue.</returns>
        public DateTime ApplyLookBack(TimeSpan lookBackDuration, DateTime lookBackStart)
        {
            return DeliveryMetrics.ApplyLookBack(lookBackDuration, lookBackStart);
        }

        /// <summary>
        /// Parse and merge additional raw delivery data into the canonical delivery data.
        /// </summary>
        /// <param name="rawDeliveryData">A raw delivery data string.</param>
        /// <param name="deliveryReportDate">Timestamp when report was retrieved.</param>
        /// <param name="parser">Raw delivery data parser.</param>
        /// <returns>True if raw data successfully added.</returns>
        public bool AddRawData(string rawDeliveryData, DateTime deliveryReportDate, IRawDeliveryDataParser parser)
        {
            // Split the raw data into records and concatonate. This will result
            // in multiple header records in the concatonated record array.
            var rawDeliveryDataRecords = parser.SplitRecords(rawDeliveryData).ToArray();

            // Parse the raw records. Parser must be tolerant of multiple header records.
            var parsedRecords = parser.ParseRawRecords(rawDeliveryDataRecords);

            // No partial success. If we cannot parse it return null
            if (parsedRecords == null)
            {
                return false;
            }

            // Add unique entries only to parsedRecordsForLookBack
            foreach (var parsedRecord in parsedRecords)
            {
                var key = "{0}:{1}".FormatInvariant(
                    parsedRecord[RawDeliveryDataParserBase.AllocationIdFieldName],
                    parsedRecord[RawDeliveryDataParserBase.HourFieldName]);
                this.ParsedRecords[key] = parsedRecord;
            }

            // Set latest delivery report date seen so far
            if (deliveryReportDate > this.LatestDeliveryReportDate)
            {
                this.LatestDeliveryReportDate = deliveryReportDate;
            }

            // Set earliest delivery report date seen so far
            if (deliveryReportDate < this.EarliestDeliveryReportDate)
            {
                this.EarliestDeliveryReportDate = deliveryReportDate;
            }

            // Determine the date range of the data accumulated so far
            var dates = this.ParsedRecords.Values.Select(r => (DateTime)r[RawDeliveryDataParserBase.HourFieldName]).ToArray();
            if (dates.Any())
            {
                // Set delivery data date range seen so far
                this.LatestDeliveryDataDate = dates.Max();
                this.EarliestDeliveryDataDate = dates.Min();
            }

            return true;
        }
    }
}