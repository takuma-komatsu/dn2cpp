using System;
using System.Runtime.InteropServices;

namespace LayoutSizeBadNarrow;

/// <summary>The refusal subject: a declared <c>[StructLayout(Size = N)]</c> that is
/// unrepresentable because the FIELDS end off the struct's alignment, not because N does.
///
/// <para>Real .NET measures this at 12 bytes with alignment 8 intact — the declared size
/// floors the field end and the rounding never happens — and C++ has no such type, since
/// sizeof is always a multiple of alignof. So the transpile must refuse loudly rather than
/// answer the rounded 16. It lives apart from LayoutSizeBad because the CLI stops at the
/// first error, which would leave the second refusal's wording unasserted. Asserted by a
/// negative arm of gates/build-and-run-marshal-pinning.sh; this program is never built
/// into a binary or run.</para></summary>
[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct PtrIntUnder
{
    public IntPtr P;
    public int I;
}

public static class Program
{
    public static void Main()
    {
        var s = default(PtrIntUnder);
        s.I = 42;
        Console.WriteLine(s.I);
    }
}
