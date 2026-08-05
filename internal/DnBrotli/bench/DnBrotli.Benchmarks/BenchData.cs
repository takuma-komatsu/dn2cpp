using System.Text;

namespace DnBrotli.Benchmarks;

internal static class BenchData
{
    public static byte[] Get(string kind)
    {
        var rnd = new Random(7);
        switch (kind)
        {
            case "text":
                var sb = new StringBuilder();
                string[] words = ["the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "and", "runs", "into", "forest"];
                while (sb.Length < 1_000_000)
                    sb.Append(words[rnd.Next(words.Length)]).Append(' ');
                return Encoding.ASCII.GetBytes(sb.ToString());
            case "binary":
                var bin = new byte[1_000_000];
                for (int i = 0; i < bin.Length; i++)
                    bin[i] = (byte)(i * 2654435761u >> 24);
                return bin;
            case "random":
                var r = new byte[1_000_000];
                rnd.NextBytes(r);
                return r;
            case "source":
                return SourceCorpus();
            default:
                throw new ArgumentException($"unknown input kind {kind}");
        }
    }

    /// <summary>
    /// A real, non-synthetic-vocabulary text sample: this repo's own .cs source, concatenated.
    /// Unlike "text" (a closed 12-word vocabulary — near-degenerate for a dictionary/LZ coder),
    /// this has realistic identifier/operator diversity and locally repetitive structure
    /// (indentation, braces), so it's representative of what compressing real text/code looks
    /// like. No network dependency: the source tree ships with the repo.
    /// </summary>
    private static byte[] SourceCorpus()
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
            }
        }

        string text = sb.ToString();
        if (text.Length == 0)
            throw new InvalidOperationException("source corpus is empty — repo layout changed?");
        if (text.Length > 1_000_000)
            text = text[..1_000_000];
        else
            text = text.PadRight(1_000_000, '\n');
        return Encoding.UTF8.GetBytes(text);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DnBrotli.sln")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("could not locate repo root (DnBrotli.sln not found)");
    }
}
