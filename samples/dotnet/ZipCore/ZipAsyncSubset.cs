#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZipAsyncSubset
{
    // The net10 async face of the in-memory ZipArchive surface, over a
    // MemoryStream: CreateAsync in both modes, OpenAsync, both WriteAsync and both
    // ReadAsync overloads, CopyToAsync, and the DisposeAsync paths.
    //
    // Determinism discipline as in ZipCreateRoundTripSubset: this program creates
    // compressed bytes, so it must never print CompressedLength or archive sizes
    // (zlib-ng and classic zlib disagree). Every await is sequential on the main
    // flow, so the output order is fixed.
    internal static class Program
    {
        private static readonly DateTimeOffset FixedStamp =
            new DateTimeOffset(2021, 6, 7, 8, 9, 10, TimeSpan.Zero);

        internal static void __GateEntry()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            byte[] textPayload = Encoding.UTF8.GetBytes(
                "Zip async round-trip payload. Zip async round-trip payload.");
            byte[] binPayload = new byte[300];
            for (int i = 0; i < binPayload.Length; i++)
            {
                binPayload[i] = (byte)(i * 7);
            }

            var ms = new MemoryStream();
            ZipArchive archive = await ZipArchive.CreateAsync(
                ms, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null,
                cancellationToken: CancellationToken.None);
            await using (archive)
            {
                ZipArchiveEntry text = archive.CreateEntry("async/text.txt");
                text.LastWriteTime = FixedStamp;
                Stream ws = await text.OpenAsync(CancellationToken.None);
                await ws.WriteAsync(textPayload, 0, textPayload.Length);
                await ws.DisposeAsync();

                ZipArchiveEntry bin = archive.CreateEntry("async/bin.dat", CompressionLevel.Fastest);
                bin.LastWriteTime = FixedStamp;
                Stream wsb = await bin.OpenAsync(CancellationToken.None);
                await wsb.WriteAsync(new ReadOnlyMemory<byte>(binPayload));
                await wsb.DisposeAsync();
            }

            ms.Position = 0;
            ZipArchive reader = await ZipArchive.CreateAsync(
                ms, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null,
                cancellationToken: CancellationToken.None);
            await using (reader)
            {
                Console.WriteLine("zip-async: count=" + reader.Entries.Count);

                ZipArchiveEntry text = reader.GetEntry("async/text.txt");
                byte[] textBack = await ReadAllArrayAsync(text);
                Console.WriteLine("zip-async text: len=" + text.Length
                    + " stamp=" + (text.LastWriteTime.DateTime == FixedStamp.DateTime)
                    + " ok=" + BytesEqual(textBack, textPayload));

                ZipArchiveEntry bin = reader.GetEntry("async/bin.dat");
                byte[] binBack = await ReadAllMemoryAsync(bin);
                Console.WriteLine("zip-async bin: len=" + bin.Length
                    + " ok=" + BytesEqual(binBack, binPayload));

                byte[] copied = await ReadAllCopyToAsync(text);
                Console.WriteLine("zip-async copyto: ok=" + BytesEqual(copied, textPayload));
            }
        }

        // The byte[] ReadAsync overload; the small buffer forces a real loop.
        private static async Task<byte[]> ReadAllArrayAsync(ZipArchiveEntry entry)
        {
            var sink = new MemoryStream();
            Stream s = await entry.OpenAsync(CancellationToken.None);
            byte[] buf = new byte[16];
            int n;
            while ((n = await s.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                sink.Write(buf, 0, n);
            }
            await s.DisposeAsync();
            return sink.ToArray();
        }

        // The Memory<byte> overload.
        private static async Task<byte[]> ReadAllMemoryAsync(ZipArchiveEntry entry)
        {
            var sink = new MemoryStream();
            Stream s = await entry.OpenAsync(CancellationToken.None);
            byte[] buf = new byte[16];
            int n;
            while ((n = await s.ReadAsync(new Memory<byte>(buf))) > 0)
            {
                sink.Write(buf, 0, n);
            }
            await s.DisposeAsync();
            return sink.ToArray();
        }

        // CopyToAsync, and the `await using` disposal of an entry stream.
        private static async Task<byte[]> ReadAllCopyToAsync(ZipArchiveEntry entry)
        {
            var sink = new MemoryStream();
            Stream s = await entry.OpenAsync(CancellationToken.None);
            await using (s)
            {
                await s.CopyToAsync(sink);
            }
            return sink.ToArray();
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
