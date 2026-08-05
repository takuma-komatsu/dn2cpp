using System;

namespace FinalizerExitSubset
{
    // Unrun finalizers are best-effort at process exit -- the
    // dedicated finalizer thread is a detached background thread, so it is
    // not guaranteed to drain before the process ends when the program
    // never calls GC.WaitForPendingFinalizers(). This only checks that
    // queuing a batch and returning immediately does not hang or crash;
    // whether the finalizer body actually ran by then is non-deterministic
    // in real .NET too, so its output is intentionally not observed here.
    internal sealed class ExitFinalizable
    {
        public ExitFinalizable(int id) { _ = id; }
        ~ExitFinalizable()
        {
            // No observable side effect: see the note above.
        }
    }

    internal static class Program
    {
        private static void CreateMany()
        {
            for (int i = 0; i < 8; i++)
                _ = new ExitFinalizable(i);
        }

        internal static void __GateEntry()
        {
            CreateMany();
            for (int i = 0; i < 64; i++)
                _ = new object();
            GC.Collect();
            // Deliberately no GC.WaitForPendingFinalizers().
            Console.WriteLine("done-exit");
        }
    }
}
