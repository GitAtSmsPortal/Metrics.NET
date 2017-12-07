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
            var tempTags = new Dictionary<string,string>();
            tempTags.Add("tag","value");
            MetricTags tags = tempTags;

            tags.Tags.Should().Equal(tempTags);
        }
    }
}
