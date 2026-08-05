using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DnZlib;

namespace DnZlibBench;

// Stopwatch-timed driver reproducing DnZlib.Benchmarks.CompressBenchmarks and
// DecompressBenchmarks (BenchmarkDotNet) on the dn2cpp path.
//
// Rationale: BenchmarkDotNet itself does not transpile via dn2cpp (Reflection.Emit
// + child process spawning). This driver reruns THE SAME workload — same seed,
// same input sizes, same level matrix — using a manual warmup + measure loop, so
// its Mean numbers slot into the BenchmarkDotNet-produced README table as dn2cpp
// columns (see internal/DnZlib/README.md#benchmarks).
//
// Coverage: the full compress matrix (level 1 deflate_quick + levels 6–9
// deflate_medium, each × text/binary/source) and decompression of level-6
// blobs. The decompress blobs come from managed .NET (--produce-blobs) so the
// decompressed input is byte-identical to what the BenchmarkDotNet baseline
// measures, independent of dn2cpp's own compressor output.
//
// Output format is one line per benchmark row:
//   compress    L1  binary   mean_us=884.9
//   decompress  L6  binary   mean_us=236.9
internal static class Program
{
    private static int Main(string[] args)
    {
        // Blob-production mode (managed .NET only): write text.zlib / binary.zlib at
        // level 6 into <dir>, then exit. The transpiled binary reads these blobs
        // via DN2CPP_ZLIB_BLOB_* env vars so the decompress rows measure the same
        // bytes as the BenchmarkDotNet baseline (see the file header).
        if (args.Length >= 2 && args[0] == "--produce-blobs")
        {
            string dir = args[1];
            Directory.CreateDirectory(dir);
            foreach (string kind in new[] { "text", "binary" })
            {
                byte[] data = BenchData.Get(kind);
                byte[] blob = Zlib.Compress(data, 6, ZlibFormat.Zlib);
                File.WriteAllBytes(Path.Combine(dir, kind + ".zlib"), blob);
                Console.WriteLine($"wrote {kind}.zlib ({blob.Length} bytes)");
            }
            return 0;
        }
        Console.WriteLine("== DnZlibBench (Stopwatch) ==");
        Console.WriteLine("frequency=" + Stopwatch.Frequency);
        SelfCheck();

        Console.WriteLine();
        RunCompressLevels(new[] { 1 });
        Console.WriteLine();
        RunCompressLevels(new[] { 6, 7, 8, 9 });
        Console.WriteLine();
        RunDecompressLevel6();
        return 0;
    }

    // Runs each level × input combination in a try/catch so a regression prints
    // a per-cell FAIL row instead of aborting the whole matrix.
    private static void RunCompressLevels(int[] levels)
    {
        foreach (int level in levels)
        {
            foreach (string input in new[] { "text", "binary", "source" })
            {
                byte[] data;
                try { data = BenchData.Get(input); }
                catch (Exception ex)
                {
                    Console.WriteLine($"compress L{level} {input,-6} mean_us=UNAVAILABLE ({ex.GetType().Name}: {ex.Message})");
                    continue;
                }
                double meanUs;
                try { meanUs = Measure(() => Zlib.Compress(data, level, ZlibFormat.Zlib)); }
                catch (Exception ex)
                {
                    Console.WriteLine($"compress L{level} {input,-6} mean_us=FAIL ({ex.GetType().Name})");
                    continue;
                }
                Console.WriteLine($"compress L{level} {input,-6} mean_us={meanUs:F1}");
            }
        }
    }

    private static void RunDecompressLevel6()
    {
        foreach (string input in new[] { "text", "binary" })
        {
            byte[] original;
            try { original = BenchData.Get(input); }
            catch (Exception ex)
            {
                Console.WriteLine($"decompress L6 {input,-6} mean_us=UNAVAILABLE ({ex.GetType().Name})");
                continue;
            }
            string blobPath = Environment.GetEnvironmentVariable("DN2CPP_ZLIB_BLOB_" + input.ToUpperInvariant());
            if (string.IsNullOrEmpty(blobPath) || !File.Exists(blobPath))
            {
                Console.WriteLine($"decompress L6 {input,-6} mean_us=UNAVAILABLE (no blob)");
                continue;
            }
            byte[] compressed = File.ReadAllBytes(blobPath);
            int originalLength = original.Length;
            double meanUs;
            try { meanUs = Measure(() => Zlib.Uncompress(compressed, ZlibFormat.Zlib, originalLength)); }
            catch (Exception ex)
            {
                Console.WriteLine($"decompress L6 {input,-6} mean_us=FAIL ({ex.GetType().Name})");
                continue;
            }
            Console.WriteLine($"decompress L6 {input,-6} mean_us={meanUs:F1}");
        }
    }

    // Round-trip check on 1 KB binary at LEVEL 1 (the compression codepath the driver
    // actually measures). Aborts on mismatch so a broken transpile doesn't silently
    // publish garbage timings.
    private static void SelfCheck()
    {
        byte[] probe = new byte[1024];
        for (int i = 0; i < probe.Length; i++) probe[i] = (byte)(i * 2654435761u >> 24);
        Console.WriteLine("self-check begin");
        byte[] c;
        try { c = Zlib.Compress(probe, 1, ZlibFormat.Zlib); }
        catch (DnZlib.ZlibException ze) { Console.WriteLine("self-check compress FAIL rc=" + (int)ze.Result); return; }
        Console.WriteLine("  compressed len=" + c.Length);
        byte[] r;
        try { r = Zlib.Uncompress(c, ZlibFormat.Zlib, probe.Length); }
        catch (DnZlib.ZlibException ze) { Console.WriteLine("self-check uncompress FAIL rc=" + (int)ze.Result); return; }
        Console.WriteLine("  uncompressed len=" + r.Length);
        if (r.Length != probe.Length) { Console.WriteLine("length mismatch"); return; }
        for (int i = 0; i < probe.Length; i++)
            if (r[i] != probe[i]) { Console.WriteLine("byte mismatch at " + i); return; }
        Console.WriteLine("self-check L1 round-trip: OK");

        // Large round-trip: decompress without a size hint so the output outgrows
        // the initial estimate several times, exercising Zlib.Uncompress's ArrayPool
        // grow loop (Rent -> copy -> Return old -> Rent bigger), large pooled
        // buffers, and buffer reuse across calls — none of which the 1 KB probe
        // above reaches. Byte-exact compare catches a pool that hands back a
        // wrong-sized or stale buffer.
        byte[] big = new byte[1_000_000];
        for (int i = 0; i < big.Length; i++) big[i] = (byte)(i * 2654435761u >> 24);
        byte[] bc;
        try { bc = Zlib.Compress(big, 6, ZlibFormat.Zlib); }
        catch (DnZlib.ZlibException ze) { Console.WriteLine("self-check big compress FAIL rc=" + (int)ze.Result); return; }
        byte[] br;
        try { br = Zlib.Uncompress(bc, ZlibFormat.Zlib); }
        catch (DnZlib.ZlibException ze) { Console.WriteLine("self-check big uncompress FAIL rc=" + (int)ze.Result); return; }
        if (br.Length != big.Length) { Console.WriteLine("self-check big length mismatch " + br.Length); return; }
        for (int i = 0; i < big.Length; i++)
            if (br[i] != big[i]) { Console.WriteLine("self-check big byte mismatch at " + i); return; }
        Console.WriteLine("self-check big round-trip (grow loop): OK");
    }

    // Warmup for ~300 ms, then measure until either 1 s of wall time elapsed or 20
    // iterations, whichever finishes last. Returns arithmetic mean in microseconds
    // (matches BenchmarkDotNet's Mean).
    private static double Measure(Func<byte[]> op)
    {
        Stopwatch warm = Stopwatch.StartNew();
        while (warm.ElapsedMilliseconds < 300)
            op();
        warm.Stop();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int minIters = 20;
        long minTicks = Stopwatch.Frequency; // 1 second
        Stopwatch sw = Stopwatch.StartNew();
        long iters = 0;
        while (sw.ElapsedTicks < minTicks || iters < minIters)
        {
            op();
            iters++;
        }
        sw.Stop();

        double totalUs = sw.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency;
        return totalUs / iters;
    }
}

// Reproduces DnZlib.Benchmarks.BenchData (see internal/DnZlib/bench/DnZlib.Benchmarks/
// BenchData.cs) so the driver does not need InternalsVisibleTo on DnZlib.Benchmarks.
// The three synthetic corpora (text/binary/random) are byte-identical to the BDN
// version because seed and length are identical. The `source` corpus walks the
// DnZlib repo tree (env-overridable via DN2CPP_DNZLIB_REPO_ROOT because the
// transpiled binary's AppContext.BaseDirectory points at gates/artifacts, not at
// internal/DnZlib — the gate script sets this before invoking the binary; the
// walk up from the base directory is the fallback for a hand-run binary).
internal static class BenchData
{
    // Every corpus is deterministic (fixed seed / fixed transform / sorted file
    // walk), and the level matrix asks for each one several times — build once.
    private static readonly Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();

    public static byte[] Get(string kind)
    {
        if (Cache.TryGetValue(kind, out byte[] cached))
            return cached;
        byte[] built = Build(kind);
        Cache[kind] = built;
        return built;
    }

    private static byte[] Build(string kind)
    {
        Random rnd = new Random(7);
        switch (kind)
        {
            case "text":
                StringBuilder sb = new StringBuilder();
                string[] words = new[]
                {
                    "the", "quick", "brown", "fox", "jumps", "over",
                    "lazy", "dog", "and", "runs", "into", "forest",
                };
                while (sb.Length < 1_000_000)
                    sb.Append(words[rnd.Next(words.Length)]).Append(' ');
                return Encoding.ASCII.GetBytes(sb.ToString());
            case "binary":
                byte[] bin = new byte[1_000_000];
                for (int i = 0; i < bin.Length; i++)
                    bin[i] = (byte)(i * 2654435761u >> 24);
                return bin;
            case "random":
                byte[] r = new byte[1_000_000];
                rnd.NextBytes(r);
                return r;
            case "source":
                return SourceCorpus();
            default:
                throw new ArgumentException("unknown input kind " + kind);
        }
    }

    private static byte[] SourceCorpus()
    {
        string root = FindRepoRoot();
        StringBuilder sb = new StringBuilder();
        foreach (string subdir in new[] { "src", "tests" })
        {
            string dir = Path.Combine(root, subdir);
            if (!Directory.Exists(dir))
                continue;
            List<string> files = new List<string>();
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
                files.Add(file);
            files.Sort(StringComparer.Ordinal);
            foreach (string file in files)
                sb.Append(File.ReadAllText(file));
        }

        string text = sb.ToString();
        if (text.Length == 0)
            throw new InvalidOperationException("source corpus is empty — DnZlib repo root not found (try DN2CPP_DNZLIB_REPO_ROOT)");
        if (text.Length > 1_000_000)
            text = text.Substring(0, 1_000_000);
        else
            text = text.PadRight(1_000_000, '\n');
        return Encoding.UTF8.GetBytes(text);
    }

    private static string FindRepoRoot()
    {
        string env = Environment.GetEnvironmentVariable("DN2CPP_DNZLIB_REPO_ROOT");
        if (!string.IsNullOrEmpty(env) && File.Exists(Path.Combine(env, "DnZlib.sln")))
            return env;

        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DnZlib.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("could not locate DnZlib.sln (set DN2CPP_DNZLIB_REPO_ROOT)");
        return dir.FullName;
    }
}
