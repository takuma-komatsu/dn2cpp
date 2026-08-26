using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Parallel.For / ForEach / Invoke over a real OS-thread fan-out. Every loop is written so
// its result is order-independent — disjoint per-element slot writes reduced sequentially
// (a parallel floating sum would be non-associative), or a commutative Interlocked
// accumulation — and every result is read only after the barriering call returns. So the
// output is exact-diffable against real .NET, and a racy or non-barriering implementation
// cannot reproduce it.
static class Program
{
    const int N = 1000;
    static string Generic<T>(T value) => value!.ToString()!;

    static void Main()
    {
        // 1) Parallel.For(int, int, Action<int>).
        long[] sq = new long[N];
        ParallelLoopResult r1 = Parallel.For(0, N, i => sq[i] = (long)i * i);
        long sum1 = 0;
        for (int i = 0; i < N; i++) sum1 += sq[i];
        Console.WriteLine(r1.IsCompleted); // True
        Console.WriteLine(sum1);           // 332833500

        int counter = 0;
        Parallel.For(0, N, _ => Interlocked.Increment(ref counter));
        Console.WriteLine(counter);        // 1000

        // 2) Parallel.For(long, long, Action<long>).
        long[] dbl = new long[N];
        Parallel.For(0L, (long)N, i => dbl[(int)i] = i * 2);
        long sum2 = 0;
        for (int i = 0; i < N; i++) sum2 += dbl[i];
        Console.WriteLine(sum2);           // 999000

        // 3) Parallel.ForEach over int[].
        int[] nums = new int[N];
        for (int i = 0; i < N; i++) nums[i] = i + 1;
        long feSum = 0;
        Parallel.ForEach(nums, n => Interlocked.Add(ref feSum, n));
        Console.WriteLine(feSum);          // 500500

        // 4) Parallel.ForEach over reference elements.
        string[] words = { "alpha", "beta", "gamma", "delta", "epsilon", "zeta" };
        int lenSum = 0;
        Parallel.ForEach(words, w => Interlocked.Add(ref lenSum, w.Length));
        Console.WriteLine(lenSum);         // 30

        // 5) Parallel.ForEach over double[].
        double[] ds = new double[N];
        for (int i = 0; i < N; i++) ds[i] = i;
        double[] dout = new double[N];
        Parallel.ForEach(ds, x => dout[(int)x] = x * 0.5);
        double dsum = 0;
        for (int i = 0; i < N; i++) dsum += dout[i];
        Console.WriteLine(dsum);           // 249750

        // 6) Parallel.Invoke(params Action[]): the join barrier publishes each action's
        // write before the read.
        int a = 0, b = 0, c = 0;
        Parallel.Invoke(
            () => a = 1,
            () => b = 2,
            () => c = 3);
        Console.WriteLine(a + b + c);      // 6

        // 7) The ParallelOptions overloads. MaxDegreeOfParallelism changes only the fan-out
        // width, never the result, so these re-check 1/3/6's outputs — thread-count
        // behaviour itself is non-deterministic by design and not asserted.
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        Console.WriteLine(options.MaxDegreeOfParallelism); // 2

        long[] sq2 = new long[N];
        ParallelLoopResult r2 = Parallel.For(0, N, options, i => sq2[i] = (long)i * i);
        long sum3 = 0;
        for (int i = 0; i < N; i++) sum3 += sq2[i];
        Console.WriteLine(r2.IsCompleted);  // True
        Console.WriteLine(sum3);            // 332833500

        int[] nums2 = new int[N];
        for (int i = 0; i < N; i++) nums2[i] = i + 1;
        long feSum2 = 0;
        Parallel.ForEach(nums2, options, n => Interlocked.Add(ref feSum2, n));
        Console.WriteLine(feSum2);          // 500500

        int a2 = 0, b2 = 0, c2 = 0;
        Parallel.Invoke(options,
            () => a2 = 1,
            () => b2 = 2,
            () => c2 = 3);
        Console.WriteLine(a2 + b2 + c2);    // 6

        // The setter rejects 0 and anything below -1; only -1 means "unlimited".
        try
        {
            new ParallelOptions { MaxDegreeOfParallelism = 0 };
            Console.WriteLine("no exception");
        }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("ArgumentOutOfRangeException"); }
        try
        {
            new ParallelOptions { MaxDegreeOfParallelism = -2 };
            Console.WriteLine("no exception");
        }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("ArgumentOutOfRangeException"); }

        // 8) ParallelLoopState (Break/Stop). Every case pins MaxDegreeOfParallelism = 1:
        // scheduling order is non-deterministic on both runtimes, so strictly sequential
        // execution is the only way LowestBreakIteration and the ran-iteration count diff.
        var seqOpts = new ParallelOptions { MaxDegreeOfParallelism = 1 };

        // Break(): iterations at or before the break point still run; later ones do not.
        int[] breakSeen = new int[N];
        ParallelLoopResult rBreak = Parallel.For(0, N, seqOpts, (i, state) =>
        {
            breakSeen[i] = 1;
            if (i == 5) state.Break();
        });
        int breakCount = 0;
        for (int i = 0; i < N; i++) breakCount += breakSeen[i];
        Console.WriteLine(rBreak.IsCompleted);                  // False
        Console.WriteLine(rBreak.LowestBreakIteration.HasValue); // True
        Console.WriteLine(rBreak.LowestBreakIteration.GetValueOrDefault()); // 5
        Console.WriteLine(breakCount);                           // 6 (iterations 0..5 ran)

        // Stop(): no further iteration starts, and LowestBreakIteration stays unset.
        int[] stopSeen = new int[N];
        ParallelLoopResult rStop = Parallel.For(0, N, seqOpts, (i, state) =>
        {
            stopSeen[i] = 1;
            if (i == 5) state.Stop();
        });
        int stopCount = 0;
        for (int i = 0; i < N; i++) stopCount += stopSeen[i];
        Console.WriteLine(rStop.IsCompleted);                     // False
        Console.WriteLine(rStop.LowestBreakIteration.HasValue);   // False
        Console.WriteLine(stopCount);                             // 6 (iterations 0..5 ran)

        // The in-body state reads, on a loop that never breaks or stops.
        bool anyShouldExit = false, anyStopped = false, anyExceptional = false;
        ParallelLoopResult rRead = Parallel.For(0, 10, seqOpts, (i, state) =>
        {
            if (state.ShouldExitCurrentIteration) anyShouldExit = true;
            if (state.IsStopped) anyStopped = true;
            if (state.IsExceptional) anyExceptional = true;
        });
        Console.WriteLine(anyShouldExit);     // False
        Console.WriteLine(anyStopped);        // False
        Console.WriteLine(anyExceptional);    // False
        Console.WriteLine(rRead.IsCompleted); // True

        // The same Break() shape with a long index. The body captures deliberately: a
        // no-capture lambda is cached behind a null-check branch that Roslyn emits after
        // pushing the source, dragging in an unrelated branch-merge stack spill.
        long[] breakSeenLong = new long[N];
        ParallelLoopResult rBreakLong = Parallel.For(0L, (long)N, seqOpts, (i, state) =>
        {
            breakSeenLong[i] = 1;
            if (i == 7L) state.Break();
        });
        long breakCountLong = 0;
        for (int i = 0; i < N; i++) breakCountLong += breakSeenLong[i];
        Console.WriteLine(rBreakLong.IsCompleted);                               // False
        Console.WriteLine(rBreakLong.LowestBreakIteration.GetValueOrDefault());  // 7
        Console.WriteLine(breakCountLong);                                       // 8 (iterations 0..7 ran)

        // Over ForEach, LowestBreakIteration is the source POSITION, not the element value.
        int[] feArr = new int[N];
        for (int i = 0; i < N; i++) feArr[i] = (i + 1) * 10; // distinct from its position
        int[] feHits = new int[N];
        ParallelLoopResult rForEachBreak = Parallel.ForEach(feArr, seqOpts, (val, state) =>
        {
            feHits[val / 10 - 1] = 1;
            if (val == 30) state.Break(); // array position 2
        });
        int feHitCount = 0;
        for (int i = 0; i < N; i++) feHitCount += feHits[i];
        Console.WriteLine(rForEachBreak.IsCompleted);                                // False
        Console.WriteLine(rForEachBreak.LowestBreakIteration.GetValueOrDefault());   // 2
        Console.WriteLine(feHitCount);                                               // 3 (positions 0..2 ran)

        // Break and Stop are mutually exclusive for one loop's lifetime: the second call
        // throws InvalidOperationException, wrapped like any other body exception.
        try
        {
            Parallel.For(0, N, seqOpts, (i, state) =>
            {
                if (i == 2)
                {
                    state.Break();
                    state.Stop();
                }
            });
        }
        catch (AggregateException ex)
        {
            // The upcast is style only; the declared-type route is asserted by
            // InnerExceptionsDeclaredType in the ParallelAggregate bucket.
            IReadOnlyList<Exception> inner = ex.InnerExceptions;
            Console.WriteLine(inner.Count);             // 1
            Console.WriteLine(inner[0].GetType().Name); // InvalidOperationException
            Console.WriteLine(inner[0].Message);        // Stop was called after Break was called.
        }

        try
        {
            Parallel.For(0, N, seqOpts, (i, state) =>
            {
                if (i == 2)
                {
                    state.Stop();
                    state.Break();
                }
            });
        }
        catch (AggregateException ex)
        {
            IReadOnlyList<Exception> inner = ex.InnerExceptions;
            Console.WriteLine(inner.Count);             // 1
            Console.WriteLine(inner[0].GetType().Name); // InvalidOperationException
            Console.WriteLine(inner[0].Message);        // Break was called after Stop was called.
        }

        // 9) Parallel.ForEach<T> over a List<T>. Every list is built with slack capacity
        // beyond its element count, so an implementation iterating the backing array's
        // capacity instead of List<T>.Count is caught.
        List<string> wordList = new List<string>(20); // capacity 20, only 6 added
        wordList.Add("alpha");
        wordList.Add("beta");
        wordList.Add("gamma");
        wordList.Add("delta");
        wordList.Add("epsilon");
        wordList.Add("zeta");
        int lenSumList = 0;
        Parallel.ForEach(wordList, w => Interlocked.Add(ref lenSumList, w.Length));
        Console.WriteLine(lenSumList);     // 30

        List<int> numList = new List<int>(N * 2); // capacity 2000, only N added
        for (int i = 0; i < N; i++) numList.Add(i + 1);
        long feSumList = 0;
        Parallel.ForEach(numList, n => Interlocked.Add(ref feSumList, n));
        Console.WriteLine(feSumList);      // 500500

        // Same two element kinds again, through the ParallelOptions overload.
        List<int> numList2 = new List<int>(N * 2);
        for (int i = 0; i < N; i++) numList2.Add(i + 1);
        long feSumList2 = 0;
        Parallel.ForEach(numList2, options, n => Interlocked.Add(ref feSumList2, n));
        Console.WriteLine(feSumList2);     // 500500

        List<string> wordList2 = new List<string>(20);
        wordList2.Add("alpha");
        wordList2.Add("beta");
        wordList2.Add("gamma");
        wordList2.Add("delta");
        wordList2.Add("epsilon");
        wordList2.Add("zeta");
        int lenSumList2 = 0;
        Parallel.ForEach(wordList2, options, w => Interlocked.Add(ref lenSumList2, w.Length));
        Console.WriteLine(lenSumList2);    // 30

        // The count-aware List<T> path combined with a Break-aware body.
        List<int> feList = new List<int>(N * 2); // slack capacity again
        for (int i = 0; i < N; i++) feList.Add((i + 1) * 10);
        int[] feListHits = new int[N];
        ParallelLoopResult rForEachListBreak = Parallel.ForEach(feList, seqOpts, (val, state) =>
        {
            feListHits[val / 10 - 1] = 1;
            if (val == 30) state.Break(); // list position 2
        });
        int feListHitCount = 0;
        for (int i = 0; i < N; i++) feListHitCount += feListHits[i];
        Console.WriteLine(rForEachListBreak.IsCompleted);                              // False
        Console.WriteLine(rForEachListBreak.LowestBreakIteration.GetValueOrDefault()); // 2
        Console.WriteLine(feListHitCount);                                             // 3

        Console.WriteLine($"parallel result tostring={r1.ToString()}|{(object)r1}|{r1}|{Generic(r1)}");
    }
}
