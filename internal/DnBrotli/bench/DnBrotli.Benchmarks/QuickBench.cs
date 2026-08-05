using System.Diagnostics;
using BclBrotliDecoder = System.IO.Compression.BrotliDecoder;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotli.Benchmarks;

/// <summary>Fast stopwatch comparison of DnBrotli vs System.IO.Compression (run: <c>-- quick</c>).</summary>
public static class QuickBench
{
    public static void Run()
    {
        Console.WriteLine($"RID={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}, .NET {Environment.Version}");
        Console.WriteLine();

        foreach (string name in new[] { "text", "binary", "source" })
        {
            byte[] data = BenchData.Get(name);
            Console.WriteLine($"== {name} ({data.Length / 1024.0:F0} KiB) ==");
            foreach (int quality in new[] { 1, 4, 9, 11 })
            {
                // Ours
                byte[] oursComp = Time($"  q{quality,-2} ours   compress", data.Length, () => Brotli.Compress(data, quality, 22), out double oursC);
                Time($"  q{quality,-2} ours   decompress", data.Length, () => Brotli.Decompress(oursComp, data.Length), out _);

                // System.IO.Compression (native brotli), same quality + window
                byte[] sysComp = Time($"  q{quality,-2} system compress", data.Length, () => SysCompress(data, quality), out double sysC);
                Time($"  q{quality,-2} system decompress", data.Length, () => SysDecompress(sysComp, data.Length), out _);

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
        while (sw.ElapsedMilliseconds < 1000)
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

    private static byte[] SysCompress(byte[] data, int quality)
    {
        // Same quality AND window as DnBrotli, via the static TryCompress overload
        // (BrotliCompressionOptions only exposes Quality; window defaults to 22 in both).
        var dest = new byte[BclBrotliEncoder.GetMaxCompressedLength(data.Length)];
        if (!BclBrotliEncoder.TryCompress(data, dest, out int written, quality, 22))
            throw new InvalidOperationException("BCL BrotliEncoder.TryCompress failed");
        return dest.AsSpan(0, written).ToArray();
    }

    private static byte[] SysDecompress(byte[] comp, int originalLength)
    {
        var outBuf = new byte[originalLength];
        if (!BclBrotliDecoder.TryDecompress(comp, outBuf, out int written) || written != originalLength)
            throw new InvalidOperationException("BCL BrotliDecoder.TryDecompress failed");
        return outBuf;
    }
}
