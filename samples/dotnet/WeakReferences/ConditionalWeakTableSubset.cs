using System;
using System.Runtime.CompilerServices;

namespace ConditionalWeakTableSubset
{
    // ConditionalWeakTable parks each pair in a DependentHandle inside its Entry[],
    // and that array is the pair's only holder. A collection between Add and
    // TryGetValue is what tells a scanned Entry[] from an unscanned one: the keys
    // and values stay strongly held, so real .NET answers every lookup with the
    // identical value, while an Entry[] the collector never scans loses the
    // handle's cell first and reads reclaimed memory on the next lookup.
    internal static class Program
    {
        // Well past the initial bucket count, so the resized Entry[] is exercised too.
        private const int Count = 4_096;
        private const int ChurnCount = 512;
        private const int ChurnBytes = 16 * 1024;

        private sealed class Payload
        {
            internal readonly int Id;

            internal Payload(int id)
            {
                Id = id;
            }
        }

        internal static void __GateEntry()
        {
            var table = new ConditionalWeakTable<object, Payload>();
            var keys = new object[Count];
            var values = new Payload[Count];
            for (int i = 0; i < Count; i++)
            {
                keys[i] = new object();
                values[i] = new Payload(i);
                table.Add(keys[i], values[i]);
            }

            Churn();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // Reclaimed cell memory is only observable once something else reuses it.
            Churn();

            int hits = 0;
            int identical = 0;
            int intact = 0;
            for (int i = 0; i < Count; i++)
            {
                if (table.TryGetValue(keys[i], out var value))
                {
                    hits++;
                    if (ReferenceEquals(value, values[i]))
                        identical++;
                    if (value.Id == i)
                        intact++;
                }
            }
            Console.WriteLine("conditional weak table after collection: " + hits + " hits, "
                + identical + " identical values, " + intact + " ids intact (of " + Count + ")");

            int removed = 0;
            for (int i = 0; i < Count; i += 2)
            {
                if (table.Remove(keys[i]))
                    removed++;
            }
            GC.Collect();
            Churn();
            int remaining = 0;
            for (int i = 0; i < Count; i++)
            {
                if (table.TryGetValue(keys[i], out _))
                    remaining++;
            }
            Console.WriteLine("conditional weak table after removal: " + removed + " removed, "
                + remaining + " remaining");

            GC.KeepAlive(keys);
            GC.KeepAlive(values);
        }

        private static void Churn()
        {
            for (int i = 0; i < ChurnCount; i++)
            {
                var garbage = new byte[ChurnBytes];
                garbage[0] = (byte)i;
                GC.KeepAlive(garbage);
            }
        }
    }
}
