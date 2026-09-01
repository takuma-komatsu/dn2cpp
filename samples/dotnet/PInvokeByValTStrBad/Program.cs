using System;
using System.Runtime.InteropServices;

namespace PInvokeByValTStrBad;

/// <summary>Refusal subject: a struct string field carrying
/// <c>[MarshalAs(UnmanagedType.ByValTStr)]</c> crossing a P/Invoke boundary. The struct
/// marshaller lays such a field out as a pointer while the marshalled-layout model sizes
/// it as the inline buffer the descriptor asks for, so the transpile must refuse (exit 2)
/// rather than let the disagreement reach the C++ compile. <c>Marshal.SizeOf</c>/
/// <c>OffsetOf</c> over the same shape keep answering; only the crossing refuses.
/// Transpiled by the negative arm of gates/build-and-run-pinvoke-native.sh, never
/// built or run.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct FixedName
{
    public int Id;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)] public string Name;
}

public static class Program
{
    // Every user import is admitted, so the transpile reaches marshalling
    // validation instead of stopping at module admission. The entry point is never
    // resolved: the transpile fails before anything links.
    [DllImport("libc")]
    private static extern void dn2cpp_never_linked_byvaltstr(FixedName s);

    // These independent scalar bodies make the measure-mode refusal a real
    // worker-scheduling subject before Main records the marshalling gap.
    private static int Increment(int value)
    {
        // The loop forces primitive locals into IL without adding token-bearing operations.
        int result = value;
        for (int i = 0; i < 1; i++)
            result++;
        return result;
    }

    private static int Double(int value) => value * 2;

    public static void Main()
    {
        var s = new FixedName { Id = Double(Increment(0)), Name = "x" };
        dn2cpp_never_linked_byvaltstr(s);
    }
}
