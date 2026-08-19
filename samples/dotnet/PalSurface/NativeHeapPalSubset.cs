using System;
using System.Runtime.InteropServices;

namespace NativeHeapPalSubset
{
    // The PAL's C heap, reached the way a program reaches it once the intercepts do
    // not apply. Marshal.{AllocHGlobal,FreeHGlobal} and the NativeMemory family are
    // lowered inline to dn2cpp_native_*, but the BSTR allocators are not: their real
    // CoreLib bodies call Interop.Sys.{Malloc,Free}, so this section is what puts
    // SystemNative_Malloc / SystemNative_Free in the emitted call set.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- NativeHeapPalSubset --");

            IntPtr bstr = Marshal.StringToBSTR("wasm pal");
            Console.WriteLine($"bstrNonNull={bstr != IntPtr.Zero}");
            // The BSTR byte-length prefix sits immediately before the data; reading it
            // proves the block the PAL allocator returned is the one that was written.
            Console.WriteLine($"bstrByteLen={Marshal.ReadInt32(bstr, -4)}");
            Console.WriteLine($"bstrRoundTrip={Marshal.PtrToStringBSTR(bstr)}");
            Marshal.FreeBSTR(bstr);
            Console.WriteLine("bstrFreed=True");

            // Zero is a documented no-op on both allocators, and the free path must
            // reach the PAL without a null check of its own.
            Marshal.FreeBSTR(IntPtr.Zero);
            Console.WriteLine("bstrFreeZeroOk=True");
        }
    }
}
