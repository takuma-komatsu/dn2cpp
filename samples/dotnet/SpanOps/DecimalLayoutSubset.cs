using System;
using System.Runtime.InteropServices;

namespace DecimalLayoutSubset;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== decimal raw layout ==");
        decimal[] values =
        {
            1.50m,
            -123.456m,
            0.0000000000000000000000000001m,
            decimal.MaxValue,
            new decimal(0, 0, 0, true, 3),
        };
        string[] names = { "scale-two", "negative", "scale-28", "max-value", "negative-zero" };
        // Array elements expose only decimal storage, without surrounding struct padding.
        Span<byte> raw = MemoryMarshal.AsBytes(values.AsSpan());
        Console.WriteLine("decimal raw bytes=" + raw.Length);
        for (int i = 0; i < values.Length; i++)
        {
            decimal value = values[i];
            ReadOnlySpan<byte> bytes = raw.Slice(i * 16, 16);
            byte[] written = new byte[16];
            MemoryMarshal.Write(written.AsSpan(), value);
            decimal readRaw = MemoryMarshal.Read<decimal>(bytes);
            decimal readWritten = MemoryMarshal.Read<decimal>(written);

            Console.WriteLine(names[i] + " raw=" + Convert.ToHexString(bytes));
            Console.WriteLine(names[i] + " write=" + Convert.ToHexString(written));
            Console.WriteLine(names[i] + " original bits=" + Bits(value));
            Console.WriteLine(names[i] + " raw-read bits=" + Bits(readRaw));
            Console.WriteLine(names[i] + " write-read bits=" + Bits(readWritten));
        }
        Console.WriteLine("decimal raw layout end");
    }

    private static string Bits(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        return bits[0].ToString("X8") + "," + bits[1].ToString("X8") + ","
            + bits[2].ToString("X8") + "," + bits[3].ToString("X8");
    }
}
