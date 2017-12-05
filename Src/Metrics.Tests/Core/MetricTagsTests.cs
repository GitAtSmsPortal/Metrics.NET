using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Metrics.Tests.Core
{
    public class MetricTagsTests
    {
        [Fact]
        public void MetricTags_CanUseDefaultValue()
        {
            var tags = default(MetricTags);
            tags.Tags.Should().NotBeNull();
            tags.Tags.Should().BeEmpty();
        }

        [Fact]
        public void MetricTags_CanCreateFromKeyValuePairs()
        {
            MetricTags tags = new[]{new KeyValuePair<string, string>("tag","value") };
            tags.Tags.Should().Equal(new KeyValuePair<string, string>("tag", "value"));

            tags = new MetricTags(new KeyValuePair<string, string>("tag","value"));
            tags.Tags.Should().Equal(new KeyValuePair<string, string>("tag", "value"));
        }
    }
}
