
using System.Collections.Generic;

namespace Metrics.Json
{
    public class JsonMetric
    {
        private KeyValuePair<string,string>[] tags = MetricTags.None.Tags;

        public string Name { get; set; }
        public string Unit { get; set; }
        public KeyValuePair<string,string>[] Tags { get { return this.tags; } set { this.tags = value ?? MetricTags.None.Tags; } }
    }
}
