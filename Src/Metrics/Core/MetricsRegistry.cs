
using System;
using System.Collections.Generic;
using Metrics.MetricData;
namespace Metrics.Core
{
    public interface RegistryDataProvider
    {
        IEnumerable<GaugeValueSource> Gauges { get; }
        IEnumerable<CounterValueSource> Counters { get; }
        IEnumerable<MeterValueSource> Meters { get; }
        IEnumerable<HistogramValueSource> Histograms { get; }
        IEnumerable<TimerValueSource> Timers { get; }
        IEnumerable<EventValueSource> Events { get; }
    }

    public interface MetricsRegistry
    {
        RegistryDataProvider DataProvider { get; }

        void Gauge(string name, Func<MetricValueProvider<double>> valueProvider, Unit unit, MetricTags tags);

        Counter Counter<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : CounterImplementation;

        Meter Meter<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, MetricTags tags)
            where T : MeterImplementation;

        Histogram Histogram<T>(string name, Func<T> builder, Unit unit, MetricTags tags)
            where T : HistogramImplementation;

        Timer Timer<T>(string name, Func<T> builder, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
            where T : TimerImplementation;

        Event Event<T>(string name, Func<T> builder, MetricTags tags)
            where T : EventImplementation;

        void ClearAllMetrics();

        void ResetMetricsValues();

        void EventValuesRemoveRange(string key, int startIndex, int count);

        void ClearEventValues();

        void DeregisterGauge(string name, MetricTags tags);

        void DeregisterMeter(string name, MetricTags tags);

        void DeregisterCounter(string name, MetricTags tags);

        void DeregisterHistogram(string name, MetricTags tags);

        void DeregisterTimer(string name, MetricTags tags);

        void DeregisterEvent(string name, MetricTags tags);
    }
}
