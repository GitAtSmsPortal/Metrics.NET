﻿using Metrics.MetricData;
using Metrics.Sampling;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Metrics.Reporters.Cleaners;

namespace Metrics.Core
{
    public abstract class BaseMetricsContext : MetricsContext, AdvancedMetricsContext
    {
        private readonly ConcurrentDictionary<string, MetricsContext> childContexts = new ConcurrentDictionary<string, MetricsContext>();

        private MetricsRegistry registry;
        private MetricsBuilder metricsBuilder;

        private bool isDisabled;

        protected BaseMetricsContext(string context, MetricsRegistry registry, MetricsBuilder metricsBuilder, Func<DateTime> timestampProvider)
        {
            this.registry = registry;
            EventMetricsCleaner.ContextRegistries.Add(context, registry);
            this.metricsBuilder = metricsBuilder;
            this.DataProvider = new DefaultDataProvider(context, timestampProvider, this.registry.DataProvider, () => this.childContexts.Values.Select(c => c.DataProvider));
        }

        protected abstract MetricsContext CreateChildContextInstance(string contextName);

        public AdvancedMetricsContext Advanced => this;

        public event EventHandler ContextShuttingDown;
        public event EventHandler ContextDisabled;

        public MetricsDataProvider DataProvider { get; }

        public MetricsContext Context(string contextName)
        {
            return this.Context(contextName, c => CreateChildContextInstance(contextName));
        }

        public MetricsContext Context(string contextName, Func<string, MetricsContext> contextCreator)
        {
            if (this.isDisabled)
            {
                return this;
            }

            if (string.IsNullOrEmpty(contextName))
            {
                return this;
            }

            return this.childContexts.GetOrAdd(contextName, contextCreator);
        }

        public bool AttachContext(string contextName, MetricsContext context)
        {
            if (this.isDisabled)
            {
                return true;
            }

            if (string.IsNullOrEmpty(contextName))
            {
                throw new ArgumentException("Context name can't be null or empty for attached contexts");
            }
            var attached = this.childContexts.GetOrAdd(contextName, context);
            return ReferenceEquals(attached, context);
        }

        public void ShutdownContext(string contextName)
        {
            if (string.IsNullOrEmpty(contextName))
            {
                throw new ArgumentException("contextName must not be null or empty", contextName);
            }

            EventMetricsCleaner.ContextRegistries.Remove(contextName);

            MetricsContext context;
            if (this.childContexts.TryRemove(contextName, out context))
            {
                using (context) { }
            }
        }

        public void PerformanceCounter(string name, string counterCategory, string counterName, string counterInstance, Unit unit, MetricTags tags)
        {
            this.Gauge(name, () => this.metricsBuilder.BuildPerformanceCounter(name, unit, counterCategory, counterName, counterInstance), unit, tags);
        }

        public void Gauge(string name, Func<double> valueProvider, Unit unit, MetricTags tags)
        {
            this.Gauge(name, () => this.metricsBuilder.BuildGauge(name, unit, valueProvider), unit, tags);
        }

        public void Gauge(string name, Func<MetricValueProvider<double>> valueProvider, Unit unit, MetricTags tags)
        {
            this.registry.Gauge(name, valueProvider, unit, tags);
        }

        public Counter Counter(string name, Unit unit, MetricTags tags)
        {
            return this.Counter(name, unit, () => this.metricsBuilder.BuildCounter(name, unit), tags);
        }

        public Counter Counter<T>(string name, Unit unit, Func<T> builder, MetricTags tags)
            where T : CounterImplementation
        {
            return this.registry.Counter(name, builder, unit, tags);
        }

        public Meter Meter(string name, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            return this.Meter(name, unit, () => this.metricsBuilder.BuildMeter(name, unit, rateUnit), rateUnit, tags);
        }

        public Meter Meter<T>(string name, Unit unit, Func<T> builder, TimeUnit rateUnit, MetricTags tags)
           where T : MeterImplementation
        {
            return this.registry.Meter(name, builder, unit, rateUnit, tags);
        }

        public Histogram Histogram(string name, Unit unit, SamplingType samplingType, MetricTags tags)
        {
            return this.Histogram(name, unit, () => this.metricsBuilder.BuildHistogram(name, unit, samplingType), tags);
        }

        public Histogram Histogram<T>(string name, Unit unit, Func<T> builder, MetricTags tags)
            where T : HistogramImplementation
        {
            return this.registry.Histogram(name, builder, unit, tags);
        }

        public Histogram Histogram(string name, Unit unit, Func<Reservoir> builder, MetricTags tags)
        {
            return Histogram(name, unit, () => this.metricsBuilder.BuildHistogram(name, unit, builder()), tags);
        }

        public Timer Timer(string name, Unit unit, SamplingType samplingType, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return this.registry.Timer(name, () => this.metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, samplingType), unit, rateUnit, durationUnit, tags);
        }

        public Timer Timer<T>(string name, Unit unit, Func<T> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
            where T : TimerImplementation
        {
            return this.registry.Timer(name, builder, unit, rateUnit, durationUnit, tags);
        }

        public Timer Timer(string name, Unit unit, Func<HistogramImplementation> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return this.Timer(name, unit, () => this.metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, builder()), rateUnit, durationUnit, tags);
        }

        public Timer Timer(string name, Unit unit, Func<Reservoir> builder, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            return this.Timer(name, unit, () => this.metricsBuilder.BuildTimer(name, unit, rateUnit, durationUnit, builder()), rateUnit, durationUnit, tags);
        }

        public Event Event(string name, MetricTags tags)
        {
            return this.Event(name, () => this.metricsBuilder.BuildEvent(name), tags);
        }

        public Event Event<T>(string name, Func<T> builder, MetricTags tags)
            where T : EventImplementation
        {
            return this.registry.Event(name, builder, tags);
        }

        public void CompletelyDisableMetrics()
        {
            if (this.isDisabled)
            {
                return;
            }

            this.isDisabled = true;

            var oldRegistry = this.registry;
            this.registry = new NullMetricsRegistry();
            oldRegistry.ClearAllMetrics();
            using (oldRegistry as IDisposable) { }

            ForAllChildContexts(c => c.Advanced.CompletelyDisableMetrics());

            this.ContextShuttingDown?.Invoke(this, EventArgs.Empty);
            this.ContextDisabled?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.isDisabled)
                {
                    this.ContextShuttingDown?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void ResetMetricsValues()
        {
            this.registry.ResetMetricsValues();
            ForAllChildContexts(c => c.Advanced.ResetMetricsValues());
        }

        public void WithCustomMetricsBuilder(MetricsBuilder metricsBuilder)
        {
            this.metricsBuilder = metricsBuilder;
            ForAllChildContexts(c => c.Advanced.WithCustomMetricsBuilder(metricsBuilder));
        }

        private void ForAllChildContexts(Action<MetricsContext> action)
        {
            foreach (var context in this.childContexts.Values)
            {
                action(context);
            }
        }

        public void DeregisterGauge(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterGauge(name, tags);
        }

        public void DeregisterMeter(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterMeter(name, tags);
        }

        public void DeregisterCounter(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterCounter(name, tags);
        }

        public void DeregisterHistogram(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterHistogram(name, tags);
        }

        public void DeregisterTimer(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterTimer(name, tags);
        }

        public void DeregisterEvent(string name, MetricTags tags = default(MetricTags))
        {
            this.registry.DeregisterEvent(name, tags);
        }
    }
}
