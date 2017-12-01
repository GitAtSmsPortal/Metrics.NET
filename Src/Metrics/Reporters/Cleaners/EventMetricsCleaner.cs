using System;
using System.Collections.Generic;
using System.Linq;
using Metrics.Core;
using Metrics.MetricData;
using Metrics.Utils;

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
        
        private static readonly TimeSpan cleanIntervalBuffer = new TimeSpan(0, 0, 0, 5);
        private static readonly ReportEventCounts reportEventCounts;
        private static readonly ThreadingTimer timer;
        private static bool timerIsDisposed = false;
        private static ITimer testTimer;
        private static TimeSpan curInterval;

        /// <summary>
        /// The MetricsRegistry.
        /// </summary>
        public static MetricsRegistry Registry { get; set; }

        static EventMetricsCleaner()
        {
            reportEventCounts = new ReportEventCounts();
            curInterval = cleanIntervalBuffer;

            timer = new ThreadingTimer(null, curInterval, curInterval);
            timer.Tick += (sender, args) => Clean();
        }

        /// <summary>
        /// Enable execution of the cleaner for unit testing.
        /// </summary>
        /// <param name="tmr">The mock timer.</param>
        public static void EnableTestTimer(ITimer tmr)
        {
            testTimer = tmr;
            testTimer.Tick += (sender, args) => Clean();
            timer.Dispose();
            timerIsDisposed = true;
        }

        private static bool TestTimerEnabled {
            get
            {
                return testTimer != null;
            }
        }

        /// <summary>
        /// Total number of registered reports.
        /// </summary>
        public static int TotalReports
        {
            get
            {
                return reportEventCounts.Count;
            }
        }

        /// <summary>
        /// The current clean interval.
        /// </summary>
        public static double CurrentIntervalSeconds
        {
            get
            {
                return curInterval.TotalSeconds;
            }
        }

        /// <summary>
        /// The total number of events that have been reported for the report.
        /// </summary>
        /// <param name="reportIdentifier">The unique identifier for a report.</param>
        /// <returns>The total number of events.</returns>
        public static int GetReportsEventCount(int reportIdentifier)
        {
            return reportEventCounts[reportIdentifier].Count;
        }

        /// <summary>
        /// The total number of EventDetail items for an event that have been reported for the report.
        /// </summary>
        /// <param name="reportIdentifier">The unique identifier for a report.</param>
        /// <param name="eventMetricIdentifier">The unique identifier for the event.</param>
        /// <returns>The total number of EventDetail items reported.</returns>
        public static int GetReportsReportedEventDetailCount(int reportIdentifier, string eventMetricIdentifier)
        {
            var eventCount = reportEventCounts[reportIdentifier].FirstOrDefault(e => e.EventMetricIdentifier == eventMetricIdentifier);
            return eventCount?.TotalEventsReported ?? 0;
        }

        /// <summary>
        /// The total number of EventDetail items for an event.
        /// </summary>
        /// <param name="eventMetricIdentifier">The unique identifier for the event.</param>
        /// <returns>The total number of EventDetail items for the event.</returns>
        public static int GetEventDetailCount(string eventMetricIdentifier)
        {
            var eventCount = Registry.DataProvider.Events.FirstOrDefault(e => e.Name == eventMetricIdentifier);
            return eventCount?.Value.EventsCopy.Count ?? 0;
        }

        /// <summary>
        /// Register a report with the cleaner.
        /// </summary>
        /// <param name="interval">The interval that the reporter reports.</param>
        /// <returns>A unique identifier for the report.</returns>
        public static int RegisterReport(TimeSpan interval)
        {
            // Ensure that the cleaner runs after the last reporter has run.
            var tmpInterval = cleanIntervalBuffer.Add(interval);
            if (tmpInterval > curInterval)
            {
                curInterval = tmpInterval;
                if (!TestTimerEnabled)
                {
                    timer.Change(tmpInterval, tmpInterval);
                }
            }

            reportEventCounts.Add(new HashSet<EventCount>());
            return reportEventCounts.Count - 1;
        }

        /// <summary>
        /// Update the number of EventDetail items reported by the report.
        /// </summary>
        /// <param name="reportIdentifier">The unique identifier for a report.</param>
        /// <param name="events">The events reported by the reporter.</param>
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

        /// <summary>
        /// Clear all reports from the cleaner.
        /// </summary>
        public static void Clear()
        {
            reportEventCounts.Clear();
        }

        /// <summary>
        /// Resets the clenaer interval to be the default interval.
        /// </summary>
        public static void ResetInterval()
        {
            curInterval = cleanIntervalBuffer;
            if (!timerIsDisposed)
            {
                timer.Change(curInterval, curInterval);
            }
        }

        /// <summary>
        /// Remove an event from all the reports within the cleaner.
        /// </summary>
        /// <param name="eventMetricIdentifier">The unique identifier for the event to remove.</param>
        public static void RemoveEvent(string eventMetricIdentifier)
        {
            foreach (var eventCount in reportEventCounts)
            {
                if (!string.IsNullOrWhiteSpace(eventMetricIdentifier))
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
            if (Registry != null)
            {
                if (reportEventCounts.Count == 0)
                {
                    Registry.ClearEventValues();
                    return;
                }

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

                if (eventMetricIdentifiers.Count == 0)
                {
                    Registry.ClearEventValues();
                    return;
                }

                foreach (var eventMetricId in eventMetricIdentifiers)
                {
                    Registry.EventValuesRemoveRange(eventMetricId, 0, lowestNumEventsReported[eventMetricId]);
                }
            }
        }
    }
}
