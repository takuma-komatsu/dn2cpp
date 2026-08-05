using System.Text;

namespace DnZlib.Tests.Support;

/// <summary>Deterministic test corpus spanning sizes and content classes (no network dependency).</summary>
public static class Corpus
{
    public static IReadOnlyList<(string Name, byte[] Data)> All { get; } = Build();

    public static IEnumerable<object[]> Data()
    {
        foreach ((string name, byte[] data) in All)
            yield return [name, data];
    }

    private static List<(string, byte[])> Build()
    {
        var list = new List<(string, byte[])>
        {
            ("empty", []),
            ("one", [0x42]),
            ("two", [0x00, 0xFF]),
            ("hello", Encoding.ASCII.GetBytes("hello, world")),
            ("zeros1k", new byte[1000]),
            ("zeros70k", new byte[70000]),
        };

        // Repeated single byte.
        var aaa = new byte[5000];
        Array.Fill(aaa, (byte)'A');
        list.Add(("repeatA5k", aaa));

        // Repeating multi-byte pattern larger than the 32K window (exercises window matches).
        var pattern = new byte[200000];
        for (int i = 0; i < pattern.Length; i++)
            pattern[i] = (byte)(i % 251);
        list.Add(("pattern200k", pattern));

        // English-like text via a small word list, ~100 KB.
        list.Add(("textish100k", TextLike(100000, seed: 1)));

        // Real, non-synthetic-vocabulary text: this repo's own .cs source, concatenated (no network
        // dependency — the source tree ships with the repo). Complements "textish100k", whose closed
        // vocabulary is a near-worst-case for a chain-based match finder, not representative of real
        // text/code.
        list.Add(("sourceCode200k", SourceCode(200000)));

        // Small-alphabet (4 symbols) highly compressible, ~64 KB.
        var rnd4 = new Random(2);
        var alpha4 = new byte[65536];
        for (int i = 0; i < alpha4.Length; i++)
            alpha4[i] = (byte)"ACGT"[rnd4.Next(4)];
        list.Add(("dna64k", alpha4));

        // Incompressible pseudo-random.
        var rnd = new Random(3);
        foreach (int n in new[] { 100, 4096, 65537, 130000 })
        {
            var b = new byte[n];
            rnd.NextBytes(b);
            list.Add(($"random{n}", b));
        }

        return list;
    }

    private static byte[] SourceCode(int size)
    {
        string root = FindRepoRoot();
        var sb = new StringBuilder();
        foreach (string subdir in new[] { "src", "tests" })
        {
            string dir = Path.Combine(root, subdir);
            if (!Directory.Exists(dir))
                continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                sb.Append(File.ReadAllText(file));
                if (sb.Length >= size)
                    break;
            }
        }

        string text = sb.ToString();
        if (text.Length == 0)
            throw new InvalidOperationException("source corpus is empty — repo layout changed?");
        return Encoding.UTF8.GetBytes(text.Length > size ? text[..size] : text);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DnZlib.sln")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("could not locate repo root (DnZlib.sln not found)");
    }

    private static byte[] TextLike(int size, int seed)
    {
        string[] words =
        [
            "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "then",
            "runs", "away", "into", "forest", "where", "trees", "grow", "tall", "green", "leaves",
        ];
        var rnd = new Random(seed);
        var sb = new StringBuilder(size + 16);
        while (sb.Length < size)
        {
            sb.Append(words[rnd.Next(words.Length)]);
            sb.Append(rnd.Next(6) == 0 ? ".\n" : " ");
        }
        return Encoding.ASCII.GetBytes(sb.ToString(0, size));
    }
}
