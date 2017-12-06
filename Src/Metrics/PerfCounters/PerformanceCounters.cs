
using Metrics.Core;
using Metrics.Logging;
using System;
using System.Diagnostics;
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
            context.Register("memory.Available RAM", Unit.MegaBytes, "Memory", "Available MBytes");
            context.Register("memory.Free System Page Table Entries", Unit.Custom("entries"), "Memory", "Free System Page Table Entries");
            context.Register("memory.Pages Input/sec", Unit.Custom("pages/s"), "Memory", "Pages Input/sec");
            context.Register("memory.Pages/sec", Unit.Custom("pages/s"), "Memory", "Pages/sec");
            context.Register("memory.Pool Nonpaged MBytes", Unit.MegaBytes, "Memory", "Pool Nonpaged Bytes", derivate: v => v / (1024 * 1024.0));
            context.Register("memory.Pool Paged MBytes", Unit.MegaBytes, "Memory", "Pool Paged Bytes", derivate: v => v / (1024 * 1024.0));

            context.Register("cpu.% CPU Usage", Unit.Custom("%"), "Processor", "% Processor Time", TotalInstance);
            context.Register("cpu.Interrupts / sec", Unit.Custom("interrupts/s"), "Processor", "Interrupts/sec", TotalInstance);
            context.Register("cpu.% Interrupt time", Unit.Custom("%"), "Processor", "% Interrupt Time", TotalInstance);
            context.Register("cpu.% User Timer", Unit.Custom("%"), "Processor", "% User Time", TotalInstance);
            context.Register("cpu.% Privileged Timer", Unit.Custom("%"), "Processor", "% Privileged Time", TotalInstance);
            context.Register("cpu.% DPC Time", Unit.Custom("%"), "Processor", "% DPC Time", TotalInstance);

            context.Register("disk.Logical Disk Avg. sec/Read", Unit.Custom("ms"), "LogicalDisk", "Avg. Disk sec/Read", TotalInstance, v => v * 1024.0);
            context.Register("disk.Logical Disk Avg. sec/Write", Unit.Custom("ms"), "LogicalDisk", "Avg. Disk sec/Write", TotalInstance, v => v * 1024.0);
            context.Register("disk.Logical Disk Transfers/sec", Unit.Custom("Transfers"), "LogicalDisk", "Disk Transfers/sec", TotalInstance);
            context.Register("disk.Logical Disk Writes/sec", Unit.Custom("kb/s"), "LogicalDisk", "Disk Reads/sec", TotalInstance, f => f / 1024.0);
            context.Register("disk.Logical Disk Reads/sec", Unit.Custom("kb/s"), "LogicalDisk", "Disk Writes/sec", TotalInstance, f => f / 1024.0);

            context.Register("disk.Physical Disk Avg. sec/Read", Unit.Custom("ms"), "PhysicalDisk", "Avg. Disk sec/Read", TotalInstance, v => v * 1024.0);
            context.Register("disk.Physical Disk Avg. sec/Write", Unit.Custom("ms"), "PhysicalDisk", "Avg. Disk sec/Write", TotalInstance, v => v * 1024.0);
            context.Register("disk.Physical Disk Transfers/sec", Unit.Custom("Transfers"), "PhysicalDisk", "Disk Transfers/sec", TotalInstance);
            context.Register("disk.Physical Disk Writes/sec", Unit.Custom("kb/s"), "PhysicalDisk", "Disk Reads/sec", TotalInstance, f => f / 1024.0);
            context.Register("disk.Physical Disk Reads/sec", Unit.Custom("kb/s"), "PhysicalDisk", "Disk Writes/sec", TotalInstance, f => f / 1024.0);
        }

        internal static void RegisterAppCounters(MetricsContext context)
        {
            var app = Process.GetCurrentProcess().ProcessName;

            context.Register("cpu.Process CPU Usage", Unit.Percent, "Process", "% Processor Time", app, derivate: v => v / Environment.ProcessorCount);
            context.Register("cpu.Process User Time", Unit.Percent, "Process", "% User Time", app, derivate: v => v / Environment.ProcessorCount);
            context.Register("cpu.Process Privileged Time", Unit.Percent, "Process", "% Privileged Time", app, derivate: v => v / Environment.ProcessorCount);

            context.Register("memory.Private MBytes", Unit.MegaBytes, "Process", "Private Bytes", app, derivate: v => v / (1024 * 1024.0));
            context.Register("memory.Working Set", Unit.MegaBytes, "Process", "Working Set", app, derivate: v => v / (1024 * 1024.0));

            context.Register("memory.Mb in all Heaps", Unit.MegaBytes, Memory, "# Bytes in all Heaps", app, v => v / (1024 * 1024.0));
            context.Register("memory.Gen 0 heap size", Unit.MegaBytes, Memory, "Gen 0 heap size", app, v => v / (1024 * 1024.0));
            context.Register("memory.Gen 1 heap size", Unit.MegaBytes, Memory, "Gen 1 heap size", app, v => v / (1024 * 1024.0));
            context.Register("memory.Gen 2 heap size", Unit.MegaBytes, Memory, "Gen 2 heap size", app, v => v / (1024 * 1024.0));
            context.Register("memory.Large Object Heap size", Unit.MegaBytes, Memory, "Large Object Heap size", app, v => v / (1024 * 1024.0));
            context.Register("memory.Allocated Bytes/second", Unit.KiloBytes, Memory, "Allocated Bytes/sec", app, v => v / 1024.0);

            context.Register("memory.Time in GC", Unit.Custom("%"), Memory, "% Time in GC", app);
            context.Register("memory.Pinned Objects", Unit.Custom("Objects"), Memory, "# of Pinned Objects", app);

            context.Register("exceptions.Total Exceptions", Unit.Custom("Exceptions"), Exceptions, "# of Exceps Thrown", app);
            context.Register("exceptions.Exceptions Thrown / Sec", Unit.Custom("Exceptions"), Exceptions, "# of Exceps Thrown / Sec", app);
            context.Register("exceptions.Except Filters / Sec", Unit.Custom("Filters"), Exceptions, "# of Filters / Sec", app);
            context.Register("exceptions.Finallys / Sec", Unit.Custom("Finallys"), Exceptions, "# of Finallys / Sec", app);
            context.Register("exceptions.Throw to Catch Depth / Sec", Unit.Custom("Stack Frames"), Exceptions, "Throw to Catch Depth / Sec", app);

            context.Register("threads.Logical Threads", Unit.Threads, LocksAndThreads, "# of current logical Threads", app);
            context.Register("threads.Physical Threads", Unit.Threads, LocksAndThreads, "# of current physical Threads", app);
            context.Register("threads.Contention Rate / Sec", Unit.Custom("Attempts"), LocksAndThreads, "Contention Rate / Sec", app);
            context.Register("threads.Total Contentions", Unit.Custom("Attempts"), LocksAndThreads, "Total # of Contentions", app);
            context.Register("threads.Queue Length / sec", Unit.Threads, LocksAndThreads, "Queue Length / sec", app);

            context.Register("disk.IO Data Operations/sec", Unit.Custom("IOPS"), "Process", "IO Data Operations/sec", app);
            context.Register("disk.IO Other Operations/sec", Unit.Custom("IOPS"), "Process", "IO Other Operations/sec", app);

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
                        var counterTags = new MetricTags(tags.Tags);
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
