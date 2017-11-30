using System;
using System.Collections.Generic;
using Metrics.MetricData;

namespace Metrics.Core
{
	public interface EventImplementation : Event, MetricValueProvider<EventValue> { }

	public sealed class EventMetric : EventImplementation
	{
		private readonly List<EventDetails> events = new List<EventDetails>();

		public EventValue Value
		{
			get
			{
				return new EventValue(this.events);
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
			Record(new List<KeyValuePair<string, object>>(), timestamp);
		}

		public void Record(List<KeyValuePair<string, object>> fields)
		{
			Record(fields, DateTime.UtcNow);
		}

		public void Record(List<KeyValuePair<string, object>> fields, DateTime timestamp)
		{
			var defaultFields = new List<KeyValuePair<string, object>>();
			defaultFields.Add(new KeyValuePair<string, object>("timestamp", timestamp));

			fields = fields.Count == 0 ? defaultFields : fields;
			this.events.Add(new EventDetails(fields, timestamp));
		}

		public void Reset()
		{
			this.events.Clear();
		}
	}
}
