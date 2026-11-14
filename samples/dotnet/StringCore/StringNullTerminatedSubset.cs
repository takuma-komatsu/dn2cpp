using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Public string views over raw UTF-16: MemoryMarshal's null-terminated span
// (including a null pointer), the pinnable reference read as bytes, and the
// Marshal Uni allocation round trips with their zero-and-free releases.
namespace StringNullTerminatedSubset
{
    internal static unsafe class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== null-terminated string views ==");
            string value = "A\0あ😀";
            fixed (char* p = value)
                Console.WriteLine("utf16=" + value.Length + ":" + MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p).Length);
            Console.WriteLine("null span=" + MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)null).Length);
            ref char first = ref Unsafe.AsRef(in value.GetPinnableReference());
            Console.WriteLine("byte view=" + Unsafe.As<char, byte>(ref first));
            IntPtr co = Marshal.StringToCoTaskMemUni("coあ");
            Console.WriteLine("co=" + Marshal.PtrToStringUni(co));
            Marshal.ZeroFreeCoTaskMemUnicode(co);
            IntPtr global = Marshal.StringToHGlobalUni("globalあ");
            Console.WriteLine("global=" + Marshal.PtrToStringUni(global));
            Marshal.ZeroFreeGlobalAllocUnicode(global);
            Marshal.ZeroFreeCoTaskMemUnicode(IntPtr.Zero);
            Marshal.ZeroFreeGlobalAllocUnicode(IntPtr.Zero);
            Console.WriteLine("null-terminated string views end");
        }
    }
}
