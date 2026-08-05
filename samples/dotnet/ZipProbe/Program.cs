using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// A --measure probe, NOT a gate: it enumerates the transpile gap surface of the
// whole Zip API face — in-memory create, fixed-blob read, update mode, the
// ZipFile filesystem face and the net10 async face — in one pass. Each section
// prints a round-trip bool, all True on real .NET.

namespace ZipProbe;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("s1 in-memory create ok: " + SectionInMemoryCreate());
        Console.WriteLine("s2 fixed-blob read ok: " + SectionFixedBlobRead());
        Console.WriteLine("s3 update ok: " + SectionUpdate());
        Console.WriteLine("s4 zipfile ok: " + SectionZipFile());
        Console.WriteLine("s5 async ok: " + SectionAsync().GetAwaiter().GetResult());
        return 0;
    }

    private static readonly DateTimeOffset FixedStamp =
        new DateTimeOffset(2020, 1, 2, 3, 4, 6, TimeSpan.Zero);

    private static bool SectionInMemoryCreate()
    {
        byte[] textPayload = Encoding.UTF8.GetBytes(
            "In-memory zip entry payload. In-memory zip entry payload.");
        byte[] binPayload = new byte[256];
        for (int i = 0; i < binPayload.Length; i++)
        {
            binPayload[i] = (byte)(i * 7);
        }

        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.Comment = "created in memory";

            ZipArchiveEntry e1 = archive.CreateEntry("first.txt");
            e1.Comment = "plain CreateEntry";
            e1.LastWriteTime = FixedStamp;
            using (Stream s = e1.Open())
            {
                s.Write(textPayload, 0, textPayload.Length);
            }

            ZipArchiveEntry e2 = archive.CreateEntry("dir/second.bin", CompressionLevel.NoCompression);
            e2.LastWriteTime = FixedStamp;
            using (Stream s = e2.Open())
            {
                s.Write(binPayload, 0, binPayload.Length);
            }

            ZipArchiveEntry e3 = archive.CreateEntry("third.txt", CompressionLevel.SmallestSize);
            e3.LastWriteTime = FixedStamp;
            using (Stream s = e3.Open())
            {
                s.Write(textPayload, 0, textPayload.Length);
            }
        }

        ms.Position = 0;
        bool ok = true;
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            ok &= archive.Comment == "created in memory";
            ok &= archive.Entries.Count == 3;
            ZipArchiveEntry e1 = archive.GetEntry("first.txt");
            ok &= e1 is not null
                && e1.Comment == "plain CreateEntry"
                && e1.LastWriteTime.DateTime == FixedStamp.DateTime
                && BytesEqual(ReadAll(e1), textPayload);
            ZipArchiveEntry e2 = archive.GetEntry("dir/second.bin");
            ok &= e2 is not null
                && e2.Name == "second.bin"
                && e2.Length == binPayload.Length
                && BytesEqual(ReadAll(e2), binPayload);
            ZipArchiveEntry e3 = archive.GetEntry("third.txt");
            ok &= e3 is not null && BytesEqual(ReadAll(e3), textPayload);
        }
        return ok;
    }

    // Generated once on real .NET, so the CompressedLength / Crc32 assertions
    // against it are stable constants.
    private const string FixedZipBase64 =
        "UEsDBBQAAAAIAIMYIlBAu6IjEQAAAC8AAAAJAAAAaGVsbG8udHh080jNycnXUYjKLFBU8CDMBgBQ" +
        "SwMEFAAAAAAAXb+fU4zODhBAAAAAQAAAAAwAAABzdWIvZGF0YS5iaW4AAQIDBAUGBwgJCgsMDQ4P" +
        "EBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/UEsBAhQDFAAA" +
        "AAgAgxgiUEC7oiMRAAAALwAAAAkAAAAOAAAAAAAAAKSBAAAAAGhlbGxvLnR4dGdyZWV0aW5nIGVu" +
        "dHJ5UEsBAhQDFAAAAAAAXb+fU4zODhBAAAAAQAAAAAwAAAAAAAAAAAAAAKSBOAAAAHN1Yi9kYXRh" +
        "LmJpblBLBQYAAAAAAgACAH8AAACiAAAAEwBmaXhlZCBwcm9iZSBhcmNoaXZl";

    private static bool SectionFixedBlobRead()
    {
        byte[] blob = Convert.FromBase64String(FixedZipBase64);
        byte[] expectedText = Encoding.UTF8.GetBytes(
            "Hello, Zip! Hello, Zip! Hello, Zip! Hello, Zip!");
        byte[] expectedBin = new byte[64];
        for (int i = 0; i < expectedBin.Length; i++)
        {
            expectedBin[i] = (byte)i;
        }

        bool ok = true;
        using (var archive = new ZipArchive(new MemoryStream(blob), ZipArchiveMode.Read))
        {
            ok &= archive.Comment == "fixed probe archive";
            ok &= archive.Entries.Count == 2;

            ZipArchiveEntry e1 = archive.Entries[0];
            ok &= e1.FullName == "hello.txt"
                && e1.Name == "hello.txt"
                && e1.Length == 47
                && e1.CompressedLength == 17
                && e1.Crc32 == 597867328u
                && e1.Comment == "greeting entry"
                && e1.LastWriteTime.DateTime == new DateTime(2020, 1, 2, 3, 4, 6)
                && BytesEqual(ReadAll(e1), expectedText);

            ZipArchiveEntry e2 = archive.Entries[1];
            ok &= e2.FullName == "sub/data.bin"
                && e2.Name == "data.bin"
                && e2.Length == 64
                && e2.CompressedLength == 64
                && e2.Crc32 == 269405836u
                && e2.Comment == ""
                && e2.LastWriteTime.DateTime == new DateTime(2021, 12, 31, 23, 58, 58)
                && BytesEqual(ReadAll(e2), expectedBin);
        }
        return ok;
    }

    private static bool SectionUpdate()
    {
        byte[] blob = Convert.FromBase64String(FixedZipBase64);
        var ms = new MemoryStream();
        ms.Write(blob, 0, blob.Length);

        byte[] appended = Encoding.UTF8.GetBytes(" [appended]");
        byte[] fresh = Encoding.UTF8.GetBytes("fresh entry payload");

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            archive.GetEntry("sub/data.bin").Delete();

            ZipArchiveEntry hello = archive.GetEntry("hello.txt");
            using (Stream s = hello.Open())
            {
                s.Seek(0, SeekOrigin.End);
                s.Write(appended, 0, appended.Length);
            }

            ZipArchiveEntry added = archive.CreateEntry("added/fresh.txt", CompressionLevel.Fastest);
            added.LastWriteTime = FixedStamp;
            using (Stream s = added.Open())
            {
                s.Write(fresh, 0, fresh.Length);
            }
        }

        ms.Position = 0;
        bool ok = true;
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            ok &= archive.Entries.Count == 2;
            ok &= archive.GetEntry("sub/data.bin") is null;

            byte[] expectedHello = Encoding.UTF8.GetBytes(
                "Hello, Zip! Hello, Zip! Hello, Zip! Hello, Zip! [appended]");
            ZipArchiveEntry hello = archive.GetEntry("hello.txt");
            ok &= hello is not null && BytesEqual(ReadAll(hello), expectedHello);

            ZipArchiveEntry added = archive.GetEntry("added/fresh.txt");
            ok &= added is not null && BytesEqual(ReadAll(added), fresh);
        }
        return ok;
    }

    // The temp-dir name is FIXED rather than a Guid: Guid formatting drags its own
    // non-Zip gap rows into the measurement and would pollute the inventory. Each
    // section pre-cleans its directory instead.
    private static bool SectionZipFile()
    {
        string work = Path.Combine(Path.GetTempPath(), "zipprobe-work-s4");
        if (Directory.Exists(work))
        {
            Directory.Delete(work, recursive: true);
        }
        try
        {
            string srcDir = Path.Combine(work, "src");
            Directory.CreateDirectory(Path.Combine(srcDir, "nested"));
            File.WriteAllText(Path.Combine(srcDir, "a.txt"), "alpha file contents");
            File.WriteAllBytes(Path.Combine(srcDir, "b.bin"), new byte[] { 1, 2, 3, 4, 5 });
            File.WriteAllText(Path.Combine(srcDir, "nested", "c.txt"), "nested file contents");

            string zipPath = Path.Combine(work, "archive.zip");
            ZipFile.CreateFromDirectory(srcDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            string outDir = Path.Combine(work, "out");
            ZipFile.ExtractToDirectory(zipPath, outDir);

            bool ok = true;
            ok &= File.ReadAllText(Path.Combine(outDir, "a.txt")) == "alpha file contents";
            ok &= BytesEqual(File.ReadAllBytes(Path.Combine(outDir, "b.bin")), new byte[] { 1, 2, 3, 4, 5 });
            ok &= File.ReadAllText(Path.Combine(outDir, "nested", "c.txt")) == "nested file contents";

            string extraSrc = Path.Combine(work, "extra.txt");
            File.WriteAllText(extraSrc, "late-added file");
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                archive.CreateEntryFromFile(extraSrc, "extra/added.txt", CompressionLevel.Fastest);
            }

            string extracted = Path.Combine(work, "added-back.txt");
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                ok &= archive.Entries.Count == 4;
                archive.GetEntry("extra/added.txt").ExtractToFile(extracted, overwrite: false);
            }
            ok &= File.ReadAllText(extracted) == "late-added file";
            return ok;
        }
        finally
        {
            if (Directory.Exists(work))
            {
                Directory.Delete(work, recursive: true);
            }
        }
    }

    private static async Task<bool> SectionAsync()
    {
        bool ok = await InMemoryAsync();
        ok &= await ZipFileAsync();
        return ok;
    }

    private static async Task<bool> InMemoryAsync()
    {
        byte[] payload = Encoding.UTF8.GetBytes("async entry payload async entry payload");

        var ms = new MemoryStream();
        ZipArchive archive = await ZipArchive.CreateAsync(
            ms, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null,
            cancellationToken: CancellationToken.None);
        ZipArchiveEntry entry = archive.CreateEntry("async.txt");
        entry.LastWriteTime = FixedStamp;
        Stream ws = await entry.OpenAsync(CancellationToken.None);
        await ws.WriteAsync(payload, 0, payload.Length);
        await ws.DisposeAsync();
        await archive.DisposeAsync();

        ms.Position = 0;
        bool ok = true;
        ZipArchive reader = await ZipArchive.CreateAsync(
            ms, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null,
            cancellationToken: CancellationToken.None);
        await using (reader)
        {
            ZipArchiveEntry back = reader.GetEntry("async.txt");
            ok &= back is not null;
            var sink = new MemoryStream();
            Stream rs = await back.OpenAsync(CancellationToken.None);
            byte[] buf = new byte[16];
            int n;
            while ((n = await rs.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                sink.Write(buf, 0, n);
            }
            await rs.DisposeAsync();
            ok &= BytesEqual(sink.ToArray(), payload);
        }
        return ok;
    }

    private static async Task<bool> ZipFileAsync()
    {
        string work = Path.Combine(Path.GetTempPath(), "zipprobe-work-s5");
        if (Directory.Exists(work))
        {
            Directory.Delete(work, recursive: true);
        }
        try
        {
            string srcDir = Path.Combine(work, "src");
            Directory.CreateDirectory(Path.Combine(srcDir, "nested"));
            File.WriteAllText(Path.Combine(srcDir, "a.txt"), "async alpha");
            File.WriteAllText(Path.Combine(srcDir, "nested", "c.txt"), "async nested");

            string zipPath = Path.Combine(work, "archive.zip");
            await ZipFile.CreateFromDirectoryAsync(
                srcDir, zipPath, CancellationToken.None);

            string outDir = Path.Combine(work, "out");
            await ZipFile.ExtractToDirectoryAsync(
                zipPath, outDir, CancellationToken.None);

            bool ok = true;
            ok &= File.ReadAllText(Path.Combine(outDir, "a.txt")) == "async alpha";
            ok &= File.ReadAllText(Path.Combine(outDir, "nested", "c.txt")) == "async nested";

            ZipArchive archive = await ZipFile.OpenAsync(
                zipPath, ZipArchiveMode.Read, CancellationToken.None);
            await using (archive)
            {
                ok &= archive.Entries.Count == 2;
                ok &= archive.GetEntry("nested/c.txt") is not null;
            }
            return ok;
        }
        finally
        {
            if (Directory.Exists(work))
            {
                Directory.Delete(work, recursive: true);
            }
        }
    }

    private static byte[] ReadAll(ZipArchiveEntry entry)
    {
        var sink = new MemoryStream();
        using (Stream s = entry.Open())
        {
            s.CopyTo(sink);
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
