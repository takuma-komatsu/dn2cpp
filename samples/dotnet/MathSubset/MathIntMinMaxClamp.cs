using System;

// Math.Clamp (int/long/double) and Math.Sign (always an int result, whatever
// the numeric input). Also the integral Min/Max/Clamp/Abs overloads across
// every width and signedness: the unsigned and sub-word overloads must keep an
// integral stack kind — routing them through std::fmin/fmax would fail the
// stack-kind merge and lose precision above 2^53.
namespace MathIntMinMaxClamp;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine(Math.Clamp(15, 0, 10));   // 10
        Console.WriteLine(Math.Clamp(-3, 0, 10));    // 0
        Console.WriteLine(Math.Clamp(5, 0, 10));     // 5
        Console.WriteLine(Math.Clamp(7L, 0L, 4L));   // 4
        Console.WriteLine(Math.Clamp(2.5, 0.0, 5.0));// 2.5
        Console.WriteLine(Math.Clamp(9.0, 0.0, 5.0));// 5

        Console.WriteLine(Math.Sign(-5));            // -1
        Console.WriteLine(Math.Sign(0));             // 0
        Console.WriteLine(Math.Sign(42));            // 1
        Console.WriteLine(Math.Sign(-2.5));          // -1
        Console.WriteLine(Math.Sign(3L));            // 1

        // Min/Max over uint: wrong if the operands are compared as int32
        // (uint.MaxValue would read as -1 and lose to 1).
        Console.WriteLine(Math.Min(uint.MaxValue, 1u));   // 1
        Console.WriteLine(Math.Max(uint.MaxValue, 1u));   // 4294967295
        Console.WriteLine(Math.Min(0u, 5u));              // 0

        // Min/Max over ulong including values above 2^53: wrong through double.
        Console.WriteLine(Math.Max(9007199254740993UL, 9007199254740992UL)); // 9007199254740993
        Console.WriteLine(Math.Min(9007199254740993UL, 9007199254740992UL)); // 9007199254740992
        Console.WriteLine(Math.Max(18446744073709551615UL, 1UL));            // 18446744073709551615

        // Min/Max over the sub-word integers (byte/sbyte/short/ushort).
        Console.WriteLine(Math.Min((byte)200, (byte)100));      // 100
        Console.WriteLine(Math.Max((sbyte)-100, (sbyte)50));    // 50
        Console.WriteLine(Math.Min((short)-30000, (short)100)); // -30000
        Console.WriteLine(Math.Max((ushort)60000, (ushort)100));// 60000

        // Clamp over uint/ulong/byte/short/ushort with out-of-range inputs.
        Console.WriteLine(Math.Clamp(uint.MaxValue, 10u, 20u));               // 20
        Console.WriteLine(Math.Clamp(5u, 10u, 20u));                          // 10
        Console.WriteLine(Math.Clamp(18446744073709551615UL, 1UL, 100UL));    // 100
        Console.WriteLine(Math.Clamp((byte)5, (byte)10, (byte)200));          // 10
        Console.WriteLine(Math.Clamp((short)-30000, (short)-10, (short)10));   // -10
        Console.WriteLine(Math.Clamp((ushort)60000, (ushort)10, (ushort)20)); // 20

        // Abs over sbyte/short (negative inputs; no unsigned Abs overloads).
        Console.WriteLine(Math.Abs((sbyte)-100));   // 100
        Console.WriteLine(Math.Abs((short)-5));     // 5
        Console.WriteLine(Math.Abs((short)-30000)); // 30000

        // Stack-kind-merge shape: a Math.Min result consumed across a branch —
        // the pattern that previously threw "inconsistent stack" when the
        // integer overload took the R8 (double) arm. Array reads keep the
        // condition and operands off the constant-folding path so a real branch
        // with a merge point is emitted.
        uint[] uv = { 7u, 3u, 100u };
        uint ur = uv.Length == 3 ? Math.Min(uv[0], uv[1]) : uv[2];
        Console.WriteLine(ur);   // 3
        ulong[] lv = { 9007199254740993UL, 9007199254740992UL, 0UL };
        ulong lr = lv.Length == 3 ? Math.Max(lv[0], lv[1]) : lv[2];
        Console.WriteLine(lr);   // 9007199254740993
    }
}
