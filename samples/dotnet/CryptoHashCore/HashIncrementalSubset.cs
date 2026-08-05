#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

// IncrementalHash: AppendData in pieces, a mid-stream GetCurrentHash
// (exercises DigestCurrent's non-destructive clone-then-final contract),
// then GetHashAndReset and reuse of the same instance after the reset.
namespace HashIncrementalSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- IncrementalHash --");
        byte[] part1 = Encoding.ASCII.GetBytes("a");
        byte[] part2 = Encoding.ASCII.GetBytes("b");
        byte[] part3 = Encoding.ASCII.GetBytes("c");
        using (IncrementalHash inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            inc.AppendData(part1);
            inc.AppendData(part2);
            // Peek mid-stream: hash of "ab" without disturbing the running state.
            Console.WriteLine("current(ab): " + Convert.ToHexString(inc.GetCurrentHash()));
            inc.AppendData(part3);
            Console.WriteLine("final (abc): " + Convert.ToHexString(inc.GetHashAndReset()));
            // After the reset the same instance starts fresh.
            inc.AppendData(Encoding.ASCII.GetBytes("abc"));
            Console.WriteLine("reuse (abc): " + Convert.ToHexString(inc.GetHashAndReset()));
            Console.WriteLine(inc.AlgorithmName.Name + " " + inc.HashLengthInBytes);
        }
        using (IncrementalHash inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
        {
            inc.AppendData(HashOneShotSubset.Program.SeededBuffer(70_001, 424242));
            Console.WriteLine(Convert.ToHexString(inc.GetHashAndReset()));
        }
    }
}
