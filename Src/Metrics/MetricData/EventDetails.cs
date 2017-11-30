using System;
using System.Collections.Generic;

namespace Metrics.MetricData
{
	public class EventDetails
	{
		public EventDetails(List<KeyValuePair<string, object>> fields, DateTime timestamp)
		{
			Fields = fields;
			Timestamp = timestamp;
		}

		public DateTime Timestamp { get; private set; }

		public List<KeyValuePair<string, object>> Fields { get; private set; }
	}
}
