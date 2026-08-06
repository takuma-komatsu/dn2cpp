using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PInvokeLibcSubset;

// Blittable primitives, pointers and an ASCII string into the always-linked libc, so no
// link flag is emitted — unlike every other section in this bucket.
internal static unsafe class Program
{
    // Implicit entry point: the method name is the symbol.
    [DllImport("libc")]
    private static extern int abs(int n);

    // Explicit EntryPoint maps a PascalCase managed name onto the C symbol.
    [DllImport("libc", EntryPoint = "llabs")]
    private static extern long LongAbs(long n);

    // The double return rides libc's own converters, NOT sqrt/pow/floor/ceil: glibc keeps the
    // math symbols in libm, so a "libc" import of one is an EntryPointNotFoundException on
    // Linux — macOS's libSystem and the UCRT fold math in and hide that. The double ARGUMENT
    // crossing is PInvokeCustomLibSubset's (dn2cpptest_scale).
    [DllImport("libc")]
    private static extern double atof(string s);

    // Explicit EntryPoint over the same double return, plus the end pointer strtod writes
    // back through a byref out-param.
    [DllImport("libc", EntryPoint = "strtod")]
    private static extern double StrToD(byte* s, out IntPtr end);

    // A native-pointer-sized return.
    [DllImport("libc", EntryPoint = "strlen")]
    private static extern nuint StrLen(byte* s);

    // memset returns its destination, so this covers a pointer return too.
    [DllImport("libc", EntryPoint = "memset")]
    private static extern void* MemSet(void* dst, int value, nuint count);

    // "libc" names the always-linked platform C library, which on Windows is the UCRT —
    // a name no default probe of "libc" reaches. The transpiled binary resolves these
    // symbols at LINK time, so it needs no resolver and drops the registration; this
    // exists so real .NET, the oracle this section is diffed against, resolves the same
    // ucrtbase entry points the C++ link does.
    private static IntPtr ResolveLibc(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
        libraryName == "libc" && OperatingSystem.IsWindows()
            ? NativeLibrary.Load("ucrtbase.dll", assembly, searchPath)
            : IntPtr.Zero;

    internal static void __GateEntry()
    {
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, ResolveLibc);

        Console.WriteLine(abs(-7));               // 7
        Console.WriteLine(LongAbs(-1234567890123L)); // 1234567890123
        Console.WriteLine(atof("14142135623730951e-16")); // 1.4142135623730951

        // "5e-1x": strtod stops at the 'x' and reports where through its end pointer. Both
        // parsed spellings carry an exponent and no decimal point on purpose — the radix
        // character is LC_NUMERIC's, and the diff spans two processes.
        byte* num = stackalloc byte[6];
        num[0] = 53; num[1] = 101; num[2] = 45; num[3] = 49; num[4] = 120; num[5] = 0;
        IntPtr end;
        Console.WriteLine(StrToD(num, out end));  // 0.5
        Console.WriteLine((int)((byte*)end - num)); // 4

        // "Hello\0" in a stack buffer.
        byte* s = stackalloc byte[6];
        s[0] = 72; s[1] = 101; s[2] = 108; s[3] = 108; s[4] = 111; s[5] = 0;
        Console.WriteLine((int)StrLen(s));        // 5

        // The returned pointer must alias the destination.
        byte* buf = stackalloc byte[8];
        for (int i = 0; i < 8; i++) buf[i] = (byte)'X';
        void* r = MemSet(buf, (byte)'A', 3);
        buf[3] = 0;
        Console.WriteLine((int)StrLen(buf));      // 3
        Console.WriteLine(buf[0]);                // 65 ('A')
        Console.WriteLine(r == buf);              // True
    }
}
