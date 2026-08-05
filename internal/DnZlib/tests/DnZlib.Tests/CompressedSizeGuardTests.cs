using DnZlib;
using DnZlib.Tests.Support;

namespace DnZlib.Tests;

/// <summary>
/// Guards compression ratio at levels 6-9 against a snapshot of this project's own output (not
/// zlib's — DnZlib doesn't target byte-identical output). Round-trip tests only prove correctness;
/// a chain-walk shortcut (e.g. moving a level to the DeflateMedium strategy) trades ratio for
/// speed, and that trade needs an explicit guardrail rather than silent drift.
/// </summary>
public class CompressedSizeGuardTests
{
    public static IEnumerable<object[]> Corpora() => Corpus.Data();

    private static readonly int[] Levels = [2, 3, 6, 7, 8, 9];

    // Baseline sizes captured from `Zlib.Compress(data, level, ZlibFormat.Zlib).Length` at the
    // point levels 7-9 all used DeflateFunc.Slow (before any DeflateMedium-tuning experiment).
    // Levels 2-3 (DeflateFunc.Fast) were captured separately, before the DeflateFast
    // pointer-caching optimization (see README Deferred notes) — that change is pure refactoring
    // of already-invariant pointer reloads, so it must not move these numbers at all.
    //
    // sourceCode200k is special: Corpus.cs builds it by concatenating this repo's own src/*.cs at
    // test time, so editing any file inside the first ~200 KB of that scan (currently everything
    // through src/DnZlib/Raw/*) shifts the corpus content itself. Its baselines therefore drift
    // whenever those source files change, independent of the compressor. Re-snapshot them (all
    // levels together) after touching those files. Do NOT read this drift as a compression-ratio
    // regression — cross-check that the compressor's output is byte-identical on a fixed input
    // (SendBits 64-bit accumulator was verified this way).
    private static readonly Dictionary<(string Name, int Level), int> Baseline = new()
    {
        [("empty", 2)] = 8, [("empty", 3)] = 8, [("empty", 6)] = 8, [("empty", 7)] = 8, [("empty", 8)] = 8, [("empty", 9)] = 8,
        [("one", 2)] = 9, [("one", 3)] = 9, [("one", 6)] = 9, [("one", 7)] = 9, [("one", 8)] = 9, [("one", 9)] = 9,
        [("two", 2)] = 10, [("two", 3)] = 10, [("two", 6)] = 10, [("two", 7)] = 10, [("two", 8)] = 10, [("two", 9)] = 10,
        [("hello", 2)] = 20, [("hello", 3)] = 20, [("hello", 6)] = 20, [("hello", 7)] = 20, [("hello", 8)] = 20, [("hello", 9)] = 20,
        [("zeros1k", 2)] = 19, [("zeros1k", 3)] = 19, [("zeros1k", 6)] = 18, [("zeros1k", 7)] = 17, [("zeros1k", 8)] = 17, [("zeros1k", 9)] = 17,
        [("zeros70k", 2)] = 329, [("zeros70k", 3)] = 329, [("zeros70k", 6)] = 91, [("zeros70k", 7)] = 91, [("zeros70k", 8)] = 91, [("zeros70k", 9)] = 91,
        [("repeatA5k", 2)] = 46, [("repeatA5k", 3)] = 46, [("repeatA5k", 6)] = 29, [("repeatA5k", 7)] = 29, [("repeatA5k", 8)] = 29, [("repeatA5k", 9)] = 29,
        [("pattern200k", 2)] = 1837, [("pattern200k", 3)] = 1837, [("pattern200k", 6)] = 1830, [("pattern200k", 7)] = 1100, [("pattern200k", 8)] = 1100, [("pattern200k", 9)] = 1100,
        [("textish100k", 2)] = 25643, [("textish100k", 3)] = 23204, [("textish100k", 6)] = 19613, [("textish100k", 7)] = 19347, [("textish100k", 8)] = 19296, [("textish100k", 9)] = 19296,
        [("sourceCode200k", 2)] = 48847, [("sourceCode200k", 3)] = 47387, [("sourceCode200k", 6)] = 42212, [("sourceCode200k", 7)] = 42161, [("sourceCode200k", 8)] = 42143, [("sourceCode200k", 9)] = 42143,
        [("dna64k", 2)] = 20964, [("dna64k", 3)] = 20430, [("dna64k", 6)] = 19840, [("dna64k", 7)] = 19653, [("dna64k", 8)] = 19683, [("dna64k", 9)] = 19683,
        [("random100", 2)] = 111, [("random100", 3)] = 111, [("random100", 6)] = 111, [("random100", 7)] = 111, [("random100", 8)] = 111, [("random100", 9)] = 111,
        [("random4096", 2)] = 4107, [("random4096", 3)] = 4107, [("random4096", 6)] = 4107, [("random4096", 7)] = 4107, [("random4096", 8)] = 4107, [("random4096", 9)] = 4107,
        [("random65537", 2)] = 65565, [("random65537", 3)] = 65565, [("random65537", 6)] = 65565, [("random65537", 7)] = 65565, [("random65537", 8)] = 65565, [("random65537", 9)] = 65565,
        [("random130000", 2)] = 130046, [("random130000", 3)] = 130046, [("random130000", 6)] = 130046, [("random130000", 7)] = 130046, [("random130000", 8)] = 130046, [("random130000", 9)] = 130046,
    };

    [Theory]
    [MemberData(nameof(Corpora))]
    public void CompressedSize_StaysWithinTolerance(string name, byte[] data)
    {
        foreach (int level in Levels)
        {
            int baseline = Baseline[(name, level)];
            int actual = Zlib.Compress(data, level, ZlibFormat.Zlib).Length;

            // Levels 8-9 were measured moving from DeflateFunc.Slow to DeflateFunc.Medium (see
            // README Deferred notes): worst observed drift across the corpus set was ~0.9%, so
            // these tolerances keep a comfortable margin above that rather than reusing L6/7's
            // untightened 5%. Level 9's "max compression" contract still gets the tightest value.
            // A small absolute margin (+8 bytes) keeps tiny corpus entries (a handful of bytes)
            // from failing on rounding noise that a percentage alone can't absorb.
            double tolerance = level switch
            {
                9 => 0.01,
                8 => 0.02,
                _ => 0.05,
            };
            int maxAllowed = (int)(baseline * (1 + tolerance)) + 8;

            Assert.True(actual <= maxAllowed,
                $"{name} L{level}: expected <= {maxAllowed} bytes (baseline {baseline}, tolerance {tolerance:P0}), got {actual}");
        }
    }
}
