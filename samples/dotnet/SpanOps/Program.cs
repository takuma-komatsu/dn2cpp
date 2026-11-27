using System;
using System.Globalization;

namespace SpanOps
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to a standalone program.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            SpanBulkSubset.Program.__GateEntry();
            SpanScanSubset.Program.__GateEntry();
            SpanComparerSubset.Program.__GateEntry();
            SpanStructElementSubset.Program.__GateEntry();
            SpanSortSubset.Program.__GateEntry();
            SpanInstanceSubset.Program.__GateEntry();
            SpanIndexOfAnySubset.Program.__GateEntry();
            ReadOnlySpanByteSubset.Program.__GateEntry();
            CreateSpanSubset.Program.__GateEntry();
            MemorySpanSubset.Program.__GateEntry();
            MemoryMarshalMemorySubset.Program.__GateEntry();
            MemoryMarshalPinnedSubset.Program.__GateEntry();
            MemoryManagerSubset.Program.__GateEntry();
            MemoryMarshalSubset.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-decimal-layout")
                return;

            DecimalLayoutSubset.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-span-overlap")
                return;

            SpanOverlapSubset.Run();
            if (args.Length > 0 && args[0] == "before-marshal-bounds")
                return;

            MemoryMarshalBoundsSubset.Run();
            if (args.Length > 0 && args[0] == "before-span-numeric-smoke")
                return;

            Console.WriteLine("== span numeric smoke ==");
            Console.WriteLine(int.TryParse(" -42 ".AsSpan(), out int signed) && signed == -42);
            Console.WriteLine(!int.TryParse("2147483648".AsSpan(), out _));
            Console.WriteLine(double.TryParse("1.25"u8, out double value) && value == 1.25);
            Console.WriteLine(MathF.Round(2.5f, MidpointRounding.ToEven) == 2.0f);
            Console.WriteLine("span numeric smoke end");
        }
    }
}
