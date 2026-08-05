#nullable enable
using System;
using System.Threading;

// A [ThreadStatic] field is per-thread GC-rooted storage: an object parked there stays
// alive with nothing else referencing it. Parked on both the main and a worker thread,
// then stale stack slots are scrubbed, collections forced, and the heap flooded with the
// same size class — collection alone does not clobber contents, so the reuse is what turns
// a missing root into observable corruption rather than a silent pass.
namespace ThreadStaticRoot;

static class Program
{
    sealed class Payload
    {
        public int Id;
        public string Tag = "";
        public Payload? Next;
    }

    // The struct-with-references flavour of per-thread rooting.
    struct RefPair
    {
        public object? Obj;
        public int N;
    }

    // Lands in the same small pointer-containing allocation class as Payload, so the
    // storm below actually recycles any block a collection freed by mistake.
    sealed class Blob
    {
        public object? A;
        public object? B;
    }

    [ThreadStatic] static Payload? t_payload;
    [ThreadStatic] static RefPair t_pair;

    static object? s_occupy;       // keeps the post-collect allocation storm reachable
    static int s_workerCheck = -1; // worker's verification result, read after Join

    // Overwrites the stack region Park used, so conservative stack scanning cannot keep
    // the parked objects alive by accident through a stale slot.
    static long Stomp(int depth)
    {
        if (depth <= 0)
            return 1;
        long a = depth;
        long b = depth * 2;
        long c = depth * 3;
        long d = depth * 4;
        return a + b + c + d + Stomp(depth - 1);
    }

    // After this returns, the ONLY references to these objects live in the current
    // thread's static storage.
    static void Park(int id)
    {
        var head = new Payload { Id = id, Tag = "tag" + id };
        head.Next = new Payload { Id = id * 2, Tag = "next" + id };
        t_payload = head;
        t_pair = new RefPair { Obj = new Payload { Id = id * 3, Tag = "pair" + id }, N = id };
    }

    // Full collections + allocation storm, so a block freed by mistake is overwritten
    // and a missing per-thread root shows up as corruption rather than staying invisible.
    static long Churn()
    {
        long sink = Stomp(128);
        for (int i = 0; i < 4; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int j = 0; j < 8192; j++)
            {
                var b = new Blob();
                b.A = s_occupy;
                b.B = b;
                s_occupy = b;
            }
            sink += i + 1;
        }
        return sink;
    }

    // Content-integrity read-back: 0 when intact, a distinct code per first mismatch.
    static int Verify(int id)
    {
        var p = t_payload;
        if (p is null)
            return 1;
        if (p.Id != id || p.Tag != "tag" + id)
            return 2;
        if (p.Next is null || p.Next.Id != id * 2 || p.Next.Tag != "next" + id)
            return 3;
        var q = t_pair.Obj as Payload;
        if (q is null || t_pair.N != id)
            return 4;
        if (q.Id != id * 3 || q.Tag != "pair" + id)
            return 5;
        return 0;
    }

    internal static void __GateEntry()
    {
        // Main parks first: the worker's collections below must not eat it.
        Park(1);

        long workerSink = 0;
        var worker = new Thread(() =>
        {
            Park(2);
            workerSink = Churn();      // collections + reuse storm triggered off-main
            s_workerCheck = Verify(2); // read back on the worker itself
        });
        worker.Start();
        worker.Join();

        long mainSink = Churn();       // and the same churn on the main thread
        int mainCheck = Verify(1);

        Console.WriteLine("ts worker=" + s_workerCheck);         // 0
        Console.WriteLine("ts main=" + mainCheck);               // 0
        Console.WriteLine("ts sink=" + (workerSink + mainSink)); // deterministic
    }
}
