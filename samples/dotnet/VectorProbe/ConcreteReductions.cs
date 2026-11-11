using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VectorProbe;

// Vector2/Vector3 reductions see only their named components: the padding lanes
// of the widened register never match, count or block All. Numeric equality
// treats signed zeros as equal and NaN as unequal; WhereAllBitsSet tests bits.
internal static partial class Program
{
    private static readonly uint[][] ReductionLanes =
    {
        new uint[] { 0, 0, 0 },
        new uint[] { 0x80000000, 0, 0x80000000 },
        new uint[] { 0x3F800000, 0x40000000, 0x40400000 },
        new uint[] { 0xBF800000, 0x40000000, 0xBF800000 },
        new uint[] { 0x7F800000, 0xFF800000, 0x7F800000 },
        new uint[] { 0x7FC01234, 0x7F800001, 0 },
        new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF },
        new uint[] { 0xFFFFFFFF, 0, 0xFFFFFFFF },
    };
    private static readonly uint[] ReductionScalars = { 0, 0x80000000, 0x40000000, 0xBF800000, 0x7F800000, 0x7FC01234 };
    private static string ReductionEffects = "";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 ReductionVector()
    {
        ReductionEffects += "V";
        return new Vector3(1, 2, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ReductionScalar()
    {
        ReductionEffects += "S";
        return 1;
    }

    private static void ConcreteReductionsSection()
    {
        Console.WriteLine("-- concrete vector reductions --");
        for (int row = 0; row < ReductionLanes.Length; row++)
        {
            uint[] bits = ReductionLanes[row];
            var v2 = new Vector2(BitConverter.UInt32BitsToSingle(bits[0]), BitConverter.UInt32BitsToSingle(bits[1]));
            var v3 = new Vector3(BitConverter.UInt32BitsToSingle(bits[0]), BitConverter.UInt32BitsToSingle(bits[1]),
                BitConverter.UInt32BitsToSingle(bits[2]));
            foreach (uint scalarBits in ReductionScalars)
            {
                float s = BitConverter.UInt32BitsToSingle(scalarBits);
                Console.WriteLine("reduce v2." + row + "." + scalarBits.ToString("X8") + "=" + Vector2.All(v2, s) + "," + Vector2.Any(v2, s)
                    + "," + Vector2.None(v2, s) + "," + Vector2.Count(v2, s) + "," + Vector2.IndexOf(v2, s) + "," + Vector2.LastIndexOf(v2, s));
                Console.WriteLine("reduce v3." + row + "." + scalarBits.ToString("X8") + "=" + Vector3.All(v3, s) + "," + Vector3.Any(v3, s)
                    + "," + Vector3.None(v3, s) + "," + Vector3.Count(v3, s) + "," + Vector3.IndexOf(v3, s) + "," + Vector3.LastIndexOf(v3, s));
            }
            Console.WriteLine("reduce bits v2." + row + "=" + Vector2.AllWhereAllBitsSet(v2) + "," + Vector2.AnyWhereAllBitsSet(v2)
                + "," + Vector2.NoneWhereAllBitsSet(v2) + "," + Vector2.CountWhereAllBitsSet(v2) + "," + Vector2.IndexOfWhereAllBitsSet(v2)
                + "," + Vector2.LastIndexOfWhereAllBitsSet(v2));
            Console.WriteLine("reduce bits v3." + row + "=" + Vector3.AllWhereAllBitsSet(v3) + "," + Vector3.AnyWhereAllBitsSet(v3)
                + "," + Vector3.NoneWhereAllBitsSet(v3) + "," + Vector3.CountWhereAllBitsSet(v3) + "," + Vector3.IndexOfWhereAllBitsSet(v3)
                + "," + Vector3.LastIndexOfWhereAllBitsSet(v3));
        }
        ReductionEffects = "";
        int count = Vector3.Count(ReductionVector(), ReductionScalar());
        Console.WriteLine("reduce order=" + count + " effects=" + ReductionEffects);
        Console.WriteLine("-- concrete vector reductions end --");
    }
}
