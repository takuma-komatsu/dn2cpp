using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KnownConstantSubset;

internal static class Program
{
    private enum Choice { First, Last = 17 }
    private struct Plain { public long Value; }
    private struct WithReference { public string Value; }
    private struct Box<T> { public T Value; public string Reference; }
    private static void Check(string label, bool value)
    {
        Console.WriteLine("known-constant-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static void Searches<T>(T a, T b, T c, T other) where T : IEquatable<T>
    {
        foreach (int length in new[] { 0, 1, 2, 3, 7, 15, 16, 17, 31, 32, 33, 65, 129 })
        foreach (int position in new[] { -1, 0, length / 2, length - 1 })
        {
            T[] data = new T[length + 2]; Array.Fill(data, other); data[0] = a; data[length + 1] = b;
            int expected = position >= 0 && position < length ? position : -1;
            if (expected >= 0) data[expected + 1] = a;
            ReadOnlySpan<T> span = new ReadOnlySpan<T>(data, 1, length);
            if (span.IndexOfAny(a, b) != expected || span.IndexOfAny(a, b, c) != expected || span.IndexOfAny(a, a) != expected)
                throw new InvalidOperationException("IndexOfAny slice boundary");
            Array.Fill(data, a); data[0] = other; data[length + 1] = other;
            if (expected >= 0) data[expected + 1] = other;
            if (span.IndexOfAnyExcept(a, b) != expected || span.IndexOfAnyExcept(a, b, c) != expected || span.IndexOfAnyExcept(a, a) != expected)
                throw new InvalidOperationException("IndexOfAnyExcept slice boundary");
            T[] candidates = { a, b, c };
            for (int candidate = 0; candidate < candidates.Length; candidate++)
            {
                Array.Fill(data, other); data[0] = a; data[length + 1] = b;
                if (expected >= 0) data[expected + 1] = candidates[candidate];
                int expectedTwo = candidate < 2 ? expected : -1;
                if (span.IndexOfAny(a, b) != expectedTwo || span.IndexOfAny(a, b, c) != expected)
                    throw new InvalidOperationException("IndexOfAny independent candidate");
            }
            for (int count = 2; count <= 3; count++)
            {
                for (int i = 1; i <= length; i++) data[i] = candidates[(i - 1) % count];
                data[0] = other; data[length + 1] = other;
                if (expected >= 0) data[expected + 1] = other;
                int actual = count == 2 ? span.IndexOfAnyExcept(a, b) : span.IndexOfAnyExcept(a, b, c);
                if (actual != expected) throw new InvalidOperationException("IndexOfAnyExcept mixed candidates");
            }
        }
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Known constant public searches ==");
        Searches((byte)1, (byte)2, (byte)3, (byte)255);
        Searches('A', 'a', 'B', '\0'); Searches('\u1234', '\uffff', '\ud800', '\0');
        Searches((short)65, (short)97, (short)66, (short)-1); Searches(1, 2, 3, -1);
        Check("public-searches", true);
        // Byte SearchValues over a vector-length input take the ASCII shuffle searcher.
        byte[] text = new byte[96];
        for (int i = 0; i < text.Length; i++) text[i] = (byte)('b' + i % 3);
        text[70] = (byte)'o';
        System.Buffers.SearchValues<byte> vowels = System.Buffers.SearchValues.Create("aeiouAEIOU"u8);
        Console.WriteLine("known-constant-search-values:" + text.AsSpan().IndexOfAny(vowels) + ":" + text.AsSpan().LastIndexOfAny(vowels)
            + ":" + text.AsSpan().IndexOfAnyExcept(vowels) + ":" + text.AsSpan(0, 70).IndexOfAny(vowels) + ":" + text.AsSpan(71).IndexOfAny(vowels));
        Console.WriteLine("Known constant public searches end");
    }
}
