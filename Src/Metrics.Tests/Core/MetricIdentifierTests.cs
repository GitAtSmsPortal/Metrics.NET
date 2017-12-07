
using System.Collections.Generic;
using FluentAssertions;
using Metrics.Core;
using Xunit;

namespace Metrics.Tests.Core
{
    public class MetricIdentifierTests
    {
        [Fact]
        public void Calculate_WithName_UsesDefaultTags()
        {
            const string emptyTagsHashCode = "17";
            var result = MetricIdentifier.Calculate("test");
            result.Should().Be("test" + emptyTagsHashCode);
        }

        [Fact]
        public void Calculate_WithNullName_ReturnsTagsHashCode()
        {
            const string emptyTagsHashCode = "17";
            var result = MetricIdentifier.Calculate(null);
            result.Should().Be(emptyTagsHashCode);
        }

        [Fact]
        public void Calculate_WithSameTagKeyDiffValue_ReturnsDifferentHashCodes()
        {
            var tempTags1 = new Dictionary<string, string>();
            tempTags1.Add("key", "abc");
            var tempTags2 = new Dictionary<string, string>();
            tempTags2.Add("key", "123");
            var result1 = MetricIdentifier.Calculate("test", tempTags1);
            var result2 = MetricIdentifier.Calculate("test", tempTags2);
            result1.Should().NotBe(result2);
        }

        [Fact]
        public void Calculate_WithSameTagKeySameValue_ReturnsSameHashCodes()
        {
            var tempTags1 = new Dictionary<string, string>();
            tempTags1.Add("key", "abc");
            var tempTags2 = new Dictionary<string, string>();
            tempTags2.Add("key", "abc");
            var result1 = MetricIdentifier.Calculate("test", tempTags1);
            var result2 = MetricIdentifier.Calculate("test", tempTags2);
            result1.Should().Be(result2);
        }

        [Fact]
        public void Calculate_WithDiffTagKeyDiffValue_ReturnsDifferentHashCodes()
        {
            var tempTags1 = new Dictionary<string, string>();
            tempTags1.Add("key1", "abc");
            var tempTags2 = new Dictionary<string, string>();
            tempTags2.Add("key2", "123");
            var result1 = MetricIdentifier.Calculate("test", tempTags1);
            var result2 = MetricIdentifier.Calculate("test", tempTags2);
            result1.Should().NotBe(result2);
        }

        [Fact]
        public void Calculate_WithDiffTagKeySameValue_ReturnsDifferentHashCodes()
        {
            var tempTags1 = new Dictionary<string, string>();
            tempTags1.Add("key1", "abc");
            var tempTags2 = new Dictionary<string, string>();
            tempTags2.Add("key2", "abc");
            var result1 = MetricIdentifier.Calculate("test", tempTags1);
            var result2 = MetricIdentifier.Calculate("test", tempTags2);
            result1.Should().NotBe(result2);
        }

        [Fact]
        public void Calculate_WithInvertedTagValueOptions_ReturnsDifferentHashCodes()
        {
            var tempTags1 = new Dictionary<string, string>();
            tempTags1.Add("key", "value");
            var tempTags2 = new Dictionary<string, string>();
            tempTags2.Add("value", "key");
            var result1 = MetricIdentifier.Calculate("test", tempTags1);
            var result2 = MetricIdentifier.Calculate("test", tempTags2);
            result1.Should().NotBe(result2);
        }
    }
}
