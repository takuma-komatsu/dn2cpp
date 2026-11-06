using System;
using System.Buffers;
using System.Buffers.Text;

namespace Dn2Cpp;

internal static class DecimalDivModSubset
{
    private delegate uint Divider(ref decimal value);
    private sealed class Holder { public decimal Value; }
    private static int _refCalls;
    private static ref decimal GetRef(Holder holder) { _refCalls++; return ref holder.Value; }

    // The CLR oracle uses public integer arithmetic. The fixture replaces references
    // to this method only in the transpiler input, never in the CLR oracle.
    internal static uint Divide(ref decimal value)
    {
        int[] bits = decimal.GetBits(value);
        UInt128 magnitude = ((UInt128)(uint)bits[2] << 64) | ((UInt128)(uint)bits[1] << 32) | (uint)bits[0];
        UInt128 quotient = magnitude / 1000000000u;
        uint remainder = (uint)(magnitude % 1000000000u);
        value = new decimal((int)(uint)quotient, (int)(uint)(quotient >> 32), (int)(uint)(quotient >> 64), bits[3] < 0, (byte)((uint)bits[3] >> 16));
        return remainder;
    }

    internal static uint RetainedOnly(ref decimal value) => Divide(ref value);

    private static void Check(decimal value, uint lo, uint mid, uint hi, int flags)
    {
        int[] bits = decimal.GetBits(value);
        if ((uint)bits[0] != lo || (uint)bits[1] != mid || (uint)bits[2] != hi || bits[3] != flags)
            throw new Exception("divmod coefficient/flags");
    }

    internal static void Run()
    {
        Console.WriteLine("== decimal coefficient divmod ==");
        Divider address = Divide;
        int cases = 0;
        foreach (uint[] row in Vectors)
        for (byte scale = 0; scale <= 28; scale++)
        for (int sign = 0; sign < 2; sign++)
        {
            decimal input = new decimal((int)row[0], (int)row[1], (int)row[2], sign != 0, scale);
            int flags = (sign << 31) | (scale << 16);
            decimal local = input;
            ref decimal alias = ref local;
            uint remainder = Divide(ref local);
            Check(alias, row[3], row[4], row[5], flags);
            if (remainder != row[6]) throw new Exception("direct remainder");
            decimal[] array = { 17m, input, -19m };
            ref decimal arrayAlias = ref array[1];
            remainder = address(ref array[1]);
            Check(arrayAlias, row[3], row[4], row[5], flags);
            if (remainder != row[6] || array[0] != 17m || array[2] != -19m) throw new Exception("address/storage");
            var holder = new Holder { Value = input };
            int before = _refCalls;
            remainder = Divide(ref GetRef(holder));
            Check(holder.Value, row[3], row[4], row[5], flags);
            if (remainder != row[6] || _refCalls != before + 1) throw new Exception("ref evaluation");
            cases++;
        }
        decimal repeated = new decimal(-1, -1, -1, true, 28);
        uint[] remainders = { 543950335u, 264337593u, 228162514u, 79u, 0u };
        uint[][] quotients = { new uint[] { 3047500985u, 1266874889u, 4u }, new uint[] { 1918751186u, 18u, 0u }, new uint[] { 79u, 0u, 0u }, new uint[] { 0u, 0u, 0u }, new uint[] { 0u, 0u, 0u } };
        int repeatedIndex = 0;
        foreach (uint expected in remainders)
        {
            uint actual = address(ref repeated);
            if (actual != expected || decimal.GetBits(repeated)[3] != unchecked((int)0x801c0000))
                throw new Exception("repeated division");
            uint[] quotient = quotients[repeatedIndex++];
            Check(repeated, quotient[0], quotient[1], quotient[2], unchecked((int)0x801c0000));
            Console.WriteLine("divmod repeated:" + actual);
        }
        Check(repeated, 0, 0, 0, unchecked((int)0x801c0000));
        Console.WriteLine("divmod magnitude/sign/scale cases:" + cases);
        foreach (decimal value in new[] { 0m, new decimal(0, 0, 0, true, 28), decimal.MaxValue, -1234567890123456789.000000001m, 1000000000000000001m, 1.2300m })
        {
            byte[] buffer = new byte[128];
            if (!Utf8Formatter.TryFormat(value, buffer.AsSpan(1, 100), out int written)) throw new Exception("format");
            string text = System.Text.Encoding.UTF8.GetString(buffer, 1, written);
            Console.WriteLine("divmod utf8:" + text + ":" + written);
            foreach (int length in new[] { written, written - 1 })
            {
                Array.Fill(buffer, (byte)0x5a);
                bool ok = Utf8Formatter.TryFormat(value, buffer.AsSpan(1, length), out int count);
                if (buffer[0] != 0x5a || buffer[length + 1] != 0x5a || ok != (length == written)
                    || count != (ok ? written : 0) || ok && System.Text.Encoding.UTF8.GetString(buffer, 1, count) != text) throw new Exception("format bounds");
            }
        }
        Console.WriteLine("decimal coefficient divmod end");
    }

    private static readonly uint[][] Vectors = {
        new uint[] { 0u, 0u, 0u, 0u, 0u, 0u, 0u },
        new uint[] { 1u, 0u, 0u, 0u, 0u, 0u, 1u },
        new uint[] { 999999999u, 0u, 0u, 0u, 0u, 0u, 999999999u },
        new uint[] { 1000000000u, 0u, 0u, 1u, 0u, 0u, 0u },
        new uint[] { 1000000001u, 0u, 0u, 1u, 0u, 0u, 1u },
        new uint[] { 4294967295u, 0u, 0u, 4u, 0u, 0u, 294967295u },
        new uint[] { 0u, 1u, 0u, 4u, 0u, 0u, 294967296u },
        new uint[] { 1u, 1u, 0u, 4u, 0u, 0u, 294967297u },
        new uint[] { 4294967295u, 999999999u, 0u, 4294967295u, 0u, 0u, 999999999u },
        new uint[] { 0u, 1000000000u, 0u, 0u, 1u, 0u, 0u },
        new uint[] { 1u, 1000000000u, 0u, 0u, 1u, 0u, 1u },
        new uint[] { 4294967295u, 4294967295u, 0u, 1266874889u, 4u, 0u, 709551615u },
        new uint[] { 0u, 0u, 1u, 1266874889u, 4u, 0u, 709551616u },
        new uint[] { 1u, 0u, 1u, 1266874889u, 4u, 0u, 709551617u },
        new uint[] { 4294967295u, 4294967295u, 999999999u, 4294967295u, 4294967295u, 0u, 999999999u },
        new uint[] { 0u, 0u, 1000000000u, 0u, 0u, 1u, 0u },
        new uint[] { 1u, 0u, 1000000000u, 0u, 0u, 1u, 1u },
        new uint[] { 4294967295u, 4294967295u, 4294967295u, 3047500985u, 1266874889u, 4u, 543950335u },
    };
}
