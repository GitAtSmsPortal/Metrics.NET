using System;
using System.Collections.Generic;
using Metrics.MetricData;

namespace Metrics.Core
{
    public interface EventImplementation : Event, MetricValueProvider<EventValue> { }

    public sealed class EventMetric : EventImplementation
    {
        private readonly List<EventDetails> events = new List<EventDetails>();
        private readonly object locker = new object();

        public EventValue Value
        {
            get
            {
                lock (locker)
                {
                    return new EventValue(this.events);
                }
            }
        }

        public EventValue GetValue(bool resetMetric = false)
        {
            var value = this.Value;
            if (resetMetric)
            {
                this.Reset();
            }
            return value;
        }

        public void Record()
        {
            Record(DateTime.UtcNow);
        }

        public void Record(DateTime timestamp)
        {
            Record(new Dictionary<string, object>(), timestamp);
        }

        public void Record(Dictionary<string, object> fields)
        {
            Record(fields, DateTime.UtcNow);
        }

        public void Record(Dictionary<string, object> fields, DateTime timestamp)
        {
            if (fields.Count == 0)
            {
                fields.Add("timestamp", timestamp.ToString());
            }

            lock (locker)
            {
                this.events.Add(new EventDetails(fields, timestamp));
            }
        }

        public void Reset()
        {
            lock (locker)
            {
                this.events.Clear();
            }
        }
    }
}
