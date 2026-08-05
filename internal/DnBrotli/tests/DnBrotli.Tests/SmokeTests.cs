using DnBrotli.Tests.Oracles;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>Infrastructure smoke: the corpus is deterministic and the BCL oracle round-trips
/// every entry. Establishes the baseline the DnBrotli engine is later verified against.</summary>
public class SmokeTests
{
    [Theory]
    [MemberData(nameof(CorpusNames))]
    public void OracleRoundTrips(string name)
    {
        byte[] original = Corpus.Get(name);

        foreach (int quality in (int[])[1, 4])
        {
            byte[] compressed = SystemBrotli.Compress(original, quality);
            byte[] decompressed = SystemBrotli.Decompress(compressed);
            Assert.Equal(original, decompressed);
        }
    }

    [Fact]
    public void CorpusIsDeterministic()
    {
        // Rebuilding an entry yields identical bytes (fixed seeds, no ambient state).
        Assert.Equal(Corpus.Get("english-200k"), Corpus.Get("english-200k"));
        Assert.Equal(12, Corpus.Entries.Count);
    }

    public static IEnumerable<object[]> CorpusNames()
    {
        return Corpus.Names();
    }
}
