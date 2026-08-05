#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

// Instance-shaped hashing: HashAlgorithm.Create()/ComputeHash with the SAME
// instance reused across calls (exercises DigestFinal's re-initialize
// contract), plus the TransformBlock/TransformFinalBlock streaming form over
// odd-sized chunks and its Hash property.
namespace HashInstanceSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- ComputeHash instance reuse --");
        byte[] abc = Encoding.ASCII.GetBytes("abc");
        byte[] dog = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        using (SHA256 sha = SHA256.Create())
        {
            Console.WriteLine(Convert.ToHexString(sha.ComputeHash(abc)));
            Console.WriteLine(Convert.ToHexString(sha.ComputeHash(dog))); // same instance, second use
            Console.WriteLine(Convert.ToHexString(sha.ComputeHash(abc))); // and back — must equal row 1
            Console.WriteLine(sha.HashSize);
        }
        using (MD5 md5 = MD5.Create())
        {
            Console.WriteLine(Convert.ToHexString(md5.ComputeHash(dog)));
            Console.WriteLine(Convert.ToHexString(md5.ComputeHash(dog)));
        }

        Console.WriteLine("-- ComputeHash(buffer, offset, count) --");
        using (SHA1 sha1 = SHA1.Create())
        {
            Console.WriteLine(Convert.ToHexString(sha1.ComputeHash(dog, 4, 15))); // "quick brown fox"
        }

        Console.WriteLine("-- TransformBlock odd chunks --");
        byte[] big = HashOneShotSubset.Program.SeededBuffer(100_003, 777); // prime-ish length
        using (SHA256 sha = SHA256.Create())
        {
            int pos = 0;
            int[] chunks = { 1, 63, 64, 65, 4097, 33333 };
            int ci = 0;
            while (pos < big.Length)
            {
                int take = Math.Min(chunks[ci % chunks.Length], big.Length - pos);
                sha.TransformBlock(big, pos, take, null, 0);
                pos += take;
                ci++;
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            Console.WriteLine(Convert.ToHexString(sha.Hash!));
            Console.WriteLine(Convert.ToHexString(SHA256.HashData(big))); // must match the streamed row
        }
    }
}
