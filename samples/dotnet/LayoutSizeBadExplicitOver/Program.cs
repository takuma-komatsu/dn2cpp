using System;
using System.Runtime.InteropServices;

namespace LayoutSizeBadExplicitOver;

/// <summary>The refusal subject: an explicit layout whose fields end PAST the declared
/// size, so the offender is max(Size, end) and not Size.
///
/// <para>Fields end at byte 9 over a declared Size=4, and real .NET measures that 9 from
/// sizeof, Unsafe.SizeOf, Marshal.SizeOf and the array stride alike — size 9 with
/// alignment 8 intact, which no C++ struct expresses. Asserted by a negative arm of
/// gates/build-and-run-marshal-pinning.sh; this program is never built into a binary or
/// run.</para></summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct PtrOverExplicit
{
    [FieldOffset(0)]
    public IntPtr P;

    [FieldOffset(8)]
    public byte B;
}

public static class Program
{
    public static void Main()
    {
        var s = default(PtrOverExplicit);
        s.P = new IntPtr(42);
        Console.WriteLine(s.P.ToInt64() + s.B);
    }
}
