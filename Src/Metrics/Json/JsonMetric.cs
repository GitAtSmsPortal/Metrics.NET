
using System.Collections.Generic;

namespace Metrics.Json
{
    public class JsonMetric
    {
        private Dictionary<string,string> tags = MetricTags.None.Tags;

        public string Name { get; set; }
        public string Unit { get; set; }
        public Dictionary<string,string> Tags { get { return this.tags; } set { this.tags = value ?? MetricTags.None.Tags; } }
    }
}
