
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
			var result1 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key", "abc"));
			var result2 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key", "123"));
			result1.Should().NotBe(result2);
		}

		[Fact]
		public void Calculate_WithSameTagKeySameValue_ReturnsSameHashCodes()
		{
			var result1 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key", "abc"));
			var result2 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key", "abc"));
			result1.Should().Be(result2);
		}

		[Fact]
		public void Calculate_WithDiffTagKeyDiffValue_ReturnsDifferentHashCodes()
		{
			var result1 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key1", "abc"));
			var result2 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key2", "123"));
			result1.Should().NotBe(result2);
		}

		[Fact]
		public void Calculate_WithDiffTagKeySameValue_ReturnsDifferentHashCodes()
		{
			var result1 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key1", "abc"));
			var result2 = MetricIdentifier.Calculate("test", new KeyValuePair<string, string>("key2", "abc"));
			result1.Should().NotBe(result2);
		}
	}
}
