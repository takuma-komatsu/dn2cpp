using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StringPrivateEntryPointsSubset
{
    internal static unsafe class Probe
    {
        internal delegate ref byte RawGetter(string value);
        internal delegate int LengthGetter(char* value);

        // Public equivalents are the CLR oracle; the native fixture rewrites only these entry calls.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ref byte RawDirect(string value) => ref Unsafe.As<char, byte>(ref Unsafe.AsRef(in value.GetPinnableReference()));
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ref byte RawVirtual(string value) => ref RawDirect(value);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ref byte RawPointer(string value) => ref RawDirect(value);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int LengthDirect(char* value) => MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value).Length;
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int LengthPointer(char* value) => LengthDirect(value);
        internal static MethodInfo LookupRaw() => typeof(string).GetMethod("GetRawStringDataAsUInt8", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static MethodInfo LookupLength() => typeof(string).GetMethod("wcslen", BindingFlags.Static | BindingFlags.NonPublic);
        // The native fixture supplies genuine method tokens; these remain outside CLR parity.
        internal static MethodInfo TokenRaw() => throw new InvalidOperationException("native token fixture missing");
        internal static MethodInfo TokenLength() => throw new InvalidOperationException("native token fixture missing");
        internal static void RunNativeProbe()
        {
            RunProbe();
            Console.WriteLine("== string method token boundary ==");
            try { TokenRaw(); throw new InvalidOperationException("raw method token unexpectedly returned"); }
            catch (PlatformNotSupportedException error)
            {
                if (error.GetType() != typeof(PlatformNotSupportedException)) throw;
                Console.WriteLine("token raw=PlatformNotSupportedException");
            }
            try { TokenLength(); throw new InvalidOperationException("length method token unexpectedly returned"); }
            catch (PlatformNotSupportedException error)
            {
                if (error.GetType() != typeof(PlatformNotSupportedException)) throw;
                Console.WriteLine("token length=PlatformNotSupportedException");
            }
            Console.WriteLine("string method token boundary end");
        }

        internal static string Allocating(int count) => new string('x', count);
        internal static void RunAllocationControl() { PinCulture(); Console.WriteLine(Allocating(3)); }
        internal static void RunRawReferenceControl() { PinCulture(); Console.WriteLine(RawDirect("A")); }
        internal static void RunLengthReferenceControl() { PinCulture(); fixed (char* p = "A") Console.WriteLine(LengthDirect(p)); }

        private static void PinCulture()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        internal static void RunPublicPrefix()
        {
            PinCulture();
            Console.WriteLine("== string private public prefix ==");
            string value = "A\0\u3042\uD83D\uDE00";
            fixed (char* p = value)
                Console.WriteLine("public utf16=" + value.Length + ":" + MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p).Length);
            Console.WriteLine("public null span=" + MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)null).Length);
            ref char first = ref Unsafe.AsRef(in value.GetPinnableReference());
            Console.WriteLine("public byte view=" + Unsafe.As<char, byte>(ref first));
            IntPtr co = Marshal.StringToCoTaskMemUni("co\u3042");
            Console.WriteLine("public co=" + Marshal.PtrToStringUni(co));
            Marshal.ZeroFreeCoTaskMemUnicode(co);
            IntPtr global = Marshal.StringToHGlobalUni("global\u3042");
            Console.WriteLine("public global=" + Marshal.PtrToStringUni(global));
            Marshal.ZeroFreeGlobalAllocUnicode(global);
            Marshal.ZeroFreeCoTaskMemUnicode(IntPtr.Zero);
            Marshal.ZeroFreeGlobalAllocUnicode(IntPtr.Zero);
            Console.WriteLine("string private public prefix end");
        }

        internal static void RunProbe()
        {
            RunPublicPrefix();
            Console.WriteLine("== string private entry points ==");
            string[] values = { "", "A", "A\0B", "\u3042", "\uD83D\uDE00", "\uD800x\uDC00" };
            MethodInfo namedRaw = typeof(string).GetMethod("GetRawStringDataAsUInt8", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo namedLength = typeof(string).GetMethod("wcslen", BindingFlags.NonPublic | BindingFlags.Static);
            if (namedRaw == null || namedLength == null) throw new InvalidOperationException("Missing named String entry row");
            Console.WriteLine("named declaring=" + (namedRaw.DeclaringType == typeof(string)) + ":" + (namedLength.DeclaringType == typeof(string)));
            RawGetter rawNamed = (RawGetter)namedRaw.CreateDelegate(typeof(RawGetter));
            RawGetter rawLookup = (RawGetter)LookupRaw().CreateDelegate(typeof(RawGetter));
            LengthGetter lengthNamed = (LengthGetter)namedLength.CreateDelegate(typeof(LengthGetter));
            LengthGetter lengthLookup = (LengthGetter)LookupLength().CreateDelegate(typeof(LengthGetter));
            Console.WriteLine("named flags="
                + (typeof(string).GetMethod("GetRawStringDataAsUInt8", BindingFlags.Public | BindingFlags.Instance) == null) + ":"
                + (typeof(string).GetMethod("GetRawStringDataAsUInt8", BindingFlags.NonPublic | BindingFlags.Static) == null) + ":"
                + (typeof(string).GetMethod("wcslen", BindingFlags.NonPublic | BindingFlags.Instance) == null));
            Console.WriteLine("named case=" + (typeof(string).GetMethod("WCSLEN", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase) != null));
            Console.WriteLine("named signature="
                + (typeof(string).GetMethod("wcslen", 0, BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(char*) }, null) != null) + ":"
                + (typeof(string).GetMethod("wcslen", 1, BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(char*) }, null) == null) + ":"
                + (typeof(string).GetMethod("wcslen", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(byte*) }, null) == null) + ":"
                + (typeof(string).GetMethod("GetRawStringDataAsUInt8", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(byte) }, null) == null));
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                ref byte expected = ref Unsafe.As<char, byte>(ref Unsafe.AsRef(in value.GetPinnableReference()));
                ref byte direct = ref RawDirect(value);
                ref byte virt = ref RawVirtual(value);
                ref byte pointer = ref RawPointer(value);
                ref byte named = ref rawNamed(value);
                ref byte lookup = ref rawLookup(value);
                bool identity = Unsafe.AreSame(ref expected, ref direct) && Unsafe.AreSame(ref direct, ref virt)
                    && Unsafe.AreSame(ref direct, ref pointer) && Unsafe.AreSame(ref direct, ref named) && Unsafe.AreSame(ref direct, ref lookup);
                Console.WriteLine("raw " + i + " identity=" + identity + ":nonnull=" + !Unsafe.IsNullRef(ref direct));
                Console.Write("bytes " + i + "=");
                for (int b = 0; b < value.Length * 2 + 2; b++) Console.Write(Unsafe.Add(ref direct, b).ToString("X2"));
                Console.WriteLine();
            }
            try { byte ignored = RawDirect(null); Console.WriteLine("null direct=returned"); }
            catch (NullReferenceException) { Console.WriteLine("null direct=NullReferenceException"); }
            try { byte ignored = RawVirtual(null); Console.WriteLine("null virtual=returned"); }
            catch (NullReferenceException) { Console.WriteLine("null virtual=NullReferenceException"); }
            try { byte ignored = RawPointer(null); Console.WriteLine("null pointer=returned"); }
            catch (NullReferenceException) { Console.WriteLine("null pointer=NullReferenceException"); }
            int[] lengths = { 0, 1, 2, 3, 7, 8, 15, 16, 31, 32, 63, 64, 127, 128, 257 };
            char[] data = new char[280];
            for (int offset = 0; offset < 8; offset++)
            {
                foreach (int count in lengths)
                {
                    for (int i = 0; i < data.Length; i++) data[i] = i % 2 == 0 ? '\u3042' : '\uD800';
                    data[offset + count] = '\0';
                    fixed (char* start = data)
                    {
                        char* p = start + offset;
                        Console.WriteLine("length " + offset + ":" + count + "=" + LengthDirect(p) + ":" + LengthPointer(p)
                            + ":" + lengthNamed(p) + ":" + lengthLookup(p) + ":sentinel=" + (p[count + 1] != '\0'));
                    }
                }
            }
            Console.WriteLine("string private entry points end");
        }
    }
}
