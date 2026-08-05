#nullable enable
using System;
using System.Security.Cryptography;

// RandomNumberGenerator through AppleCryptoNative_GetRandomBytes. Only
// derived properties are printed (real entropy — raw bytes would break the
// diff): fills differ from each other and from zero, GetInt32 respects its
// bounds, GetBytes returns the requested length.
namespace RandomBytesSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- RandomNumberGenerator --");
        byte[] a = RandomNumberGenerator.GetBytes(32);
        byte[] b = RandomNumberGenerator.GetBytes(32);
        Console.WriteLine(a.Length + " " + b.Length);
        Console.WriteLine(!a.AsSpan().SequenceEqual(b)); // 2^-256 false-positive: fine
        bool anyNonZero = false;
        foreach (byte x in a)
            if (x != 0) { anyNonZero = true; break; }
        Console.WriteLine(anyNonZero);

        var buf = new byte[16];
        RandomNumberGenerator.Fill(buf);
        byte[] before = (byte[])buf.Clone();
        RandomNumberGenerator.Fill(buf);
        Console.WriteLine(!buf.AsSpan().SequenceEqual(before));

        bool inRange = true;
        for (int i = 0; i < 1000; i++)
        {
            int v = RandomNumberGenerator.GetInt32(10, 20);
            if (v < 10 || v >= 20) { inRange = false; break; }
        }
        Console.WriteLine(inRange);
    }
}
