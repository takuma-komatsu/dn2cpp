#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using ZipFileCore;

namespace ZipFileAsyncSubset
{
    // The net10 async face of ZipFile plus the pre-canceled-token early-outs. Its
    // real subject is the file-backed async underlay: BufferedFileStreamStrategy's
    // WaitAsync semaphore and the pool-scheduled reads beneath Stream.ReadAsync.
    // Determinism follows ZipScratch's rules. The cancel catches print a fixed
    // label, not the exception type: real .NET raises TaskCanceledException where
    // the transpiled runtime raises the base OperationCanceledException.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            Console.WriteLine("-- ZipFile async --");
            string work = ZipScratch.Fresh("async");
            try
            {
                string src = Path.Combine(work, "src");
                Directory.CreateDirectory(Path.Combine(src, "nested"));
                File.WriteAllText(Path.Combine(src, "a.txt"), "async alpha contents");
                File.WriteAllBytes(Path.Combine(src, "b.bin"), new byte[] { 9, 8, 7, 6, 5, 4, 3 });
                File.WriteAllText(Path.Combine(src, "nested", "c.txt"), "async nested contents");

                string zipPath = Path.Combine(work, "async.zip");
                await ZipFile.CreateFromDirectoryAsync(src, zipPath, CancellationToken.None);
                string outDir = Path.Combine(work, "out");
                await ZipFile.ExtractToDirectoryAsync(zipPath, outDir, CancellationToken.None);
                ZipScratch.Print("async extracted", ZipScratch.ListTree(outDir));
                bool contentsOk = File.ReadAllText(Path.Combine(outDir, "a.txt")) == "async alpha contents"
                    && File.ReadAllText(Path.Combine(outDir, "nested", "c.txt")) == "async nested contents"
                    && File.ReadAllBytes(Path.Combine(outDir, "b.bin")).Length == 7;
                Console.WriteLine("async contents ok: " + contentsOk);

                // An async entry read over the file-backed stream.
                ZipArchive archive = await ZipFile.OpenAsync(
                    zipPath, ZipArchiveMode.Read, CancellationToken.None);
                await using (archive)
                {
                    Console.WriteLine("async open: count=" + archive.Entries.Count);
                    ZipArchiveEntry nested = archive.GetEntry("nested/c.txt");
                    Console.WriteLine("async open nested: found=" + (nested is not null)
                        + " len=" + nested.Length);
                    var sink = new MemoryStream();
                    Stream s = await nested.OpenAsync(CancellationToken.None);
                    await using (s)
                    {
                        byte[] buf = new byte[8];
                        int n;
                        while ((n = await s.ReadAsync(buf, 0, buf.Length)) > 0)
                        {
                            sink.Write(buf, 0, n);
                        }
                    }
                    Console.WriteLine("async open read ok: "
                        + (System.Text.Encoding.UTF8.GetString(sink.ToArray()) == "async nested contents"));
                }

                // Each entry point must early-out before touching the filesystem,
                // so the canceled paths below stay unused.
                using (var cts = new CancellationTokenSource())
                {
                    cts.Cancel();

                    bool createCanceled = false;
                    try
                    {
                        await ZipFile.CreateFromDirectoryAsync(
                            src, Path.Combine(work, "canceled.zip"), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        createCanceled = true;
                    }
                    Console.WriteLine("async cancel create: canceled=" + createCanceled);

                    bool extractCanceled = false;
                    try
                    {
                        await ZipFile.ExtractToDirectoryAsync(
                            zipPath, Path.Combine(work, "canceled-out"), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        extractCanceled = true;
                    }
                    Console.WriteLine("async cancel extract: canceled=" + extractCanceled);

                    bool openCanceled = false;
                    try
                    {
                        ZipArchive canceledArchive = await ZipFile.OpenAsync(
                            zipPath, ZipArchiveMode.Read, cts.Token);
                        await canceledArchive.DisposeAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        openCanceled = true;
                    }
                    Console.WriteLine("async cancel open: canceled=" + openCanceled);
                }
            }
            finally
            {
                ZipScratch.Cleanup(work);
            }
        }
    }
}
