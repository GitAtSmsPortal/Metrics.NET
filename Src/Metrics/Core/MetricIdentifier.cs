
namespace Metrics.Core
{
    public static class MetricIdentifier
    {
        public static string Calculate(string name, string[] tags)
        {
            return MetricsConfig.UseTagIdentifiers ? name + GetHashCode(tags) : name;
        }

        private static int GetHashCode(string[] tags)
        {
            const int prime = 31;
            var hash = 17;
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    hash = (hash * prime) + tag.GetHashCode();
                }
            }
            return hash;
        }
    }
}
