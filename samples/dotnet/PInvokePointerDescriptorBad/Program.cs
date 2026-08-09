using System;
using System.Runtime.InteropServices;

namespace PInvokePointerDescriptorBad;

/// <summary>Refusal subject: a <c>void*</c> field carrying
/// <c>[MarshalAs(UnmanagedType.SysInt)]</c> across a P/Invoke boundary. Real .NET raises
/// <c>TypeLoadException</c>; the transpile refuses before the descriptor can be ignored.
/// Transpiled by the negative arm of gates/build-and-run-pinvoke-native.sh, never run.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DescribedPointer
{
    [MarshalAs(UnmanagedType.SysInt)] public void* Value;
}

public static class Program
{
    // libc needs no --pinvoke-module opt-in, so validation reaches the field descriptor.
    [DllImport("libc")]
    private static extern void dn2cpp_never_linked_pointer_descriptor(DescribedPointer value);

    public static void Main()
    {
        dn2cpp_never_linked_pointer_descriptor(default);
        Console.WriteLine("unreachable");
    }
}
