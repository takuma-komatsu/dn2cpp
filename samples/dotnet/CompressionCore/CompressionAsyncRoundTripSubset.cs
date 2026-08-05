#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompressionAsyncRoundTripSubset
{
    // The async Stream face of DeflateStream/GZipStream over a MemoryStream
    // backing: WriteAsync(ReadOnlyMemory<byte>) / WriteAsync(byte[], int, int)
    // + FlushAsync on the compress side, loop-read ReadAsync(Memory<byte>) /
    // ReadAsync(byte[], int, int) and CopyToAsync on the decompress side, and
    // `await using` (the DisposeAsync path) — plus the pre-canceled-token
    // early-outs of WriteAsync/ReadAsync. Prints only implementation-invariant
    // facts (lengths, round-trip equality), never compressed bytes or their
    // exact length. Everything runs on the main thread's async flow (no pool
    // work, sequential awaits only), so the output is deterministic. The
    // canceled-token catches print a fixed label because real .NET raises
    // TaskCanceledException where the transpiled runtime raises the base
    // OperationCanceledException. The deterministic xorshift32 buffer mirrors
    // the large-buffer section: mostly incompressible and big enough to span
    // several of DeflateStream's ~8192-byte internal-buffer refills on both
    // the compress and decompress paths.
    internal static class Program
    {
        internal static void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            byte[] original = BuildBuffer(32768);

            // DeflateStream: WriteAsync(ReadOnlyMemory<byte>) + FlushAsync +
            // await using compress, ReadAsync(Memory<byte>) loop decompress.
            byte[] deflated = await CompressDeflateAsync(original);
            byte[] inflated = await DecompressDeflateAsync(deflated);
            Console.WriteLine("async-deflate: origLen=" + original.Length
                + " restoredLen=" + inflated.Length + " ok=" + BytesEqual(original, inflated));

            // GZipStream: WriteAsync(byte[], int, int) compress,
            // ReadAsync(byte[], int, int) loop decompress.
            byte[] gzipped = await CompressGZipAsync(original);
            byte[] gunzipped = await DecompressGZipAsync(gzipped);
            Console.WriteLine("async-gzip: origLen=" + original.Length
                + " restoredLen=" + gunzipped.Length + " ok=" + BytesEqual(original, gunzipped));

            // CopyToAsync: decompress-mode DeflateStream into a MemoryStream.
            byte[] copied = await DecompressDeflateCopyToAsync(deflated);
            Console.WriteLine("async-copyto: restoredLen=" + copied.Length
                + " ok=" + BytesEqual(original, copied));

            // A pre-canceled token makes WriteAsync/ReadAsync throw before any
            // stream work happens (the FromCanceled guard at the entry point).
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();

                bool writeCanceled = false;
                using (MemoryStream output = new MemoryStream())
                using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress))
                {
                    try
                    {
                        await deflate.WriteAsync(original, 0, original.Length, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        writeCanceled = true;
                    }
                }
                Console.WriteLine("async-cancel-write: canceled=" + writeCanceled);

                bool readCanceled = false;
                using (MemoryStream input = new MemoryStream(deflated))
                using (DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress))
                {
                    try
                    {
                        await deflate.ReadAsync(new Memory<byte>(new byte[16]), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        readCanceled = true;
                    }
                }
                Console.WriteLine("async-cancel-read: canceled=" + readCanceled);
            }
        }

        private static async Task<byte[]> CompressDeflateAsync(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            await using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                await deflate.WriteAsync(new ReadOnlyMemory<byte>(data));
                await deflate.FlushAsync();
            }
            return output.ToArray();
        }

        private static async Task<byte[]> DecompressDeflateAsync(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            await using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            byte[] chunk = new byte[512];
            int read;
            while ((read = await deflate.ReadAsync(new Memory<byte>(chunk))) > 0)
            {
                output.Write(chunk, 0, read);
            }
            return output.ToArray();
        }

        private static async Task<byte[]> DecompressDeflateCopyToAsync(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            await using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            await deflate.CopyToAsync(output);
            return output.ToArray();
        }

        private static async Task<byte[]> CompressGZipAsync(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            await using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                await gzip.WriteAsync(data, 0, data.Length);
                await gzip.FlushAsync();
            }
            return output.ToArray();
        }

        private static async Task<byte[]> DecompressGZipAsync(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            await using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            byte[] chunk = new byte[512];
            int read;
            while ((read = await gzip.ReadAsync(chunk, 0, chunk.Length)) > 0)
            {
                output.Write(chunk, 0, read);
            }
            return output.ToArray();
        }

        private static byte[] BuildBuffer(int length)
        {
            byte[] buffer = new byte[length];
            byte[] prefix = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. ");
            uint state = 0x2545F491u;
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
