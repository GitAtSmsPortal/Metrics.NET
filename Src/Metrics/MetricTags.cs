using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics
{
    /// <summary>
    /// Collection of tags that can be attached to a metric.
    /// </summary>
    public struct MetricTags : Utils.IHideObjectMembers
    {
        private static readonly string[] empty = new string[0];

        public static readonly MetricTags None = new MetricTags(Enumerable.Empty<string>());

        private readonly string[] tags;

        public MetricTags(params string[] tags)
        {
            this.tags = tags.ToArray();
        }

        public MetricTags(IEnumerable<string> tags)
            : this(tags.ToArray())
        { }

        public MetricTags(string commaSeparatedTags)
            : this(ToTags(commaSeparatedTags))
        { }

        public string[] Tags
        {
            get
            {
                return tags ?? empty;
            }
        }

        private static IEnumerable<string> ToTags(string commaSeparatedTags)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedTags))
            {
                return Enumerable.Empty<string>();
            }

            return commaSeparatedTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
        }

        public static implicit operator MetricTags(string commaSeparatedTags)
        {
            return new MetricTags(commaSeparatedTags);
        }

        public static implicit operator MetricTags(string[] tags)
        {
            return new MetricTags(tags);
		}

		public override int GetHashCode()
		{
			const int prime = 31;
			var hash = 17;
			foreach (var tag in this.Tags)
			{
				if (!string.IsNullOrWhiteSpace(tag))
				{
					hash = (hash*prime) + tag.GetHashCode();
				}
			}
			return hash;
		}
	}
}
