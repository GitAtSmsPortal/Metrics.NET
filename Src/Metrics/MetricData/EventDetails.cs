using System;
using System.Collections.Generic;

namespace Metrics.MetricData
{
    public class EventDetails
    {
        public EventDetails(Dictionary<string, string> fields, DateTime timestamp)
        {
            Fields = fields;
            Timestamp = timestamp;
        }

        public DateTime Timestamp { get; private set; }

        public Dictionary<string, string> Fields { get; private set; }
    }
}
