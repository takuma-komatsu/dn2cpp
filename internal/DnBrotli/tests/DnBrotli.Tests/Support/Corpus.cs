using System.Text;

namespace DnBrotli.Tests.Support;

/// <summary>
/// Deterministic, no-network test corpus. Every entry is reproduced byte-identically on every
/// run and host (fixed seeds, closed vocabularies), so tests never depend on ambient state.
/// Entries deliberately cover brotli-relevant shapes: RFC 7932 static-dictionary words with
/// transform-triggering casing/suffix forms, UTF-8 multibyte text (literal-cost UTF-8 mode),
/// long zero/repeat runs, and incompressible pseudo-random data.
/// </summary>
internal static class Corpus
{
    internal static IReadOnlyList<(string Name, byte[] Data)> Entries { get; } = Build();

    internal static IEnumerable<object[]> Names()
    {
        foreach ((string name, _) in Entries)
        {
            yield return new object[] { name };
        }
    }

    internal static byte[] Get(string name)
    {
        foreach ((string entryName, byte[] data) in Entries)
        {
            if (entryName == name)
            {
                return data;
            }
        }
        throw new ArgumentException($"unknown corpus entry '{name}'", nameof(name));
    }

    private static List<(string, byte[])> Build()
    {
        return
        [
            ("empty", Array.Empty<byte>()),
            ("one-byte", new byte[] { 0x42 }),
            ("two-bytes", new byte[] { 0xDE, 0xAD }),
            ("hello", Encoding.ASCII.GetBytes("hello, brotli world")),
            ("zeros-1k", new byte[1024]),
            ("zeros-70k", new byte[70 * 1024]),
            ("repeat-7-40k", RepeatingPattern(40 * 1024, 7)),
            ("english-200k", EnglishLikeText(200 * 1024)),
            ("utf8-64k", Utf8Text(64 * 1024)),
            ("random-64k", PseudoRandom(64 * 1024, 0x12345678u)),
            ("random-3", PseudoRandom(3, 0x9E3779B9u)),
            ("binary-structured-128k", BinaryStructured(128 * 1024)),
        ];
    }

    /// <summary>A short repeating period — long LZ matches at small distances, ring-buffer wraps.</summary>
    private static byte[] RepeatingPattern(int length, int period)
    {
        var data = new byte[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = (byte)('A' + (i % period));
        }
        return data;
    }

    /// <summary>English-like text from a closed vocabulary rich in RFC 7932 dictionary words
    /// ("the", "of", "and", "time", "work", "with", ...), with capitalized sentence starts
    /// (UppercaseFirst transform shape) and common suffix forms.</summary>
    private static byte[] EnglishLikeText(int minLength)
    {
        string[] vocabulary =
        [
            "the", "of", "and", "a", "to", "in", "is", "that", "for", "was",
            "time", "down", "life", "work", "code", "data", "site", "with",
            "have", "this", "from", "they", "which", "people", "there", "being",
            "working", "different", "important", "development", "information",
        ];
        var builder = new StringBuilder(minLength + 128);
        uint state = 0xC0FFEEu;
        int wordsInSentence = 0;
        bool sentenceStart = true;
        while (builder.Length < minLength)
        {
            state = XorShift(state);
            string word = vocabulary[state % (uint)vocabulary.Length];
            if (sentenceStart)
            {
                builder.Append(char.ToUpperInvariant(word[0])).Append(word.AsSpan(1));
                sentenceStart = false;
            }
            else
            {
                builder.Append(word);
            }
            wordsInSentence++;
            if (wordsInSentence >= 8 + (int)(state % 7u))
            {
                builder.Append(". ");
                wordsInSentence = 0;
                sentenceStart = true;
            }
            else
            {
                builder.Append(' ');
            }
        }
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    /// <summary>UTF-8 multibyte text (Japanese-ish closed vocabulary) — exercises the encoder's
    /// UTF-8 literal-cost model and multibyte literal contexts.</summary>
    private static byte[] Utf8Text(int minLength)
    {
        string[] vocabulary =
        [
            "圧縮", "展開", "辞書", "符号", "文字列", "データ", "変換", "検証",
            "これは", "テスト", "です。", "した。", "する。", "、そして", "、また",
        ];
        var builder = new StringBuilder();
        uint state = 0xB40C0DEu;
        while (Encoding.UTF8.GetByteCount(builder.ToString()) < minLength)
        {
            for (int i = 0; i < 256; i++)
            {
                state = XorShift(state);
                builder.Append(vocabulary[state % (uint)vocabulary.Length]);
            }
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>Incompressible xorshift32 noise with a fixed seed.</summary>
    private static byte[] PseudoRandom(int length, uint seed)
    {
        var data = new byte[length];
        uint state = seed;
        for (int i = 0; i < length; i++)
        {
            state = XorShift(state);
            data[i] = (byte)state;
        }
        return data;
    }

    /// <summary>Record-structured pseudo-binary: fixed 32-byte records whose leading fields
    /// repeat and trailing fields vary — mixed match/literal behaviour like real binary formats.</summary>
    private static byte[] BinaryStructured(int length)
    {
        var data = new byte[length];
        uint state = 0x0BADF00Du;
        for (int i = 0; i < length; i++)
        {
            int column = i % 32;
            if (column < 20)
            {
                data[i] = (byte)(column * 3);
            }
            else
            {
                state = XorShift(state);
                data[i] = (byte)state;
            }
        }
        return data;
    }

    private static uint XorShift(uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }
}
