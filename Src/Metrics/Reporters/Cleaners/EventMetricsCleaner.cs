using System;
using System.Collections.Generic;
using System.Linq;
using Metrics.Core;
using Metrics.MetricData;

namespace Metrics.Reporters.Cleaners
{
    public static class EventMetricsCleaner
    {
        private class EventCount
        {
            /// <summary>
            /// The unique identifier for the event metric.
            /// </summary>
            public string EventMetricIdentifier { get; set; }

            /// <summary>
            /// The total <c>EventDetail</c> items that have been reported for the event metric.
            /// </summary>
            public int TotalEventsReported { get; set; }
        }
        
        /// <summary>
        /// Collection of events and the total number of associated <c>EventDetail</c> items that have reported.
        /// </summary>
        private class ReportEventCounts : List<HashSet<EventCount>> { }
        
        private static readonly TimeSpan cleanIntervalBuffer = new TimeSpan(0, 0, 0, 0, 10000);
        private static readonly ReportEventCounts reportEventCounts;
        private static readonly System.Threading.Timer timer;
        private static TimeSpan curInterval;

        public static MetricsRegistry Registry { get; set; }

        static EventMetricsCleaner()
        {
            reportEventCounts = new ReportEventCounts();
            curInterval = cleanIntervalBuffer;
            timer = new System.Threading.Timer(state => { Clean(); }, new object(), curInterval, curInterval);
        }

        public static int RegisterReport(TimeSpan interval)
        {
            // Ensure that the cleaner runs after the last reporter has run.
            var tmpInterval = cleanIntervalBuffer.Add(interval);
            if (tmpInterval > curInterval)
            {
                curInterval = tmpInterval;
                timer.Change(tmpInterval, tmpInterval);
            }

            reportEventCounts.Add(new HashSet<EventCount>());
            return reportEventCounts.Count - 1;
        }

        public static void UpdateTotalReportedEvents(int reportIdentifier, IEnumerable<EventValueSource> events)
        {
            if (reportIdentifier >= 0 && reportIdentifier < reportEventCounts.Count)
            {
                foreach (var evntSrc in events)
                {
                    var count = evntSrc.Value.EventsCopy.Count;
                    var eventMetricId = MetricIdentifier.Calculate(evntSrc.Name, evntSrc.Tags);
                    var report = reportEventCounts[reportIdentifier];

                    var eventCount = report.FirstOrDefault(e => e.EventMetricIdentifier == eventMetricId);
                    if (eventCount != null)
                    {
                        eventCount.TotalEventsReported = count;
                    }
                    else
                    {
                        report.Add(new EventCount
                        {
                            EventMetricIdentifier = eventMetricId,
                            TotalEventsReported = count
                        });
                    }
                }
            }
        }

        public static void RemoveEvent(string eventMetricIdentifier)
        {
            foreach (var eventCount in reportEventCounts)
            {
                if (string.IsNullOrWhiteSpace(eventMetricIdentifier))
                { 
                    eventCount.RemoveWhere(e => e.EventMetricIdentifier == eventMetricIdentifier);
                }
            }
        }

        /// <summary>
        /// Remove the least number of reported EventDetail items from the MetricsRegistry for each event metric.
        /// </summary>
        public static void Clean()
        {
            var eventMetricIdentifiers = new HashSet<string>();
            var lowestNumEventsReported = new Dictionary<string, int>();
            foreach (var eventCounts in reportEventCounts)
            {
                foreach (var eventCount in eventCounts)
                {
                    var eventMetricId = eventCount.EventMetricIdentifier;
                    var totalEventsReported = eventCount.TotalEventsReported;

                    if (totalEventsReported == 0)
                    {
                        // The cleaner runs after all reports have run, therefore if a report has reported an event, 
                        // the count will always be greater than zero for that report for the event.
                        // Otherwise the reporter has filtered out those event metrics and will never report their associated EventDetail items.
                        // So we ignore the count if it is equal to zero as we know the report will not report the event and to ensure the 
                        // calculation for finding the least number of EventDetail items that have been reported will be correct.
                        continue;
                    }

                    eventMetricIdentifiers.Add(eventMetricId);
                    if (lowestNumEventsReported.ContainsKey(eventMetricId))
                    {
                        if (totalEventsReported < lowestNumEventsReported[eventMetricId])
                        {
                            lowestNumEventsReported[eventMetricId] = totalEventsReported;
                        }
                    }
                    else
                    {
                        lowestNumEventsReported.Add(eventMetricId, totalEventsReported);
                    }
                }
            }
            foreach (var eventMetricId in eventMetricIdentifiers)
            {
                Registry.EventValuesRemoveRange(eventMetricId, 0, lowestNumEventsReported[eventMetricId]);
            }
        }
    }
}
