
using System;
using System.Collections.Generic;

namespace Metrics.MetricData
{
    public struct EventValue
    {
        public List<EventDetails> Events { get; }

        public EventValue(List<EventDetails> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            this.Events = events;
        }
    }

    /// <summary>
    /// Combines the value for an event with the defined unit for the value.
    /// </summary>
    public sealed class EventValueSource : MetricValueSource<EventValue>
    {
        public EventValueSource(string name, MetricValueProvider<EventValue> value, MetricTags tags)
            : base(name, value, tags)
        { }
    }
}
