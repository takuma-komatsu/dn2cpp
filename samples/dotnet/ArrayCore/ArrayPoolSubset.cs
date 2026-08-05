#nullable disable
using System;
using System.Buffers;

namespace ArrayPoolSubset
{
    // App-module code uses
    // System.Buffers.ArrayPool<T>.Shared.Rent/Return directly with byte / int /
    // reference (string) element types, in the typical Rent -> write -> read ->
    // Return pattern. The pool runs from the REAL BCL IL (SharedArrayPool<T> and
    // its TLS buckets / per-core partitions / ConditionalWeakTable registry; only
    // its ArrayPoolEventSource diagnostics are modeled as a no-op) — so beyond the
    // value round-trips this also asserts the pool's observable pooling behavior:
    // Return-then-Rent on the same thread and bucket hands back the SAME instance
    // (the TLS-bucket fast path), and Return(clearArray: true) zeroes/nulls the
    // buffer the next Rent observes. Both hold on real .NET (same IL, same
    // single-threaded schedule), so the output stays a deterministic exact diff.
    // We only print values we wrote ourselves plus those two facts (never the array
    // Length, which the pool rounds up). CoreLib only (no Linq shim): numeric
    // values are printed as int (Console.WriteLine(int) / int concat) to stay on
    // the transpilable int-format path, matching the existing sample conventions.
    //
    // ORDERING: driven FIRST, and the driver says so. This is the only section in the
    // bucket asserting a fact about process-wide state — `ReferenceEquals` on a buffer
    // handed back by ArrayPool<int>.Shared's thread-local bucket — so first is where it
    // sees a pristine pool. That is a margin rather than a requirement: the identity is
    // a property of the Return/Rent PAIR (Return writes the calling thread's TLS slot and
    // the next Rent of that bucket reads it), and the only asynchronous clearer,
    // SharedArrayPool.Trim off Gen2GcCallback, needs machine-wide High memory pressure
    // this program cannot create.
    internal static class Program
    {
        internal static void Run()
        {
            // --- byte ---
            byte[] b = ArrayPool<byte>.Shared.Rent(5);
            for (int i = 0; i < 5; i++) b[i] = (byte)((i * 7 + 3) & 0xFF);
            int bsum = 0;
            for (int i = 0; i < 5; i++) bsum += b[i];
            Console.WriteLine("byte first=" + (int)b[0] + " last=" + (int)b[4] + " sum=" + bsum);
            ArrayPool<byte>.Shared.Return(b);

            // --- int (Return with clearArray) ---
            int[] a = ArrayPool<int>.Shared.Rent(10);
            for (int i = 0; i < 10; i++) a[i] = i * i;
            int isum = 0;
            for (int i = 0; i < 10; i++) isum += a[i];
            Console.WriteLine("int first=" + a[0] + " last=" + a[9] + " sum=" + isum);
            ArrayPool<int>.Shared.Return(a, clearArray: true);

            // --- reference type (string) ---
            string[] s = ArrayPool<string>.Shared.Rent(3);
            s[0] = "alpha"; s[1] = "beta"; s[2] = "gamma";
            Console.WriteLine("str=" + s[0] + "," + s[1] + "," + s[2]);
            ArrayPool<string>.Shared.Return(s, clearArray: true);

            // --- Return then Rent again: the TLS-bucket fast path hands the SAME
            //     instance back (Rent(10) and Rent(4) share the 16-element bucket),
            //     and the clearArray Return above means it comes back zeroed ---
            int[] a2 = ArrayPool<int>.Shared.Rent(4);
            Console.WriteLine("same=" + (ReferenceEquals(a, a2) ? 1 : 0)
                + " cleared=" + (a2[0] == 0 && a2[3] == 0 ? 1 : 0));
            for (int i = 0; i < 4; i++) a2[i] = 1000 - i;
            Console.WriteLine("reuse=" + a2[0] + "," + a2[1] + "," + a2[2] + "," + a2[3]);
            ArrayPool<int>.Shared.Return(a2);

            // --- the string bucket the same way: cleared to null on Return ---
            string[] s2 = ArrayPool<string>.Shared.Rent(3);
            Console.WriteLine("strSame=" + (ReferenceEquals(s, s2) ? 1 : 0)
                + " strCleared=" + (s2[0] is null && s2[2] is null ? 1 : 0));
            ArrayPool<string>.Shared.Return(s2);

            // --- a helper that takes a rented buffer through a local (exercises the
            //     pool receiver flowing across a method, not just inline) ---
            Console.WriteLine("checksum=" + Checksum(8));
        }

        private static int Checksum(int n)
        {
            int[] buf = ArrayPool<int>.Shared.Rent(n);
            for (int i = 0; i < n; i++) buf[i] = (i + 1) * 3;
            int acc = 0;
            for (int i = 0; i < n; i++) acc += buf[i];
            ArrayPool<int>.Shared.Return(buf);
            return acc;
        }
    }
}
