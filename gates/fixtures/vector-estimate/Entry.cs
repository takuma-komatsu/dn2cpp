using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VectorProbe;

internal static partial class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length != 0 && args[0] == "--capabilities")
        {
            Console.WriteLine("vector64=" + Vector64.IsHardwareAccelerated);
            Console.WriteLine("vector128=" + Vector128.IsHardwareAccelerated);
            Console.WriteLine("vector256=" + Vector256.IsHardwareAccelerated);
            Console.WriteLine("vector512=" + Vector512.IsHardwareAccelerated);
            Console.WriteLine("numerics=" + Vector.IsHardwareAccelerated);
            Console.WriteLine("numerics-single-count=" + Vector<float>.Count);
            Console.WriteLine("fma=" + Fma.IsSupported);
            return;
        }
        MultiplyAddEstimates(true);
    }
}
