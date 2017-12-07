
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
		private const string MetricName = "test";
		private const string MetricNameType = "test.event";
		private readonly string metricNameTypeTags = MetricIdentifier.Calculate("test.event");

		public void ResetCleaner()
        {
            EventMetricsCleaner.Clear();
            EventMetricsCleaner.ResetInterval();
        }

        [Fact]
        public void StartsWithZeroReports()
        {
            EventMetricsCleaner.TotalReports.Should().Be(0);
        }

        [Fact]
        public void DefaultIntervalIsGreaterThanZero()
        {
            EventMetricsCleaner.CurrentIntervalSeconds.Should().BeGreaterThan(0);

            ResetCleaner();
        }

        [Fact]
        public void RegisterReport_IncrementsReportCount()
        {
            EventMetricsCleaner.RegisterReport(new TimeSpan(0));

            EventMetricsCleaner.TotalReports.Should().Be(1);

            ResetCleaner();
        }

        [Fact]
        public void RegisterReport_UpdatesCleanerInterval()
        {
            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(5);

            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 60));

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(65);

            ResetCleaner();
        }

        [Fact]
        public void RegisterMultipleReports_UpdatesCleanerInterval_UsingLargestReportInverval()
        {
            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(5);

            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 3));
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 8));
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 2));

            EventMetricsCleaner.CurrentIntervalSeconds.Should().Be(13);

            ResetCleaner();
        }

        [Fact]
        public void Update_RegistersEventAssociatedWithReport()
        {
            var registry = new DefaultMetricsRegistry();
            registry.Event(MetricName, () => { return new EventMetric(); }, MetricTags.None);
			var reportIndex = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));

            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex, registry.DataProvider.Events);

            EventMetricsCleaner.GetReportsEventCount(reportIndex).Should().Be(1);

            ResetCleaner();
        }

        [Fact]
        public void RemoveEvent_RemovesEventFromAllReports()
        {
            var registry = new DefaultMetricsRegistry();
            registry.Event(MetricName, () => { return new EventMetric(); }, MetricTags.None);
			var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex2, registry.DataProvider.Events);

            EventMetricsCleaner.RemoveEvent(metricNameTypeTags);

            EventMetricsCleaner.GetReportsEventCount(reportIndex1).Should().Be(0);
            EventMetricsCleaner.GetReportsEventCount(reportIndex2).Should().Be(0);

            ResetCleaner();
        }

        [Fact]
        public void Clear_RemovesAllReports()
        {
            EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));

            EventMetricsCleaner.Clear();

            EventMetricsCleaner.TotalReports.Should().Be(0);

            ResetCleaner();
        }

        [Fact]
        public void Clean_OnlyRemovesEventDetailItems_ThatHaveBeenReportedByAllReports()
        {
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
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

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, metricNameTypeTags).Should().Be(1);
            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex2, metricNameTypeTags).Should().Be(2);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(1);

            ResetCleaner();
        }

        [Fact]
        public void Clean_IgnoresReportsThatHaveNotReportedAnyEvents()
        {
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var reportIndex2 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, registry.DataProvider.Events);

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, metricNameTypeTags).Should().Be(1);
            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex2, metricNameTypeTags).Should().Be(0);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(0);

            ResetCleaner();
        }

        [Fact]
        public void Clean_WithNoReportsRegistered_RemovesAllEvents()
        {
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

			metric.Record();
            metric.Record();
            metric.Record();
            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(3);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(0);

            ResetCleaner();
        }

        [Fact]
        public void Clean_WithAllReportsFilteringOutEvents_RemovesAllEvents()
        {
            var registry = new DefaultMetricsRegistry();
            EventMetricsCleaner.Registry = registry;
            var timer = new MockTimer();
            EventMetricsCleaner.EnableTestTimer(timer);

            var reportIndex1 = EventMetricsCleaner.RegisterReport(new TimeSpan(0, 0, 0, 60));
            var metric = new EventMetric();
            registry.Event(MetricName, () => { return metric; }, MetricTags.None);

			metric.Record();
            metric.Record();
            metric.Record();
            EventMetricsCleaner.UpdateTotalReportedEvents(reportIndex1, new List<EventValueSource>());

            EventMetricsCleaner.GetReportsReportedEventDetailCount(reportIndex1, MetricNameType).Should().Be(0);
            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(3);

            timer.OnTimerCallback();

            EventMetricsCleaner.GetEventDetailCount(MetricNameType).Should().Be(0);

            ResetCleaner();
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
