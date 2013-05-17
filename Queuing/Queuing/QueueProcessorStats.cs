//-----------------------------------------------------------------------
// <copyright file="QueueProcessorStats.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Diagnostics;
using WorkItems;

namespace Queuing
{
    /// <summary>Container for queue processor statistics</summary>
    internal class QueueProcessorStats
    {
        /// <summary>Format for the summary statistics</summary>
        private const string SummaryFormat =
@"QueueProcessor Statistics
Thread: ""{0}""
Categories: {1}
Started: {2} (UpTime: {3})
Avg Time in Queue (by Category) - {4}
Avg Time Processing (by Category) - {5}";

        /// <summary>Format for the per-period statistics</summary>
        private const string PeriodStatsFormat = @"
{0:#.#}hr: Totals - {1}
{0:#.#}hr: Avg/Hr - {2}
{0:#.#}hr: Categories - {3}";

        /// <summary>Format for the hour keys in the counts dictionaries</summary>
        private const string HourKeyFormat = "yyyyMMddHH";

        /// <summary>QueueProcessor the stats are for</summary>
        /// <remarks>Only used for information such as Categories</remarks>
        private QueueProcessor processor;

        /// <summary>Counts of work items by category and by hour</summary>
        private IDictionary<CountType, IDictionary<string, IDictionary<string, int>>> hourlyCounts;

        /// <summary>Running average of time spent in queue by category</summary>
        private IDictionary<string, Tuple<TimeSpan, int>> averageTimeInQueues;

        /// <summary>Running average of time spent in processing by category</summary>
        private IDictionary<string, Tuple<TimeSpan, int>> averageTimeInProcessing;

        /// <summary>Initializes a new instance of the QueueProcessorStats class.</summary>
        /// <param name="processor">QueueProcessor the stats are for.</param>
        public QueueProcessorStats(QueueProcessor processor)
        {
            this.processor = processor;
            this.StartTime = DateTime.UtcNow;

            this.averageTimeInQueues = new Dictionary<string, Tuple<TimeSpan, int>>();
            this.averageTimeInProcessing = new Dictionary<string, Tuple<TimeSpan, int>>();

            var countTypes = Enum.GetValues(typeof(CountType)).Cast<CountType>();
            this.hourlyCounts = new Dictionary<CountType, IDictionary<string, IDictionary<string, int>>>();
            foreach (var type in countTypes)
            {
                this.hourlyCounts.Add(type, new Dictionary<string, IDictionary<string, int>>());
            }
        }

        /// <summary>Types of counts</summary>
        private enum CountType
        {
            /// <summary>
            /// Dequeued work items
            /// </summary>
            Dequeued,

            /// <summary>
            /// Processed work items
            /// </summary>
            Processed,

            /// <summary>
            /// Failed work items
            /// </summary>
            Failed
        }

        /// <summary>Gets or sets the time at which the queue processor was created</summary>
        internal DateTime StartTime { get; set; }

        /// <summary>Increment the dequeued work item count</summary>
        /// <param name="category">Category to increment counts for</param>
        /// <param name="count">How much to increment by</param>
        public void AddDequeued(string category, int count)
        {
            this.IncrementCounts(CountType.Dequeued, category, count);
        }

        /// <summary>Increment the processed work item count</summary>
        /// <param name="workItem">WorkItem to increment counts for</param>
        public void AddProcessed(WorkItem workItem)
        {
            this.IncrementCounts(CountType.Processed, workItem.Category, 1);
            this.IncrementAverageTimes(workItem);
        }

        /// <summary>Increment the failed work item count</summary>
        /// <param name="workItem">WorkItem to increment counts for</param>
        public void AddFailed(WorkItem workItem)
        {
            this.IncrementCounts(CountType.Failed, workItem.Category, 1);
            this.IncrementAverageTimes(workItem);
        }

        /// <summary>Returns a string representation of the statistics</summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            var countTypes = Enum.GetValues(typeof(CountType)).Cast<CountType>();
            var sb = new StringBuilder();
            var lifetime = DateTime.UtcNow - this.StartTime;

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                SummaryFormat,
                Thread.CurrentThread.Name,
                string.Join(", ", this.processor.Categories),
                this.StartTime,
                lifetime,
                string.Join(", ", this.averageTimeInQueues.Select(kvp => "{0}: {1}".FormatInvariant(kvp.Key, kvp.Value))),
                string.Join(", ", this.averageTimeInProcessing.Select(kvp => "{0}: {1}".FormatInvariant(kvp.Key, kvp.Value))));

            foreach (var hours in new[] { lifetime.TotalHours, 24, 12, 6, 1 })
            {
                var periodTotals = countTypes
                    .Select(type => new KeyValuePair<CountType, int>(type, this.TotalCountForPastHours(type, hours)))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var periodHourlyAverages = countTypes
                    .Select(type => new KeyValuePair<CountType, double>(type, (double)periodTotals[type] / hours))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var pastHoursCategoryPercentages = this.CategoryPercentagesForPastHours(hours);
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    PeriodStatsFormat,
                    hours,
                    string.Join(", ", periodTotals.Select(kvp => "{0}: {1}".FormatInvariant(kvp.Key, kvp.Value))),
                    string.Join(", ", periodHourlyAverages.Select(kvp => "{0}: {1:0.###}".FormatInvariant(kvp.Key, kvp.Value))),
                    string.Join(", ", pastHoursCategoryPercentages.Select(kvp => "{0}: {1:#0.00%}".FormatInvariant(kvp.Key, kvp.Value))));
            }

            return sb.ToString();
        }

        /// <summary>Increments a running time average</summary>
        /// <param name="averageTimes">Dictionary of time averages</param>
        /// <param name="category">Category to update the time for</param>
        /// <param name="time">The time to update the average with</param>
        private static void IncrementAverageTime(
            ref IDictionary<string, Tuple<TimeSpan, int>> averageTimes,
            string category,
            TimeSpan time)
        {
            if (!averageTimes.ContainsKey(category))
            {
                averageTimes[category] = new Tuple<TimeSpan, int>(time, 1);
                return;
            }

            var currentTotalMilliseconds = averageTimes[category].Item1.TotalMilliseconds * averageTimes[category].Item2;
            var newSampleCount = averageTimes[category].Item2 + 1;
            var newAverageMilliseconds = (time.TotalMilliseconds + currentTotalMilliseconds) / newSampleCount;
            averageTimes[category] = new Tuple<TimeSpan, int>(
                new TimeSpan((long)Math.Round(newAverageMilliseconds * TimeSpan.TicksPerMillisecond)),
                newSampleCount);
        }

        /// <summary>Get the sum of the counts from the past number of hours</summary>
        /// <param name="type">Type of count to sum</param>
        /// <param name="hours">How many hours into the past to sum</param>
        /// <returns>The sum of the past number of hours' counts</returns>
        private int TotalCountForPastHours(CountType type, double hours)
        {
            var pastHourKey = DateTime.UtcNow.AddHours(-hours).ToString(HourKeyFormat, CultureInfo.InvariantCulture);
            return this.hourlyCounts[type]
                .SelectMany(kvp => kvp.Value.ToArray())
                .Where(kvp => string.CompareOrdinal(kvp.Key, pastHourKey) >= 0)
                .Sum(kvp => kvp.Value);
        }

        /// <summary>
        /// Gets a dictionary of category percentages for the past number of hours
        /// </summary>
        /// <param name="hours">How many hours into the past to get percentages</param>
        /// <returns>The percentages for each category</returns>
        private IDictionary<string, double> CategoryPercentagesForPastHours(double hours)
        {
            var total = (double)this.TotalCountForPastHours(CountType.Processed, hours);
            return this.hourlyCounts[CountType.Processed]
                .Select(kvp => new KeyValuePair<string, double>(
                    kvp.Key,
                    kvp.Value.Sum(hourlyCount => hourlyCount.Value)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value / total);
        }

        /// <summary>Increment counts</summary>
        /// <param name="type">Type of count to increment</param>
        /// <param name="category">Category to increment counts for</param>
        /// <param name="count">How much to increment by</param>
        private void IncrementCounts(CountType type, string category, int count)
        {
            if (!this.hourlyCounts[type].ContainsKey(category))
            {
                this.hourlyCounts[type][category] = new Dictionary<string, int>();
            }

            var hourKey = DateTime.UtcNow.ToString(HourKeyFormat, CultureInfo.InvariantCulture);
            if (!this.hourlyCounts[type][category].ContainsKey(hourKey))
            {
                this.hourlyCounts[type][category][hourKey] = 0;
            }

            this.hourlyCounts[type][category][hourKey] += count;
        }

        /// <summary>Increment average times</summary>
        /// <param name="workItem">WorkItem to increment times for</param>
        private void IncrementAverageTimes(WorkItem workItem)
        {
            IncrementAverageTime(
                ref this.averageTimeInQueues,
                workItem.Category,
                workItem.TimeInQueue);
            IncrementAverageTime(
                ref this.averageTimeInProcessing,
                workItem.Category,
                workItem.TimeInProcessing);
        }
    }
}
