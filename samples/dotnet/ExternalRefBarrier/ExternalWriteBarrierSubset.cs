using System;

namespace ExternalWriteBarrierSubset
{
    // The payload type is System.Exception because this project is transpiled
    // with NO references: every store below then asks whether an EXTERNAL name
    // contains GC references, the predicate arm a CoreLib-loaded bucket cannot
    // reach (there Exception is a loaded Class and answers true structurally).
    // Five store systems, one per emit-side barrier decision: stfld, whole-struct
    // stobj, stelem of a reference-bearing struct element, Array.Fill of a
    // reference element, and a rank-2 accessor Set.
    internal static class Program
    {
        private const int HolderCount = 256;
        private const int PathCount = 5;
        private const int CycleCount = 4;
        // GC.CollectionCount has no corelib-less mapping, so the cycle count
        // cannot be checked from here; the gate reads it off DN2CPP_GC_STATS and
        // asserts a floor. This budget is what puts that count in the hundreds,
        // so the mutations above land inside cycles rather than between them.
        private const int AllocationBudget = 30_000;

        private struct ExPair
        {
            internal Exception Value;
        }

        private sealed class Holder
        {
            internal Exception ExField;
            internal ExPair Pair;
            internal readonly ExPair[] PairArr = new ExPair[1];
            internal readonly Exception[] Fill = new Exception[1];
            internal readonly Exception[,] Grid = new Exception[1, 1];
        }

        private static Exception MakePayload(int id)
        {
            return new InvalidOperationException("payload " + id);
        }

        private static void Store(Holder holder, int path, Exception value)
        {
            switch (path)
            {
                case 0:
                    holder.ExField = value;
                    break;
                case 1:
                    holder.Pair = new ExPair { Value = value };
                    break;
                case 2:
                    holder.PairArr[0] = new ExPair { Value = value };
                    break;
                case 3:
                    Array.Fill(holder.Fill, value);
                    break;
                default:
                    holder.Grid[0, 0] = value;
                    break;
            }
        }

        private static Exception Load(Holder holder, int path)
        {
            return path switch
            {
                0 => holder.ExField,
                1 => holder.Pair.Value,
                2 => holder.PairArr[0].Value,
                3 => holder.Fill[0],
                _ => holder.Grid[0, 0],
            };
        }

        private static int Validate(Holder[] holders, int[] expected)
        {
            int checksum = 17;
            for (int i = 0; i < HolderCount; i++)
            {
                for (int path = 0; path < PathCount; path++)
                {
                    int id = expected[i * PathCount + path];
                    if (id == 0)
                        continue;
                    Exception payload = Load(holders[i], path);
                    if (payload is null || !(payload is InvalidOperationException)
                        || payload.Message != "payload " + id)
                        throw new InvalidOperationException("External-typed reference store lost its payload");
                    checksum = unchecked(checksum * 31 + id);
                }
            }
            return checksum;
        }

        internal static void __GateEntry()
        {
            var holders = new Holder[HolderCount];
            for (int i = 0; i < holders.Length; i++)
                holders[i] = new Holder();

            // The destinations must predate the incremental cycles. Mutations below
            // then race the collector's scan of these already-reachable objects.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var expected = new int[HolderCount * PathCount];
            var pressure = new byte[64][];
            for (int cycle = 0; cycle < CycleCount; cycle++)
            {
                for (int i = 0; i < HolderCount; i++)
                {
                    for (int path = 0; path < PathCount; path++)
                    {
                        int id = cycle * HolderCount * PathCount + i * PathCount + path + 1;
                        Store(holders[i], path, MakePayload(id));
                        expected[i * PathCount + path] = id;
                        pressure[id & (pressure.Length - 1)] = new byte[4096];
                    }
                }

                for (int allocations = 0; allocations < AllocationBudget; allocations++)
                    pressure[allocations & (pressure.Length - 1)] = new byte[4096];
                _ = Validate(holders, expected);
            }

            GC.KeepAlive(holders);
            GC.KeepAlive(pressure);
            Console.WriteLine("external reference stores: True");
        }
    }
}
