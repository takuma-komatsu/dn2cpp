#nullable disable
using System;

namespace ReadOnlySpanByteSubset
{
    // ReadOnlySpan<byte> over RVA static data: a byte[]-literal-backed property
    // compiles to a <PrivateImplementationDetails> blob read via `ldsflda` plus a
    // span over a `ref byte`. Indexing it pins the byref stride (ref byte ->
    // uint8_t*, not int32_t*) and Unsafe.Add's element width — get either wrong
    // and the buffer is silently misindexed.
    internal static class Program
    {
        private static ReadOnlySpan<byte> Table => new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };

        internal static void __GateEntry()
        {
            ReadOnlySpan<byte> t = Table;
            Console.WriteLine(t.Length);        // 8
            Console.WriteLine(t[0]);            // 10
            Console.WriteLine(t[3]);            // 40
            Console.WriteLine(t[7]);            // 80

            int sum = 0;
            for (int i = 0; i < t.Length; i++)
                sum += t[i];
            Console.WriteLine(sum);             // 360

            ReadOnlySpan<byte> tail = t.Slice(5);
            Console.WriteLine(tail.Length);     // 3
            Console.WriteLine(tail[0]);         // 60
            Console.WriteLine(tail[2]);         // 80
        }
    }
}
