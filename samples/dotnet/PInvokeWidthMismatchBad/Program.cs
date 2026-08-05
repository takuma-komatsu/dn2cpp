using System;
using System.Runtime.InteropServices;

namespace PInvokeWidthMismatchBad;

/// <summary>Refusal subject: a blittable struct field whose <c>[MarshalAs]</c> descriptor
/// mismatches its width (<c>[MarshalAs(I2)] int</c>) crossing a P/Invoke boundary. Real
/// .NET raises <c>TypeLoadException</c> in every position (by value, byref, nested), so
/// <c>CppTypes.StructFieldDescriptorSupported</c> demotes the struct out of the blittable
/// fast path and the transpile refuses (exit 2) naming the field and the descriptor.
/// Width-matching descriptors stay no-ops. Transpiled by the negative arm of
/// gates/build-and-run-pinvoke-native.sh, never built or run.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MisWidth
{
    [MarshalAs(UnmanagedType.I2)] public int X;
}

public static class Program
{
    // libc needs no --pinvoke-module opt-in, so the transpile reaches marshalling
    // validation instead of stopping at module admission. The entry point is never
    // resolved: the transpile fails before anything links.
    [DllImport("libc")]
    private static extern int dn2cpp_never_linked_widthmismatch(MisWidth s);

    public static void Main()
    {
        var s = new MisWidth { X = -7 };
        Console.WriteLine(dn2cpp_never_linked_widthmismatch(s));
    }
}
