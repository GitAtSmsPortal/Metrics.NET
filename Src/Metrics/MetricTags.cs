using System.Collections.Generic;
using System.Linq;

namespace Metrics
{
    /// <summary>
    /// Collection of tags that can be attached to a metric.
    /// </summary>
    public struct MetricTags : Utils.IHideObjectMembers
    {
        private static readonly Dictionary<string,string> empty = new Dictionary<string, string>();

        public static readonly MetricTags None = new MetricTags();

        private readonly Dictionary<string,string> tags;

        public MetricTags(Dictionary<string, string> tags)
        {
            this.tags = CleanTags(tags);
        }

        public Dictionary<string, string> Tags
        {
            get
            {
                return this.tags ?? empty;
            }
        }

        private static Dictionary<string, string> CleanTags(Dictionary<string, string> tags)
        {
            return tags
                .Where(t => !string.IsNullOrEmpty(t.Value) && !string.IsNullOrEmpty(t.Key))
                .ToDictionary(kvp=>kvp.Key.ToLowerInvariant(), kvp=>kvp.Value.ToLowerInvariant());
        }

        public static implicit operator MetricTags(Dictionary<string,string> tags)
        {
            return new MetricTags(tags);
        }
    }
}
