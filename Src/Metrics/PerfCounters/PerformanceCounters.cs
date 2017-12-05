
using Metrics.Core;
using Metrics.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace Metrics.PerfCounters
{
    internal static class PerformanceCounters
    {
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        private static readonly bool isMono = Type.GetType("Mono.Runtime") != null;

        private const string TotalInstance = "_Total";

        private const string Exceptions = ".NET CLR Exceptions";
        private const string Memory = ".NET CLR Memory";
        private const string LocksAndThreads = ".NET CLR LocksAndThreads";

        internal static void RegisterSystemCounters(MetricsContext context)
        {
            context.Register("Available RAM", Unit.MegaBytes, "Memory", "Available MBytes", tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Free System Page Table Entries", Unit.Custom("entries"), "Memory", "Free System Page Table Entries", tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Pages Input/sec", Unit.Custom("pages/s"), "Memory", "Pages Input/sec", tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Pages/sec", Unit.Custom("pages/s"), "Memory", "Pages/sec", tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Pool Nonpaged MBytes", Unit.MegaBytes, "Memory", "Pool Nonpaged Bytes", derivate: v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Pool Paged MBytes", Unit.MegaBytes, "Memory", "Pool Paged Bytes", derivate: v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));

            context.Register("CPU Usage", Unit.Custom("%"), "Processor", "% Processor Time", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("Interrupts / sec", Unit.Custom("interrupts/s"), "Processor", "Interrupts/sec", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("% Interrupt time", Unit.Custom("%"), "Processor", "% Interrupt Time", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("% User Timer", Unit.Custom("%"), "Processor", "% User Time", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("% Privileged Timer", Unit.Custom("%"), "Processor", "% Privileged Time", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("% DPC Time", Unit.Custom("%"), "Processor", "% DPC Time", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));

            context.Register("Logical Disk Avg. sec/Read", Unit.Custom("ms"), "LogicalDisk", "Avg. Disk sec/Read", TotalInstance, v => v * 1024.0, new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Logical Disk Avg. sec/Write", Unit.Custom("ms"), "LogicalDisk", "Avg. Disk sec/Write", TotalInstance, v => v * 1024.0,  new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Logical Disk Transfers/sec", Unit.Custom("Transfers"), "LogicalDisk", "Disk Transfers/sec", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Logical Disk Writes/sec", Unit.Custom("kb/s"), "LogicalDisk", "Disk Reads/sec", TotalInstance, f => f / 1024.0,  new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Logical Disk Reads/sec", Unit.Custom("kb/s"), "LogicalDisk", "Disk Writes/sec", TotalInstance, f => f / 1024.0,  new KeyValuePair<string, string>("PerfCounterType", "disk"));

            context.Register("Physical Disk Avg. sec/Read", Unit.Custom("ms"), "PhysicalDisk", "Avg. Disk sec/Read", TotalInstance, v => v * 1024.0, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Physical Disk Avg. sec/Write", Unit.Custom("ms"), "PhysicalDisk", "Avg. Disk sec/Write", TotalInstance, v => v * 1024.0, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Physical Disk Transfers/sec", Unit.Custom("Transfers"), "PhysicalDisk", "Disk Transfers/sec", TotalInstance, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Physical Disk Writes/sec", Unit.Custom("kb/s"), "PhysicalDisk", "Disk Reads/sec", TotalInstance, f => f / 1024.0, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("Physical Disk Reads/sec", Unit.Custom("kb/s"), "PhysicalDisk", "Disk Writes/sec", TotalInstance, f => f / 1024.0, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
        }

        internal static void RegisterAppCounters(MetricsContext context)
        {
            var app = Process.GetCurrentProcess().ProcessName;

            context.Register("Process CPU Usage", Unit.Percent, "Process", "% Processor Time", app, derivate: v => v / Environment.ProcessorCount, tags: new KeyValuePair<string, string>("AppCounterType", "cpu"));
            context.Register("Process User Time", Unit.Percent, "Process", "% User Time", app, derivate: v => v / Environment.ProcessorCount, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));
            context.Register("Process Privileged Time", Unit.Percent, "Process", "% Privileged Time", app, derivate: v => v / Environment.ProcessorCount, tags: new KeyValuePair<string, string>("PerfCounterType", "cpu"));

            context.Register("Private MBytes", Unit.MegaBytes, "Process", "Private Bytes", app, derivate: v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Working Set", Unit.MegaBytes, "Process", "Working Set", app, derivate: v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));

            context.Register("Mb in all Heaps", Unit.MegaBytes, Memory, "# Bytes in all Heaps", app, v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Gen 0 heap size", Unit.MegaBytes, Memory, "Gen 0 heap size", app, v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Gen 1 heap size", Unit.MegaBytes, Memory, "Gen 1 heap size", app, v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Gen 2 heap size", Unit.MegaBytes, Memory, "Gen 2 heap size", app, v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Large Object Heap size", Unit.MegaBytes, Memory, "Large Object Heap size", app, v => v / (1024 * 1024.0), tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Allocated Bytes/second", Unit.KiloBytes, Memory, "Allocated Bytes/sec", app, v => v / 1024.0, tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));

            context.Register("Time in GC", Unit.Custom("%"), Memory, "% Time in GC", app, tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));
            context.Register("Pinned Objects", Unit.Custom("Objects"), Memory, "# of Pinned Objects", app, tags: new KeyValuePair<string, string>("PerfCounterType", "memory"));

            context.Register("Total Exceptions", Unit.Custom("Exceptions"), Exceptions, "# of Exceps Thrown", app, tags: new KeyValuePair<string, string>("PerfCounterType", "exceptions"));
            context.Register("Exceptions Thrown / Sec", Unit.Custom("Exceptions"), Exceptions, "# of Exceps Thrown / Sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "exceptions"));
            context.Register("Except Filters / Sec", Unit.Custom("Filters"), Exceptions, "# of Filters / Sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "exceptions"));
            context.Register("Finallys / Sec", Unit.Custom("Finallys"), Exceptions, "# of Finallys / Sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "exceptions"));
            context.Register("Throw to Catch Depth / Sec", Unit.Custom("Stack Frames"), Exceptions, "Throw to Catch Depth / Sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "exceptions"));

            context.Register("Logical Threads", Unit.Threads, LocksAndThreads, "# of current logical Threads", app, tags: new KeyValuePair<string, string>("PerfCounterType", "threads"));
            context.Register("Physical Threads", Unit.Threads, LocksAndThreads, "# of current physical Threads", app, tags: new KeyValuePair<string, string>("PerfCounterType", "threads"));
            context.Register("Contention Rate / Sec", Unit.Custom("Attempts"), LocksAndThreads, "Contention Rate / Sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "threads"));
            context.Register("Total Contentions", Unit.Custom("Attempts"), LocksAndThreads, "Total # of Contentions", app, tags: new KeyValuePair<string, string>("PerfCounterType", "threads"));
            context.Register("Queue Length / sec", Unit.Threads, LocksAndThreads, "Queue Length / sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "threads"));

            context.Register("IO Data Operations/sec", Unit.Custom("IOPS"), "Process", "IO Data Operations/sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));
            context.Register("IO Other Operations/sec", Unit.Custom("IOPS"), "Process", "IO Other Operations/sec", app, tags: new KeyValuePair<string, string>("PerfCounterType", "disk"));

            ThreadPoolMetrics.RegisterThreadPoolGauges(context);
        }

        private static void Register(this MetricsContext context, string name, Unit unit,
            string category, string counter, string instance = null,
            Func<double, double> derivate = null,
            MetricTags tags = default(MetricTags))
        {
            try
            {
                WrappedRegister(context, name, unit, category, counter, instance, derivate, tags);
            }
            catch (UnauthorizedAccessException x)
            {
                var message = "Error reading performance counter data. The application is currently running as user " + GetIdentity() +
                   ". Make sure the user has access to the performance counters. The user needs to be either Admin or belong to Performance Monitor user group.";
                MetricsErrorHandler.Handle(x, message);
            }
            catch (Exception x)
            {
                var message = "Error reading performance counter data. The application is currently running as user " + GetIdentity() +
                   ". Make sure the user has access to the performance counters. The user needs to be either Admin or belong to Performance Monitor user group.";
                MetricsErrorHandler.Handle(x, message);
            }
        }

        private static string GetIdentity()
        {
            try
            {
                return WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception x)
            {
                return "[Unknown user | " + x.Message + " ]";
            }
        }

        private static void WrappedRegister(MetricsContext context, string name, Unit unit,
            string category, string counter, string instance = null,
            Func<double, double> derivate = null,
            MetricTags tags = default(MetricTags))
        {
            log.Debug(() => $"Registering performance counter [{counter}] in category [{category}] for instance [{instance ?? "none"}]");

            if (PerformanceCounterCategory.Exists(category))
            {
                if (instance == null || PerformanceCounterCategory.InstanceExists(instance, category))
                {
                    if (PerformanceCounterCategory.CounterExists(counter, category))
                    {
                        var counterTags = new MetricTags(tags.Tags.Concat(new[] {new KeyValuePair<string, string>("MetricType", "PerfCounter") }));
                        if (derivate == null)
                        {
                            context.Advanced.Gauge(name, () => new PerformanceCounterGauge(category, counter, instance), unit, counterTags);
                        }
                        else
                        {
                            context.Advanced.Gauge(name, () => new DerivedGauge(new PerformanceCounterGauge(category, counter, instance), derivate), unit, counterTags);
                        }
                        return;
                    }
                }
            }

            if (!isMono)
            {
                log.ErrorFormat("Performance counter does not exist [{0}] in category [{1}] for instance [{2}]", counter, category, instance ?? "none");
            }
        }
    }
}
