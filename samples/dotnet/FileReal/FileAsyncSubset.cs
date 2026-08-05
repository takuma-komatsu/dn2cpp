using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileAsyncSubset
{
    // The ASYNC File.* / FileStream surface.
    //
    // This used to be recorded as a carve-out ("the ValueTask/ThreadPool-generic
    // machinery"). It is not one any more, and had not been for some time: the Zip epic
    // landed exactly that machinery — ZipFile.OpenAsync / CreateFromDirectoryAsync are
    // gated and drive FileStream.ReadAsync/WriteAsync through the same RandomAccess ->
    // ThreadPoolValueTaskSource path these overloads take. The carve-out note outlived
    // the carve-out; this section is what replaces it.
    //
    // A live CancellationToken is threaded into file I/O here too — explicitly outside
    // the envelope the token machinery had previously been proven inside.
    internal static class Program
    {
        internal static void __GateEntry(string dir) => RunAsync(dir).GetAwaiter().GetResult();

        private static async Task RunAsync(string dir)
        {
            // The async File.* one-shots, each round-tripped through its sync sibling so
            // the two lanes must agree byte for byte.
            string t = Path.Combine(dir, "async.txt");
            await File.WriteAllTextAsync(t, "one\ntwo\n");
            Console.WriteLine("waTextAsync=[" + File.ReadAllText(t) + "]");
            Console.WriteLine("raTextAsync=[" + await File.ReadAllTextAsync(t) + "]");

            await File.AppendAllTextAsync(t, "three\n");
            Console.WriteLine("appendTextAsync=" + (await File.ReadAllTextAsync(t)).Length);

            string[] lines = await File.ReadAllLinesAsync(t);
            Console.WriteLine("raLinesAsync=" + lines.Length + ":" + lines[0] + "," + lines[2]);

            await File.WriteAllLinesAsync(t, new[] { "x", "y" });
            Console.WriteLine("waLinesAsync=" + (await File.ReadAllLinesAsync(t)).Length);

            string b = Path.Combine(dir, "async.bin");
            await File.WriteAllBytesAsync(b, new byte[] { 7, 8, 9 });
            byte[] raw = await File.ReadAllBytesAsync(b);
            Console.WriteLine("bytesAsync=" + raw.Length + ":" + raw[0] + "," + raw[2]);

            // The Encoding-taking async overloads (UTF-8 only — see StreamTextSubset).
            await File.WriteAllTextAsync(t, "encé\n", Encoding.UTF8);
            Console.WriteLine("encAsync=[" + await File.ReadAllTextAsync(t, Encoding.UTF8) + "]");

            // FileStream's own async surface, driven directly.
            string p = Path.Combine(dir, "fsasync.bin");
            using (FileStream fs = new FileStream(p, FileMode.Create, FileAccess.ReadWrite))
            {
                await fs.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
                await fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 6, 7 }));
                await fs.FlushAsync();
                Console.WriteLine("fsWriteAsync=" + fs.Length);

                fs.Position = 0;
                byte[] buf = new byte[7];
                int n = await fs.ReadAsync(buf, 0, 7);
                Console.WriteLine("fsReadAsync=" + n + ":" + buf[0] + "," + buf[6]);

                fs.Position = 2;
                Memory<byte> mem = new byte[3];
                int n2 = await fs.ReadAsync(mem);
                Console.WriteLine("fsReadAsyncMem=" + n2 + ":" + mem.Span[0] + "," + mem.Span[2]);
            }

            // CopyToAsync between two real FileStreams, and DisposeAsync.
            string q = Path.Combine(dir, "fsasync-copy.bin");
            FileStream from = new FileStream(p, FileMode.Open, FileAccess.Read);
            FileStream to = new FileStream(q, FileMode.Create, FileAccess.Write);
            await from.CopyToAsync(to);
            await from.DisposeAsync();
            await to.DisposeAsync();
            Console.WriteLine("fsCopyToAsync=" + new FileInfo(q).Length
                + ":" + File.ReadAllBytes(q)[6]);

            // A LIVE CancellationToken threaded through file I/O: one that is never
            // canceled completes normally…
            using (CancellationTokenSource live = new CancellationTokenSource())
            {
                string ct = Path.Combine(dir, "ct.txt");
                await File.WriteAllTextAsync(ct, "tokened", live.Token);
                Console.WriteLine("ctLive=[" + await File.ReadAllTextAsync(ct, live.Token) + "]");

                using (FileStream fs = new FileStream(ct, FileMode.Open, FileAccess.Read))
                {
                    byte[] buf = new byte[7];
                    Console.WriteLine("ctLiveFs=" + await fs.ReadAsync(buf, 0, 7, live.Token)
                        + ":" + (char)buf[0]);
                }
            }

            // …and one that is ALREADY canceled early-outs, at every layer.
            using (CancellationTokenSource dead = new CancellationTokenSource())
            {
                dead.Cancel();
                string cp = Path.Combine(dir, "ct2.txt");
                File.WriteAllText(cp, "already");

                try
                {
                    await File.ReadAllTextAsync(cp, dead.Token);
                    Console.WriteLine("ctDeadFile=noexc");
                }
                catch (OperationCanceledException) { Console.WriteLine("ctDeadFile=OperationCanceledException"); }

                try
                {
                    await File.WriteAllTextAsync(cp, "nope", dead.Token);
                    Console.WriteLine("ctDeadWrite=noexc");
                }
                catch (OperationCanceledException) { Console.WriteLine("ctDeadWrite=OperationCanceledException"); }

                using (FileStream fs = new FileStream(cp, FileMode.Open, FileAccess.Read))
                {
                    byte[] buf = new byte[4];
                    try
                    {
                        await fs.ReadAsync(buf, 0, 4, dead.Token);
                        Console.WriteLine("ctDeadFs=noexc");
                    }
                    catch (OperationCanceledException) { Console.WriteLine("ctDeadFs=OperationCanceledException"); }
                }
                // The cancellation did not eat the file.
                Console.WriteLine("ctIntact=[" + File.ReadAllText(cp) + "]");
            }

            // An async read of a file that is not there throws the real type, through the
            // async path.
            try
            {
                await File.ReadAllTextAsync(Path.Combine(dir, "none-async.txt"));
                Console.WriteLine("asyncMissing=noexc");
            }
            catch (FileNotFoundException) { Console.WriteLine("asyncMissing=FileNotFoundException"); }
        }
    }
}
