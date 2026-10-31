#nullable disable
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BrotliRoundTripSubset
{
    // BrotliStream / BrotliEncoder / BrotliDecoder round trips against the
    // vendored brotli (third_party/brotli/): the stream face through both the
    // CompressionMode and CompressionLevel constructor overloads (every
    // CompressionLevel value, over a large mostly-incompressible buffer), the
    // struct face through the one-shot statics (TryCompress/TryDecompress,
    // incl. the explicit quality/window overload sized via
    // GetMaxCompressedLength), explicit Flush() between writes (the
    // BROTLI_OPERATION_FLUSH path), the incremental struct API resumed across
    // deliberately small destination windows (DestinationTooSmall/NeedMoreData
    // discipline), empty-input framing on both faces, and a corrupted-input
    // error path.
    //
    // Determinism discipline (same as CompressionErrorPathsSubset): compressed
    // byte values and compressed LENGTHS are never printed -- brotli's
    // compressed encoding is implementation-defined, so real .NET's bundled
    // brotli and the vendored one may legitimately shape output differently
    // across library versions.
    // Only round-trip equality bools and exception TYPE names are printed; the
    // corrupted buffer is a literal constant, not mutated real compressed
    // output, so its rejection is governed purely by the brotli stream format.
    internal static class Program
    {
        internal static void Run()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "The quick brown fox jumps over the lazy dog. " +
                "The quick brown fox jumps over the lazy dog.");

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

            byte[] oneShotBuf = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
            bool tryCompressOk = BrotliEncoder.TryCompress(original, oneShotBuf, out int oneShotLen);
            byte[] oneShotOut = new byte[original.Length];
            bool tryDecompressOk = BrotliDecoder.TryDecompress(
                new ReadOnlySpan<byte>(oneShotBuf, 0, oneShotLen), oneShotOut, out int oneShotOutLen);
            Console.WriteLine("Brotli TryCompress/TryDecompress roundtrip ok: "
                + (tryCompressOk && tryDecompressOk && oneShotOutLen == original.Length
                   && BytesEqual(original, oneShotOut)));

            byte[] qualityBuf = new byte[BrotliEncoder.GetMaxCompressedLength(original.Length)];
            bool tryCompressQOk = BrotliEncoder.TryCompress(
                original, qualityBuf, out int qualityLen, quality: 9, window: 22);
            byte[] qualityOut = new byte[original.Length];
            bool tryDecompressQOk = BrotliDecoder.TryDecompress(
                new ReadOnlySpan<byte>(qualityBuf, 0, qualityLen), qualityOut, out int qualityOutLen);
            Console.WriteLine("Brotli TryCompress(quality,window) roundtrip ok: "
                + (tryCompressQOk && tryDecompressQOk && qualityOutLen == original.Length
                   && BytesEqual(original, qualityOut)));

            // Explicit Flush() between writes drives BrotliEncoderCompressStream
            // with BROTLI_OPERATION_FLUSH, which the write-then-dispose shape
            // never hits. Only the final round-trip equality is printed.
            byte[] flushCompressed = CompressStreamWithFlushes(large);
            byte[] flushDecompressed = DecompressStream(flushCompressed);
            Console.WriteLine("Brotli flush-between-writes roundtrip ok: "
                + BytesEqual(large, flushDecompressed));

            // Incremental struct API resumed across small destination windows:
            // the DestinationTooSmall (encoder+decoder) and NeedMoreData
            // (decoder) resumption paths a reimplementation is most likely to
            // get wrong. The booleans are format-level facts (64 KiB of mostly
            // incompressible input can never fit a 512-byte window), not
            // compressed-length prints.
            Console.WriteLine("Brotli incremental roundtrip ok: " + IncrementalRoundTrip(large));

            // Empty input: degenerate stream framing on both faces.
            byte[] emptyStreamCompressed = CompressStream(new byte[0]);
            byte[] emptyStreamBack = DecompressStream(emptyStreamCompressed);
            byte[] emptyOneShotBuf = new byte[BrotliEncoder.GetMaxCompressedLength(0)];
            bool emptyTryCompressOk = BrotliEncoder.TryCompress(
                ReadOnlySpan<byte>.Empty, emptyOneShotBuf, out int emptyOneShotLen);
            bool emptyTryDecompressOk = BrotliDecoder.TryDecompress(
                new ReadOnlySpan<byte>(emptyOneShotBuf, 0, emptyOneShotLen),
                Span<byte>.Empty, out int emptyOutLen);
            Console.WriteLine("Brotli empty-input roundtrip ok: "
                + (emptyStreamBack.Length == 0 && emptyTryCompressOk && emptyTryDecompressOk
                   && emptyOutLen == 0));

            // Corrupted input: 0x91 as a stream head declares a reserved/invalid
            // metadata block shape per the brotli stream format, so a run of
            // them fails fast in any conforming decoder regardless of which
            // encoder exists on either side.
            ExpectThrowStream("brotli-invalid-stream", Fill(32, 0x91));
            byte[] garbageOut = new byte[64];
            bool garbageOk = BrotliDecoder.TryDecompress(Fill(32, 0x91), garbageOut, out int garbageLen);
            Console.WriteLine("brotli-invalid-oneshot: TryDecompress ok=" + garbageOk);

            // Node.js test/parallel/test-zlib-invalid-input.js, CVE-2026-111497.
            // The stream exhausts a single-type block, which has no switch tree.
            byte[] invalidBlockSwitch =
            {
                0x20, 0x00, 0x10, 0x41, 0x41, 0x41, 0xFC, 0xFF, 0xFF, 0x07,
                0x00, 0x22, 0x28, 0xF0, 0x0B, 0x60, 0xF7, 0xF4, 0x1F, 0x00, 0x00, 0x00,
            };
            // Leave space after the exhausted block so destination exhaustion
            // cannot masquerade as malformed-input rejection.
            bool invalidBlockAccepted = BrotliDecoder.TryDecompress(invalidBlockSwitch,
                new byte[(1 << 24) + 1], out int invalidBlockWritten);
            Console.WriteLine("Brotli invalid block switch oneshot rejected: "
                + (!invalidBlockAccepted && invalidBlockWritten == (1 << 24)));
            Console.WriteLine("Brotli invalid block switch chunked rejected: "
                + RejectInvalidBlockSwitch(invalidBlockSwitch, 1));
            byte[] paddedInvalidBlockSwitch = new byte[invalidBlockSwitch.Length + 64];
            invalidBlockSwitch.CopyTo(paddedInvalidBlockSwitch, 0);
            Console.WriteLine("Brotli invalid block switch buffered rejected: "
                + RejectInvalidBlockSwitch(paddedInvalidBlockSwitch, paddedInvalidBlockSwitch.Length));
            ExpectThrowStream("brotli-invalid-block-switch-stream", invalidBlockSwitch);
        }

        private static bool RejectInvalidBlockSwitch(byte[] input, int chunkSize)
        {
            byte[] output = Fill(65538, 0xA5);
            using BrotliDecoder decoder = new BrotliDecoder();
            int offset = 0;
            int remaining = 0;
            for (int iteration = 0; iteration < 1024; iteration++)
            {
                if (remaining == 0 && offset < input.Length)
                {
                    remaining = Math.Min(chunkSize, input.Length - offset);
                }
                OperationStatus status = decoder.Decompress(
                    new ReadOnlySpan<byte>(input, offset, remaining),
                    new Span<byte>(output, 1, 65536), out int consumed, out int written);
                if (output[0] != 0xA5 || output[65537] != 0xA5)
                {
                    return false;
                }
                if (status == OperationStatus.InvalidData)
                {
                    // A terminal error must not resume on later input.
                    return decoder.Decompress(input, new Span<byte>(output, 1, 65536),
                        out consumed, out written) == OperationStatus.InvalidData
                        && consumed == 0 && written == 0
                        && output[0] == 0xA5 && output[65537] == 0xA5;
                }
                if (status == OperationStatus.Done || (consumed == 0 && written == 0))
                {
                    return false;
                }
                offset += consumed;
                remaining -= consumed;
            }
            return false;
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
        // incompressible, fixed seed -> byte-identical input on every run, so
        // the transpiled binary and real dotnet compress the exact same bytes.
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

        private static byte[] CompressStreamWithFlushes(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (BrotliStream brotli = new BrotliStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                int third = data.Length / 3;
                brotli.Write(data, 0, third);
                brotli.Flush();
                brotli.Write(data, third, third);
                brotli.Flush();
                brotli.Write(data, 2 * third, data.Length - 2 * third);
            }
            return output.ToArray();
        }

        private static bool IncrementalRoundTrip(byte[] original)
        {
            byte[] window = new byte[512];
            bool encoderSawSmallWindow = false;
            using MemoryStream compressed = new MemoryStream();
            using (BrotliEncoder encoder = new BrotliEncoder(quality: 5, window: 22))
            {
                ReadOnlySpan<byte> remaining = original;
                while (true)
                {
                    OperationStatus status = encoder.Compress(
                        remaining, window, out int consumed, out int written, isFinalBlock: true);
                    compressed.Write(window, 0, written);
                    remaining = remaining.Slice(consumed);
                    if (status == OperationStatus.Done)
                    {
                        break;
                    }
                    if (status != OperationStatus.DestinationTooSmall)
                    {
                        Console.WriteLine("incremental-encoder unexpected status: " + status);
                        return false;
                    }
                    encoderSawSmallWindow = true;
                }
            }

            byte[] compressedBytes = compressed.ToArray();
            bool decoderSawSmallWindow = false;
            using MemoryStream restored = new MemoryStream();
            using (BrotliDecoder decoder = new BrotliDecoder())
            {
                ReadOnlySpan<byte> remaining = compressedBytes;
                while (true)
                {
                    OperationStatus status = decoder.Decompress(
                        remaining, window, out int consumed, out int written);
                    restored.Write(window, 0, written);
                    remaining = remaining.Slice(consumed);
                    if (status == OperationStatus.Done)
                    {
                        break;
                    }
                    if (status == OperationStatus.DestinationTooSmall)
                    {
                        decoderSawSmallWindow = true;
                        continue;
                    }
                    if (status != OperationStatus.NeedMoreData || remaining.Length == 0)
                    {
                        Console.WriteLine("incremental-decoder unexpected status: " + status);
                        return false;
                    }
                }
            }

            return encoderSawSmallWindow && decoderSawSmallWindow
                && BytesEqual(original, restored.ToArray());
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
}
