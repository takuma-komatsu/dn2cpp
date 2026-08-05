using System;
using System.Runtime.CompilerServices;

namespace FinalizerSuppressSubset
{
    // The canonical Dispose(bool) pattern calls
    // GC.SuppressFinalize(this) once resources are released deterministically,
    // so the finalizer must NOT run for a disposed instance.
    internal sealed class DisposableFinalizable : IDisposable
    {
        private readonly string _tag;
        private bool _disposed;

        public DisposableFinalizable(string tag) { _tag = tag; }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Console.WriteLine("disposed " + _tag);
            GC.SuppressFinalize(this);
        }

        ~DisposableFinalizable()
        {
            Console.WriteLine("finalized " + _tag);
            Program.Finalized++;
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // NoInlining (here and on the control below) is load-bearing under the
        // conservative (Boehm) GC: an inlined body's reference slots join the
        // retry loop's live frame and are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateDisposeAndDrop(string tag)
        {
            var obj = new DisposableFinalizable(tag);
            obj.Dispose();
        }

        // Never disposed -> the finalizer must still run (control: proves
        // SuppressFinalize actually suppresses, rather than the runtime having
        // stopped running finalizers altogether).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop(string tag)
        {
            _ = new DisposableFinalizable(tag);
        }

        internal static void __GateEntry()
        {
            CreateDisposeAndDrop("suppressed");
            CreateAndDrop("unsuppressed");
            // Collect until the unsuppressed control's finalizer
            // reports in: one round is not guaranteed to reclaim it under a
            // conservative collector. The extra rounds cannot make the
            // suppressed instance fire (it is deregistered), and real .NET
            // always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("done-suppress");
        }
    }
}
