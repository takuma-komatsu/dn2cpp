using System;
using System.Collections.Generic;

namespace SpanOps;

internal static class SpanOverlapSubset
{
    internal static void Run()
    {
        Console.WriteLine("== overlapping span copies ==");
        Check("bool", new[] { false, true, false, true, true, false });
        Check("char", new[] { 'a', '漢', 'c', 'd', 'e', 'f' });
        Check("sbyte", new sbyte[] { sbyte.MinValue, -1, 0, 1, 42, sbyte.MaxValue });
        Check("byte", new byte[] { 0, 1, 42, 128, 254, 255 });
        Check("short", new short[] { short.MinValue, -1, 0, 1, 42, short.MaxValue });
        Check("ushort", new ushort[] { 0, 1, 42, 32768, 65534, 65535 });
        Check("int", new[] { int.MinValue, -1, 0, 1, 42, int.MaxValue });
        Check("uint", new uint[] { 0, 1, 42, 2147483648, 4294967294, uint.MaxValue });
        Check("long", new long[] { long.MinValue, -1, 0, 1, 42, long.MaxValue });
        Check("ulong", new ulong[] { 0, 1, 42, 9223372036854775808, 18446744073709551614, ulong.MaxValue });
        Check("nint", new nint[] { nint.MinValue, -1, 0, 1, 42, nint.MaxValue });
        Check("nuint", new nuint[] { 0, 1, 42, 128, 254, nuint.MaxValue });
        Check("float", new[] { -1.25f, float.NaN, 0, 1, 42, float.PositiveInfinity });
        Check("double", new[] { -1.25, double.NaN, 0, 1, 42, double.PositiveInfinity });
        Check("Half", new[] { (Half)(-1.25), Half.NaN, (Half)0, (Half)1, (Half)42, Half.PositiveInfinity });
        Check("decimal", new[] { decimal.MinValue, -1.25m, 0, 1, 42, decimal.MaxValue });
        Check("Int128", new Int128[] { Int128.MinValue, -1, 0, 1, 42, Int128.MaxValue });
        Check("UInt128", new UInt128[] { 0, 1, 42, 128, 254, UInt128.MaxValue });
        Check("string", new[] { "a", "漢", null, "d", "e", "f" });
        Check("reference struct", new[] {
            new Item("a", 0), new Item("漢", 1), new Item(null, 2),
            new Item("d", 3), new Item("e", 4), new Item("f", 5)
        });
        Console.WriteLine("overlapping span copies end");
    }

    private static void Check<T>(string name, T[] values)
    {
        foreach (var window in new[] { (0, 1, 5), (1, 0, 5), (0, 0, 6), (0, 4, 2), (6, 6, 0) })
        {
            T[] expected = (T[])values.Clone();
            for (int i = 0; i < window.Item3; i++)
                expected[window.Item2 + i] = values[window.Item1 + i];
            for (int form = 0; form < 4; form++)
            {
                T[] actual = (T[])values.Clone();
                Span<T> source = actual.AsSpan(window.Item1, window.Item3);
                Span<T> destination = actual.AsSpan(window.Item2, window.Item3);
                bool success = true;
                if (form == 0) source.CopyTo(destination);
                if (form == 1) success = source.TryCopyTo(destination);
                if (form == 2) ((ReadOnlySpan<T>)source).CopyTo(destination);
                if (form == 3) success = ((ReadOnlySpan<T>)source).TryCopyTo(destination);
                GC.Collect();
                for (int i = 0; i < actual.Length; i++)
                    if (!EqualityComparer<T>.Default.Equals(actual[i], expected[i]))
                        throw new InvalidOperationException(name + " overlap corrupted at " + i);
                Console.WriteLine(name + " " + window.Item1 + ":" + window.Item2 + ":" + window.Item3 + ":" + form + "=" + success);
            }
        }
        Span<T> shortDestination = new T[1];
        Span<T> two = values.AsSpan(0, 2);
        Console.WriteLine(name + " short span=" + two.TryCopyTo(shortDestination));
        Console.WriteLine(name + " short readonly=" + ((ReadOnlySpan<T>)two).TryCopyTo(shortDestination));
        try { two.CopyTo(shortDestination); }
        catch (ArgumentException e) { Console.WriteLine(name + " short copy=" + e.ParamName + ":" + e.Message); }
        try { ((ReadOnlySpan<T>)two).CopyTo(shortDestination); }
        catch (ArgumentException e) { Console.WriteLine(name + " short readonly copy=" + e.ParamName + ":" + e.Message); }
    }

    private readonly struct Item : IEquatable<Item>
    {
        private readonly string _text;
        private readonly int _number;
        internal Item(string text, int number) { _text = text; _number = number; }
        public bool Equals(Item other) => _text == other._text && _number == other._number;
    }
}
