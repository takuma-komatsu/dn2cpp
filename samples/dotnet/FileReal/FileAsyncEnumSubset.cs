using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileAsyncEnumSubset
{
    // File.ReadLinesAsync — the one File.* member returning IAsyncEnumerable<string>
    // rather than a Task. Roslyn lowers the `await foreach` to a MoveNextAsync loop over
    // a ValueTask<bool> promise, and the BCL's own producer is an async iterator
    // (AsyncIteratorMethodBuilder over a ManualResetValueTaskSourceCore). Same machinery
    // the async-streams section of the async-core bucket already asserts — it just had
    // never been driven from a real file.
    internal static class Program
    {
        internal static void __GateEntry(string dir) => RunAsync(dir).GetAwaiter().GetResult();

        private static async Task RunAsync(string dir)
        {
            string p = Path.Combine(dir, "lines-async.txt");
            await File.WriteAllTextAsync(p, "aa\nbb\ncc\n");

            int count = 0;
            await foreach (string line in File.ReadLinesAsync(p))
            {
                Console.WriteLine("rla:" + line);
                count++;
            }
            Console.WriteLine("rlaCount=" + count);

            // Abandoning the enumeration early must run the iterator's finally (which is
            // what closes the underlying StreamReader).
            await foreach (string line in File.ReadLinesAsync(p))
            {
                Console.WriteLine("rlaBreak:" + line);
                break;
            }
            // If the reader had leaked, this exclusive open would be refused.
            using (FileStream fs = new FileStream(p, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Console.WriteLine("rlaClosed=" + fs.CanWrite);

            // The CancellationToken overload, with a live (never-canceled) token.
            using (CancellationTokenSource live = new CancellationTokenSource())
            {
                int n = 0;
                await foreach (string line in File.ReadLinesAsync(p, live.Token))
                    n += line.Length;
                Console.WriteLine("rlaToken=" + n);
            }

            // …and with one already canceled: the enumeration throws rather than yielding.
            using (CancellationTokenSource dead = new CancellationTokenSource())
            {
                dead.Cancel();
                try
                {
                    await foreach (string line in File.ReadLinesAsync(p, dead.Token))
                        Console.WriteLine("rlaDead:" + line);
                    Console.WriteLine("rlaCanceled=noexc");
                }
                catch (OperationCanceledException) { Console.WriteLine("rlaCanceled=OperationCanceledException"); }
            }
        }
    }
}
