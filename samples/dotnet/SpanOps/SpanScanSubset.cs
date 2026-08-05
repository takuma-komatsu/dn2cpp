#nullable enable
using System;

// The SIMD-backed span scan/structural ops. The BCL bodies vectorize and are
// untranspilable, so the transpiler emits scalar loops over the span; the results
// are diffed exact vs .NET. Arrays are kept in LOCALS so the array->span
// conversion goes through op_Implicit rather than the CreateSpan constant-blob
// path, which CreateSpanSubset covers separately.
namespace SpanScanSubset;

class Program
{internal static void __GateEntry()
    {
        // --- Reverse (in-place, via the array-backed span) ---
        int[] a = { 1, 2, 3, 4, 5 };
        ((Span<int>)a).Reverse();
        Console.WriteLine($"{a[0]},{a[1]},{a[2]},{a[3]},{a[4]}");      // 5,4,3,2,1

        string[] sr = { "x", "y", "z" };
        ((Span<string>)sr).Reverse();
        Console.WriteLine($"{sr[0]},{sr[2]}");                         // z,x

        // --- StartsWith / EndsWith (span, span) ---
        int[] b = { 1, 2, 3, 4 };
        int[] pre = { 1, 2 };
        int[] suf = { 3, 4 };
        int[] longer = { 1, 2, 3, 4, 5 };
        Console.WriteLine(MemoryExtensions.StartsWith<int>(b, pre));   // True
        Console.WriteLine(MemoryExtensions.StartsWith<int>(b, suf));   // False
        Console.WriteLine(MemoryExtensions.StartsWith<int>(b, longer)); // False (prefix longer)
        Console.WriteLine(MemoryExtensions.EndsWith<int>(b, suf));     // True
        Console.WriteLine(MemoryExtensions.EndsWith<int>(b, pre));     // False

        string[] ss = { "a", "b", "c" };
        string[] sp1 = { "a", "b" };
        Console.WriteLine(MemoryExtensions.StartsWith<string>(ss, sp1)); // True

        // --- LastIndexOf (span, value) ---
        int[] c = { 5, 6, 5, 7, 5 };
        Console.WriteLine(MemoryExtensions.LastIndexOf<int>(c, 5));    // 4
        Console.WriteLine(MemoryExtensions.LastIndexOf<int>(c, 9));    // -1
        string[] sc = { "p", "q", "p" };
        Console.WriteLine(MemoryExtensions.LastIndexOf<string>(sc, "p")); // 2

        // --- IndexOfAny (span, v0, v1[, v2]) ---
        int[] d = { 10, 20, 30, 40 };
        Console.WriteLine(MemoryExtensions.IndexOfAny<int>(d, 99, 30));     // 2
        Console.WriteLine(MemoryExtensions.IndexOfAny<int>(d, 40, 20, 30)); // 1 (first match)
        Console.WriteLine(MemoryExtensions.IndexOfAny<int>(d, 1, 2));       // -1

        // --- empty-span edges ---
        int[] empty = Array.Empty<int>();
        int[] none = Array.Empty<int>();
        ((Span<int>)empty).Reverse();                                  // no-op
        Console.WriteLine(MemoryExtensions.StartsWith<int>(empty, none)); // True (both empty)
        Console.WriteLine(MemoryExtensions.LastIndexOf<int>(empty, 1));   // -1
    }
}
