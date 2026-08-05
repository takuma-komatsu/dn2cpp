using System.Security.Cryptography;
using DnBrotli.Tests.Support;

namespace DnBrotli.Tests;

/// <summary>
/// Pins DnBrotli's own encoder determinism (PORTING.md): identical inputs and parameters
/// must always produce byte-identical output, and selected (entry, quality, window)
/// seeds carry a golden SHA-256 of DnBrotli's output. The goldens pin DnBrotli against
/// itself — never against native brotli, whose bytes are implementation-defined.
/// Structured to grow one seed row per quality as later DB stages land.
/// </summary>
public sealed class DeterminismTests
{
    public static IEnumerable<object[]> CorpusQuality()
    {
        foreach ((string name, _) in Corpus.Entries)
        {
            foreach (int quality in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 })
            {
                yield return new object[] { name, quality };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CorpusQuality))]
    public void CompressingTwice_IsByteIdentical(string name, int quality)
    {
        byte[] input = Corpus.Get(name);
        byte[] first = EncoderDrivers.CompressOneShot(input, quality, 22);
        byte[] second = EncoderDrivers.CompressOneShot(input, quality, 22);
        Assert.Equal(first, second);
    }

    /// <summary>Golden seeds: one row per (entry, quality, window) whose DnBrotli output
    /// hash is pinned. Add rows here as further qualities become functional.</summary>
    public static IEnumerable<object[]> GoldenSeeds()
    {
        yield return new object[]
        {
            "english-200k", 1, 22,
            "efdb5ffb1950f9f00ccf9cc9e3b88503ba2f9ebdf67cc37968f4f725da5f916f",
        };
        yield return new object[]
        {
            "english-200k", 2, 22,
            "11170bd7ac1a278e5138858b541dbe623b59558650d0a517b6e94e9976dc05c0",
        };
        yield return new object[]
        {
            "english-200k", 3, 22,
            "ec24de42d0cecf15ba03a5edd4f12168909b47d65086eb9dd0cb69e2e7b6a4e2",
        };
        yield return new object[]
        {
            "english-200k", 4, 22,
            "0401454bebfa65892ad601889ea69395438c986ed98dab3ca6acaa443050db77",
        };
        yield return new object[]
        {
            "english-200k", 5, 22,
            "c44a4982677f07cf2b314a05d234f313742dd4b8ae8e14aeeeaca283c1a65769",
        };
        yield return new object[]
        {
            "english-200k", 6, 22,
            "eaacac6fb5dc36f1e94e4394186e41b0e979cf3f73b509e978cc44b616568d28",
        };
        yield return new object[]
        {
            "english-200k", 7, 22,
            "524613672db678d20b4b7088485a6a133f405c8f6c7043b8dcd8ec3312f349a0",
        };
        yield return new object[]
        {
            "english-200k", 8, 22,
            "87f6794e42f917cb0cb8c38665dca7526c998b8d684a4d9b72d6a97c90f24c75",
        };
        yield return new object[]
        {
            "english-200k", 9, 22,
            "b759cc831459aaf360733077fa696bd127b5f6297873ec795f87683f1387685b",
        };
        yield return new object[]
        {
            "english-200k", 10, 22,
            "17f2bb4b4ef88f741843e1e2b84558a8677ea3fb499f7ecb12a63b5af2ca2f3b",
        };
        yield return new object[]
        {
            "english-200k", 11, 22,
            "d5756994a1b0ffcb275273f01d274d2caed905f14aedd0ef94e0ff0bc8b7c3b2",
        };
    }

    [Theory]
    [MemberData(nameof(GoldenSeeds))]
    public void GoldenOutputHash_IsStable(string name, int quality, int window, string sha256Hex)
    {
        byte[] input = Corpus.Get(name);
        byte[] compressed = EncoderDrivers.CompressOneShot(input, quality, window);
        string actual = Convert.ToHexStringLower(SHA256.HashData(compressed));
        Assert.Equal(sha256Hex, actual);
    }
}
