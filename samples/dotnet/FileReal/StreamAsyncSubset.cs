using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StreamAsyncSubset
{
    // The BASE System.IO.Stream async surface — the slots a Stream subclass that
    // overrides only the abstract SYNCHRONOUS Read/Write inherits. That is the shape
    // of every wrapper and decorator stream there is, and it is the one the BCL's own
    // stream types are not: FileStream, MemoryStream and DeflateStream each override
    // the whole async surface, so no gate that goes through them can see this slot at
    // all.
    //
    // The base ReadAsync/WriteAsync bodies funnel through two private methods whose
    // real implementations reach the APM ReadWriteTask scheduler machinery dn2cpp does
    // not model; they are cut, and the cut used to leave the call site pushing the
    // default — a NULL Task. `await myStream.ReadAsync(...)` therefore awaited a
    // nullptr. The funnel is now rewritten to dispatch synchronously through the
    // Read/Write slot the subclass does override
    // (MethodCompiler.TryEmitStreamSyncOverAsyncFunnel), which is what the machinery
    // being replaced amounts to once its scheduler is taken out.

    // Overrides ONLY the abstract synchronous Read/Write: the inherited base slots are
    // what this section is about.
    internal sealed class SyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner = new MemoryStream();

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        // Disposes the inner stream too, so a read after Dispose throws out of the SYNC
        // slot — which is what makes the disposed-read assertion below test the rewritten
        // funnel's exception path instead of nothing.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    // Overrides the async surface TOO, and deliberately disagrees with its own sync
    // Read/Write about what it returns: async reads yield 2s, sync reads yield 1s. The
    // rewrite supplies the BASE slot's body only, so a rewrite that hijacked an
    // overriding type — dispatching ReadAsync into the sync Read — would print 1s here.
    // This is the assertion that the dispatch is right, not merely that it runs.
    internal sealed class AsyncOverridingStream : Stream
    {
        public int SyncWrites;
        public int AsyncWrites;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                buffer[offset + i] = 1;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count) => SyncWrites++;

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            for (int i = 0; i < count; i++)
                buffer[offset + i] = 2;
            return Task.FromResult(count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AsyncWrites++;
            return Task.CompletedTask;
        }
    }

    internal static class Program
    {
        internal static void __GateEntry(string dir) => RunAsync(dir).GetAwaiter().GetResult();

        private static async Task RunAsync(string dir)
        {
            SyncOnlyStream s = new SyncOnlyStream();

            // The base WriteAsync/ReadAsync(byte[], int, int, CancellationToken) slots.
            await s.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            Console.WriteLine("baseWriteAsync=" + s.Length);

            s.Position = 0;
            byte[] buf = new byte[5];
            int n = await s.ReadAsync(buf, 0, 5);
            Console.WriteLine("baseReadAsync=" + n + ":" + buf[0] + "," + buf[4]);

            // The base Memory<byte>/ReadOnlyMemory<byte> ValueTask overloads. Their real
            // bodies forward to the byte[] overloads above — virtually, so an overriding
            // type still wins — after unwrapping the memory to its backing array.
            s.Position = 0;
            await s.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 9, 8, 7 }));
            s.Position = 0;
            byte[] buf2 = new byte[3];
            int n2 = await s.ReadAsync(new Memory<byte>(buf2));
            Console.WriteLine("baseReadAsyncMem=" + n2 + ":" + buf2[0] + "," + buf2[2]);

            // Base FlushAsync, and base CopyToAsync — which drives the Memory overloads in
            // a loop, so it exercises the rewritten funnel from inside a BCL async method.
            await s.FlushAsync();
            s.Position = 0;
            MemoryStream dst = new MemoryStream();
            await s.CopyToAsync(dst);
            Console.WriteLine("baseCopyToAsync=" + dst.Length + ":" + dst.ToArray()[0]);

            // CopyToAsync from the sync-only stream INTO a real FileStream: the read side
            // takes the rewritten base slot, the write side dispatches to FileStream's own
            // async override. Both halves of the boundary in one call.
            string p = Path.Combine(dir, "streamasync.bin");
            s.Position = 0;
            using (FileStream fs = File.Create(p))
                await s.CopyToAsync(fs);
            Console.WriteLine("baseCopyToFile=" + File.ReadAllBytes(p).Length + ":" + File.ReadAllBytes(p)[0]);

            // A pre-canceled token: the real ReadAsync IL early-outs to Task.FromCanceled
            // BEFORE the funnel, and that arm is left intact by the rewrite.
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                await s.ReadAsync(buf, 0, 5, cts.Token);
                Console.WriteLine("baseReadAsyncCanceled=noexc");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("baseReadAsyncCanceled=OperationCanceledException");
            }

            // An exception out of the sync slot must reach the awaiter. (The rewrite runs
            // the read on the calling thread, so it propagates from the ReadAsync call
            // rather than as a faulted Task — indistinguishable from inside an async
            // method, which is every await shape.)
            SyncOnlyStream closed = new SyncOnlyStream();
            closed.Dispose();
            try
            {
                await closed.ReadAsync(buf, 0, 5);
                Console.WriteLine("baseReadAsyncDisposed=noexc");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("baseReadAsyncDisposed=ObjectDisposedException");
            }

            // A subclass that DOES override the async surface keeps dispatching to its own
            // override. Its async reads write 2s and its sync reads write 1s, so a rewrite
            // that hijacked the override instead of supplying the base slot would print 1.
            AsyncOverridingStream over = new AsyncOverridingStream();
            byte[] ov = new byte[3];
            over.Read(ov, 0, 3);
            Console.Write("overrideSync=" + ov[0] + ",");
            int n3 = await over.ReadAsync(ov, 0, 3);
            Console.WriteLine(" overrideAsync=" + n3 + ":" + ov[0]);

            // …including through the Memory overload, whose base body forwards virtually,
            // and through the WriteAsync side (its sync Write is never called).
            byte[] ov2 = new byte[2];
            await over.ReadAsync(new Memory<byte>(ov2));
            await over.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 1 }));
            Console.WriteLine("overrideMem=" + ov2[0] + " sync=" + over.SyncWrites + " async=" + over.AsyncWrites);
        }
    }
}
