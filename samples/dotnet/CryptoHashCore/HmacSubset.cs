#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

// HMAC through the real IL, all five algorithms: one-shot HashData, instance
// ComputeHash with key changes and instance reuse, an over-block-length key
// (exercises the hash-then-pad K0 derivation), and IncrementalHash's HMAC
// flavor with a mid-stream GetCurrentHash. RFC 4231 case 1 / RFC 2104 vectors
// double as known-answer asserts.
namespace HmacSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- HMAC one-shot --");
        byte[] key = Encoding.ASCII.GetBytes("secret-key");
        byte[] msg = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        Console.WriteLine(Convert.ToHexString(HMACSHA256.HashData(key, msg)));
        Console.WriteLine(Convert.ToHexString(HMACSHA1.HashData(key, msg)));
        Console.WriteLine(Convert.ToHexString(HMACMD5.HashData(key, msg)));
        Console.WriteLine(Convert.ToHexString(HMACSHA384.HashData(key, msg)));
        Console.WriteLine(Convert.ToHexString(HMACSHA512.HashData(key, msg)));

        Console.WriteLine("-- known vectors --");
        // RFC 4231 test case 1: key = 0x0b × 20, data = "Hi There".
        byte[] k1 = new byte[20];
        for (int i = 0; i < k1.Length; i++) k1[i] = 0x0b;
        byte[] hi = Encoding.ASCII.GetBytes("Hi There");
        Console.WriteLine(Convert.ToHexString(HMACSHA256.HashData(k1, hi))
            == "B0344C61D8DB38535CA8AFCEAF0BF12B881DC200C9833DA726E9376C2E32CFF7");
        Console.WriteLine(Convert.ToHexString(HMACSHA512.HashData(k1, hi))
            == "87AA7CDEA5EF619D4FF0B4241A1D6CB02379F4E2CE4EC2787AD0B30545E17CDE"
             + "DAA833B7D6B8A702038B274EAEA3F4E4BE9D914EEB61F1702E696C203A126854");

        Console.WriteLine("-- HMAC instance / rekey / long key --");
        using (var h = new HMACSHA256(key))
        {
            Console.WriteLine(Convert.ToHexString(h.ComputeHash(msg)));
            Console.WriteLine(Convert.ToHexString(h.ComputeHash(msg))); // reuse — must repeat
            Console.WriteLine(h.HashSize + " " + Convert.ToHexString(h.Key));
        }
        byte[] longKey = HashOneShotSubset.Program.SeededBuffer(200, 5); // > SHA256 block (64)
        using (var h = new HMACSHA256(longKey))
            Console.WriteLine(Convert.ToHexString(h.ComputeHash(msg)));

        Console.WriteLine("-- IncrementalHash HMAC --");
        using (IncrementalHash inc = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key))
        {
            inc.AppendData(Encoding.ASCII.GetBytes("The quick brown fox "));
            Console.WriteLine("current: " + Convert.ToHexString(inc.GetCurrentHash()));
            inc.AppendData(Encoding.ASCII.GetBytes("jumps over the lazy dog"));
            Console.WriteLine("final:   " + Convert.ToHexString(inc.GetHashAndReset()));
            // Must equal the one-shot over the whole message.
            Console.WriteLine(Convert.ToHexString(HMACSHA256.HashData(key, msg)));
        }
    }
}
