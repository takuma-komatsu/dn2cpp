using System;
using System.Runtime.InteropServices;

namespace LayoutSizeBadNarrowExplicit;

/// <summary>The refusal subject: an explicit layout whose total only the 64-bit reading
/// can express.
///
/// <para>The declared size sits between the two natural field ends (4 at 32 bits, 8 at
/// 64), so at 64 bits it is below the end and ignored, while at 32 bits it dominates and
/// rounds past itself — the shape the explicit model refuses. The emitted union FIXES the
/// total, so the alternative to refusing is a struct whose size is 8 on a 32-bit target.
/// Asserted by a negative arm of gates/build-and-run-marshal-pinning.sh; this program is
/// never built into a binary or run.</para></summary>
[StructLayout(LayoutKind.Explicit, Size = 5)]
public struct PtrUnderExplicit
{
    [FieldOffset(0)]
    public IntPtr P;
}

public static class Program
{
    public static void Main()
    {
        var s = default(PtrUnderExplicit);
        s.P = new IntPtr(42);
        Console.WriteLine(s.P.ToInt64());
    }
}
