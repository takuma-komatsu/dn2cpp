#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Stream-shaped hashing: the static HashData(Stream) one-shots (the LiteHash
// pump loop) and HashAlgorithm.ComputeHash(Stream), over a MemoryStream big
// enough to take multiple internal reads.
namespace HashStreamSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- HashData(Stream) --");
        byte[] big = HashOneShotSubset.Program.SeededBuffer(300_000, 999);
        using (var ms = new MemoryStream(big))
            Console.WriteLine(Convert.ToHexString(SHA256.HashData(ms)));
        using (var ms = new MemoryStream(big))
            Console.WriteLine(Convert.ToHexString(MD5.HashData(ms)));
        // Must agree with the byte[] one-shot.
        Console.WriteLine(Convert.ToHexString(SHA256.HashData(big)));

        Console.WriteLine("-- ComputeHash(Stream) --");
        using (SHA384 sha = SHA384.Create())
        using (var ms = new MemoryStream(Encoding.ASCII.GetBytes("stream me please")))
            Console.WriteLine(Convert.ToHexString(sha.ComputeHash(ms)));
    }
}
