using System;
using System.Collections.Concurrent;
using System.Threading;

// BlockingCollection<T> — a producer/consumer blocking queue on real threads. Every
// section's output is order-independent (totals / counts / FIFO sequences read only
// after Join), so it is deterministic and diffed exact vs real .NET. A lost, duplicated,
// or leaked element — or a missed CompleteAdding wake — would not reproduce these totals.
static class Program
{
    static int s_consumedCount;
    static int s_consumedSum;

    const int Producers = 4;
    const int PerProducer = 25;
    const int Consumers = 3;

    // A producer adds PerProducer distinct ints into a private band (p*1000 + 1..25), so
    // every item is unique and the global sum/count are fixed regardless of interleaving.
    static void Producer(object? arg)
    {
        int p = (int)arg!;
        var bc = s_pcCollection!;
        for (int j = 1; j <= PerProducer; j++)
            bc.Add(p * 1000 + j);
    }

    // A consumer drains via blocking Take() until the collection is completed and empty,
    // at which point Take throws InvalidOperationException — the canonical exit signal.
    static void Consumer(object? arg)
    {
        var bc = s_pcCollection!;
        try
        {
            while (true)
            {
                int v = bc.Take();
                Interlocked.Add(ref s_consumedSum, v);
                Interlocked.Increment(ref s_consumedCount);
            }
        }
        catch (InvalidOperationException)
        {
            // completed && empty — done.
        }
    }

    static BlockingCollection<int>? s_pcCollection;

    static void ProducerConsumer()
    {
        var bc = new BlockingCollection<int>();
        s_pcCollection = bc;

        var consumers = new Thread[Consumers];
        for (int i = 0; i < Consumers; i++)
        {
            consumers[i] = new Thread(Consumer);
            consumers[i].Start();
        }
        var producers = new Thread[Producers];
        for (int i = 0; i < Producers; i++)
        {
            producers[i] = new Thread(Producer);
            producers[i].Start(i);
        }

        for (int i = 0; i < Producers; i++) producers[i].Join();
        bc.CompleteAdding();                 // no more items: consumers drain then exit
        for (int i = 0; i < Consumers; i++) consumers[i].Join();

        Console.WriteLine("ProducerConsumer");
        Console.WriteLine(s_consumedCount);  // Producers * PerProducer = 100
        Console.WriteLine(s_consumedSum);    // fixed: 4*(1..25) + 25*(0+1000+2000+3000)
        Console.WriteLine(bc.IsCompleted);   // True
    }

    static void FifoSingleThread()
    {
        var bc = new BlockingCollection<int>();
        for (int i = 1; i <= 5; i++) bc.Add(i);
        Console.WriteLine("Fifo");
        Console.WriteLine(bc.Count);         // 5
        for (int i = 0; i < 5; i++)
            Console.Write(bc.Take() + (i < 4 ? " " : "\n")); // 1 2 3 4 5 (FIFO)
        Console.WriteLine(bc.Count);         // 0
    }

    static void CompleteDrain()
    {
        var bc = new BlockingCollection<int>();
        bc.Add(10);
        bc.Add(20);
        bc.Add(30);
        bc.CompleteAdding();
        Console.WriteLine("CompleteDrain");
        Console.WriteLine(bc.IsAddingCompleted); // True
        Console.WriteLine(bc.Take());            // 10
        Console.WriteLine(bc.Take());            // 20
        Console.WriteLine(bc.Take());            // 30
        // TryTake on a completed-empty collection returns false (out -> default).
        Console.WriteLine(bc.TryTake(out int leftover) + " " + leftover); // False 0
        Console.WriteLine(bc.IsCompleted);       // True
        // Take on a completed-empty collection throws InvalidOperationException.
        try
        {
            bc.Take();
            Console.WriteLine("no-throw");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("threw");
        }
    }

    static BlockingCollection<int>? s_boundedCollection;

    static void BoundedProducer(object? arg)
    {
        var bc = s_boundedCollection!;
        for (int i = 1; i <= 6; i++)
            bc.Add(i); // blocks while the capacity-2 queue is full until the consumer drains
    }

    static void Bounded()
    {
        var bc = new BlockingCollection<int>(2);
        s_boundedCollection = bc;
        Console.WriteLine("Bounded");
        Console.WriteLine(bc.BoundedCapacity);   // 2

        var producer = new Thread(BoundedProducer);
        producer.Start();

        int sum = 0, count = 0;
        for (int i = 0; i < 6; i++)              // consume exactly the 6 produced items
        {
            sum += bc.Take();
            count++;
        }
        producer.Join();
        Console.WriteLine(count);                // 6
        Console.WriteLine(sum);                  // 21
    }

    static void TryTimeouts()
    {
        Console.WriteLine("TryTimeouts");
        // TryTake on an empty (not-completed) collection: immediate and timed both fail.
        var empty = new BlockingCollection<int>();
        Console.WriteLine(empty.TryTake(out int a) + " " + a);     // False 0
        Console.WriteLine(empty.TryTake(out int b, 30) + " " + b); // False 0 (after ~30ms)

        // TryAdd into a full bounded collection times out (returns false); the item is
        // not added, so a subsequent Take still yields the original element.
        var full = new BlockingCollection<int>(1);
        Console.WriteLine(full.TryAdd(7));        // True
        Console.WriteLine(full.TryAdd(8, 30));    // False (full, times out)
        Console.WriteLine(full.Take());           // 7
    }

    static void ReferenceAndWidthKinds()
    {
        Console.WriteLine("Kinds");
        // Reference T (string): FIFO order preserved.
        var strings = new BlockingCollection<string>();
        strings.Add("alpha");
        strings.Add("beta");
        strings.Add("gamma");
        Console.WriteLine(strings.Take() + "|" + strings.Take() + "|" + strings.Take());

        // 64-bit value T (long): exercises the i8 box/unbox kind.
        var longs = new BlockingCollection<long>();
        longs.Add(1L << 40);
        longs.Add(2L << 40);
        longs.Add(3L << 40);
        long ls = longs.Take() + longs.Take() + longs.Take();
        Console.WriteLine(ls);                    // 6 << 40 = 6597069766656

        // double value T: exercises the r8 box/unbox kind. Printed as an integer to keep
        // the assertion independent of floating-point formatting.
        var doubles = new BlockingCollection<double>();
        doubles.Add(1.5);
        doubles.Add(2.5);
        doubles.Add(4.0);
        double ds = doubles.Take() + doubles.Take() + doubles.Take();
        Console.WriteLine((long)(ds * 2));        // 16
    }

    // The IDisposable INTERFACE mouth. BlockingCollection<T> is intrinsic — a
    // Dn2CppBlockingCollection* with no per-class emitted type-info — so `using` (a
    // callvirt IDisposable::Dispose), an interface-typed local and isinst/castclass all
    // depend on the init prologue installing an interface-dispatch map onto its runtime
    // type-info, while the DIRECT Dispose() call is routed at the intrinsic call site and
    // works regardless. Every value is read BEFORE the dispose: real .NET's Dispose tears
    // the collection down and dn2cpp's is a no-op, so a post-dispose read would diverge
    // for unrelated reasons.
    static void DisposeMouths()
    {
        using (var q = new BlockingCollection<int>())
        {
            q.Add(11);
            q.Add(22);
            Console.WriteLine(q.Take() + q.Take());   // 33
        }

        var q2 = new BlockingCollection<string>(4);
        q2.Add("bc");
        Console.WriteLine(q2.Take() + "-itf");        // bc-itf
        IDisposable d2 = q2;
        d2.Dispose();

        var q3 = new BlockingCollection<int>();
        q3.Add(5);
        Console.WriteLine(q3.Count);                  // 1
        object boxed = q3;
        Console.WriteLine(boxed is IDisposable);      // True
        ((IDisposable)boxed).Dispose();

        // The direct route stays covered beside the interface ones.
        var q4 = new BlockingCollection<int>();
        q4.Add(9);
        Console.WriteLine(q4.Take());                 // 9
        q4.Dispose();
    }

    // Every BlockingCollection<T> shares one runtime struct and one type-info handle, so
    // the type token must name that SHARED handle (the choice Task<T> already made): a
    // per-instantiation ti_ nothing stamps on an instance makes `o is
    // BlockingCollection<string>` read False for every T.
    //
    // The residue — one handle cannot tell instantiations apart, so `o is
    // BlockingCollection<int>` on a <string> reads True where .NET reads False — is
    // deliberately NOT probed here: this gate diffs against real .NET, so a known
    // divergence has nowhere to live in it. It is asserted in the reflect-types bucket's
    // frozen BoundHandleSubset section, beside the identical Task<T> line.
    static void ErasedIdentity()
    {
        object bc = new BlockingCollection<string>();
        Console.WriteLine(bc is BlockingCollection<string>);           // True
        Console.WriteLine(bc.GetType() == typeof(BlockingCollection<string>)); // True
        var back = (BlockingCollection<string>)bc;
        back.Add("erased");
        Console.WriteLine(back.Take());                                // erased
        Console.WriteLine(new object() is BlockingCollection<string>); // False
        back.Dispose();
    }

    static void Main()
    {
        ProducerConsumer();
        FifoSingleThread();
        CompleteDrain();
        Bounded();
        TryTimeouts();
        ReferenceAndWidthKinds();
        DisposeMouths();
        ErasedIdentity();
    }
}
