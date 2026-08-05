using System;
using System.Threading;

namespace EventSubset
{
    // SUBJECT: the field-like `event` pipeline (+= / -= / invoke), whose
    // compiler-generated accessors go through Interlocked.CompareExchange. The
    // integral Interlocked overloads ride the same section.
    internal sealed class Bus
    {
        public event Action<int> OnTick;

        public void Tick(int n) => OnTick?.Invoke(n);
    }

    internal static class Program
    {
        private static int s_sum;

        private static void Add(int x) => s_sum += x;

        private static void AddTenfold(int x) => s_sum += x * 10;

        internal static void Run()
        {
            var bus = new Bus();

            // No subscribers yet: ?.Invoke is a no-op.
            bus.Tick(99);
            Console.WriteLine(s_sum); // 0

            bus.OnTick += Add;
            bus.OnTick += AddTenfold;
            bus.Tick(3); // +3 +30
            Console.WriteLine(s_sum); // 33

            bus.OnTick -= AddTenfold;
            bus.Tick(4); // +4 only
            Console.WriteLine(s_sum); // 37

            // A lambda subscriber alongside the method-group ones.
            int hits = 0;
            bus.OnTick += _ => hits++;
            bus.Tick(1); // +1, hits++
            Console.WriteLine(s_sum); // 38
            Console.WriteLine(hits);  // 1

            // Integral Interlocked overloads.
            int counter = 0;
            Interlocked.Increment(ref counter);
            Interlocked.Add(ref counter, 5);
            Interlocked.Decrement(ref counter);
            Console.WriteLine(counter); // 5
            Console.WriteLine(Interlocked.CompareExchange(ref counter, 100, 5)); // 5 (old)
            Console.WriteLine(counter); // 100
        }
    }
}
