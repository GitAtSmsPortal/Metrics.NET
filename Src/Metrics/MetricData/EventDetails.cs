using System;
using System.Collections.Generic;

namespace Metrics.MetricData
{
    public class EventDetails
    {
        public EventDetails(Dictionary<string, object> fields, DateTime timestamp)
        {
            Fields = fields;
            Timestamp = timestamp;
        }

        public DateTime Timestamp { get; private set; }

        public Dictionary<string, object> Fields { get; private set; }
    }
}
