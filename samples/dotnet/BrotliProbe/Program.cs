using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Text;

// Discovery-spike throwaway probe for the real System.IO.Compression.Brotli
// surface (no shim). Sections:
//
//   1. BrotliStream round-trips via the CompressionMode constructor overload
//      (small buffer) and the CompressionLevel constructor overload (a large,
//      mostly-incompressible buffer across every CompressionLevel value), plus
//      the BrotliCompressionOptions (Quality) constructor overload.
//   2. The struct API driven directly: the one-shot statics
//      (BrotliEncoder.TryCompress / BrotliDecoder.TryDecompress /
//      BrotliEncoder.GetMaxCompressedLength) and the streaming instance
//      state machines (BrotliEncoder.Compress/Flush, BrotliDecoder.Decompress,
//      OperationStatus-driven) — the SafeBrotli*Handle + raw-brotli-P/Invoke
//      path end to end.
//   3. Corrupted-input error paths, printing only the exception TYPE name
//      (never message text) — literal constant buffers, not mutated real
//      compressed output, so the throw/no-throw outcome is governed by the
//      brotli stream format, not by which encoder produced the bytes.
//
// Determinism discipline (matches the CompressionCore gates): compressed byte
// values and compressed LENGTHS are never printed — brotli's compressed
// encoding is implementation-defined, so real .NET's bundled brotli and the
// vendored one may legitimately shape output differently. Only round-trip
// equality bools, OperationStatus names of ample-buffer calls, and exception
// type names are printed.
//
// Still a throwaway sample — NOT the eventual regression gate (that is a
// section of samples/dotnet/CompressionCore/).

namespace BrotliProbe;

public static class Program
{
    public static int Main()
    {
        byte[] original = Encoding.UTF8.GetBytes(
            "The quick brown fox jumps over the lazy dog. " +
            "The quick brown fox jumps over the lazy dog.");

        // -- Section 1: BrotliStream round-trips --------------------------------

        byte[] compressed = CompressStream(original);
        byte[] decompressed = DecompressStream(compressed);
        Console.WriteLine("Brotli stream roundtrip ok: " + BytesEqual(original, decompressed));

        byte[] large = BuildLargeBuffer(65536);
        CompressionLevel[] levels =
        {
            CompressionLevel.Optimal,
            CompressionLevel.Fastest,
            CompressionLevel.NoCompression,
            CompressionLevel.SmallestSize,
        };
        foreach (CompressionLevel level in levels)
        {
            byte[] largeCompressed = CompressStreamLevel(large, level);
            byte[] largeDecompressed = DecompressStream(largeCompressed);
            Console.WriteLine("Brotli large/" + level + " roundtrip ok: " + BytesEqual(large, largeDecompressed));
        }

        byte[] optCompressed = CompressStreamOptions(original, quality: 5);
        byte[] optDecompressed = DecompressStream(optCompressed);
        Console.WriteLine("Brotli options/Quality=5 roundtrip ok: " + BytesEqual(original, optDecompressed));

        // -- Section 2: the struct API ------------------------------------------

        int maxLen = BrotliEncoder.GetMaxCompressedLength(original.Length);
        Console.WriteLine("Brotli GetMaxCompressedLength positive: " + (maxLen > original.Length));

        byte[] oneShotBuf = new byte[maxLen];
        bool tryCompressOk = BrotliEncoder.TryCompress(original, oneShotBuf, out int oneShotLen);
        Console.WriteLine("Brotli TryCompress ok: " + tryCompressOk);

        byte[] oneShotOut = new byte[original.Length];
        bool tryDecompressOk = BrotliDecoder.TryDecompress(
            new ReadOnlySpan<byte>(oneShotBuf, 0, oneShotLen), oneShotOut, out int oneShotOutLen);
        Console.WriteLine("Brotli TryDecompress ok: "
            + (tryDecompressOk && oneShotOutLen == original.Length
               && BytesEqual(original, oneShotOut)));

        byte[] qualityBuf = new byte[maxLen];
        bool tryCompressQOk = BrotliEncoder.TryCompress(original, qualityBuf, out int qualityLen, quality: 9, window: 22);
        byte[] qualityOut = new byte[original.Length];
        bool tryDecompressQOk = BrotliDecoder.TryDecompress(
            new ReadOnlySpan<byte>(qualityBuf, 0, qualityLen), qualityOut, out int qualityOutLen);
        Console.WriteLine("Brotli TryCompress(quality,window) roundtrip ok: "
            + (tryCompressQOk && tryDecompressQOk && qualityOutLen == original.Length
               && BytesEqual(original, qualityOut)));

        // Streaming instance state machines, ample buffers so each single call
        // finishes with a deterministic OperationStatus.
        using (BrotliEncoder encoder = new BrotliEncoder(quality: 6, window: 22))
        {
            byte[] encBuf = new byte[BrotliEncoder.GetMaxCompressedLength(large.Length)];
            OperationStatus encStatus = encoder.Compress(
                large, encBuf, out int encConsumed, out int encWritten, isFinalBlock: true);
            Console.WriteLine("Brotli encoder.Compress status: " + encStatus
                + " consumedAll=" + (encConsumed == large.Length));

            using (BrotliDecoder decoder = new BrotliDecoder())
            {
                byte[] decBuf = new byte[large.Length];
                OperationStatus decStatus = decoder.Decompress(
                    new ReadOnlySpan<byte>(encBuf, 0, encWritten), decBuf, out int decConsumed, out int decWritten);
                Console.WriteLine("Brotli decoder.Decompress status: " + decStatus
                    + " roundtrip ok: " + (decWritten == large.Length && BytesEqual(large, decBuf)));
            }
        }

        // -- Section 3: corrupted-input error paths ------------------------------

        // 0x91 as a stream head declares a metadata block shape that is
        // reserved/invalid per the brotli stream format; a run of them fails
        // fast in any conforming decoder.
        ExpectThrowStream("brotli-invalid-stream", Fill(32, 0x91));

        byte[] garbageOut = new byte[64];
        bool garbageOk = BrotliDecoder.TryDecompress(Fill(32, 0x91), garbageOut, out int garbageLen);
        Console.WriteLine("brotli-invalid-oneshot: TryDecompress ok=" + garbageOk);

        return 0;
    }

    private static byte[] Fill(int length, byte value)
    {
        byte[] buffer = new byte[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = value;
        }
        return buffer;
    }

    private static void ExpectThrowStream(string label, byte[] corrupted)
    {
        try
        {
            using MemoryStream input = new MemoryStream(corrupted);
            using BrotliStream brotli = new BrotliStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            brotli.CopyTo(output);
            Console.WriteLine(label + ": threw=False");
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
        }
    }

    // Deterministic xorshift32 fill behind a short readable prefix: mostly
    // incompressible, fixed seed -> byte-identical input on every run, so the
    // transpiled binary and real dotnet compress the exact same bytes.
    private static byte[] BuildLargeBuffer(int length)
    {
        byte[] buffer = new byte[length];
        byte[] prefix = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. ");
        uint state = 0x9E3779B9u;
        for (int i = 0; i < length; i++)
        {
            if (i < prefix.Length)
            {
                buffer[i] = prefix[i];
                continue;
            }
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            buffer[i] = (byte)state;
        }
        return buffer;
    }

    private static byte[] CompressStream(byte[] data)
    {
        using MemoryStream output = new MemoryStream();
        using (BrotliStream brotli = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressStreamLevel(byte[] data, CompressionLevel level)
    {
        using MemoryStream output = new MemoryStream();
        using (BrotliStream brotli = new BrotliStream(output, level, leaveOpen: true))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressStreamOptions(byte[] data, int quality)
    {
        using MemoryStream output = new MemoryStream();
        BrotliCompressionOptions options = new BrotliCompressionOptions { Quality = quality };
        using (BrotliStream brotli = new BrotliStream(output, options, leaveOpen: true))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressStream(byte[] compressed)
    {
        using MemoryStream input = new MemoryStream(compressed);
        using BrotliStream brotli = new BrotliStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    private static bool BytesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}
