
using System.Collections.Generic;

namespace Metrics.Core
{
    public static class MetricIdentifier
	{
		public static string Calculate(string name, MetricTags tags = default(MetricTags))
		{
			return name + GetHashCode(tags.Tags);
		}

		private static int GetHashCode(KeyValuePair<string, string>[] tags)
        {
            const int prime = 31;
            var hash = 17;
            foreach (var tag in tags)
            {
                hash = hash * prime + tag.GetHashCode();
            }
            return hash;
        }
    }
}
