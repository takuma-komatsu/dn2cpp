using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using DnZlib;

namespace DnZlib.Benchmarks;

/// <summary>Fast stopwatch comparison of DnZlib vs System.IO.Compression (run: <c>-- quick</c>).</summary>
public static class QuickBench
{
    public static void Run()
    {
        Console.WriteLine($"RID={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}, .NET {Environment.Version}");
        Console.WriteLine();

        foreach ((string name, byte[] data) in Inputs())
        {
            Console.WriteLine($"== {name} ({data.Length / 1024.0:F0} KiB) ==");
            foreach (int level in new[] { 1, 6, 8, 9 })
            {
                // Ours
                byte[] oursComp = Time($"  L{level} ours   compress", data.Length, () => Zlib.Compress(data, level, ZlibFormat.Zlib), out double oursC);
                Time($"  L{level} ours   decompress", data.Length, () => Zlib.Uncompress(oursComp, ZlibFormat.Zlib), out _);

                // System.IO.Compression
                byte[] sysComp = Time($"  L{level} system compress", data.Length, () => SysCompress(data, level), out double sysC);
                Time($"  L{level} system decompress", data.Length, () => SysDecompress(sysComp), out _);

                Console.WriteLine($"      ratio ours={oursComp.Length * 100.0 / data.Length:F1}%  system={sysComp.Length * 100.0 / data.Length:F1}%   compress speed ours/system={sysC / oursC:F2}x");
            }
            Console.WriteLine();
        }
    }

    private static byte[] Time(string label, int inputLen, Func<byte[]> op, out double mbPerSec)
    {
        op(); // warmup
        var sw = Stopwatch.StartNew();
        int iters = 0;
        byte[] result = [];
        while (sw.ElapsedMilliseconds < 300)
        {
            result = op();
            iters++;
        }
        sw.Stop();
        double totalBytes = (double)inputLen * iters;
        mbPerSec = totalBytes / (1024 * 1024) / (sw.Elapsed.TotalSeconds);
        Console.WriteLine($"{label}: {mbPerSec,8:F1} MB/s");
        return result;
    }

    private static byte[] SysCompress(byte[] data, int level)
    {
        // Same numeric level as DnZlib (net10 ZLibCompressionOptions), not the coarse CompressionLevel enum.
        using var ms = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(ms, new ZLibCompressionOptions { CompressionLevel = level }, leaveOpen: true))
            z.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] SysDecompress(byte[] comp)
    {
        using var src = new MemoryStream(comp);
        using var z = new System.IO.Compression.ZLibStream(src, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        z.CopyTo(outMs);
        return outMs.ToArray();
    }

    private static IEnumerable<(string, byte[])> Inputs()
    {
        var rnd = new Random(7);

        var text = new StringBuilder();
        string[] words = ["the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "runs"];
        while (text.Length < 2_000_000)
            text.Append(words[rnd.Next(words.Length)]).Append(' ');
        yield return ("text", Encoding.ASCII.GetBytes(text.ToString()));

        var random = new byte[2_000_000];
        rnd.NextBytes(random);
        yield return ("random", random);

        var binary = new byte[2_000_000];
        for (int i = 0; i < binary.Length; i++)
            binary[i] = (byte)(i * 2654435761u >> 24);
        yield return ("binary", binary);
    }
}
