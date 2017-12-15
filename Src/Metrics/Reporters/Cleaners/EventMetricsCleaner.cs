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
        private static readonly TimeSpan cleanIntervalBuffer = new TimeSpan(0, 0, 0, 5);
        private static readonly ThreadingTimer timer;
        private static bool timerIsDisposed = false;
        private static ITimer testTimer;
        private static TimeSpan curInterval;

        /// <summary>
        /// Collection of reports and their respective counts associated with the events thay have reported.
        /// </summary>
        public static readonly ReportEventCounts ReportEventCounts;

        /// <summary>
        /// Collection of MetricsRegistry objects related to their contexts.
        /// </summary>
        public static ContextRegistries ContextRegistries;

        static EventMetricsCleaner()
        {
            ReportEventCounts = new ReportEventCounts();
            ContextRegistries = new ContextRegistries();
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
                return ReportEventCounts.Count;
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

            ReportEventCounts.Add(new HashSet<EventCount>());
            return ReportEventCounts.Count - 1;
        }

        /// <summary>
        /// Update the number of EventDetail items reported by the report.
        /// </summary>
        /// <param name="reportIdentifier">The unique identifier for a report.</param>
        /// <param name="events">The events reported by the reporter.</param>
        public static void UpdateTotalReportedEvents(int reportIdentifier, IEnumerable<EventValueSource> events)
        {
            if (reportIdentifier >= 0 && reportIdentifier < ReportEventCounts.Count)
            {
                foreach (var evntSrc in events)
                {
                    var count = evntSrc.Value.Events.Count;
                    var eventMetricId = MetricIdentifier.Calculate(evntSrc.Name, evntSrc.Tags);
                    var report = ReportEventCounts[reportIdentifier];

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
        /// Resets the cleaner interval to be the default interval.
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
            foreach (var eventCount in ReportEventCounts)
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
            foreach (var kvp in ContextRegistries.Values)
            {
                var registry = kvp.Value;
                if (registry != null)
                {
                    if (ReportEventCounts.Count == 0)
                    {
                        registry.ClearEventValues();
                        return;
                    }

                    var eventMetricIdentifiers = new HashSet<string>();
                    var lowestNumEventsReported = new Dictionary<string, int>();
                    foreach (var eventCounts in ReportEventCounts)
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
                        registry.ClearEventValues();
                        return;
                    }

                    foreach (var eventMetricId in eventMetricIdentifiers)
                    {
                        registry.EventValuesRemoveRangeFromStartIndex(eventMetricId, lowestNumEventsReported[eventMetricId]);
                    }
                }
            }
        }
    }

    public class EventCount
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
    public class ReportEventCounts : List<HashSet<EventCount>> { }

    /// <summary>
    /// Collection of MetricsRegistry objects related to their contexts.
    /// </summary>
    public class ContextRegistries
    {
        private readonly Dictionary<string, MetricsRegistry> cntxtRegistry;

        public Dictionary<string, MetricsRegistry> Values
        {
            get
            {
                var tmp = new Dictionary<string, MetricsRegistry>();
                foreach (var kvp in this.cntxtRegistry)
                {
                    tmp.Add(kvp.Key, kvp.Value);
                }
                return tmp;
            }
        }

        public ContextRegistries()
        {
            this.cntxtRegistry = new Dictionary<string, MetricsRegistry>();
        }

        public void Add(string contextName, MetricsRegistry registry)
        {
            if (!this.cntxtRegistry.ContainsKey(contextName))
            {
                this.cntxtRegistry.Add(contextName, registry);
            }
        }

        public void Remove(string contextName)
        {
            if (this.cntxtRegistry.ContainsKey(contextName))
            {
                this.cntxtRegistry.Remove(contextName);
            }
        }

        public void Clear()
        {
            this.cntxtRegistry.Clear();
        }
    }
}
