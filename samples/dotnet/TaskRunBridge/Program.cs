using System;
using System.Threading;
using System.Threading.Tasks;

// Task.Run on the real worker pool, bridged back to the cooperative virtual-time async
// scheduler: a worker completes the Task and the bridge routes the continuation to the
// awaiting thread's scheduler. Every result is awaited, so the output diffs exact vs .NET.

// 16-byte layout: too wide for the 8-byte task result slot, so it takes the boxing
// trampoline shared by Task.Run / StartNew / ContinueWith.
struct Pt
{
    public int X;
    public long Y;
    public override string ToString() => $"({X},{Y})";
}

static class Program
{
    // A struct/ValueTuple result is boxed by the worker and read back by TResult, across
    // Task.Run, TaskFactory.StartNew, ContinueWith and a nested tuple.
    static async Task<string> StructResults()
    {
        var (n, sz) = await Task.Run(() => (3 + 4, 1000UL + 24));           // (7, 1024)
        var sn = await Task.Factory.StartNew(() => (n * 2, sz + 1));         // (14, 1025)
        Pt pt = await Task.Run(() => n)
            .ContinueWith(t => new Pt { X = t.Result, Y = (long)sz });       // (7,1024)
        var nested = await Task.Run(() => (n, (sz, "ok")));                  // (7,(1024,ok))
        return $"run={n},{sz} startnew={sn.Item1},{sn.Item2} contwith={pt} " +
               $"nested={nested.Item1},{nested.Item2.Item1},{nested.Item2.Item2}";
    }

    static async Task<int> Compute()
    {
        int a = await Task.Run(() =>
        {
            int s = 0;
            for (int i = 1; i <= 100; i++) s += i;
            return s;            // 5050
        });
        int b = await Task.Run(() => 42);
        return a + b;            // 5092
    }

    static async Task<int> ParallelSquares()
    {
        var tasks = new Task<int>[4];
        for (int i = 0; i < 4; i++)
        {
            int k = i;
            tasks[i] = Task.Run(() => k * k);
        }
        int[] r = await Task.WhenAll(tasks);
        int total = 0;
        foreach (var x in r) total += x;
        return total;            // 0+1+4+9 = 14
    }

    static async Task<int> ActionSideEffect()
    {
        int sink = 0;
        await Task.Run(() => { Interlocked.Add(ref sink, 10); });
        return sink;             // 10
    }

    static async Task<string> RefResult()
    {
        string s = await Task.Run(() => "hello");
        // A virtual-time delay on the awaiting thread: the cooperative path stays intact.
        await Task.Delay(5);
        return s + "/done";      // hello/done
    }

    static void Main()
    {
        Console.WriteLine(Compute().Result);          // 5092
        Console.WriteLine(ParallelSquares().Result);  // 14
        Console.WriteLine(ActionSideEffect().Result); // 10
        Console.WriteLine(RefResult().Result);        // hello/done
        Console.WriteLine(StructResults().Result);    // struct/tuple results via the pool
    }
}
