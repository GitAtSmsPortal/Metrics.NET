using System;
using System.Diagnostics;
using System.Threading;

namespace Metrics.PerfCounters
{
    internal static class ThreadPoolMetrics
    {
        internal static void RegisterThreadPoolGauges(MetricsContext context)
        {
            context.Gauge("threads.Thread Pool Available Threads", () => { int threads, ports; ThreadPool.GetAvailableThreads(out threads, out ports); return threads; }, Unit.Threads);
            context.Gauge("threads.Thread Pool Available Completion Ports", () => { int threads, ports; ThreadPool.GetAvailableThreads(out threads, out ports); return ports; }, Unit.Custom("Ports"));

            context.Gauge("threads.Thread Pool Min Threads", () => { int threads, ports; ThreadPool.GetMinThreads(out threads, out ports); return threads; }, Unit.Threads);
            context.Gauge("threads.Thread Pool Min Completion Ports", () => { int threads, ports; ThreadPool.GetMinThreads(out threads, out ports); return ports; }, Unit.Custom("Ports"));

            context.Gauge("threads.Thread Pool Max Threads", () => { int threads, ports; ThreadPool.GetMaxThreads(out threads, out ports); return threads; }, Unit.Threads);
            context.Gauge("threads.Thread Pool Max Completion Ports", () => { int threads, ports; ThreadPool.GetMaxThreads(out threads, out ports); return ports; }, Unit.Custom("Ports"));

            var currentProcess = Process.GetCurrentProcess();
            Func<TimeSpan> uptime = () => (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime());
            context.Gauge("process." + currentProcess.ProcessName + " Uptime Seconds", () => uptime().TotalSeconds, Unit.Custom("Seconds"));

            context.Gauge("process." + currentProcess.ProcessName + " Uptime Hours", () => uptime().TotalHours, Unit.Custom("Hours"));
            context.Gauge("threads." + currentProcess.ProcessName + " Threads", () => Process.GetCurrentProcess().Threads.Count, Unit.Threads);
        }
    }
}
