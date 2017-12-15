
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Metrics.Core;
using Metrics.MetricData;
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

        [Fact]
        public void EventMetric_CanBeRecordedOnMultipleThreads()
        {
            const int threadCount = 16;
            const long iterations = 1000 * 100;

            var threads = new List<Thread>();
            var tcs = new TaskCompletionSource<int>();

            for (var i = 0; i < threadCount; i++)
            {
                threads.Add(new Thread(s =>
                {
                    tcs.Task.Wait();
                    for (long j = 0; j < iterations; j++)
                    {
                        evnt.Record();
                    }
                }));
            }

            threads.ForEach(t => t.Start());
            tcs.SetResult(0);
            threads.ForEach(t => t.Join());

            evnt.Value.Events.Count.Should().Be(threadCount * (int)iterations);
        }

        [Fact]
        public void EventMetric_IsReadWriteThreadSafe()
        {
            const int threadCount = 16;
            const long iterations = 1000 * 100;

            var threads = new List<Thread>();
            var tcs = new TaskCompletionSource<int>();

            var threwException = false;
            for (var i = 0; i < threadCount; i++)
            {
                if (i % 2 == 0)
                {
                    threads.Add(new Thread(s =>
                    {
                        tcs.Task.Wait();
                        for (long j = 0; j < iterations; j++)
                        {
                            if (threwException)
                            {
                                break;
                            }
                            evnt.Value.Events.Add(new EventDetails(new Dictionary<string, object>(), DateTime.UtcNow));
                        }
                    }));
                }
                else
                {
                    threads.Add(new Thread(s =>
                    {
                        tcs.Task.Wait();
                        for (long j = 0; j < iterations; j++)
                        {
                            try
                            {
                                if (threwException)
                                {
                                    break;
                                }
                                var copy = evnt.Value.Events;
                            }
                            catch
                            {
                                threwException = true;
                                break;
                            }
                        }
                    }));
                }
            }

            threads.ForEach(t => t.Start());
            tcs.SetResult(0);
            threads.ForEach(t => t.Join());

            threwException.Should().BeFalse();
        }
    }
}
