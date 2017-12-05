using System.Collections.Generic;
using System.Linq;

namespace Metrics
{
    /// <summary>
    /// Collection of tags that can be attached to a metric.
    /// </summary>
    public struct MetricTags : Utils.IHideObjectMembers
    {
        private static readonly KeyValuePair<string,string>[] empty = Enumerable.Empty<KeyValuePair<string, string>>().ToArray();

        public static readonly MetricTags None = new MetricTags(Enumerable.Empty<KeyValuePair<string, string>>());

        private readonly KeyValuePair<string, string>[] tags;

        public MetricTags(params KeyValuePair<string, string>[] tags)
        {
            this.tags = tags.ToArray();
        }

        public MetricTags(IEnumerable<KeyValuePair<string, string>> tags) : this(ToTags(tags).ToArray())
        { }

        public KeyValuePair<string, string>[] Tags
        {
            get
            {
                return this.tags ?? empty;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> ToTags(IEnumerable<KeyValuePair<string, string>> tags)
        {
            return tags
                .Where(t => !string.IsNullOrEmpty(t.Value) && !string.IsNullOrEmpty(t.Key))
                .Select(tempTag => new KeyValuePair<string, string>(tempTag.Key.ToLowerInvariant(), tempTag.Value.ToLowerInvariant()));
        }

        public static implicit operator MetricTags(KeyValuePair<string,string> tags)
        {
            return new MetricTags(tags);
        }

        public static implicit operator MetricTags(KeyValuePair<string,string>[] tags)
        {
            return new MetricTags(tags);
        }
    }
}
