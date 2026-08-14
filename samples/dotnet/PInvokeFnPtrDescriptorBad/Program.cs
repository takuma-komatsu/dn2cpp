using System;
using System.Runtime.InteropServices;

namespace PInvokeFnPtrDescriptorBad;

public static unsafe class Program
{
    /// <summary>Refusal subject: <c>[MarshalAs(UnmanagedType.FunctionPtr)]</c> on a
    /// <c>void*</c> parameter. The descriptor is honoured only on a genuine function
    /// pointer (<c>delegate*</c>, the PInvokeMarshalAsSubset rows); real .NET raises
    /// <c>MarshalDirectiveException</c> at the call ("pointers must not have a MarshalAs
    /// attribute set"), and the transpile refuses before the descriptor can be ignored.
    /// Transpiled by the negative arm of gates/build-and-run-pinvoke-native.sh, never
    /// run. libc needs no --pinvoke-module opt-in, so validation reaches the
    /// descriptor.</summary>
    [DllImport("libc")]
    private static extern void dn2cpp_never_linked_fnptr_descriptor(
        [MarshalAs(UnmanagedType.FunctionPtr)] void* value);

    public static void Main()
    {
        dn2cpp_never_linked_fnptr_descriptor(null);
        Console.WriteLine("unreachable");
    }
}
