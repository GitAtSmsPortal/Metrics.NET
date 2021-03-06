﻿using Metrics.MetricData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Metrics.Reporters.Cleaners;

namespace Metrics.Core
{
    public sealed class DefaultMetricsRegistry : MetricsRegistry
    {
        private class MetricMetaCatalog<TMetric, TValue, TMetricValue>
            where TValue : MetricValueSource<TMetricValue>
        {
            private readonly object locker = new object();

            private class MetricMeta
            {
                public MetricMeta(TMetric metric, TValue valueUnit)
                {
                    this.Metric = metric;
                    this.Value = valueUnit;
                }

                public string Name
                {
                    get { return this.Value.Name; }
                }

                public TMetric Metric { get; }
                public TValue Value { get; }
            }

            private ConcurrentDictionary<string, MetricMeta> metrics =
                new ConcurrentDictionary<string, MetricMeta>();

            public IEnumerable<TValue> All
            {
                get
                {
                    List<MetricMeta> valuesCopy;
                    lock (this.locker)
                    {
                        valuesCopy = new List<MetricMeta>(this.metrics.Values);
                    }
                    return valuesCopy.OrderBy(m => m.Name).Select(v => v.Value);
                }
            }

            public TMetric GetOrAdd(string name, MetricTags tags, Func<Tuple<TMetric, TValue>> metricProvider)
            {
                var key = MetricIdentifier.Calculate(name, tags.Tags);
                return this.metrics.GetOrAdd(key, n =>
                {
                    var result = metricProvider();
                    return new MetricMeta(result.Item1, result.Item2);
                }).Metric;
            }

            public void Clear()
            {
                lock (this.locker)
                {
                    var values = this.metrics.Values;
                    this.metrics.Clear();
                    foreach (var value in values)
                    {
                        using (value.Metric as IDisposable)
                        {
                        }
                    }
                }
            }

            public void EventValuesRemoveRangeFromStartIndex(string key, int count)
            {
                lock (this.locker)
                {
                    MetricMeta metricMeta;
                    if (this.metrics.TryGetValue(key, out metricMeta))
                    {
                        var src = metricMeta.Metric as Event;
                        if (src != null)
                        {
                            src.RemoveRangeFromStartIndex(count);
                        }
                    }
                }
            }

            public void ClearEventValues()
            {
                lock (this.locker)
                {
                    foreach (var metricMeta in this.metrics.Values)
                    {
                        var src = metricMeta.Metric as Event;
                        if (src != null)
                        {
                            src.Reset();
                        }
                    }
                }
            }    

            public void Reset()
            {
                lock (this.locker)
                {
                    foreach (var metric in this.metrics.Values)
                    {
                        var resetable = metric.Metric as ResetableMetric;
                        resetable?.Reset();
                    }
                }
            }

            public void Remove(string name, MetricTags tags)
            {
                var key = MetricIdentifier.Calculate(name, tags.Tags);
                MetricMeta m;
                this.metrics.TryRemove(key, out m);
                EventMetricsCleaner.RemoveEvent(key);
            }
        }

        private readonly MetricMetaCatalog<MetricValueProvider<double>, GaugeValueSource, double> gauges = new MetricMetaCatalog<MetricValueProvider<double>, GaugeValueSource, double>();
        private readonly MetricMetaCatalog<Counter, CounterValueSource, CounterValue> counters = new MetricMetaCatalog<Counter, CounterValueSource, CounterValue>();
        private readonly MetricMetaCatalog<Meter, MeterValueSource, MeterValue> meters = new MetricMetaCatalog<Meter, MeterValueSource, MeterValue>();
        private readonly MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue> histograms = new MetricMetaCatalog<Histogram, HistogramValueSource, HistogramValue>();
        private readonly MetricMetaCatalog<Timer, TimerValueSource, TimerValue> timers = new MetricMetaCatalog<Timer, TimerValueSource, TimerValue>();
        private readonly MetricMetaCatalog<Event, EventValueSource, EventValue> events = new MetricMetaCatalog<Event, EventValueSource, EventValue>();
        
        public DefaultMetricsRegistry()
        {
            this.DataProvider = new DefaultRegistryDataProvider(
                () => this.gauges.All, 
                () => this.counters.All, 
                () => this.meters.All, 
                () => this.histograms.All, 
                () => this.timers.All,
                () => this.events.All);
        }

        public void EventValuesRemoveRangeFromStartIndex(string key, int count)
        {
            events.EventValuesRemoveRangeFromStartIndex(key, count);
        }

        public void ClearEventValues()
        {
            events.ClearEventValues();
        }

        public RegistryDataProvider DataProvider { get; }

        public void Gauge(string name, Func<MetricValueProvider<double>> valueProvider, Unit unit, MetricTags tags)
        {
            name = name + ".gauge";
            this.gauges.GetOrAdd(name, tags, () =>
            {
                MetricValueProvider<double> gauge = valueProvider();
                return Tuple.Create(gauge, new GaugeValueSource(name, gauge, unit, tags));
            });
        }

        public Counter Counter<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : CounterImplementation
        {
            name = name + ".counter";
            return this.counters.GetOrAdd(name, tags, () =>
            {
                T counter = builder();
                return Tuple.Create((Counter)counter, new CounterValueSource(name, counter, unit, tags));
            });
        }

        public Meter Meter<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, MetricTags tags)
            where T : MeterImplementation
        {
            name = name + ".meter";
            return this.meters.GetOrAdd(name, tags, () =>
            {
                T meter = builder();
                return Tuple.Create((Meter)meter, new MeterValueSource(name, meter, unit, rateUnit, tags));
            });
        }

        public Histogram Histogram<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : HistogramImplementation
        {
            name = name + ".histogram";
            return this.histograms.GetOrAdd(name, tags, () =>
            {
                T histogram = builder();
                return Tuple.Create((Histogram)histogram, new HistogramValueSource(name, histogram, unit, tags));
            });
        }

        public Timer Timer<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
            where T : TimerImplementation
        {
            name = name + ".timer";
            return this.timers.GetOrAdd(name, tags, () =>
            {
                T timer = builder();
                return Tuple.Create((Timer)timer, new TimerValueSource(name, timer, unit, rateUnit, durationUnit, tags));
            });
        }

        public Event Event<T>(string name, Func<T> builder, MetricTags tags)
            where T : EventImplementation
        {
            name = name + ".event";
            return this.events.GetOrAdd(name, tags, () =>
            {
                T evnt = builder();
                return Tuple.Create((Event)evnt, new EventValueSource(name, evnt, tags));
            });
        }

        public void ClearAllMetrics()
        {
            this.gauges.Clear();
            this.counters.Clear();
            this.meters.Clear();
            this.histograms.Clear();
            this.timers.Clear();
            this.events.Clear();
        }

        public void ResetMetricsValues()
        {
            this.gauges.Reset();
            this.counters.Reset();
            this.meters.Reset();
            this.histograms.Reset();
            this.timers.Reset();
            this.events.Reset();
        }

        public void DeregisterGauge(string name, MetricTags tags)
        {
            name = name + ".gauge";
            this.gauges.Remove(name, tags);
        }

        public void DeregisterMeter(string name, MetricTags tags)
        {
            name = name + ".meter";
            this.meters.Remove(name, tags);
        }

        public void DeregisterCounter(string name, MetricTags tags)
        {
            name = name + ".counter";
            this.counters.Remove(name, tags);
        }

        public void DeregisterHistogram(string name, MetricTags tags)
        {
            name = name + ".histogram";
            this.histograms.Remove(name, tags);
        }

        public void DeregisterTimer(string name, MetricTags tags)
        {
            name = name + ".timer";
            this.timers.Remove(name, tags);
        }

        public void DeregisterEvent(string name, MetricTags tags)
        {
            name = name + ".event";
            this.events.Remove(name, tags);
        }
    }
}
