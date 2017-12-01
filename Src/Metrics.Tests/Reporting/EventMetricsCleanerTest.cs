
using System;
using System.Collections.Generic;
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
        [Fact]
        public void StartsWithZeroReports()
        {
            EventMetricsCleaner.Clear();

            EventMetricsCleaner.TotalReports.Should().Be(0);
        }

        [Fact]
        public void DefaultIntervalIsGreaterThanZero()
        {
            EventMetricsCleaner.Clear();

            EventMetricsCleaner.CurrentIntervalSeconds.Should().BeGreaterThan(0);
        }

        [Fact]
        public void RegisterReport_IncrementsReportCount()
        {
            EventMetricsCleaner.Clear();

            EventMetricsCleaner.RegisterReport(new TimeSpan(0));

            EventMetricsCleaner.TotalReports.Should().Be(1);
        }

        [Fact]
        public void RegisterReport_WithGreaterTimespan_UpdatesIntervalToBeGreaterThanReportInterval()
        {
            EventMetricsCleaner.Clear();

            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 60));

            EventMetricsCleaner.CurrentIntervalSeconds.Should().BeGreaterThan(60);
        }

        [Fact]
        public void Update_RegistersEventAssociatedWithReport()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            registry.Event("test", () => { return new EventMetric(); }, MetricTags.None);
            var reportIndex = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));

            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex, registry.DataProvider.Events);

            EventMetricsCleaner.GetReportsEventCount(reportIndex).Should().Be(1);
        }

        [Fact]
        public void RemoveEvent_RemovesEventFromAllReports()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            registry.Event("test", () => { return new EventMetric(); }, MetricTags.None);
            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry.DataProvider.Events);

            EventMetricsCleaner.RemoveEvent("test");

            EventMetricsCleaner.GetReportsEventCount(reportIndex1).Should().Be(0);
            EventMetricsCleaner.GetReportsEventCount(reportIndex2).Should().Be(0);
        }

        [Fact]
        public void Clear_RemovesAllReports()
        {
            EventMetricsCleaner.Clear();
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));

            EventMetricsCleaner.Clear();

            EventMetricsCleaner.TotalReports.Should().Be(0);
        }

        [Fact]
        public void Clean_OnlyRemovesEventDetailItems_ThatHaveBeenReportedByAllReports()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event("test", () => { return metric; }, MetricTags.None);

            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);
            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry.DataProvider.Events);

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, "test").Should().Be(1);
            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex2, "test").Should().Be(2);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(1);
        }

        [Fact]
        public void Clean_IgnoresReportsThatHaveNotReportedAnyEvents()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event("test", () => { return metric; }, MetricTags.None);

            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, "test").Should().Be(1);
            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex2, "test").Should().Be(0);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(0);
        }

        [Fact]
        public void Clean_WithNoReportsRegistered_RemovesAllEvents()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var metric = new EventMetric();
            registry.Event("test", () => { return metric; }, MetricTags.None);

            metric.Record();
            metric.Record();
            metric.Record();
            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(3);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(0);
        }

        [Fact]
        public void Clean_WithAllReportsFilteringOutEvents_RemovesAllEvents()
        {
            EventMetricsCleaner.Clear();
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event("test", () => { return metric; }, MetricTags.None);

            metric.Record();
            metric.Record();
            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, new List<EventValueSource>());

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, "test").Should().Be(0);
            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(3);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount("test").Should().Be(0);
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
