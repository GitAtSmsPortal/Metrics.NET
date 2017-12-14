
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Metrics.Core;
using Metrics.MetricData;
using Metrics.Reporters.Cleaners;
using Metrics.Utils;
using Xunit;

namespace Metrics.Tests.Reporting
{
    public class EventMetricsCleanerTest
    {
        private const string MetricName = "test";
        private const string MetricNameType = "test.event";
        private readonly string metricNameTypeTags = MetricIdentifier.Calculate("test.event");

        public void ResetCleaner()
        {
            EventMetricsCleaner.ContextRegistries.Clear();
            EventMetricsCleaner.ReportEventCounts.Clear();
            EventMetricsCleaner.ResetInterval();
        }

        [Fact]
        public void DefaultIntervalIsGreaterThanZero()
        {
            ResetCleaner();

            EventMetricsCleaner.CurrentIntervalSeconds.Should().BeGreaterThan(0);
        }

        [Fact]
        public void RegisterReport_IncrementsReportCount()
        {
            ResetCleaner();

            EventMetricsCleaner.RegisterReport(new TimeSpan(0));

            EventMetricsCleaner.TotalReports.Should().Be(1);
        }

        [Fact]
        public void RegisterReport_UpdatesCleanerInterval()
        {
            ResetCleaner();

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(5);

            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 60));

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(65);
        }

        [Fact]
        public void RegisterMultipleReports_UpdatesCleanerInterval_UsingLargestReportInverval()
        {
            ResetCleaner();

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(5);

            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 3));
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 8));
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 2));

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(13);
        }

        [Fact]
        public void Update_RegistersEventAssociatedWithReport()
        {
            ResetCleaner();

            var registry = new DefaultMetricsRegistry();
            registry.Event(MetricName, () => { return new EventMetric(); }, MetricTags.None);
            var reportIndex = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));

            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex, registry.DataProvider.Events);

            GetReportsEventCount(reportIndex).Should().Be(1);
        }

        [Fact]
        public void RemoveEvent_RemovesEventFromAllReports()
        {
            ResetCleaner();

            var registry = new DefaultMetricsRegistry();
            registry.Event(MetricName, () => { return new EventMetric(); }, MetricTags.None);
            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry.DataProvider.Events);

            EventMetricsCleaner.RemoveEvent(metricNameTypeTags);

            GetReportsEventCount(reportIndex1).Should().Be(0);
            GetReportsEventCount(reportIndex2).Should().Be(0);
        }

        [Fact]
        public void Clean_OnlyRemovesEventDetailItems_ThatHaveBeenReportedByAllReports()
        {
            ResetCleaner();

            const string context = "";
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(context, registry);
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);
            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry.DataProvider.Events);

            GetReportsReportedEventDetailCount(reportIndex1, metricNameTypeTags).Should().Be(1);
            GetReportsReportedEventDetailCount(reportIndex2, metricNameTypeTags).Should().Be(2);

            timer.OnTimerCallback();

            var registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(1);
        }

        [Fact]
        public void Clean_IgnoresReportsThatHaveNotReportedAnyEvents()
        {
            ResetCleaner();

            const string context = "";
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(context, registry);
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);

            GetReportsReportedEventDetailCount(reportIndex1, metricNameTypeTags).Should().Be(1);
            GetReportsReportedEventDetailCount(reportIndex2, metricNameTypeTags).Should().Be(0);

            timer.OnTimerCallback();
            
            var registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(0);
        }

        [Fact]
        public void Clean_WithNoReportsRegistered_RemovesAllEvents()
        {
            ResetCleaner();

            const string context = "";
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(context, registry);
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

            metric.Record();
            metric.Record();
            metric.Record();
            var registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(3);

            timer.OnTimerCallback();
            
            registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(0);
        }

        [Fact]
        public void Clean_WithAllReportsFilteringOutEvents_RemovesAllEvents()
        {
            ResetCleaner();

            const string context = "";
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(context, registry);
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);
            metric.Record();
            metric.Record();
            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, new List<EventValueSource>());
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, new List<EventValueSource>());

            GetReportsReportedEventDetailCount(reportIndex1, MetricNameType).Should().Be(0);
            GetReportsReportedEventDetailCount(reportIndex2, MetricNameType).Should().Be(0);
            var registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(3);

            timer.OnTimerCallback();

            registryCounts = GetRegistryEventDetailCounts(MetricNameType);
            registryCounts[context].Should().Be(0);
        }

        [Fact]
        public void Clean_WithMultipleRegistriesInSeparateContexts_RemovesEvents()
        {
            ResetCleaner();

            const string ctx1 = "ctx1";
            var registry1 = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(ctx1, registry1);

            const string ctx2 = "ctx2";
            var registry2 = new DefaultMetricsRegistry();
            EventMetricsCleaner.ContextRegistries.Add(ctx2, registry2);

            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric1 = new EventMetric();
            registry1.Event("test1", () => { return metric1; }, MetricTags.None);
            metric1.Record();
            metric1.Record();
            metric1.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry1.DataProvider.Events);

            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric2 = new EventMetric();
            registry2.Event("test2", () => { return metric2; }, MetricTags.None);
            metric2.Record();
            metric2.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry2.DataProvider.Events);
            
            var registryCounts = GetRegistryEventDetailCounts("test1.event");
            registryCounts[ctx1].Should().Be(3);
            registryCounts[ctx2].Should().Be(0);
            registryCounts = GetRegistryEventDetailCounts("test2.event");
            registryCounts[ctx1].Should().Be(0);
            registryCounts[ctx2].Should().Be(2);

            timer.OnTimerCallback();

            registryCounts = GetRegistryEventDetailCounts("test1.event");
            registryCounts[ctx1].Should().Be(0);
            registryCounts[ctx2].Should().Be(0);
            registryCounts = GetRegistryEventDetailCounts("test2.event");
            registryCounts[ctx1].Should().Be(0);
            registryCounts[ctx2].Should().Be(0);
        }

        private static Dictionary<string, int> GetRegistryEventDetailCounts(string eventMetricIdentifier)
        {
            var registryCounts = new Dictionary<string, int>();
            foreach (var kvp in EventMetricsCleaner.ContextRegistries.Values)
            {
                var registry = kvp.Value;
                var eventCount = registry.DataProvider.Events.FirstOrDefault(e => e.Name == eventMetricIdentifier);
                registryCounts.Add(kvp.Key, eventCount?.Value.EventsCopy.Count ?? 0);
            }
            return registryCounts;
        }

        private static int GetReportsEventCount(int reportIdentifier)
        {
            return EventMetricsCleaner.ReportEventCounts[reportIdentifier].Count;
        }

        private static int GetReportsReportedEventDetailCount(int reportIdentifier, string eventMetricIdentifier)
        {
            var eventCount = EventMetricsCleaner.ReportEventCounts[reportIdentifier].FirstOrDefault(e => e.EventMetricIdentifier == eventMetricIdentifier);
            return eventCount?.TotalEventsReported ?? 0;
        }

        public class MockTimer : ITimer
        {
            public event TimerEventHandler Tick;

            public void Change(TimeSpan dueTime, TimeSpan period)
            {
                // Do nothing
            }

            public void Dispose()
            {
                // Do nothing
            }

            public void OnTimerCallback()
            {
                Tick?.Invoke(this, new EventArgs());
            }
        }
    }
}
