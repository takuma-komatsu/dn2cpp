using System.Reflection;
using BenchmarkDotNet.Running;
using DnBrotli.Benchmarks;

if (args.Length > 0 && args[0] == "quick")
{
    QuickBench.Run();
    return;
}

// Dispatches to benchmark classes by name/filter; benchmarks are added per build phase.
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
