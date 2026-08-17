using System;
using System.Linq;
using System.Numerics;

namespace MiscIntrinsicSubset;

// Misc intrinsics the transpiler's own code reaches: float NaN/infinity tests,
// static Object.Equals, BitOperations, Debugger.IsAttached, GC statistics, the
// degraded AppDomain.GetAssemblies set, and the null-receiver runtime helpers.
internal static class Program
{
    // A record, i.e. value equality, compared through static Object.Equals.
    private sealed record Box(int V);

    internal static void __GateEntry()
    {
        double nan = double.NaN, pinf = double.PositiveInfinity, ninf = double.NegativeInfinity, fin = 3.5;
        Console.WriteLine($"d_nan={double.IsNaN(nan)}/{double.IsNaN(fin)}/{double.IsNaN(pinf)}");
        Console.WriteLine($"d_pinf={double.IsPositiveInfinity(pinf)}/{double.IsPositiveInfinity(ninf)}/{double.IsPositiveInfinity(fin)}/{double.IsPositiveInfinity(nan)}");
        Console.WriteLine($"d_ninf={double.IsNegativeInfinity(ninf)}/{double.IsNegativeInfinity(pinf)}/{double.IsNegativeInfinity(fin)}");

        float fnan = float.NaN, fpinf = float.PositiveInfinity, fninf = float.NegativeInfinity, ffin = -2.5f;
        Console.WriteLine($"f_nan={float.IsNaN(fnan)}/{float.IsNaN(ffin)}");
        Console.WriteLine($"f_pinf={float.IsPositiveInfinity(fpinf)}/{float.IsPositiveInfinity(fninf)}/{float.IsPositiveInfinity(ffin)}");
        Console.WriteLine($"f_ninf={float.IsNegativeInfinity(fninf)}/{float.IsNegativeInfinity(fpinf)}");

        // Static Object.Equals: null pairs, same reference, value-equal distinct
        // references (virtual Equals: record + string), unequal.
        object n1 = null, n2 = null;
        object r1 = new Box(5), r2 = new Box(5), r3 = new Box(9);
        object same = r1;
        object s1 = "hi", s2 = "xhi".Substring(1), s3 = "bye"; // s2 is a distinct allocation
        Console.WriteLine($"eq_nn={object.Equals(n1, n2)}");
        Console.WriteLine($"eq_n1={object.Equals(n1, r1)}/{object.Equals(r1, n1)}");
        Console.WriteLine($"eq_ref={object.Equals(r1, same)}");
        Console.WriteLine($"eq_rec={object.Equals(r1, r2)}/{object.Equals(r1, r3)}");
        Console.WriteLine($"eq_str={object.Equals(s1, s2)}/{object.Equals(s1, s3)}");

        // BitOperations: all four ops, 32- and 64-bit, signed and unsigned, plus the
        // defined zero-input results (*ZeroCount -> width, Log2 -> 0).
        Console.WriteLine($"tzc32={BitOperations.TrailingZeroCount(0b1011000u)}");        // 3
        Console.WriteLine($"tzc32i={BitOperations.TrailingZeroCount(0b100000)}");         // 5 (int)
        Console.WriteLine($"tzc64={BitOperations.TrailingZeroCount(0x8000000000UL)}");    // 39
        Console.WriteLine($"tzc64i={BitOperations.TrailingZeroCount(1L << 40)}");         // 40 (long)
        Console.WriteLine($"lzc32={BitOperations.LeadingZeroCount(1u)}");                 // 31
        Console.WriteLine($"lzc64={BitOperations.LeadingZeroCount(1UL)}");                // 63
        Console.WriteLine($"pop32={BitOperations.PopCount(0xF0F0F0F0u)}");                // 16
        Console.WriteLine($"pop64={BitOperations.PopCount(0xFFFFFFFFFFFFFFFFUL)}");       // 64
        Console.WriteLine($"log32={BitOperations.Log2(1000u)}");                          // 9
        Console.WriteLine($"log64={BitOperations.Log2((ulong)uint.MaxValue + 1)}");       // 32
        Console.WriteLine($"z32={BitOperations.TrailingZeroCount(0u)}/{BitOperations.LeadingZeroCount(0u)}/{BitOperations.PopCount(0u)}/{BitOperations.Log2(0u)}");     // 32/32/0/0
        Console.WriteLine($"z64={BitOperations.TrailingZeroCount(0UL)}/{BitOperations.LeadingZeroCount(0UL)}/{BitOperations.PopCount(0UL)}/{BitOperations.Log2(0UL)}"); // 64/64/0/0
        // nuint/nint overloads are not intercepted: they must still transpile as
        // BCL-as-IL and match .NET at the host pointer width.
        nuint nu = 0b110000; nint ni = 256;
        Console.WriteLine($"np={BitOperations.TrailingZeroCount(nu)}/{BitOperations.LeadingZeroCount(nu)}/{BitOperations.PopCount(nu)}/{BitOperations.Log2(nu)}");
        Console.WriteLine($"ni={BitOperations.TrailingZeroCount(ni)}");

        // Bound runtime-dead: a transpiled binary has no managed debugger, and the
        // oracle runs under `dotnet` with none attached.
        Console.WriteLine($"dbg={System.Diagnostics.Debugger.IsAttached}");

        // Debugger.IsLogging const-folds false (NativeAOT parity), so a Trace/Debug
        // write lands in SystemNative_SysLog and a direct Debugger.Log is a bound
        // no-op. Neither may reach stdout — the oracle agrees, hence the marker line
        // below: a section whose subject prints nothing cannot prove it ran.
        System.Diagnostics.Trace.WriteLine("must not reach stdout");
        System.Diagnostics.Debugger.Log(0, null, "must not reach stdout");
        Console.WriteLine($"dbg_log={System.Diagnostics.Debugger.IsAttached}|done");

        // GC statistics bind to the vendored Boehm GC. Raw counts are nondeterministic,
        // so assert only what holds identically on real .NET: non-negative and monotone.
        int cc0 = GC.CollectionCount(0);
        GC.Collect();
        int cc1 = GC.CollectionCount(0);
        Console.WriteLine($"gc_cc_nonneg={cc0 >= 0}");
        Console.WriteLine($"gc_cc_monotone={cc1 >= cc0}");

        // Boehm heap accounting fills the honest fields and leaves the rest 0. Byte
        // counts are nondeterministic; assert the shape both runtimes agree on.
        var mi = GC.GetGCMemoryInfo();
        Console.WriteLine($"gc_heap_pos={mi.HeapSizeBytes > 0}");
        Console.WriteLine($"gc_committed_pos={mi.TotalCommittedBytes > 0}");

        // An AOT binary has no run-time assembly set, so this degrades to an empty —
        // never null — array. The count diverges; assert only null-safety.
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        Console.WriteLine($"asm_nonnull={asms is not null}");
        Console.WriteLine($"asm_len_nonneg={asms!.Length >= 0}");

        // LINQ over that degraded set: the empty array must carry the DECLARED
        // Assembly[] runtime identity, or the IEnumerable<Assembly> dispatch inside
        // Where's iterator rightly refuses it. Assert only that the query completes.
        int asmWhere = AppDomain.CurrentDomain.GetAssemblies().Where(a => a != null).Count();
        Console.WriteLine($"asm_where_nonneg={asmWhere >= 0}");

        // Loading IL at run time is a structural non-goal: dn2cpp throws
        // PlatformNotSupported, real .NET FileNotFound. Assert only that it threw.
        bool alcThrew = false;
        try { System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath("/dn2cpp/no/such.dll"); }
        catch (Exception) { alcThrew = true; }
        Console.WriteLine($"alc_load_threw={alcThrew}");

        // Null managed receivers that reach a runtime entry point as a pointer argument
        // (dn2cpp_unbox, dn2cpp_task_exception): the NullReferenceException is the
        // runtime's to raise, and execution must continue — hence the catch handlers.
        object nbox = null;
        try { s_sink = (int)nbox; Console.WriteLine("unbox null -> no throw"); }
        catch (Exception e) { Console.WriteLine("unbox null -> " + e.GetType().Name); }
        System.Threading.Tasks.Task ntask = null;
        try { s_sink = ntask.Exception; Console.WriteLine("null-task.Exception -> no throw"); }
        catch (Exception e) { Console.WriteLine("null-task.Exception -> " + e.GetType().Name); }
        // The recovery is real: a typed catch selects the NRE.
        try { s_sink = (int)nbox; }
        catch (NullReferenceException) { Console.WriteLine("typed catch: NullReferenceException"); }
        catch (Exception) { Console.WriteLine("typed catch: fell through to Exception"); }
    }

    // Stored, never read: a dead store would be dropped along with the expression
    // that fed it, silently turning a probe into a no-op.
    private static object s_sink;
}
