using System;
using System.Threading;
using Metrics.MetricData;
using Metrics.Reporters.Cleaners;
using Metrics.Utils;

namespace Metrics.Reporters
{
	public sealed class ScheduledReporter : IDisposable
    {
        private readonly Scheduler scheduler;
        private readonly MetricsReport report;
        private readonly MetricsDataProvider metricsDataProvider;
		private readonly Func<HealthStatus> healthStatus;
		private readonly int reportIdentifier;

		public ScheduledReporter(MetricsReport reporter, MetricsDataProvider metricsDataProvider, Func<HealthStatus> healthStatus, TimeSpan interval)
            : this(reporter, metricsDataProvider, healthStatus, interval, new ActionScheduler()) { }

        public ScheduledReporter(MetricsReport report, MetricsDataProvider metricsDataProvider, Func<HealthStatus> healthStatus, TimeSpan interval, Scheduler scheduler)
        {
            this.report = report;
            this.metricsDataProvider = metricsDataProvider;
            this.healthStatus = healthStatus;
            this.scheduler = scheduler;
            this.scheduler.Start(interval, t => RunReport(t));
			this.reportIdentifier = EventMetricsCleaner.RegisterReport(interval);
		}

        private void RunReport(CancellationToken token)
		{
			var data = this.metricsDataProvider.CurrentMetricsData;
			this.report.RunReport(data, this.healthStatus, token);
			EventMetricsCleaner.UpdateTotalReportedEvents(this.reportIdentifier, data.Events);
		}

        public void Dispose()
        {
            using (this.scheduler) { }
            using (this.report as IDisposable) { }
        }
    }
}
