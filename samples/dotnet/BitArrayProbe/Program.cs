using System;
using System.Collections;
using System.Text;

namespace BitArrayProbe;

// Exercises System.Collections.BitArray's And / Or / Xor / Not. In real .NET each has a
// vectorized fast path over the backing int[], guarded by Vector(128/256).IsHardwareAccelerated
// — the ISimdVector static-abstract dispatch the transpiler lowers to dn2cpp_vec_and / _or /
// _xor / _ones_complement. The scalar build folds the HW-accel gate and runs the int-at-a-time
// loop; the Highway build flips it true and runs the SIMD path. Both must produce identical
// bits, so the output is diffed EXACT vs real .NET. Sizes cross the SIMD width: 256 bits is a
// whole number of Vector256 chunks, 300 bits adds a scalar tail past the last full chunk.
internal static class Program
{
    private static void Main()
    {
        RunPair(256);
        RunPair(300);
        Dump("not256", new BitArray(Pattern(256, 4)).Not());
        Dump("not300", new BitArray(Pattern(300, 4)).Not());
    }

    private static void RunPair(int n)
    {
        BitArray a = Pattern(n, 3);   // every 3rd bit set
        BitArray b = Pattern(n, 5);   // every 5th bit set
        Dump("and" + n, new BitArray(a).And(b));
        Dump("or" + n, new BitArray(a).Or(b));
        Dump("xor" + n, new BitArray(a).Xor(b));
    }

    private static BitArray Pattern(int n, int step)
    {
        BitArray ba = new BitArray(n);
        for (int i = 0; i < n; i++)
            ba[i] = (i % step) == 0;
        return ba;
    }

    private static void Dump(string label, BitArray ba)
    {
        int words = (ba.Length + 31) / 32;
        int[] buf = new int[words];
        ba.CopyTo(buf, 0);
        StringBuilder sb = new StringBuilder();
        sb.Append(label).Append('=');
        for (int i = 0; i < words; i++)
            sb.Append(buf[i].ToString("x8"));
        Console.WriteLine(sb.ToString());
    }
}
