using System;

namespace Dn2Cpp;

internal static class DecimalBitsSubset
{
    public static void Run()
    {
        Console.WriteLine("== decimal bit conversions ==");
        foreach (int[]? bits in new int[]?[]
        {
            null, Array.Empty<int>(), new int[3], new int[5],
            new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, int.MinValue },
            new[] { -1, -1, -1, 28 << 16 }, new[] { 12345, 0, 0, (2 << 16) | int.MinValue },
            new[] { 0, 0, 0, 1 }, new[] { 0, 0, 0, 1 << 24 },
            new[] { 0, 0, 0, 29 << 16 }, new[] { 0, 0, 0, 255 << 16 }
        })
        {
            string label = bits is null ? "null" : string.Join(",", bits);
            Show("array:" + label, () => Words(new decimal(bits!)));
            Show("span:" + label, () => Words(new decimal(bits.AsSpan())));
        }
        foreach (decimal value in new[] { 0m, new decimal(0, 0, 0, true, 28), -123.4500m, decimal.MaxValue })
        {
            foreach (int capacity in new[] { 0, 3, 4, 6 })
            {
                int[] destination = new int[capacity];
                Array.Fill(destination, 77);
                Show("get bits:" + value + ":" + capacity, () => decimal.GetBits(value, destination.AsSpan()).ToString());
                Console.WriteLine("get destination=" + string.Join(",", destination));
                Array.Fill(destination, 77);
                int written = -1;
                bool success = decimal.TryGetBits(value, destination.AsSpan(), out written);
                Console.WriteLine("try bits:" + value + ":" + capacity + "=" + success + ":" + written + ":" + string.Join(",", destination));
            }
        }
        for (int scale = 0; scale <= byte.MaxValue; scale++)
        {
            int current = scale;
            Show("parts:" + current, () => Words(new decimal(-1, -1, -1, true, (byte)current)));
        }
        Console.WriteLine("decimal bit conversions end");
    }

    private static string Words(decimal value) => string.Join(",", decimal.GetBits(value));

    private static void Show(string label, Func<string> action)
    {
        try { Console.WriteLine(label + "=" + action()); }
        catch (ArgumentException ex)
        {
            var range = ex as ArgumentOutOfRangeException;
            Console.WriteLine(label + "=" + ex.GetType().Name + ":" + ex.ParamName + ":" +
                (range?.ActualValue is null ? "null" : range.ActualValue.GetType().Name + ":" + range.ActualValue) + ":" +
                ex.Message.Replace("\r", "").Replace("\n", "|"));
        }
    }
}
