#nullable enable
using System;
using System.Runtime.InteropServices;

// [NativeImplementation] redirects each [DllImport] below to its C# implementation at
// transpile time, so the nonexistent libdn2cppnimpl is never linked — a regression fails
// the build loudly. Under real .NET the attribute is inert and the import throws
// DllNotFoundException, which the catch turns into a direct call, so both sides print the
// same. The attribute is matched by full name, so this app declares its own copy rather
// than referencing Dn2Cpp.Runtime.
namespace Dn2Cpp.Runtime
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class NativeImplementationAttribute : Attribute
    {
        public NativeImplementationAttribute(string moduleName) { }
        public NativeImplementationAttribute(string moduleName, string entryPoint) { }
    }
}

namespace NativeImplSubset
{
    internal static unsafe class Program
    {
        // Explicit entry point; int + pointer parameters.
        [DllImport("dn2cppnimpl")]
        private static extern int nimpl_combine(int seed, int* p, int n);

        [Dn2Cpp.Runtime.NativeImplementation("dn2cppnimpl", "nimpl_combine")]
        private static int Combine(int seed, int* p, int n)
        {
            int acc = seed;
            for (int i = 0; i < n; i++)
                acc = acc * 31 + p[i];
            return acc;
        }

        // Entry point defaulting to the implementation method's own name.
        [DllImport("dn2cppnimpl", EntryPoint = "ScaleAdd")]
        private static extern long nimpl_scaleadd(long a, long b);

        [Dn2Cpp.Runtime.NativeImplementation("dn2cppnimpl")]
        private static long ScaleAdd(long a, long b)
        {
            return a * 1000 + b;
        }

        // Pointer-width class bridging: the import speaks nint, the implementation a
        // typed pointer, for imports whose BCL-internal pointer type is unnameable.
        [DllImport("dn2cppnimpl")]
        private static extern nint nimpl_offset(nint p, int delta);

        [Dn2Cpp.Runtime.NativeImplementation("dn2cppnimpl", "nimpl_offset")]
        private static void* Offset(void* p, int delta)
        {
            return (byte*)p + delta;
        }

        internal static void __GateEntry()
        {
            int* v = stackalloc int[3];
            v[0] = 7; v[1] = 11; v[2] = 13;

            int combined;
            try { combined = nimpl_combine(5, v, 3); }
            catch (DllNotFoundException) { combined = Combine(5, v, 3); }
            Console.WriteLine($"nimpl combine={combined}");

            long scaled;
            try { scaled = nimpl_scaleadd(42, 7); }
            catch (DllNotFoundException) { scaled = ScaleAdd(42, 7); }
            Console.WriteLine($"nimpl scaleadd={scaled}");

            nint off;
            try { off = nimpl_offset((nint)v, 8); }
            catch (DllNotFoundException) { off = (nint)Offset(v, 8); }
            Console.WriteLine($"nimpl offset-delta={off - (nint)v}");
        }
    }
}
