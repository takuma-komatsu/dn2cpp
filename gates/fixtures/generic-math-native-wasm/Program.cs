using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace GenericMathNativeWasm;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Fields<T> where T : unmanaged
    {
        public T Before;
        public T Value;
        public T After;
    }

    private static void Check(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException("native32: " + label);
        Console.WriteLine(label + "=True");
    }

    private static void Run<T>(string label, bool signed, T min, T max, T high, T negativeOne,
        T signedMax, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        Endian(label, signed, min, max, high, negativeOne, signedMax, before, after);
        Parsing(label, signed, min, max, high, negativeOne, signedMax, before, after);
        return;
    }

    private static unsafe void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool oracle = args.Length == 1 && args[0] == "--oracle32";
        if (oracle)
        {
            // The CLR host's native integer width cannot be the wasm32 oracle.
            Check("generic native width=4", sizeof(int) == 4 && sizeof(uint) == 4);
            Bind<int>(int.Parse, int.TryParse, int.Parse, int.TryParse, int.Parse, int.TryParse);
            Run<int>("signed", true, int.MinValue, int.MaxValue, int.MinValue, -1,
                int.MaxValue, 0x12345678, unchecked((int)0xFEDCBA98));
            Bind<uint>(uint.Parse, uint.TryParse, uint.Parse, uint.TryParse, uint.Parse, uint.TryParse);
            Run<uint>("unsigned", false, 0, uint.MaxValue, 0x80000000, 0,
                int.MaxValue, 0x12345678, 0xFEDCBA98);
        }
        else
        {
            Check("generic native width=4", IntPtr.Size == 4 && UIntPtr.Size == 4
                && sizeof(nint) == 4 && sizeof(nuint) == 4);
            Bind<nint>(nint.Parse, nint.TryParse, nint.Parse, nint.TryParse, nint.Parse, nint.TryParse);
            Run<nint>("signed", true, int.MinValue, int.MaxValue, int.MinValue, -1,
                int.MaxValue, 0x12345678, unchecked((int)0xFEDCBA98));
            // Keep both the value and canary comparisons in T, including the high bit.
            Bind<nuint>(nuint.Parse, nuint.TryParse, nuint.Parse, nuint.TryParse, nuint.Parse, nuint.TryParse);
            Run<nuint>("unsigned", false, 0, uint.MaxValue, 0x80000000U, 0,
                int.MaxValue, 0x12345678, 0xFEDCBA98U);
        }
        Console.WriteLine("generic native32 end");
        return;
    }
}
