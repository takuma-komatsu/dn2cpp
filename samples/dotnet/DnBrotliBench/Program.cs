using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DnBrotli;
using BclBrotliEncoder = System.IO.Compression.BrotliEncoder;

namespace DnBrotliBench;

// Stopwatch-timed driver reproducing DnBrotli.Benchmarks.CompressBenchmarks and
// DecompressBenchmarks (BenchmarkDotNet) on the dn2cpp path.
//
// Rationale: BenchmarkDotNet itself does not transpile via dn2cpp (Reflection.Emit
// + child process spawning). This driver reruns THE SAME workload — same seed,
// same input sizes, same quality matrix — using a manual warmup + measure loop, so
// its Mean numbers slot into the README table as dn2cpp columns
// (see internal/DnBrotli/README.md#benchmarks).
//
// Coverage: the full compress matrix (quality 1 fast path + qualities 4/9/11:
// generic path and zopfli, each × text/binary/source) and decompression of
// quality-4 blobs. The decompress blobs come from the BCL (= native brotli)
// under managed .NET (--produce-blobs), so the decompress rows measure decoding
// of real-world-encoder output, byte-identical to what the BenchmarkDotNet
// DecompressBenchmarks baseline measures, independent of dn2cpp's own encoder
// output.
//
// Output format is one line per benchmark row:
//   compress   q1  binary   mean_us=884.9
//   decompress q4  binary   mean_us=236.9
internal static class Program
{
    private static int Main(string[] args)
    {
        // Blob-production mode (managed .NET only): write text.br / binary.br at
        // quality 4 / window 22 into <dir> using the BCL's native brotli, then
        // exit. The transpiled binary reads these blobs via DN2CPP_BROTLI_BLOB_*
        // env vars so the decompress rows measure the same bytes as the
        // BenchmarkDotNet baseline (see the file header).
        if (args.Length >= 2 && args[0] == "--produce-blobs")
        {
            string dir = args[1];
            Directory.CreateDirectory(dir);
            foreach (string kind in new[] { "text", "binary" })
            {
                byte[] data = BenchData.Get(kind);
                byte[] dest = new byte[BclBrotliEncoder.GetMaxCompressedLength(data.Length)];
                int written;
                if (!BclBrotliEncoder.TryCompress(data, dest, out written, 4, 22))
                {
                    Console.WriteLine("blob compress FAILED for " + kind);
                    return 1;
                }
                byte[] blob = new byte[written];
                Array.Copy(dest, blob, written);
                File.WriteAllBytes(Path.Combine(dir, kind + ".br"), blob);
                Console.WriteLine($"wrote {kind}.br ({blob.Length} bytes)");
            }
            return 0;
        }
        Console.WriteLine("== DnBrotliBench (Stopwatch) ==");
        Console.WriteLine("frequency=" + Stopwatch.Frequency);
        if (!SelfCheck())
        {
            Console.WriteLine("self-check FAILED — aborting before any measurement");
            return 1;
        }

        Console.WriteLine();
        RunCompressQualities(new[] { 1 });
        Console.WriteLine();
        RunCompressQualities(new[] { 4, 9, 11 });
        Console.WriteLine();
        RunDecompressQ4();
        return 0;
    }

    // Runs each quality × input combination in a try/catch so a regression prints
    // a per-cell FAIL row instead of aborting the whole matrix.
    private static void RunCompressQualities(int[] qualities)
    {
        foreach (int quality in qualities)
        {
            foreach (string input in new[] { "text", "binary", "source" })
            {
                byte[] data;
                try { data = BenchData.Get(input); }
                catch (Exception ex)
                {
                    Console.WriteLine($"compress q{quality,-2} {input,-6} mean_us=UNAVAILABLE ({ex.GetType().Name}: {ex.Message})");
                    continue;
                }
                double meanUs;
                try { meanUs = Measure(() => Brotli.Compress(data, quality, 22)); }
                catch (Exception ex)
                {
                    Console.WriteLine($"compress q{quality,-2} {input,-6} mean_us=FAIL ({ex.GetType().Name})");
                    continue;
                }
                Console.WriteLine($"compress q{quality,-2} {input,-6} mean_us={meanUs:F1}");
            }
        }
    }

    private static void RunDecompressQ4()
    {
        foreach (string input in new[] { "text", "binary" })
        {
            byte[] original;
            try { original = BenchData.Get(input); }
            catch (Exception ex)
            {
                Console.WriteLine($"decompress q4  {input,-6} mean_us=UNAVAILABLE ({ex.GetType().Name})");
                continue;
            }
            string blobPath = Environment.GetEnvironmentVariable("DN2CPP_BROTLI_BLOB_" + input.ToUpperInvariant());
            if (string.IsNullOrEmpty(blobPath) || !File.Exists(blobPath))
            {
                Console.WriteLine($"decompress q4  {input,-6} mean_us=UNAVAILABLE (no blob)");
                continue;
            }
            byte[] compressed = File.ReadAllBytes(blobPath);
            int originalLength = original.Length;
            double meanUs;
            try { meanUs = Measure(() => Brotli.Decompress(compressed, originalLength)); }
            catch (Exception ex)
            {
                Console.WriteLine($"decompress q4  {input,-6} mean_us=FAIL ({ex.GetType().Name})");
                continue;
            }
            Console.WriteLine($"decompress q4  {input,-6} mean_us={meanUs:F1}");
        }
    }

    // Round-trip check on 1 KB binary at QUALITY 1 (the fast-path codepath the driver
    // actually measures), plus a 1 MiB no-size-hint round-trip. Returns false on any
    // mismatch so a broken transpile doesn't silently publish garbage timings.
    private static bool SelfCheck()
    {
        byte[] probe = new byte[1024];
        for (int i = 0; i < probe.Length; i++) probe[i] = (byte)(i * 2654435761u >> 24);
        Console.WriteLine("self-check begin");
        byte[] c;
        try { c = Brotli.Compress(probe, 1, 22); }
        catch (BrotliException be) { Console.WriteLine("self-check compress FAIL: " + be.Message); return false; }
        Console.WriteLine("  compressed len=" + c.Length);
        byte[] r;
        try { r = Brotli.Decompress(c, probe.Length); }
        catch (BrotliException be) { Console.WriteLine("self-check decompress FAIL: " + be.Message); return false; }
        Console.WriteLine("  decompressed len=" + r.Length);
        if (r.Length != probe.Length) { Console.WriteLine("length mismatch"); return false; }
        for (int i = 0; i < probe.Length; i++)
            if (r[i] != probe[i]) { Console.WriteLine("byte mismatch at " + i); return false; }
        Console.WriteLine("self-check q1 round-trip: OK");

        // Large round-trip: decompress without a size hint so the output outgrows
        // the initial estimate several times, exercising Brotli.Decompress's
        // ArrayPool grow loop (Rent -> copy -> Return old -> Rent bigger), large
        // pooled buffers, and buffer reuse across calls — none of which the 1 KB
        // probe above reaches. Byte-exact compare catches a pool that hands back
        // a wrong-sized or stale buffer.
        byte[] big = new byte[1_000_000];
        for (int i = 0; i < big.Length; i++) big[i] = (byte)(i * 2654435761u >> 24);
        byte[] bc;
        try { bc = Brotli.Compress(big, 4, 22); }
        catch (BrotliException be) { Console.WriteLine("self-check big compress FAIL: " + be.Message); return false; }
        byte[] br;
        try { br = Brotli.Decompress(bc); }
        catch (BrotliException be) { Console.WriteLine("self-check big decompress FAIL: " + be.Message); return false; }
        if (br.Length != big.Length) { Console.WriteLine("self-check big length mismatch " + br.Length); return false; }
        for (int i = 0; i < big.Length; i++)
            if (br[i] != big[i]) { Console.WriteLine("self-check big byte mismatch at " + i); return false; }
        Console.WriteLine("self-check big round-trip (grow loop): OK");
        return true;
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

// Reproduces DnBrotli.Benchmarks.BenchData (see internal/DnBrotli/bench/
// DnBrotli.Benchmarks/BenchData.cs) so the driver does not need
// InternalsVisibleTo on DnBrotli.Benchmarks. The three synthetic corpora
// (text/binary/random) are byte-identical to the BDN version because seed and
// length are identical. The `source` corpus walks the DnBrotli repo tree
// (env-overridable via DN2CPP_DNBROTLI_REPO_ROOT because the transpiled
// binary's AppContext.BaseDirectory points at gates/artifacts, not at
// internal/DnBrotli — the measure script sets this before invoking the binary).
internal static class BenchData
{
    // Every corpus is deterministic (fixed seed / fixed transform / sorted file
    // walk), and the quality matrix asks for each one several times — build once.
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
            throw new InvalidOperationException("source corpus is empty — DnBrotli repo root not found (try DN2CPP_DNBROTLI_REPO_ROOT)");
        if (text.Length > 1_000_000)
            text = text.Substring(0, 1_000_000);
        else
            text = text.PadRight(1_000_000, '\n');
        return Encoding.UTF8.GetBytes(text);
    }

    private static string FindRepoRoot()
    {
        string env = Environment.GetEnvironmentVariable("DN2CPP_DNBROTLI_REPO_ROOT");
        if (!string.IsNullOrEmpty(env) && File.Exists(Path.Combine(env, "DnBrotli.sln")))
            return env;

        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DnBrotli.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("could not locate DnBrotli.sln (set DN2CPP_DNBROTLI_REPO_ROOT)");
        return dir.FullName;
    }
}
