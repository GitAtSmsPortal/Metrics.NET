
using System;
using System.Collections.Generic;
using FluentAssertions;
using Metrics.Core;
using Xunit;

namespace Metrics.Tests.Metrics
{
    public class EventMetricTests
    {
        private readonly EventMetric evnt = new EventMetric();

        [Fact]
        public void EventMetric_StartsWithEmptyList()
        {
            evnt.Value.Events.Count.Should().Be(0);
        }

        [Fact]
        public void EventMetric_CanRecord()
        {
            evnt.Record();
            evnt.Value.Events.Count.Should().Be(1);
        }

        [Fact]
        public void EventMetric_CanRecordWithTimestamp()
        {
            evnt.Record(DateTime.UtcNow);
            evnt.Value.Events.Count.Should().Be(1);
        }

        [Fact]
        public void EventMetric_CanRecordWithFields()
        {
            var fields = new Dictionary<string, object>();
            fields.Add("field", "0");
            evnt.Record(fields);
            evnt.Value.Events.Count.Should().Be(1);
        }

        [Fact]
        public void EventMetric_CanRecordWithFieldsAndTimestamp()
        {
            var fields = new Dictionary<string, object>();
            fields.Add("field", "0");
            evnt.Record(fields, DateTime.UtcNow);
            evnt.Value.Events.Count.Should().Be(1);
        }

        [Fact]
        public void EventMetric_CanRecordMultipleTimes()
        {
            evnt.Record();
            evnt.Record();
            evnt.Record();
            evnt.Value.Events.Count.Should().Be(3);
        }

        [Fact]
        public void EventMetric_CanReset()
        {
            evnt.Record();
            evnt.Value.Events.Count.Should().Be(1);
            evnt.Reset();
            evnt.Value.Events.Count.Should().Be(0);
        }
    }
}
