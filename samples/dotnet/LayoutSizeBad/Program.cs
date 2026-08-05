using System;
using System.Runtime.InteropServices;

namespace LayoutSizeBad;

/// <summary>The refusal subject: <c>[StructLayout(Size = N)]</c> where N is not
/// a multiple of the struct's alignment.
///
/// <para>Real .NET MEASURES this shape — Size=6 over an int reads 6 from <c>sizeof</c>,
/// <c>Unsafe.SizeOf</c>, <c>Marshal.SizeOf</c> and the array stride alike, i.e. size 6
/// with alignment 4 intact — and no C++ type can express that (sizeof is always a multiple
/// of alignof), so the transpile must refuse loudly (exit 2, one <c>error:</c> line naming
/// the type, the Size and the alignment) rather than emit a struct whose C++ sizeof
/// silently rounds to 8. Where Size IS a multiple of a sub-word alignment the emitted pad
/// reproduces .NET exactly — the live-diffed Sized3/Sized6Short rows of MarshalPinning's
/// SizeOfOffsetOfSubset. Asserted by the negative arm of
/// gates/build-and-run-marshal-pinning.sh; this program is never built into a binary or
/// run.</para></summary>
[StructLayout(LayoutKind.Sequential, Size = 6)]
public struct Sized6Int
{
    public int A;
}

public static class Program
{
    public static void Main()
    {
        var s = default(Sized6Int);
        s.A = 42;
        Console.WriteLine(s.A);
    }
}
