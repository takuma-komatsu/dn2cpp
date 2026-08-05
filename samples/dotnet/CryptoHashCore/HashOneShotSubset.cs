#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

// One-shot static hashing through the real System.Security.Cryptography IL:
// SHA256/SHA1/MD5/SHA384/SHA512.HashData over "abc", the empty input, and a
// 1 MiB deterministic (seeded PRNG) buffer. The "abc" rows double as NIST /
// RFC 1321 test-vector asserts; all output is Convert.ToHexString, so the
// native run diffs exactly against real .NET.
namespace HashOneShotSubset;

static class Program
{
    internal static byte[] SeededBuffer(int length, int seed)
    {
        var buf = new byte[length];
        var rng = new Random(seed);
        rng.NextBytes(buf);
        return buf;
    }

    private static void Show(string name, Func<byte[], byte[]> hash, byte[] abc, byte[] empty, byte[] big)
    {
        Console.WriteLine(name + " abc:   " + Convert.ToHexString(hash(abc)));
        Console.WriteLine(name + " empty: " + Convert.ToHexString(hash(empty)));
        Console.WriteLine(name + " 1MiB:  " + Convert.ToHexString(hash(big)));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- one-shot HashData --");
        byte[] abc = Encoding.ASCII.GetBytes("abc");
        byte[] empty = Array.Empty<byte>();
        byte[] big = SeededBuffer(1024 * 1024, 12345);

        Show("SHA256", SHA256.HashData, abc, empty, big);
        Show("SHA1  ", SHA1.HashData, abc, empty, big);
        Show("MD5   ", MD5.HashData, abc, empty, big);
        Show("SHA384", SHA384.HashData, abc, empty, big);
        Show("SHA512", SHA512.HashData, abc, empty, big);

        // Known-answer sanity asserts (independent of the diff oracle).
        Console.WriteLine("-- known vectors --");
        Console.WriteLine(Convert.ToHexString(SHA256.HashData(abc))
            == "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
        Console.WriteLine(Convert.ToHexString(SHA1.HashData(abc))
            == "A9993E364706816ABA3E25717850C26C9CD0D89D");
        Console.WriteLine(Convert.ToHexString(MD5.HashData(abc))
            == "900150983CD24FB0D6963F7D28E17F72");

        // Span destination form (TryHashData) + hash sizes.
        Console.WriteLine("-- TryHashData / sizes --");
        Span<byte> dest = stackalloc byte[SHA256.HashSizeInBytes];
        Console.WriteLine(SHA256.TryHashData(abc, dest, out int written) + " " + written);
        Console.WriteLine(Convert.ToHexString(dest));
        Console.WriteLine(SHA1.HashSizeInBytes + " " + MD5.HashSizeInBytes + " "
            + SHA256.HashSizeInBytes + " " + SHA384.HashSizeInBytes + " " + SHA512.HashSizeInBytes);
    }
}
